namespace AsyncAuthFlowCore;

/// <summary>
/// Represents a repository for verifying user permissions.
/// </summary>
public interface IUserPermissionsRepository
{
    /// <summary>
    /// Asynchronously verifies if a given user has the specified permission.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="permissionRequired">The name of the permission to verify.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the user has the required permission.</returns>
    Task<bool> VerifyUserPermission(Guid userId, string permissionRequired, CancellationToken cancellationToken);
}