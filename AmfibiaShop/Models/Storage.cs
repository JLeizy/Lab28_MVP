using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AmfibiaShop.Models;

/// <summary>
/// Хранилище данных пользователя в JSON-файлах
/// (по умолчанию %AppData%\AMFIBIA\Lab28_MVP).
/// Сюда сохраняются регистрации, корзина, избранное, заказы и отзывы.
/// </summary>
public class Storage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // кириллица в файлах остаётся читаемой
    };

    public Storage(string folder)
    {
        Folder = folder;
        Directory.CreateDirectory(folder);
    }

    public string Folder { get; }

    public static Storage CreateDefault() =>
        new Storage(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AMFIBIA", "Lab28_MVP"));

    /// <summary>Читает файл. Если его нет или он повреждён — возвращает null.</summary>
    public T? Load<T>(string name) where T : class
    {
        string path = Path.Combine(Folder, name);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Сохраняет через временный файл, чтобы не испортить данные при сбое.</summary>
    public void Save<T>(string name, T value)
    {
        string path = Path.Combine(Folder, name);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Безопасное имя файла по email пользователя.</summary>
    public static string UserFile(string email) =>
        "user-" + string.Concat(email.ToLowerInvariant().Split(Path.GetInvalidFileNameChars())).Replace('@', '_') + ".json";
}
