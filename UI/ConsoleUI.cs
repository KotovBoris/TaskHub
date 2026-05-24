using System.Globalization;
using Spectre.Console;
using TaskHub.Delegates;
using TaskHub.Exceptions;
using TaskHub.Models;
using TaskHub.Services;
using TaskHub.Statistics;

namespace TaskHub.UI;

/// <summary>
/// Интерактивный REPL-интерфейс. Spectre.Console для таблиц.
/// </summary>
public class ConsoleUI
{
    private readonly TaskManager _taskManager;
    private readonly FileService _fileService;
    private readonly DeadlineChecker _deadlineChecker;
    private bool _running = true;

    public ConsoleUI(TaskManager taskManager, FileService fileService, DeadlineChecker deadlineChecker)
    {
        _taskManager = taskManager;
        _fileService = fileService;
        _deadlineChecker = deadlineChecker;

        // Подписка на события через делегаты
        _taskManager.TaskCreated += task =>
            AnsiConsole.MarkupLine($"[green]✓ Задача \"{Markup.Escape(task.Title)}\" создана (ID: {task.Id})[/]");

        _taskManager.TaskDeleted += task =>
            AnsiConsole.MarkupLine($"[red]✗ Задача \"{Markup.Escape(task.Title)}\" удалена[/]");

        _taskManager.TaskOverdue += task =>
            AnsiConsole.MarkupLine($"[yellow]⏰ Задача \"{Markup.Escape(task.Title)}\" просрочена![/]");
    }

    public async Task RunAsync()
    {
        AnsiConsole.Write(new FigletText("TaskHub").Color(Color.Blue));
        AnsiConsole.MarkupLine("[grey]Менеджер задач. Введите [bold]help[/] для списка команд.[/]");
        AnsiConsole.WriteLine();

        // Автозагрузка при старте
        await LoadTasksAsync();

        // Запуск фоновой проверки дедлайнов
        _deadlineChecker.Start();

        while (_running)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            var parts = SplitInput(input);
            var command = parts[0].ToLower();
            var args = parts.Skip(1).ToList();

            try
            {
                switch (command)
                {
                    case "help":
                    case "?":
                        ShowHelp(args);
                        break;
                    case "add":
                        AddTask(args);
                        break;
                    case "list":
                    case "ls":
                        ListTasks();
                        break;
                    case "edit":
                        EditTask(args);
                        break;
                    case "delete":
                    case "rm":
                        DeleteTask(args);
                        break;
                    case "search":
                    case "find":
                        SearchTasks(args);
                        break;
                    case "stats":
                        ShowStats();
                        break;
                    case "save":
                        await SaveTasksAsync();
                        break;
                    case "load":
                        await LoadTasksAsync();
                        break;
                    case "quit":
                    case "exit":
                    case "q":
                        _running = false;
                        break;
                    default:
                        AnsiConsole.MarkupLine($"[red]Неизвестная команда: {command}. Введите help.[/]");
                        break;
                }
            }
            catch (TaskNotFoundException ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка: {Markup.Escape(ex.Message)}[/]");
            }
        }

