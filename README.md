# AuthFlow

AuthFlow is a versatile .NET library designed to streamline the authentication and authorization process for applications. By providing a highly configurable user context, AuthFlow enables developers to easily define and enforce permission requirements, manage user access, and handle exceptions in a robust and scalable manner.

## Features

- **Flexible Configuration**: Set up user contexts with specific permissions and actions using a fluent API.
- **Permission Verification**: Seamlessly integrate with existing user permission repositories to verify access rights.
- **Asynchronous Support**: Built for modern applications with async/await patterns, ensuring non-blocking I/O operations.
- **Comprehensive Event Handling**: React to permission grants, denials, operation cancellations, and exceptions with custom logic.
- **Easy Integration**: Designed to fit into existing .NET projects with minimal setup.

## Getting Started

To get started with AuthFlow, first install the package via NuGet:

```shell
dotnet add package AsyncAuthFlowCore
```

Then, you can configure a user context like so:
```shell
var userContext = AuthFlow.UserContext.Create(userPermissionsRepository, userId)
    .RequirePermission("ADMIN_ACCESS")
    .OnPermissionGranted(async cancellationToken =>
    {
        // Code to execute when permission is granted
    })
    .OnPermissionDenied(async cancellationToken =>
    {
        // Code to execute when permission is denied
    })
    .OnException(async (exception, cancellationToken) =>
    {
        // Exception handling code
    })
    .ExecuteAsync();
```
## Contributing
We welcome contributions to AuthFlow! If you have suggestions, bug reports, or would like to contribute code, please submit an issue or pull request on GitHub.

## License
AuthFlow is released under the MIT License. See the LICENSE file for more details.
