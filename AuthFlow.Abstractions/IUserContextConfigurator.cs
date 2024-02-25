namespace AsyncAuthFlowCore.Abstractions
{
    /// <summary>
    /// Defines the configuration interface for setting up a user context with specific permissions and actions.
    /// </summary>
    public interface IUserContextConfigurator
    {
        /// <summary>
        /// Specifies the permission required for the user context to execute its actions.
        /// </summary>
        /// <param name="permissionName">The name of the required permission.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        IUserContextConfigurator RequirePermission(string permissionName);

        /// <summary>
        /// Configures the action to be executed when the specified permission is granted.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the user ID and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        IUserContextConfigurator OnPermissionGranted(Func<string, CancellationToken, Task> action);

        /// <summary>
        /// Configures the action to be executed when the specified permission is denied.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the user ID and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        IUserContextConfigurator OnPermissionDenied(Func<string, CancellationToken, Task> action);

        /// <summary>
        /// Configures the action to be executed when the operation is cancelled.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the user ID and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        IUserContextConfigurator OnOperationCancelled(Func<string, CancellationToken, Task> action);

        /// <summary>
        /// Configures the action to be executed when an exception occurs during the permission check or action execution.
        /// </summary>
        /// <param name="action">The action to execute, encapsulating the exception, user ID, and a cancellation token.</param>
        /// <returns>The same <see cref="IUserContextConfigurator"/> instance for chaining further configuration.</returns>
        IUserContextConfigurator OnException(Func<Exception, string, CancellationToken, Task> action);

        /// <summary>
        /// Executes the configured user context, performing the permission check and triggering the appropriate actions based on the outcome.
        /// </summary>
        /// <param name="cancellationToken">A token for cancelling the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
