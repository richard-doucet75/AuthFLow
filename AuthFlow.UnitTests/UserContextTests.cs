namespace AsyncAuthFlowCore.UnitTests;


[Trait("Category", "Unit Tests")]
public class UserContextTests
{
    private readonly InMemoryUserPermissionsRepository _repository = new();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task PermissionGranted_ExecutesGrantedAction()
    {
        _repository.GrantPermission(_userId, "READ");
        var wasCalled = false;

        var userContext = UserContext.Create(_repository, _userId)
            .RequirePermission("READ")
            .OnPermissionGranted(async _ =>
            {
                wasCalled = true;
                await Task.CompletedTask;
            })
            .OnPermissionDenied(_ => Task.CompletedTask)
            .OnException((_, _) => Task.CompletedTask);

        await userContext.ExecuteAsync();

        Assert.True(wasCalled, "The OnPermissionGranted action was not called as expected.");
    }

    [Fact]
    public async Task PermissionDenied_ExecutesDeniedAction()
    {
        var wasCalled = false;

        var userContext = UserContext.Create(_repository, _userId)
            .RequirePermission("WRITE")
            .OnPermissionGranted(_ => Task.CompletedTask)
            .OnPermissionDenied(async _ =>
            {
                wasCalled = true;
                await Task.CompletedTask;
            })
            .OnException((_, _) => Task.CompletedTask);

        await userContext.ExecuteAsync();

        Assert.True(wasCalled, "The OnPermissionDenied action was not called as expected.");
    }

    [Fact]
    public async Task OperationCancelled_ExecutesCancellationAction()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        _repository.GrantPermission(_userId, "ANY");
        var wasCalled = false;

        var userContext = UserContext.Create(_repository, _userId)
            .RequirePermission("ANY")
            .OnPermissionGranted(async c =>
            {
                await Task.Run(() =>
                {
                    // Simulating a long-running operation
                    while (!c.IsCancellationRequested)
                    {
                        // Simulate work
                        Task.Delay(10, c).Wait(c);
                    }
                    c.ThrowIfCancellationRequested();
                }, c);
            })
            .OnPermissionDenied(_ => Task.CompletedTask)
            .OnOperationCancelled(async _ =>
            {
                wasCalled = true;
                await Task.CompletedTask;
            })
            .OnException((_, _) => Task.CompletedTask);

        cancellationTokenSource.Cancel();

        await userContext.ExecuteAsync(cancellationTokenSource.Token);