        // Автосохранение при выходе
        await SaveTasksAsync();
        _deadlineChecker.Dispose();
        AnsiConsole.MarkupLine("[grey]До свидания![/]");
    }

    private static List<string> SplitInput(string input)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    private void ShowHelp(List<string> args)
    {
        if (args.Count > 0)
        {
            var cmd = args[0].ToLower();
            var helpText = cmd switch
            {
                "add" => "[bold]add[/] — Создать задачу\nФормат: add \"Название\" \"Описание\" Приоритет Дедлайн\nПриоритет: Low / Medium / High\nДедлайн: dd.MM.yyyy\nПример: add \"Купить молоко\" \"Сходить в магазин\" High 01.06.2026",
                "list" or "ls" => "[bold]list[/] — Показать все задачи\nФормат: list",
                "edit" => "[bold]edit[/] — Редактировать задачу\nФормат: edit ID поле=значение\nПоля: title, description, priority, state, deadline\nПример: edit abc123 state=Done\nПример: edit abc123 priority=High title=\"Новое название\"",
                "delete" or "rm" => "[bold]delete[/] — Удалить задачу\nФормат: delete ID\nПример: delete abc123",
                "search" or "find" => "[bold]search[/] — Поиск задач\nФормат: search поле=значение\nПоля: title (частичное совпадение), state, priority\nПример: search title=молок\nПример: search state=Done\nПример: search priority=High",
                "stats" => "[bold]stats[/] — Показать статистику\nФормат: stats",
                "save" => "[bold]save[/] — Сохранить задачи в файл\nФормат: save",
                "load" => "[bold]load[/] — Загрузить задачи из файла\nФормат: load",
                _ => "[red]Неизвестная команда. Введите help.[/]"
            };
            AnsiConsole.MarkupLine(helpText);
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).Title("[bold]Команды TaskHub[/]");
        table.AddColumn("Команда");
        table.AddColumn("Описание");
        table.AddRow("help", "Показать помощь (help <команда> — подробно)");
        table.AddRow("add", "Создать задачу");
        table.AddRow("list", "Показать все задачи");
        table.AddRow("edit ID field=value", "Редактировать задачу");
        table.AddRow("delete ID", "Удалить задачу");
        table.AddRow("search field=value", "Поиск задач");
        table.AddRow("stats", "Статистика");
        table.AddRow("save", "Сохранить в файл");
        table.AddRow("load", "Загрузить из файла");
        table.AddRow("quit", "Выход");
        AnsiConsole.Write(table);
    }

    private void AddTask(List<string> args)
    {
        if (args.Count < 4)
        {
            AnsiConsole.MarkupLine("[red]Формат: add \"Название\" \"Описание\" Приоритет Дедлайн[/]");
            return;
        }

        var title = args[0].Trim();
        var description = args[1].Trim();
        if (string.IsNullOrEmpty(title))
        {
            AnsiConsole.MarkupLine("[red]Название не может быть пустым[/]");
            return;
        }

        if (!Enum.TryParse<TaskPriority>(args[2], true, out var priority))
        {
            AnsiConsole.MarkupLine("[red]Приоритет: Low / Medium / High[/]");
            return;
        }

        // Парсим дату в русском формате dd.MM.yyyy
        if (!DateTime.TryParseExact(args[3], new[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "dd.MM.yyyy H:mm" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
        {
            AnsiConsole.MarkupLine("[red]Неверный формат даты. Используйте dd.MM.yyyy[/]");
            return;
        }

        _taskManager.Create(title, description, priority, deadline);
    }

    private void ListTasks()
    {
        var tasks = _taskManager.Tasks.ToList();

        if (tasks.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Задач нет.[/]");
            return;
        }

        PrintTaskTable(tasks);
    }

    private void EditTask(List<string> args)
    {
        if (args.Count < 2)
        {
            AnsiConsole.MarkupLine("[red]Формат: edit ID поле=значение[/]");
            return;
        }

        var idStr = args[0];
        var edits = args.Skip(1).ToList();

        // Используем делегат Action<TaskItem> для редактирования
        var task = _taskManager.ResolveId(idStr);
        _taskManager.Edit(task.Id, t =>
        {
            foreach (var edit in edits)
            {
                var eqIndex = edit.IndexOf('=');
                if (eqIndex < 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]Пропуск: '{Markup.Escape(edit)}' (нет =)[/]");
                    continue;
                }

                var field = edit[..eqIndex].Trim();
                var value = edit[(eqIndex + 1)..].Trim().Trim('"');

                switch (field.ToLower())
                {
                    case "title":
                        t.Title = value;
                        break;
                    case "description":
                        t.Description = value;
                        break;
                    case "priority":
                        if (Enum.TryParse<TaskPriority>(value, true, out var p))
                            t.Priority = p;
                        else
                            AnsiConsole.MarkupLine($"[red]Неверный приоритет: {value}[/]");
                        break;
                    case "state":
                    case "status":
                        if (Enum.TryParse<TaskState>(value, true, out var s))
                            t.State = s;
                        else
                            AnsiConsole.MarkupLine($"[red]Неверный статус: {value}[/]");
                        break;
                    case "deadline":
                        if (DateTime.TryParseExact(value, new[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm" },
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                            t.Deadline = d;
                        else
                            AnsiConsole.MarkupLine($"[red]Неверная дата: {value}[/]");
                        break;
                    default:
                        AnsiConsole.MarkupLine($"[yellow]Неизвестное поле: {field}[/]");
                        break;
                }
            }
        });

        AnsiConsole.MarkupLine("[green]✓ Задача обновлена[/]");
    }

    private void DeleteTask(List<string> args)
    {
        if (args.Count < 1)
        {
            AnsiConsole.MarkupLine("[red]Формат: delete ID[/]");
            return;
        }

        try
        {
            var task = _taskManager.ResolveId(args[0]);
            if (!_taskManager.Delete(task.Id))
            {
                AnsiConsole.MarkupLine("[red]Задача не найдена[/]");
            }
        }
        catch (TaskNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
        }
    }

    private void SearchTasks(List<string> args)
    {
        if (args.Count < 1)
        {
            AnsiConsole.MarkupLine("[red]Формат: search поле=значение[/]");
            return;
        }

        var eqIndex = args[0].IndexOf('=');
        if (eqIndex < 0)
        {
            // Поиск по названию без поля
            var results = _taskManager.SearchByTitle(args[0]);
            PrintSearchResults(results);
            return;
        }

        var field = args[0][..eqIndex].Trim();
        var value = args[0][(eqIndex + 1)..].Trim().Trim('"');

        List<TaskItem> results2;

        switch (field.ToLower())
        {
            case "title":
                results2 = _taskManager.SearchByTitle(value);
                break;
            case "state":
            case "status":
                if (Enum.TryParse<TaskState>(value, true, out var state))
                    results2 = _taskManager.SearchByField("State", state);
                else
                {
                    AnsiConsole.MarkupLine($"[red]Неверный статус: {value}[/]");
                    return;
                }
                break;
            case "priority":
                if (Enum.TryParse<TaskPriority>(value, true, out var priority))
                    results2 = _taskManager.SearchByField("Priority", priority);
                else
                {
                    AnsiConsole.MarkupLine($"[red]Неверный приоритет: {value}[/]");
                    return;
                }
                break;
            default:
                // Generic-поиск через рефлексию для произвольных полей
                try
                {
                    results2 = _taskManager.SearchByField(field, value);
                }
                catch (ArgumentException ex)
                {
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                    return;
                }
                break;
        }

        PrintSearchResults(results2);
    }

    private void ShowStats()
    {
        var report = TaskStatistics.GetFullReport(_taskManager.Tasks);
        AnsiConsole.Write(report);
    }

    private async Task SaveTasksAsync()
    {
        await _fileService.SaveAsync(_taskManager.Tasks.ToList());
        AnsiConsole.MarkupLine("[green]✓ Задачи сохранены в файл[/]");
    }

    private async Task LoadTasksAsync()
    {
        var tasks = await _fileService.LoadAsync();
        _taskManager.LoadTasks(tasks);
        AnsiConsole.MarkupLine($"[green]✓ Загружено задач: {tasks.Count}[/]");
    }

    private void PrintTaskTable(List<TaskItem> tasks)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Название");
        table.AddColumn("Описание");
        table.AddColumn("Приоритет");
        table.AddColumn("Статус");
        table.AddColumn("Дедлайн");
        table.AddColumn("Просрочена");

        foreach (var task in tasks)
        {
            var priorityColor = task.Priority switch
            {
                TaskPriority.High => "red",
                TaskPriority.Medium => "yellow",
                _ => "green"
            };

            var stateColor = task.State switch
            {
                TaskState.Done => "green",
                TaskState.InProgress => "blue",
                _ => "white"
            };

            table.AddRow(
                task.Id.ToString()[..8] + "...",
                Markup.Escape(task.Title),
                Markup.Escape(task.Description.Length > 30 ? task.Description[..30] + "..." : task.Description),
                $"[{priorityColor}]{task.Priority}[/]",
                $"[{stateColor}]{task.State}[/]",
                task.Deadline.ToString("dd.MM.yyyy HH:mm"),
                task.IsOverdue ? "[red]Да[/]" : "[green]Нет[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    private void PrintSearchResults(List<TaskItem> results)
    {
        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Ничего не найдено.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Найдено: {results.Count}[/]");
        PrintTaskTable(results);
    }
}
