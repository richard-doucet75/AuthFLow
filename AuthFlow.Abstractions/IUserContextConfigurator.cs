namespace AsyncAuthFlowCore;

/// <summary>
/// Defines the configuration interface for setting up a user context with specific permissions and actions.
/// <example>
/// Using async lambda expression to configure access 
/// <code>
/// Assume userPermissionsRepository is an instance of IUserPermissionsRepository and userId is the current user's GUID.
/// 
/// var userContext = AuthFlow.UserContext.Create(userPermissionsRepository, userId)
///     .RequirePermission("ADMIN_ACCESS")
///     .OnPermissionGranted(async cancellationToken =>
///     {
///         Console.WriteLine("Permission Granted. Performing admin operations.");
///         // Perform operation that requires admin access.
///     })
///     .OnPermissionDenied(async cancellationToken =>
///     {
///         Console.WriteLine("Permission Denied. Access to admin operations is not allowed.");
///     })
///     .OnException(async (exception, cancellationToken) =>
///     {
///         Console.WriteLine($"An error occurred: {exception.Message}");
///     })
///     .ExecuteAsync();
/// </code>
/// </example>
/// <example>
/// Using async Tasks expression to configure access
/// <code>
///    public async Task HandleAdminOperations(CancellationToken cancellationToken) {
///     // Complex admin operations logic here.
///    }
///
///    public async Task HandlePermissionDeniedOperations(CancellationToken cancellationToken) {
///     // Logic for handling denied permissions.
///    }
///
///   var userContext = AuthFlow.UserContext.Create(userPermissionsRepository, userId)
///     .RequirePermission("ADMIN_ACCESS")
///     .OnPermissionGranted(async cancellationToken => await HandleAdminOperations(cancellationToken))
///     .OnPermissionDenied(async cancellationToken => await HandlePermissionDeniedOperations(cancellationToken));
/// </code>
/// </example>
/// <example>
/// Configuring Handling cancellation while executing
/// 
/// <code>
///     .OnPermissionGranted(async cancellationToken => {
///         while (longRunningCondition) {
///             cancellationToken.ThrowIfCancellationRequested();
///             // Long-running operation logic here.
///         }
///     })
///
///
/// </code>
/// <code>
///     .OnOperationCancelled(async (exception, cancellationToken) => {
///         Log.Error(exception, "An error occurred during permission check or action execution.");
///         // Additional exception handling logic here.
///     })
/// </code>
/// </example>
/// </summary>
public interface IUserContextConfigurator
{
    /// <summary>
    /// Specifies the permission required for the user context.
    /// </summary>
    /// <param name="permissionName">The name of the required permission.</param>
    /// <returns>The same configurator instance for chaining further configuration.</returns>
    IUserContextConfigurator RequirePermission(string permissionName);

    /// <summary>
    /// Defines the action to be executed when the specified permission is granted.
    /// </summary>
    /// <param name="action">The action to execute, encapsulated in a <see cref="Func{CancellationToken, Task}"/>.</param>
    /// <returns>The same configurator instance for chaining further configuration.</returns>
    IUserContextConfigurator OnPermissionGranted(Func<CancellationToken, Task> action);

    /// <summary>
    /// Defines the action to be executed when the specified permission is denied.
    /// </summary>
    /// <param name="action">The action to execute, encapsulated in a <see cref="Func{CancellationToken, Task}"/>.</param>
    /// <returns>The same configurator instance for chaining further configuration.</returns>
    IUserContextConfigurator OnPermissionDenied(Func<CancellationToken, Task> action);

    /// <summary>
    /// Defines the action to be executed when the operation is cancelled.
    /// </summary>
    /// <param name="action">The action to execute, encapsulated in a <see cref="Func{CancellationToken, Task}"/>.</param>
    /// <returns>The same configurator instance for chaining further configuration.</returns>
    IUserContextConfigurator OnOperationCancelled(Func<CancellationToken, Task> action);

    /// <summary>
    /// Defines the action to be executed when an exception occurs during the permission check or action execution.
    /// </summary>
    /// <param name="action">The action to execute, encapsulated in a <see cref="Func{Exception, CancellationToken, Task}"/>.</param>
    /// <returns>The same configurator instance for chaining further configuration.</returns>
    IUserContextConfigurator OnException(Func<Exception, CancellationToken, Task> action);

    /// <summary>
    /// Executes the configured user context, performing the permission check and triggering the appropriate actions.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}