using TaskHub.Models;

namespace TaskHub.Services;

/// <summary>
/// Фоновая проверка дедлайнов — многопоточность через Task + CancellationToken.
/// </summary>
public class DeadlineChecker : IDisposable
{
    private readonly TaskManager _taskManager;
    private readonly TimeSpan _checkInterval;
    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;
    private volatile bool _disposed;

    public DeadlineChecker(TaskManager taskManager, TimeSpan? checkInterval = null)
    {
        _taskManager = taskManager;
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>Запустить фоновую проверку</summary>
    public void Start()
    {
        if (_backgroundTask != null) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _backgroundTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_checkInterval, token);
                    CheckAndNotify();
                }
                catch (OperationCanceledException)
                {
                    // Нормальное завершение
                }
            }
        }, token);
    }

    /// <summary>Остановить фоновую проверку</summary>
    public void Stop()
    {
        if (_cts == null) return;
        try { _cts.Cancel(); } catch (ObjectDisposedException) { }
    }

    private void CheckAndNotify()
    {
        var overdue = _taskManager.GetOverdue();
        if (overdue.Count == 0) return;

        Console.WriteLine();
        Console.WriteLine($"[⏰ DEADLINE] Найдено просроченных задач: {overdue.Count}");
        foreach (var task in overdue)
        {
            Console.WriteLine($"  - \"{task.Title}\" (дедлайн: {task.Deadline:dd.MM.yyyy HH:mm}, статус: {task.State})");
        }
        Console.Write("> ");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts?.Dispose();
        // Не диспозим Task — он завершится по CancellationToken
    }
}
