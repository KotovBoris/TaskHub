namespace TaskHub.Exceptions;

public class TaskNotFoundException : Exception
{
    public TaskNotFoundException(Guid id) : base($"Задача с ID {id} не найдена.") { }
    public TaskNotFoundException(string message) : base(message) { }
    public TaskNotFoundException(string message, Exception inner) : base(message, inner) { }
}
