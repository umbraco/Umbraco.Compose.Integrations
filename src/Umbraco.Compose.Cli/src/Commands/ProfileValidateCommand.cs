using System.CommandLine;
using Umbraco.Compose.Cli.Models;
using Umbraco.Compose.Cli.Services;
using Umbraco.Compose.Cli.Utilities;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class ProfileValidateCommand : BaseCommand
{
    private static readonly Argument<string?> s_nameArgument = new("name")
    {
        Description = "Profile name to validate (uses default profile if not specified)",
        Arity = ArgumentArity.ZeroOrOne
    };

    private readonly ProfileResolver _profileResolver;
    private readonly IOAuthService _oAuthService;

    public ProfileValidateCommand(
        IConsole console,
        ProfileResolver profileResolver,
        IOAuthService oAuthService)
        : base("validate", "Validate a profile's credentials against the Umbraco Compose authentication service", console)
    {
        _profileResolver = profileResolver;
        _oAuthService = oAuthService;

        Arguments.Add(s_nameArgument);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string? profileName = parseResult.GetValue(s_nameArgument);

        // Resolve profile
        var (resolvedName, profile, error) = await _profileResolver.ResolveAsync(profileName, cancellationToken).ConfigureAwait(false);
        if (error != null)
            return error;

        try
        {
            await _oAuthService.AuthenticateAsync(profile!.ClientId, profile!.ClientSecret, cancellationToken);

            Console.DisplayMessage(Emojis.CheckMark, $"Credentials for profile '{resolvedName}' are valid.");
            return CommandResult.Success();
        }
        catch (HttpRequestException ex)
        {
            return HttpErrorHelper.HandleHttpRequestException(ex);
        }
        catch (Exception ex)
        {
            return HttpErrorHelper.HandleGenericException(ex, "Validation");
        }
    }
}
