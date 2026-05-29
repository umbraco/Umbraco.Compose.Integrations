using UmbracoCompose.Cli.Commands;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;

namespace UmbracoCompose.Cli.Services;

internal class ProfileResolver
{
    private readonly ProfileConfigService _profileConfigService;

    public ProfileResolver(ProfileConfigService profileConfigService)
    {
        _profileConfigService = profileConfigService;
    }

    public async Task<(string? ResolvedName, Models.Profile? Profile, CommandResult? Error)> ResolveAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        string? resolvedName = null;
        Models.Profile? profile = null;
        CommandResult? error = null;

        ProfileConfig? config = await _profileConfigService.LoadAsync(cancellationToken).ConfigureAwait(false);

        if (config is null || config.Profiles.Count == 0)
        {
            error = CommandResult.Failure(ExitCodes.ValidationError, "No profiles configured. Add a profile first with 'profiles add'.");
            return (resolvedName, profile, error);
        }

        resolvedName = profileName;

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            if (!string.IsNullOrWhiteSpace(config.Default) && config.Profiles.ContainsKey(config.Default))
            {
                resolvedName = config.Default;
            }

            if (resolvedName is null)
            {
                error = CommandResult.Failure(ExitCodes.ValidationError, "Default profile is not configured.");
                return (resolvedName, profile, error);
            }
        }

        if (!config.Profiles.TryGetValue(resolvedName, out profile))
        {
            error = CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{resolvedName}' not found.");
            return (resolvedName, profile, error);
        }

        if (string.IsNullOrWhiteSpace(profile.Region))
        {
            error = CommandResult.Failure(ExitCodes.ValidationError, "Profile is missing 'Region'.");
            return (resolvedName, profile, error);
        }

        if (string.IsNullOrWhiteSpace(profile.ProjectAlias))
        {
            error = CommandResult.Failure(ExitCodes.ValidationError, "Profile is missing 'ProjectAlias'.");
            return (resolvedName, profile, error);
        }

        if (string.IsNullOrWhiteSpace(profile.EnvironmentAlias))
        {
            error = CommandResult.Failure(ExitCodes.ValidationError, "Profile is missing 'EnvironmentAlias'.");
            return (resolvedName, profile, error);
        }

        return (resolvedName, profile, error);
    }
}
