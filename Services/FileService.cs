using System.Text.Json;
using TaskHub.Models;

namespace TaskHub.Services;

/// <summary>
/// Сервис файловых операций — async/await, IDisposable.
/// Сохраняет/загружает задачи в JSON.
/// </summary>
public class FileService : IDisposable
{
    private readonly string _filePath;
    private FileStream? _lockStream;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileService(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>Асинхронное сохранение задач в файл</summary>
    public async Task SaveAsync(List<TaskItem> tasks)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var json = JsonSerializer.Serialize(tasks, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[ОШИБКА] Не удалось сохранить файл: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ОШИБКА] Ошибка сериализации: {ex.Message}");
        }
    }

    /// <summary>Асинхронная загрузка задач из файла</summary>
    public async Task<List<TaskItem>> LoadAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine("[INFO] Файл задач не найден. Начинаем с пустым списком.");
                return new List<TaskItem>();
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOptions);
            return tasks ?? new List<TaskItem>();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[ОШИБКА] Не удалось прочитать файл: {ex.Message}");
            return new List<TaskItem>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ОШИБКА] Ошибка десериализации: {ex.Message}");
            return new List<TaskItem>();
        }
    }

    /// <summary>Открыть файловый поток (демонстрация IDisposable)</summary>
    public void AcquireLock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _lockStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _lockStream?.Dispose();
        _lockStream = null;
        _disposed = true;
    }
}
