namespace UmbracoCompose.Cli;

internal static class ExitCodes
{
    public const int Success = 0;
    public const int InvalidCommand = 1;
    public const int ValidationError = 2;
    public const int RuntimeError = 3;
    public const int AuthenticationFailure = 4;
    public const int NetworkError = 5;
}
