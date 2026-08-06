using System.Text.Json;
using TraderUI.Models;

namespace TraderUI.Services;

/// <summary>
/// Persists data to local device storage using the app's data directory.
/// Supports all platforms: Android, iOS, Windows, macOS.
/// </summary>
public class LocalStorageService : ILocalStorageService
{
    private readonly string _basePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LocalStorageService()
    {
        _basePath = Path.Combine(FileSystem.AppDataDirectory, "TraderData");
        Directory.CreateDirectory(_basePath);
    }

    public async Task SaveAsync<T>(string key, T value)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(key);
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T?> LoadAsync<T>(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath)) return default;
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return default;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(key);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        await Task.CompletedTask;
        return File.Exists(GetFilePath(key));
    }

    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (Directory.Exists(_basePath))
            {
                foreach (var file in Directory.GetFiles(_basePath))
                    File.Delete(file);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetFilePath(string key)
    {
        var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_basePath, $"{safeKey}.json");
    }
}
