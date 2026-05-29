using UmbracoCompose.Cli.Commands;

namespace UmbracoCompose.Cli.Utilities;

internal static class HttpErrorHelper
{
    public static CommandResult HandleHttpRequestException(System.Net.Http.HttpRequestException ex)
    {
        if (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            return CommandResult.Failure(ExitCodes.AuthenticationFailure,
                "Authentication failed. Check your credentials with 'profiles login'.");
        }
        return CommandResult.Failure(ExitCodes.NetworkError,
            $"Network error: {ex.Message}");
    }

    public static CommandResult HandleGenericException(Exception ex, string context)
    {
        return CommandResult.Failure(ExitCodes.RuntimeError,
            $"{context}: {ex.Message}");
    }
}
