using System.Text.Json;

using UmbracoCompose.Cli.Models;

namespace UmbracoCompose.Cli.Services;

internal sealed class ProfileConfigService
{
    private readonly string _configPath;
    private readonly object _lock = new();

    public ProfileConfigService()
    {
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

    private static void EnsureDirectoryExists(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public ProfileConfig? Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return null;
                }

                string json = File.ReadAllText(_configPath);
                ProfileConfig? config = JsonSerializer.Deserialize(json, AppJsonContext.Default.ProfileConfig);
                return config;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }

    public bool Save(ProfileConfig config)
    {
        lock (_lock)
        {
            try
            {
                EnsureDirectoryExists(_configPath);
                string tempPath = _configPath + ".tmp";
                string json = JsonSerializer.Serialize(config, AppJsonContext.Default.ProfileConfig);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _configPath, overwrite: true);
                return true;
            }
            catch (IOException)
            {
                // Clean up temp file if it exists
                try
                {
                    File.Delete(_configPath + ".tmp");
                }
                catch
                {
                    // Ignore cleanup failures
                }
                return false;
            }
        }
    }

    public bool Update(Func<ProfileConfig, ProfileConfig> updateFn)
    {
        lock (_lock)
        {
            try
            {
                EnsureDirectoryExists(_configPath);

                ProfileConfig config;
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    config = JsonSerializer.Deserialize(json, AppJsonContext.Default.ProfileConfig)
                        ?? new();
                }
                else
                {
                    config = new();
                }

                ProfileConfig updated = updateFn(config);
                string tempPath = _configPath + ".tmp";
                string serialized = JsonSerializer.Serialize(updated, AppJsonContext.Default.ProfileConfig);
                File.WriteAllText(tempPath, serialized);
                File.Move(tempPath, _configPath, overwrite: true);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (IOException)
            {
                try
                {
                    File.Delete(_configPath + ".tmp");
                }
                catch
                {
                    // Ignore cleanup failures
                }
                return false;
            }
            catch (Exception)
            {
                // Callback threw (e.g., duplicate profile) — don't save
                return false;
            }
        }
    }
}
