using System.Reflection;
using TaskHub.Delegates;
using TaskHub.Exceptions;
using TaskHub.Models;

namespace TaskHub.Services;

/// <summary>
/// Менеджер задач — CRUD, поиск, фильтрация. Использует List, Dictionary, Generic, делегаты.
/// </summary>
public class TaskManager
{
    private readonly List<TaskItem> _tasks = new();

    // Событийные делегаты
    public event TaskEventHandler? TaskCreated;
    public event TaskEventHandler? TaskDeleted;
    public event TaskEventHandler? TaskOverdue;

    /// <summary>Все задачи</summary>
    public IReadOnlyList<TaskItem> Tasks => _tasks.AsReadOnly();

    /// <summary>Создать задачу</summary>
    public TaskItem Create(string title, string description, TaskPriority priority, DateTime deadline, TaskState state = TaskState.New)
    {
        var task = new TaskItem(title, description, priority, deadline, state);
        _tasks.Add(task);
        TaskCreated?.Invoke(task);
        return task;
    }

    /// <summary>Получить задачу по ID</summary>
    public TaskItem GetById(Guid id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        return task ?? throw new TaskNotFoundException(id);
    }

    /// <summary>Редактировать задачу через делегат Action</summary>
    public void Edit(Guid id, Action<TaskItem> editAction)
    {
        var task = GetById(id);
        editAction(task);
    }

    /// <summary>Удалить задачу</summary>
    public bool Delete(Guid id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return false;
        _tasks.Remove(task);
        TaskDeleted?.Invoke(task);
        return true;
    }

    /// <summary>Фильтрация через делегат TaskPredicate</summary>
    public List<TaskItem> Filter(TaskPredicate predicate)
    {
        return _tasks.Where(t => predicate(t)).ToList();
    }

    /// <summary>Generic-метод поиска по значению любого поля через рефлексию</summary>
    public List<TaskItem> SearchByField<T>(string fieldName, T value)
    {
        var prop = typeof(TaskItem).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null)
            throw new ArgumentException($"Поле '{fieldName}' не найдено в TaskItem");

        return _tasks.Where(t =>
        {
            var fieldValue = prop.GetValue(t);
            return fieldValue is not null && fieldValue.Equals(value);
        }).ToList();
    }

    /// <summary>Поиск по названию (частичное совпадение)</summary>
    public List<TaskItem> SearchByTitle(string substring)
    {
        return _tasks.Where(t => t.Title.Contains(substring, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Выполненные задачи</summary>
    public List<TaskItem> GetCompleted() => Filter(t => t.State == TaskState.Done);

    /// <summary>Невыполненные задачи</summary>
    public List<TaskItem> GetIncomplete() => Filter(t => t.State != TaskState.Done);

    /// <summary>Задачи с высоким приоритетом</summary>
    public List<TaskItem> GetHighPriority() => Filter(t => t.Priority == TaskPriority.High);

    /// <summary>Просроченные задачи</summary>
    public List<TaskItem> GetOverdue() => Filter(t => t.IsOverdue);

    /// <summary>Проверка просроченных задач (вызывается фоновым потоком)</summary>
    public void CheckOverdue()
    {
        var overdue = GetOverdue();
        foreach (var task in overdue)
        {
            TaskOverdue?.Invoke(task);
        }
    }

    /// <summary>Загрузить задачи из списка (при загрузке из файла)</summary>
    public void LoadTasks(IEnumerable<TaskItem> tasks)
    {
        _tasks.Clear();
        _tasks.AddRange(tasks);
    }
}
