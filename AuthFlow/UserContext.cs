namespace AuthFlow
{
    /// <summary>
    /// Provides a fluent interface for building a user context with required permissions.
    /// <example>
    /// Usage example:
    /// <code>
    /// var userContext = UserContext.Create(userPermissionsRepository, userId)
    ///                               .RequirePermission("READ_ACCESS")
    ///                               .OnPermissionGranted(async () => await PerformReadOperation())
    ///                               .OnPermissionDenied(async () => Console.WriteLine("Access Denied"))
    ///                               .OnException(async (ex) => Console.WriteLine($"Error: {ex.Message}"))
    ///                               .Execute();
    /// </code>
    /// </example>
    /// </summary>
    public interface IUserContextBuilder
    {
        /// <summary>
        /// Specifies the permission required for the user context.
        /// </summary>
        /// <param name="permissionName">The name of the required permission.</param>
        /// <returns>A builder for configuring the action taken when the permission is granted.</returns>
        IPermissionActionBuilder RequirePermission(string permissionName);
    }

    /// <summary>
    /// Configures the action to be taken when the specified permission is granted.
    /// </summary>
    public interface IPermissionActionBuilder
    {
        /// <summary>
        /// Defines the action to be executed when the permission is granted.
        /// </summary>
        /// <param name="action">The action to execute, encapsulated in a Func&lt;Task&gt;.</param>
        /// <returns>A builder for configuring the action taken when the permission is denied.</returns>
        IPermissionGrantedActionBuilder OnPermissionGranted(Func<CancellationToken, Task> action);
    }

    /// <summary>
    /// Configures the action to be taken when the specified permission is denied.
    /// </summary>
    public interface IPermissionGrantedActionBuilder
    {
        /// <summary>
        /// Defines the action to be executed when the permission is denied.
        /// </summary>
        /// <param name="action">The action to execute, encapsulated in a Func&lt;Task&gt;.</param>
        /// <returns>A builder for configuring the exception handling action.</returns>
        IPermissionDeniedActionBuilder OnPermissionDenied(Func<CancellationToken, Task> action);
    }

    /// <summary>
    /// Configures the action to be taken when an OperationCancellationException exception occurs.
    /// </summary>
    public interface IPermissionDeniedActionBuilder
    {
        /// <summary>
        /// Defines the action to be executed when an OperationCancelledException exception occurs.
        /// </summary>
        /// <param name="action">The action to execute, encapsulated in a Func&lt;Task&gt; that receives an Exception.</param>
        /// <returns>A finalizer for executing the configured user context.</returns>
        IOperationCancelledActionBuilder OnOperationCancelled(Func<Exception, CancellationToken, Task> action);
    }

    /// <summary>
    /// Configures the action to be taken when an exception occurs during the permission check.
    /// </summary>
    public interface IOperationCancelledActionBuilder
    {
        /// <summary>
        /// Defines the action to be executed when an exception occurs.
        /// </summary>
        /// <param name="action">The action to execute, encapsulated in a Func&lt;Task&gt; that receives an Exception.</param>
        /// <returns>A finalizer for executing the configured user context.</returns>
        IUserContextFinalizer OnException(Func<Exception, CancellationToken, Task> action);
    }

    /// <summary>
    /// Finalizes the configuration of the user context and executes the permission check.
    /// </summary>
    public interface IUserContextFinalizer
    {
        /// <summary>
        /// Executes the configured user context, performing the permission check and triggering the appropriate actions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a concrete implementation of the user context builder interfaces for configuring and executing a user context with specific permission requirements.
    /// <example>
    /// Usage example for requiring "WRITE_ACCESS" permission:
    /// <code>
    /// await UserContext.Create(userPermissionsRepository, userId)
    ///                   .RequirePermission("WRITE_ACCESS")
    ///                   .OnPermissionGranted(async () => await PerformWriteOperation())
    ///                   .OnPermissionDenied(async () => Console.WriteLine("Access Denied"))
    ///                   .OnException(async (ex) => Console.WriteLine($"Error: {ex.Message}"))
    ///                   .Execute();
    /// </code>
    /// </example>
    /// <remarks>
    /// This class provides a seamless and flexible way to define and enforce permission checks,
    /// allowing for custom actions based on the outcome of these checks.
    /// </remarks>
    /// </summary>
    public sealed class UserContext : IUserContextBuilder, IPermissionActionBuilder, 
        IPermissionGrantedActionBuilder, IPermissionDeniedActionBuilder, IUserContextFinalizer
    {
        private readonly IUserPermissionsRepository _userPermissionsRepository;
        private readonly Guid _userId;
        private string? _permissionRequired;
        private Func<CancellationToken, Task>? _onPermissionGrantedAsync;
        private Func<CancellationToken, Task>? _onPermissionDeniedAsync;
        private Func<Exception, CancellationToken, Task>? _onExceptionAsync;

        private UserContext(IUserPermissionsRepository userPermissionsRepository, Guid userId)
        {
            _userPermissionsRepository = userPermissionsRepository;
            _userId = userId;
        }

        public static IUserContextBuilder Create(IUserPermissionsRepository userPermissionsRepository, Guid userId)
        {
            return new UserContext(userPermissionsRepository, userId);
        }

        public IPermissionActionBuilder RequirePermission(string permissionName)
        {
            _permissionRequired = permissionName;
            return this;
        }

        public IPermissionGrantedActionBuilder OnPermissionGranted(Func<CancellationToken, Task> action)
        {
            _onPermissionGrantedAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        public IPermissionDeniedActionBuilder OnPermissionDenied(Func<CancellationToken, Task> action)
        {
            _onPermissionDeniedAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }
        
        public IUserContextFinalizer OnException(Func<Exception, CancellationToken, Task> action)
        {
            _onExceptionAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(_permissionRequired)
               || _onPermissionGrantedAsync == null
               || _onPermissionDeniedAsync == null
               || _onExceptionAsync == null)
            {
                throw CreateConfigurationException();
            }

            try
            {
                if (await _userPermissionsRepository.VerifyUserPermission(_userId, _permissionRequired, cancellationToken))
                {
                    await _onPermissionGrantedAsync(cancellationToken);
                }
                else
                {
                    await _onPermissionDeniedAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _onExceptionAsync(ex, cancellationToken);
            }
        }

        private Exception CreateConfigurationException()
        {
            var configurations = new[]
            {
                ("Required Permission", !string.IsNullOrWhiteSpace(_permissionRequired)),
                ("On Permission Granted Async", _onPermissionGrantedAsync != null),
                ("On Permission Denied Async", _onPermissionDeniedAsync != null),
                ("On Exception Async", _onExceptionAsync != null)
            };

            var missingConfigurations = string.Join(", ",
                configurations.Where(c => !c.Item2)
                    .Select(c => c.Item1));

            return new UserContextConfigurationException(
                @$"UserContext is missing the following configurations: {missingConfigurations}. " +
                "Try using the fluent interface to avoid configuration issues.");
        }

        private class UserContextConfigurationException : Exception
        {
            public UserContextConfigurationException(string message)
                : base(message)
            {
            }
        }
    }
}
