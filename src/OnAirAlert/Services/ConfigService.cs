using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnAirAlert.Models;

namespace OnAirAlert.Services;

[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class AppConfigJsonContext : JsonSerializerContext;

public class ConfigService
{
    private readonly string _configPath;

    public AppConfig Config { get; private set; } = new();

    public ConfigService()
    {
        var baseDir = AppContext.BaseDirectory;
        _configPath = Path.Combine(baseDir, "config.json");
        Load();
    }

    public void Load()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                Config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? new AppConfig();
            }
            catch
            {
                // config.json が壊れていたらデフォルトで起動
                Config = new AppConfig();
            }
        }
        else
        {
            Config = new AppConfig();
            Save();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Config, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(_configPath, json);
    }

    public string GetAbsolutePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return relativePath;
        return Path.Combine(AppContext.BaseDirectory, relativePath);
    }
}
