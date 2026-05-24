using System.Text.Json.Serialization;

namespace TaskHub.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskPriority Priority { get; set; }
    public DateTime Deadline { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskState State { get; set; }
    public DateTime CreatedAt { get; set; }

    public TaskItem()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
    }

    public TaskItem(string title, string description, TaskPriority priority, DateTime deadline, TaskState state = TaskState.New)
        : this()
    {
        Title = title;
        Description = description;
        Priority = priority;
        Deadline = deadline;
        State = state;
    }

    [JsonIgnore]
    public bool IsOverdue => State != TaskState.Done && Deadline < DateTime.Now;

    public override string ToString()
    {
        return $"[{Id}] {Title} | {Priority} | {State} | Deadline: {Deadline:dd.MM.yyyy HH:mm}";
    }
}
