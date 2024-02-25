using AsyncAuthFlowCore.Abstractions;

namespace AsyncAuthFlowCore.UnitTests;

public class InMemoryUserPermissionsRepository : IUserPermissionsRepository
{
    public bool ShouldThrowException { get; set; }
    private readonly Dictionary<string, HashSet<string>> _permissions = new();

    public Task<bool> VerifyUserPermission(string userId, string permissionName, CancellationToken cancellationToken)
    {
        if (ShouldThrowException)
        {
            throw new InvalidOperationException("Simulated exception for testing.");
        }
        
        return Task.FromResult(
            _permissions.TryGetValue(userId, out var userPermissions) 
            && userPermissions.Contains(permissionName));
    }

    public void GrantPermission(string userId, string permissionName)
    {
        if (!_permissions.ContainsKey(userId))
        {
            _permissions[userId] = new HashSet<string>();
        }

        _permissions[userId].Add(permissionName);
    }
}