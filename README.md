# TaskHub 📋

консольный менеджер задач, курсовой проект по C#

## что умеет

- создавать задачи (название, описание, приоритет, дедлайн, статус)
- смотреть список задач
- редактировать задачи
- удалять задачи
- искать по названию, статусу, приоритету
- статистика (сколько всего, выполнено, просрочено, по приоритетам)
- сохранение и загрузка из файла (json)
- фоновая проверка дедлайнов — если задача просрочена, выводит уведомление

## что использовано из требований

- ООП — классы TaskItem, TaskManager, FileService, DeadlineChecker, ConsoleUI
- Коллекции — List<TaskItem>, Dictionary<TaskPriority, int>
- Generic — метод SearchByField<T> (поиск по любому полю через рефлексию)
- Делегаты — TaskEventHandler, TaskPredicate + Action<TaskItem>
- Исключения — try/catch, TaskNotFoundException
- IDisposable — FileService, DeadlineChecker
- static — TaskStatistics
- async/await — SaveAsync(), LoadAsync()
- Многопоточность — DeadlineChecker (Task.Run + CancellationToken)

## как запустить

```
dotnet run
```

## команды

```
help                          — список команд
help add                      — подробно про команду add
add "Название" "Описание" Приоритет Дедлайн
list                          — все задачи
edit ID поле=значение         — редактировать (title, description, priority, state, deadline)
delete ID                     — удалить
search title=молок            — поиск по названию
search state=Done             — поиск по статусу
search priority=High          — поиск по приоритету
stats                         — статистика
save                          — сохранить в файл
load                          — загрузить из файла
quit                          — выход
```

ID можно указывать коротко — первые 4+ символов из таблицы

## пример

```
> add "Купить молоко" "Сходить в магазин" High 01.06.2026
> add "Сделать ДЗ" "Математика" Medium 15.07.2026
> list
> edit 613c state=Done
> search title=молок
> stats
> quit
```
