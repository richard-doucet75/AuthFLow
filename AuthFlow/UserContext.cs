namespace AsyncAuthFlowCore
{
    /// <summary>
    /// Represents a concrete implementation of the <see cref="IUserContextConfigurator"/> for configuring and executing a user context with specific permission requirements.
    /// </summary>
    public sealed class UserContext : IUserContextConfigurator
    {
        private readonly IUserPermissionsRepository _userPermissionsRepository;
        private readonly Guid _userId;
        private string? _permissionRequired;
        private Func<CancellationToken, Task>? _onPermissionGrantedAsync;
        private Func<CancellationToken, Task>? _onPermissionDeniedAsync;
        private Func<CancellationToken, Task>? _onCancelledAsync;
        private Func<Exception, CancellationToken, Task>? _onExceptionAsync;

        private UserContext(IUserPermissionsRepository userPermissionsRepository, Guid userId)
        {
            _userPermissionsRepository = userPermissionsRepository;
            _userId = userId;
        }

        /// <summary>
        /// Creates a new instance of <see cref="UserContext"/> with the specified permissions repository and user identifier.
        /// </summary>
        /// <param name="userPermissionsRepository">The repository to check user permissions.</param>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>A new <see cref="IUserContextConfigurator"/> instance.</returns>
        public static IUserContextConfigurator Create(IUserPermissionsRepository userPermissionsRepository, Guid userId)
        {
            return new UserContext(userPermissionsRepository, userId);
        }

        public IUserContextConfigurator RequirePermission(string permissionName)
        {
            if(_permissionRequired != null)
                throw new UserContextConfigurationException($"{nameof(RequirePermission)} has already been configured.");
            
            _permissionRequired = permissionName;
            return this;
        }

        public IUserContextConfigurator OnPermissionGranted(Func<CancellationToken, Task> action)
        {
            if(_onPermissionGrantedAsync != null)
                throw new UserContextConfigurationException($"{nameof(OnPermissionGranted)} has already been configured.");
            
            _onPermissionGrantedAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        public IUserContextConfigurator OnPermissionDenied(Func<CancellationToken, Task> action)
        {
            if(_onPermissionDeniedAsync != null)
                throw new UserContextConfigurationException($"{nameof(OnPermissionDenied)} has already been configured.");
            
            _onPermissionDeniedAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        public IUserContextConfigurator OnOperationCancelled(Func<CancellationToken, Task> action)
        {
            if(_onCancelledAsync != null)
                throw new UserContextConfigurationException($"{nameof(OnOperationCancelled)} has already been configured.");
            
            _onCancelledAsync = action;
            return this;
        }

        public IUserContextConfigurator OnException(Func<Exception, CancellationToken, Task> action)
        {
            if(_onExceptionAsync != null)
                throw new UserContextConfigurationException($"{nameof(OnException)} has already been configured.");
            
            _onExceptionAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Executes the configured user context, performing the permission check and triggering the appropriate actions based on the outcome.
        /// This method includes checks for cancellation at key points in the execution flow and ensures that configured actions for permission granted,
        /// permission denied, operation cancellation, and exceptions are invoked as appropriate.
        /// </summary>
        /// <param name="cancellationToken">A token for cancelling the operation. This token is checked before starting the operation,
        /// after permission checks, and after executing any configured actions to allow for graceful cancellation of the operation.</param>
        /// <returns>A task representing the asynchronous operation. This task may complete as cancelled if the operation is cancelled.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled. This exception is thrown after invoking any configured cancellation action,
        /// allowing for custom cancellation logic to be executed.</exception>
        /// <exception cref="UserContextConfigurationException">Thrown if the user context is misconfigured, such as missing required permissions or actions.</exception>
        /// <exception cref="Exception">Any unhandled exceptions that occur during the execution of the permission check or configured actions. If an exception handling action is configured,
        /// it is invoked with the exception; otherwise, the exception is rethrown.</exception>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check for configuration issues before starting.
                if (string.IsNullOrWhiteSpace(_permissionRequired) 
                    || _onPermissionGrantedAsync == null 
                    || _onPermissionDeniedAsync == null)
                {
                    throw CreateConfigurationException();
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                if (await _userPermissionsRepository.VerifyUserPermission(_userId, _permissionRequired, cancellationToken))
                {
                    await _onPermissionGrantedAsync(cancellationToken);
                }
                else
                {
                    await _onPermissionDeniedAsync(cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                if (_onCancelledAsync != null)
                {
                    await _onCancelledAsync.Invoke(cancellationToken);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (_onExceptionAsync != null)
                {
                    await _onExceptionAsync.Invoke(ex, cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }
        
        private Exception CreateConfigurationException()
        {
            var missingConfigurations = new List<string>();
            if (string.IsNullOrWhiteSpace(_permissionRequired)) missingConfigurations.Add("Required Permission");
            if (_onPermissionGrantedAsync == null) missingConfigurations.Add("On Permission Granted Async");
            if (_onPermissionDeniedAsync == null) missingConfigurations.Add("On Permission Denied Async");

            if (missingConfigurations.Any())
            {
                return new UserContextConfigurationException(
                    $"UserContext is missing the following configurations: {string.Join(", ", missingConfigurations)}. Try using the fluent interface to avoid configuration issues.");
            }

            return new InvalidOperationException("Invalid UserContext configuration.");
        }

        public class UserContextConfigurationException : Exception
        {
            public UserContextConfigurationException(string message) : base(message) { }
        }
    }
}
