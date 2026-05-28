using System.CommandLine;
using System.Net;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ProfileValidateCommand : BaseCommand
{
    private static readonly Argument<string?> s_nameArgument = new("name")
    {
        Description = "Profile name to validate (uses default profile if not specified)",
        Arity = ArgumentArity.ZeroOrOne
    };

    private readonly ProfileConfigService _profileConfigService;
    private readonly IOAuthService _oAuthService;

    public ProfileValidateCommand(
        IConsole console,
        ProfileConfigService profileConfigService,
        IOAuthService oAuthService)
        : base("validate", "Validate a profile's credentials against the Umbraco Compose authentication service", console)
    {
        _profileConfigService = profileConfigService;
        _oAuthService = oAuthService;

        Arguments.Add(s_nameArgument);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string? profileName = parseResult.GetValue(s_nameArgument);

        ProfileConfig? config = _profileConfigService.Load();

        if (config is null || config.Profiles.Count == 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "No profiles configured. Add a profile first with 'profiles add'.");
        }

        // Resolve profile: explicit name > default
        string? resolvedName = profileName;

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            if (!string.IsNullOrWhiteSpace(config.Default) && config.Profiles.ContainsKey(config.Default))
            {
                resolvedName = config.Default;
            }

            if (resolvedName is null)
            {
                return CommandResult.Failure(ExitCodes.ValidationError, $"Default profile is not configured.");
            }
        }

        if (!config.Profiles.TryGetValue(resolvedName, out Profile? profile))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{resolvedName}' not found.");
        }

        try
        {
            await _oAuthService.AuthenticateAsync(profile.ClientId, profile.ClientSecret, cancellationToken);

            Console.DisplayMessage(Emojis.CheckMark, $"Credentials for profile '{resolvedName}' are valid.");
            return CommandResult.Success();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Credentials for profile '{resolvedName}' are invalid. ({ex.StatusCode})");
        }
        catch (HttpRequestException ex)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, $"Failed to connect to authentication service: {ex.Message}");
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ExitCodes.RuntimeError, $"Validation failed: {ex.Message}");
        }
    }
}
