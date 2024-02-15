namespace AuthFlow.UnitTests;

public class InMemoryUserPermissionsRepository : IUserPermissionsRepository
{
    public bool ShouldThrowException { get; set; }
    private readonly Dictionary<Guid, HashSet<string>> _permissions = new();

    public Task<bool> VerifyUserPermission(Guid userId, string permissionName, CancellationToken cancellationToken)
    {
        if (ShouldThrowException)
        {
            throw new InvalidOperationException("Simulated exception for testing.");
        }
        
        return Task.FromResult(
            _permissions.TryGetValue(userId, out var userPermissions) 
            && userPermissions.Contains(permissionName));
    }

    public void GrantPermission(Guid userId, string permissionName)
    {
        if (!_permissions.ContainsKey(userId))
        {
            _permissions[userId] = new HashSet<string>();
        }

        _permissions[userId].Add(permissionName);
    }

    public void RevokePermission(Guid userId, string permissionName)
    {
        if (_permissions.TryGetValue(userId, out var userPermissions))
        {
            userPermissions.Remove(permissionName);
        }
    }
}