using TaskHub.Services;
using TaskHub.UI;

namespace TaskHub;

class Program
{
    static async Task Main(string[] args)
    {
        const string filePath = "tasks.json";

        using var fileService = new FileService(filePath);
        var taskManager = new TaskManager();
        using var deadlineChecker = new DeadlineChecker(taskManager, TimeSpan.FromSeconds(15));

        var ui = new ConsoleUI(taskManager, fileService, deadlineChecker);
        await ui.RunAsync();
    }
}
