using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncAuthFlowCore.Abstractions; // Ensure this namespace correctly refers to where your abstractions are defined.

namespace AsyncAuthFlowCore
{
    /// <summary>
    /// Represents a user context for configuring and executing user-specific operations with permission checks.
    /// </summary>
    public sealed class UserContext : IUserContextConfigurator
    {
        private readonly IUserPermissionsRepository _userPermissionsRepository;
        private readonly string _userId;
        private string? _permissionRequired;
        private Func<string, CancellationToken, Task>? _onPermissionGrantedAsync;
        private Func<string, CancellationToken, Task>? _onPermissionDeniedAsync;
        private Func<string, CancellationToken, Task>? _onCancelledAsync;
        private Func<Exception, string, CancellationToken, Task>? _onExceptionAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContext"/> class.
        /// </summary>
        /// <param name="userPermissionsRepository">The repository used to verify user permissions.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        private UserContext(IUserPermissionsRepository userPermissionsRepository, string userId)
        {
            _userPermissionsRepository = userPermissionsRepository ?? throw new ArgumentNullException(nameof(userPermissionsRepository));
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }
            _userId = userId;
        }

        /// <summary>
        /// Creates a new user context with specified user permissions repository and user identifier.
        /// </summary>
        /// <param name="userPermissionsRepository">The repository to check user permissions.</param>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>A new <see cref="IUserContextConfigurator"/> instance configured with the provided user and repository.</returns>
        public static IUserContextConfigurator Create(IUserPermissionsRepository userPermissionsRepository, string userId)
        {
            return new UserContext(userPermissionsRepository, userId);
        }

        /// <summary>
        /// Configures the required permission for the user context.
        /// </summary>
        /// <param name="permissionName">The name of the required permission.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        /// <exception cref="UserContextConfigurationException">Thrown when permission requirement is already set.</exception>
        public IUserContextConfigurator RequirePermission(string permissionName)
        {
            if (_permissionRequired != null)
            {
                throw new UserContextConfigurationException("Required permission has already been configured");
            }
            _permissionRequired = permissionName ?? throw new ArgumentNullException(nameof(permissionName));
            return this;
        }

        /// <summary>
        /// Configures the action to be executed when the specified permission is granted.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the user ID and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        /// <exception cref="UserContextConfigurationException">Thrown when an action for permission granted is already set.</exception>
        public IUserContextConfigurator OnPermissionGranted(Func<string, CancellationToken, Task> action)
        {
            if (_onPermissionGrantedAsync != null)
            {
                throw new UserContextConfigurationException("OnPermissionGranted has already been configured");
            }
            _onPermissionGrantedAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Configures the action to be executed when the specified permission is denied.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the user ID and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        /// <exception cref="UserContextConfigurationException">Thrown when an action for permission denied is already set.</exception>
        public IUserContextConfigurator OnPermissionDenied(Func<string, CancellationToken, Task> action)
        {
            if (_onPermissionDeniedAsync != null)
            {
                throw new UserContextConfigurationException("OnPermissionDenied has already been configured");
            }
            _onPermissionDeniedAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Configures the action to be executed when the operation is cancelled.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the user ID and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        /// <exception cref="UserContextConfigurationException">Thrown when an action for operation cancellation is already set.</exception>
        public IUserContextConfigurator OnOperationCancelled(Func<string, CancellationToken, Task> action)
        {
            if (_onCancelledAsync != null)
            {
                throw new UserContextConfigurationException("OnOperationCancelled has already been configured");
            }
            _onCancelledAsync = action; // Allowing null to remove previously set action
            return this;
        }

        /// <summary>
        /// Configures the action to be executed when an exception occurs during permission check or action execution.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the exception, user ID, and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        /// <exception cref="UserContextConfigurationException">Thrown when an action for handling exceptions is already set.</exception>
        public IUserContextConfigurator OnException(Func<Exception, string, CancellationToken, Task> action)
        {
            if (_onExceptionAsync != null)
            {
                throw new UserContextConfigurationException("OnException has already been configured");
            }
            _onExceptionAsync = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Executes the configured user context actions based on the outcome of the permission check.
        /// </summary>
        /// <param name="cancellationToken">A token to signal cancellation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="UserContextConfigurationException">Thrown if the user context is misconfigured.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(_permissionRequired) || _onPermissionGrantedAsync == null || _onPermissionDeniedAsync == null)
                {
                    throw CreateConfigurationException();
                }

                var hasPermission = await _userPermissionsRepository.VerifyUserPermission(_userId, _permissionRequired, cancellationToken);

                if (hasPermission)
                {
                    await (_onPermissionGrantedAsync.Invoke(_userId, cancellationToken));
                }
                else
                {
                    await (_onPermissionDeniedAsync.Invoke(_userId, cancellationToken));
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                if (_onCancelledAsync != null)
                {
                    await _onCancelledAsync.Invoke(_userId, cancellationToken);
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
                    await _onExceptionAsync.Invoke(ex, _userId, cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Creates a configuration exception indicating missing configurations.
        /// </summary>
        /// <returns>An exception detailing the missing configurations.</returns>
        private Exception CreateConfigurationException()
        {
            var missingConfigurations = new List<string>();
            if (string.IsNullOrWhiteSpace(_permissionRequired)) missingConfigurations.Add("Required Permission");
            if (_onPermissionGrantedAsync == null) missingConfigurations.Add("On Permission Granted Async");
            if (_onPermissionDeniedAsync == null) missingConfigurations.Add("On Permission Denied Async");

            if (missingConfigurations.Any())
            {
                return new UserContextConfigurationException(
                    $"UserContext is missing the following configurations: {string.Join(", ", missingConfigurations)}.");
            }

            return new InvalidOperationException("Invalid UserContext configuration.");
        }
    }
}