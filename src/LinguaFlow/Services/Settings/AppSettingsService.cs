namespace LinguaFlow.Services.Settings;

using System.IO;
using System.Text.Json;
using LinguaFlow.Models;

/// <summary>
/// Loads and saves user settings from the local application data folder.
/// </summary>
public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string settingsPath;

    /// <summary>
    /// Creates a settings service that stores settings under the user's local profile.
    /// </summary>
    public AppSettingsService()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LinguaFlow");

        settingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

    /// <summary>
    /// Loads saved settings or returns defaults when no settings file exists.
    /// </summary>
    /// <returns>Application settings.</returns>
    public AppSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to the local application data folder.
    /// </summary>
    /// <param name="settings">Settings to persist.</param>
    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(settingsPath, json);
    }
}
