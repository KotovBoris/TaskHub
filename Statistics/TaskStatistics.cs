using System.Text;
using TaskHub.Models;

namespace TaskHub.Statistics;

/// <summary>
/// Статистика по задачам — static класс, использует Dictionary.
/// </summary>
public static class TaskStatistics
{
    /// <summary>Общее количество задач</summary>
    public static int TotalCount(IEnumerable<TaskItem> tasks) => tasks.Count();

    /// <summary>Количество выполненных задач</summary>
    public static int CompletedCount(IEnumerable<TaskItem> tasks) =>
        tasks.Count(t => t.State == TaskState.Done);

    /// <summary>Количество просроченных задач</summary>
    public static int OverdueCount(IEnumerable<TaskItem> tasks) =>
        tasks.Count(t => t.IsOverdue);

    /// <summary>Статистика по приоритетам (Dictionary)</summary>
    public static Dictionary<TaskPriority, int> ByPriority(IEnumerable<TaskItem> tasks)
    {
        var result = new Dictionary<TaskPriority, int>
        {
            [TaskPriority.Low] = 0,
            [TaskPriority.Medium] = 0,
            [TaskPriority.High] = 0
        };

        foreach (var task in tasks)
        {
            result[task.Priority]++;
        }

        return result;
    }

    /// <summary>Вывести полную статистику</summary>
    public static string GetFullReport(IEnumerable<TaskItem> tasks)
    {
        var list = tasks.ToList();
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine("           СТАТИСТИКА ЗАДАЧ           ");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"  Всего задач:      {TotalCount(list)}");
        sb.AppendLine($"  Выполнено:        {CompletedCount(list)}");
        sb.AppendLine($"  Просрочено:       {OverdueCount(list)}");
        sb.AppendLine("───────────────────────────────────────");

        var byPriority = ByPriority(list);
        sb.AppendLine("  По приоритетам:");
        sb.AppendLine($"    Low:             {byPriority[TaskPriority.Low]}");
        sb.AppendLine($"    Medium:          {byPriority[TaskPriority.Medium]}");
        sb.AppendLine($"    High:            {byPriority[TaskPriority.High]}");
        sb.AppendLine("═══════════════════════════════════════");

        return sb.ToString();
    }
}
