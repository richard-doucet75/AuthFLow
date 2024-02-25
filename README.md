# AuthFlow

AuthFlow is a versatile .NET library designed to streamline the authentication and authorization process for applications. By providing a highly configurable user context, AuthFlow enables developers to easily define and enforce permission requirements, manage user access, and handle exceptions in a robust and scalable manner.

## Features

- **Flexible Configuration**: Set up user contexts with specific permissions and actions using a fluent API.
- **Permission Verification**: Seamlessly integrate with existing user permission repositories to verify access rights.
- **Asynchronous Support**: Built for modern applications with async/await patterns, ensuring non-blocking I/O operations.
- **Comprehensive Event Handling**: React to permission grants, denials, operation cancellations, and exceptions with custom logic.
- **Easy Integration**: Designed to fit into existing .NET projects with minimal setup.

## Getting Started

To begin using AuthFlow in your project, install the package via NuGet:

```shell
dotnet add package AsyncAuthFlowCore
```

### Configuring a User Context

Create and configure a user context by specifying permissions and defining actions for various outcomes:

```csharp
var userContext = AuthFlow.UserContext.Create(userPermissionsRepository, "USER_ID")
    .RequirePermission("ADMIN_ACCESS")
    .OnPermissionGranted(async (userId, cancellationToken) =>
    {
        Console.WriteLine($"Permission Granted for {userId}.");
        // Code to execute when permission is granted
    })
    .OnPermissionDenied(async (userId, cancellationToken) =>
    {
        Console.WriteLine($"Permission Denied for {userId}.");
        // Code to execute when permission is denied
    })
    .OnException(async (exception, userId, cancellationToken) =>
    {
        Console.WriteLine($"Exception occurred for {userId}: {exception.Message}");
        // Exception handling code
    })
    .ExecuteAsync();
```

This example demonstrates setting up a user context with the required `ADMIN_ACCESS` permission. It configures actions to handle permission grants, denials, and exceptions.

## Usage Scenarios

- **Web Applications**: Secure endpoints or actions based on user roles and permissions.
- **Background Services**: Ensure background tasks or jobs run with the appropriate user permissions.
- **Microservices**: Apply user context in inter-service communication to maintain security and permission checks.

## Contributing

We welcome contributions to AuthFlow! If you have suggestions, bug reports, or would like to contribute code, please submit an issue or pull request on GitHub. Your input helps make AuthFlow even better.

## License

AuthFlow is released under the MIT License. See the LICENSE file in the repository for more details.