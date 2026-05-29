using System.Text.Json;
using Microsoft.Extensions.Logging;

using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Services;

internal sealed class ProfileConfigService
{
    private readonly string _configPath;
    private readonly ILogger<ProfileConfigService> _logger;

    public ProfileConfigService(ILogger<ProfileConfigService> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(GetConfigDirectory(), "profiles.json");
    }

    private static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "umbraco-compose"
            );
        }

        string? xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdgConfig))
        {
            return Path.Combine(xdgConfig, "umbraco-compose");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "umbraco-compose"
        );
    }

    public async Task<ProfileConfig?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(_configPath, cancellationToken).ConfigureAwait(false);
            ProfileConfig? config = JsonSerializer.Deserialize(json, AppJsonContext.Default.ProfileConfig);
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse profile config — file may be corrupted");
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read profile config");
            return null;
        }
    }

    public async Task<bool> UpdateAsync(Func<ProfileConfig, ProfileConfig> updateFn, CancellationToken cancellationToken = default)
    {
        try
        {
            ProfileConfig config;
            if (File.Exists(_configPath))
            {
                string json = await File.ReadAllTextAsync(_configPath, cancellationToken).ConfigureAwait(false);
                config = JsonSerializer.Deserialize(json, AppJsonContext.Default.ProfileConfig)
                    ?? new();
            }
            else
            {
                config = new();
            }

            ProfileConfig updated = updateFn(config);
            FileWriteHelper.WriteAtomic(_configPath, updated, AppJsonContext.Default.ProfileConfig);
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to save profile config");
            return false;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to save profile config");
            return false;
        }
        catch (Exception ex)
        {
            // Callback threw (e.g., duplicate profile) — don't save
            _logger.LogWarning(ex, "Failed to save profile config");
            return false;
        }
    }
}
