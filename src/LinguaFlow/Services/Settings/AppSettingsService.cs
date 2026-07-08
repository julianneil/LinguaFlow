namespace LinguaFlow.Services.Settings;

using System.IO;
using System.Text.Json;
using LinguaFlow.Models;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string settingsPath;

    public AppSettingsService()
    {
        var settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LinguaFlow");

        settingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

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

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(settingsPath, json);
    }
}