        Assert.True(wasCalled, "The OnOperationCancelled action should have been called.");
    }

    [Fact]
    public async Task ExceptionDuringPermissionCheck_ExecutesExceptionAction()
    {
        _repository.ShouldThrowException = true;
        var exceptionWasCalled = false;
        Exception? capturedException = null;

        var userContext = UserContext.Create(_repository, _userId)
            .RequirePermission("READ")
            .OnPermissionGranted(_ => Task.CompletedTask)
            .OnPermissionDenied(_ => Task.CompletedTask)
            .OnException(async (ex, _) =>
            {
                exceptionWasCalled = true;
                capturedException = ex;
                await Task.CompletedTask;
            });

        await userContext.ExecuteAsync();

        Assert.True(exceptionWasCalled, "The OnException action was not called as expected.");
        Assert.NotNull(capturedException);
    }

    [Fact]
    public async Task PreemptiveCancellation_DoesNotExecuteGrantedOrDeniedActions()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Preemptive cancellation
        var operationCancelledExecuted = false;

        var userContext = UserContext.Create(_repository, _userId)
            .RequirePermission("READ")
            .OnPermissionGranted(_ => Task.FromException(new Exception("Should not execute")))
            .OnPermissionDenied(_ => Task.FromException(new Exception("Should not execute")))
            .OnOperationCancelled(_ =>
            {
                operationCancelledExecuted = true;
                return Task.CompletedTask;
            })
            .OnException((_, _) => Task.CompletedTask);

        await userContext.ExecuteAsync(cancellationTokenSource.Token);

        Assert.True(operationCancelledExecuted, "The OnOperationCancelled action should execute on preemptive cancellation.");
    }
    
    [Fact]
    public void ConfiguringRequirePermissionTwice_ThrowsConfigurationException()
    {
        // Act & Assert
        var exception = Assert.Throws<UserContext.UserContextConfigurationException>(() =>
        {
            UserContext.Create(_repository, _userId)
                .RequirePermission("READ")
                .RequirePermission("WRITE");
        });

        Assert.Contains("has already been configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void ConfiguringOnPermissionGrantedTwice_ThrowsConfigurationException()
    { 
        // Act & Assert
        var exception = Assert.Throws<UserContext.UserContextConfigurationException>(() =>
        {
            UserContext.Create(_repository, _userId)
                .RequirePermission("READ")
                .OnPermissionGranted(_ => Task.CompletedTask) // First configuration
                .OnPermissionGranted(_ => Task.CompletedTask);
        });

        Assert.Contains("OnPermissionGranted has already been configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void ConfiguringOnPermissionDeniedTwice_ThrowsConfigurationException()
    {
        // Act & Assert
        var exception = Assert.Throws<UserContext.UserContextConfigurationException>(() =>
        {
            UserContext.Create(_repository, _userId)
                .RequirePermission("READ")
                .OnPermissionDenied(_ => Task.CompletedTask) // First configuration
                .OnPermissionDenied(_ => Task.CompletedTask);
        });

        Assert.Contains("OnPermissionDenied has already been configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void ConfiguringOnOperationCancelledTwice_ThrowsConfigurationException()
    {
        // Act & Assert
        var exception = Assert.Throws<UserContext.UserContextConfigurationException>(() =>
        {
            UserContext.Create(_repository, _userId)
                .RequirePermission("READ")
                .OnOperationCancelled(_ => Task.CompletedTask) // First configuration
                .OnOperationCancelled(_ => Task.CompletedTask);
        });

        Assert.Contains("OnOperationCancelled has already been configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void ConfiguringOnExceptionTwice_ThrowsConfigurationException()
    {
        // Act & Assert
        var exception = Assert.Throws<UserContext.UserContextConfigurationException>(() =>
        {
            UserContext.Create(_repository, _userId)
                .RequirePermission("READ")
                .OnException((_, _) => Task.CompletedTask) // First configuration
                .OnException((_, _) => Task.CompletedTask);
        });

        // The specific error message expected depends on the implementation.
        // This example assumes a check is in place to prevent multiple configurations.
        Assert.Contains("OnException has already been configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task MissingRequiredPermission_ThrowsConfigurationExceptionAsync()
    {
        var userContext = UserContext.Create(_repository, _userId);

        var exception = await Assert.ThrowsAsync<UserContext.UserContextConfigurationException>(async () =>
        {
            await userContext.ExecuteAsync();
        });

        Assert.Contains("Required Permission", exception.Message);
    }
    
    [Fact]
    public async Task MissingOnPermissionGranted_ThrowsConfigurationExceptionAsync()
    {
        var repository = new InMemoryUserPermissionsRepository();
        var userId = Guid.NewGuid();

        var userContext = UserContext.Create(repository, userId)
            .RequirePermission("READ");

        var exception = await Assert.ThrowsAsync<UserContext.UserContextConfigurationException>(async () =>
        {
            await userContext.ExecuteAsync();
        });

        Assert.Contains("On Permission Granted Async", exception.Message);
    }

    
    [Fact]
    public async Task MissingOnPermissionDenied_ThrowsConfigurationException()
    {
        var repository = new InMemoryUserPermissionsRepository();
        var userId = Guid.NewGuid();

        var userContext = UserContext.Create(repository, userId)
            .RequirePermission("READ")
            .OnPermissionGranted(_ => Task.CompletedTask);

        var exception = await Assert.ThrowsAsync<UserContext.UserContextConfigurationException>(async () =>
        {
            await userContext.ExecuteAsync();
        });

        Assert.Contains("On Permission Denied Async", exception.Message);
    }
    
    [Fact]
    public async Task CancellationWithoutConfiguredAction_ThrowsOperationCanceledException()
    {
        // Arrange
        var repository = new InMemoryUserPermissionsRepository();
        var userId = Guid.NewGuid();
        repository.GrantPermission(userId, "READ");

        var cancellationTokenSource = new CancellationTokenSource();
        
        var userContext = UserContext.Create(repository, userId)
            .RequirePermission("READ")
            .OnPermissionGranted(_ => Task.CompletedTask) // Simulate granted permission
            .OnPermissionDenied(_ => Task.CompletedTask) // Include to pass configuration validation
            .OnException((_, _) => Task.CompletedTask); // Include to pass configuration validation

        // Act
        cancellationTokenSource.Cancel(); // Trigger cancellation before execution

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => 
            await userContext.ExecuteAsync(cancellationTokenSource.Token));

        // No need to assert the execution of OnOperationCancelled since it's not configured.
        // The test passes if OperationCanceledException is thrown as expected.
    }
}