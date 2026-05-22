using TaskHub.Models;

namespace TaskHub.Delegates;

/// <summary>
/// Делегат для обработки событий с задачами (создание, удаление, просрочка)
/// </summary>
public delegate void TaskEventHandler(TaskItem task);

/// <summary>
/// Делегат для фильтрации задач
/// </summary>
public delegate bool TaskPredicate(TaskItem task);
