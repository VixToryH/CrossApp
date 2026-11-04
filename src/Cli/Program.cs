using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cli;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

ILibraryStore store = StoreFactory.Create(args);

var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDir);
var logPath = Path.Combine(logDir, "app.log");     
var sinkPath = Path.Combine(logDir, "sink.log");   

var sinks = new List<INotificationSink>
{
    new ConsoleSink(),
    new FileSink(sinkPath)
};
var lendingService = new LendingService(store, sinks);

void OnCatalogChanged(object? sender, LibraryChangedEventArgs e)
{
    string dueDateStr = e.DueDate?.ToString("yyyy-MM-dd") ?? "N/A";
    string text = e.Kind switch
    {
        LibraryChangeKind.Issued   => $"видано читачу {e.ReaderId} до {dueDateStr}",
        LibraryChangeKind.Returned => $"повернено, читач {e.ReaderId}",
        _                          => "додано в каталог"
    };
    string line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.BookId} {text}";

    Console.WriteLine($"[ПОДІЯ] {line}");
    File.AppendAllText(logPath, line + Environment.NewLine);
}

lendingService.Changed += OnCatalogChanged; 
int changes = 0;
lendingService.Changed += (_, _) => changes++;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
    return;
}

Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine("Студент: Гвоздевич Вікторія, група ФЕІ-36");
Console.WriteLine();
Console.WriteLine(new string('-', 52));

Console.WriteLine($"ОС (OSDescription) : {report.OsDescription}");
Console.WriteLine($"ОС (Environment) : {report.OsVersion}");
Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR) : {report.DotNetVersion}");
Console.WriteLine($"Runtime             : {report.FrameworkDescription}");
Console.WriteLine($"RID (визначено)     : {report.DetectedRid}");
Console.WriteLine($"RID (від .NET)      : {report.ReportedRid}");
Console.WriteLine($"Каталог застосунку : {report.BaseDirectory}");
Console.WriteLine($"Поточний каталог : {report.CurrentDirectory}");

Console.WriteLine(new string('-', 52));
Console.WriteLine();
Console.WriteLine($"Примітка збірки    : {report.BuildNote}");
Console.WriteLine($"Предметна область: {report.SubjectArea}");

string[] reportCommands = ["report-top", "report-low", "report-groups", "report-sum", "report-loans", "report-benchmark"];
string? customPath = args.FirstOrDefault(a => !a.StartsWith("--") && !reportCommands.Contains(a));
string path = !string.IsNullOrEmpty(customPath) ? customPath : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return;
}

string extension = Path.GetExtension(path).ToLowerInvariant();
ImportResult<BookDto> result = extension switch
{
    ".csv" => BookCsvImporter.Load(path, err => Console.WriteLine($"[Action Log]: Пропущено рядок -> {err}")),
    ".json" => BookJsonImporter.Load(path),
    _ => new ImportResult<BookDto>([], [$"Непідтримуване розширення файлу '{extension}'"])
}; 

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
Console.WriteLine();
Console.WriteLine(new string('-', 60));

foreach (BookDto b in result.Items.Take(5))
{
    string authorInfo = string.IsNullOrEmpty(b.Author) ? "Автор невідомий" : b.Author;
    Console.WriteLine($" {b.Id,-6} {b.Isbn,-16} {b.Title,-25} {b.Year,5}  ({authorInfo})");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine(new string('-', 60));
    Console.WriteLine();
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
}

Console.WriteLine(new string('-', 60));

DomainImportResult<BookCopy> domainResult = BookCopy.FromImportResult(result);

Console.WriteLine();
Console.WriteLine($"Успішно створено доменних об'єктів: {domainResult.Entities.Count}");
Console.WriteLine($"Всього помилок (файлові + доменні): {domainResult.DomainErrors.Count}");

if (domainResult.Entities.Count > 0)
{
    Console.WriteLine("\nПерші доменні сутності:");
    foreach (var entity in domainResult.Entities.Take(3))
    {
        Console.WriteLine($"  {entity}");
    }
}

if (domainResult.DomainErrors.Count > 0)
{
    Console.WriteLine("\nУсі відхилені рядки (файловий синтаксис + бізнес-інваріанти):");
    foreach (var error in domainResult.DomainErrors.Take(5))
    {
        Console.WriteLine($"  {error}");
    }
}
Console.WriteLine(new string('-', 60));

Console.WriteLine();
Console.WriteLine("=== Сценарій 1: успіх ===");

BookCopy copy = BookCopy.Create("BC-001", "978-0141439518", "Pride and Prejudice");
Console.WriteLine($"Створено примірник: {copy}"); 
Loan loan = Loan.Open("L-101", copy.Id, "READER-42", DateTime.Now.AddDays(-5));
Console.WriteLine($"Відкрито видачу [{loan.Id}] для читача {loan.ReaderId} (Дата: {loan.IssuedOn:yyyy-MM-dd})");

copy.Issue();
Console.WriteLine($"Після видачі: {copy}");

copy.Return();
loan.Close(DateTime.Now);
Console.WriteLine($"Після повернення: {copy}");
Console.WriteLine($"Видачу закрито: IsOpen = {loan.IsOpen}, Повернуто: {loan.ReturnedOn:yyyy-MM-dd}");

BookDto dto = copy.ToDto();
BookCopy restoredCopy = BookCopy.FromDto(dto);
Console.WriteLine($"Успішно відновлено з DTO: {restoredCopy}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

TryDo("Порожній ISBN примірника", () => BookCopy.Create("BC-002", "   ", "1984"));

TryDo("Повторна видача вже виданого примірника", () =>
{
    BookCopy testCopy = BookCopy.Create("BC-003", "978-0451524935", "1984");
    testCopy.Issue();
    testCopy.Issue();
});

TryDo("Повернення книги, яка знаходиться в бібліотеці", () =>
{
    BookCopy testCopy = BookCopy.Create("BC-004", "978-0061120084", "To Kill a Mockingbird");
    testCopy.Return();
});

TryDo("Дата видачі у майбутньому", () => Loan.Open("L-102", "BC-001", "READER-01", DateTime.Now.AddDays(10)));

TryDo("Дата повернення раніше дати видачі", () =>
{
    Loan testLoan = Loan.Open("L-103", "BC-001", "READER-01", DateTime.Now.AddDays(-2));
    testLoan.Close(DateTime.Now.AddDays(-5));
});

Console.WriteLine();
Console.WriteLine(new string('=', 60));  

if (store is CachingLibraryStore cachingStore)
{
    Console.WriteLine($"Базове сховище: {cachingStore.InnerStoreType.Name}");
    Console.WriteLine($"Активне сховище (Декоратор): {store.GetType().Name}");
}
else
{
    Console.WriteLine($"Активне сховище: {store.GetType().Name}");
}

Console.WriteLine(new string('=', 60));

Console.WriteLine("\n=== Сервіс: події ===");

var newBook = lendingService.AddBook("B-101", "978-0321356680", "Effective C#");
lendingService.ChangeBookStatus(newBook.Id, BookCopyStatus.Issued, "READER-42");
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine("\n=== Сервіс: відмови (підписка ще активна) ===");
int before = changes;

TryDo("Дублікат id B-101", () => lendingService.AddBook("B-101", "978-0000000000", "Duplicate Book"));
TryDo("Повторна видача вже виданої книги", () => lendingService.ChangeBookStatus(newBook.Id, BookCopyStatus.Issued, "READER-01"));
TryDo("Неіснуючий id", () => lendingService.ChangeBookStatus("NOPE", BookCopyStatus.Issued, "READER-01"));

Console.WriteLine(changes == before
    ? $"Винятки були, подій немає: changes = {changes}"
    : "УВАГА: подія піднялась попри помилку!");

Console.WriteLine("\n=== Сервіс: відписка (-=) ===");
lendingService.Changed -= OnCatalogChanged;
lendingService.ChangeBookStatus(newBook.Id, BookCopyStatus.Available, "READER-42");
Console.WriteLine($"Подій отримано: {changes}");   

Console.WriteLine("\nУсі книги в сховищі:");
foreach (var b in lendingService.All())
{
    Console.WriteLine($"  {b.Id,-6} | {b.Isbn,-16} | {b.Status,-10} | {b.Title}");
}

Console.WriteLine(new string('=', 60));

Console.WriteLine("\n--- Пошук за допомогою Func<BookCopy, bool> ---"); 
var availableBooks = lendingService.Search(b => b.Status == BookCopyStatus.Available);
Console.WriteLine($"Знайдено доступних книг: {availableBooks.Count}");

var csharpBooks = lendingService.Search(b => b.Title.Contains("C#", StringComparison.OrdinalIgnoreCase));
Console.WriteLine($"Знайдено книг з 'C#' у назві: {csharpBooks.Count}");
foreach (var book in csharpBooks)
{
    Console.WriteLine($"   • {book.Id}: {book.Title}");
}

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("=== ДЕМО ВІДКЛАДЕНОГО ВИКОНАННЯ (Deferred Execution) ===");

var source = lendingService.All().ToList();
var query = source.Where(b => b.IsIssued); 

Console.WriteLine($"Виданих книг до змін: {query.Count()}");

source.Add(BookCopy.Create("tmp-999", "978-0000000000", "Тестова книга", isIssued: true));

Console.WriteLine($"Виданих книг після змін: {query.Count()}"); 

var snapshot = query.ToList(); 
source.Clear();

Console.WriteLine($"У знімку лишилось (після Clear): {snapshot.Count}"); 
Console.WriteLine(new string('-', 60));

var items = lendingService.All();
var command = args.FirstOrDefault(a => !a.StartsWith("--") && reportCommands.Contains(a)) ?? "report-top";

Console.WriteLine($"\n=== ВИКОНАННЯ КОМАНДИ ЗВІТУ: {command} ===");

switch (command)
{
    case "report-top":
        Console.WriteLine("=== ТОП-5 НАЗВ ЗА КІЛЬКІСТЮ ПРИМІРНИКІВ ===");
        Print(ReportService.TopByQuantity(items, 5));
        break;

    case "report-low":
        Console.WriteLine("=== КНИГИ З МАЛИМ ЗАЛИШКОМ / ДЕФІЦИТНІ ===");
        Print(ReportService.LowStock(items, 10));
        break;

    case "report-groups":
        Console.WriteLine("=== РОЗПОДІЛ ПРИМІРНИКІВ ЗА СТАТУСАМИ ===");
        foreach (var g in ReportService.ByCategory(items))
        {
            Console.WriteLine($"Статус: {g.Key,-12} Позицій: {g.Count,3} | Всього: {g.TotalQuantity,3}");
        }
        break;

    case "report-sum":
        Console.WriteLine("=== ПІДСУМКОВА СТАТИСТИКА ФОНДУ ===");
        var t = ReportService.Totals(items);
        Console.WriteLine($"Унікальних назв: {t.Items}, Всього примірників: {t.Total}, Середнє на назву: {t.Average:F2}, Макс примірників: {t.Max}");
        break;

    case "report-loans":
        Console.WriteLine("=== ЗВЕДЕНИЙ ЗВІТ ВИДАНЬ ТА ВИДАЧ (GroupJoin) ===");
        var sampleLoans = new List<Loan>
        {
            Loan.Open("L-101", "BC-001", "READER-42", DateTime.Now.AddDays(-10)),
            Loan.Open("L-102", "B-101", "READER-01", DateTime.Now.AddDays(-2))
        };
        var summaries = ReportService.GetBookLoanSummariesGroupJoin(items, sampleLoans);
        foreach (var s in summaries)
        {
            string status = s.IsCurrentlyIssued ? "Зараз видано" : "В бібліотеці";
            Console.WriteLine($"[{s.BookId,-6}] {s.Title,-30} | Всього видач: {s.TotalLoansCount,2} | Статус: {status}");
        }
        break;

    case "report-benchmark":
        Console.WriteLine("=== БЕНЧМАРК LINQ (100 000 ЗАПИСІВ) ===");
        Console.WriteLine("Генерація 100 000 примірників та вимірювання часу виконання...");

        var (timeNoList, timeWithList) = ReportService.BenchmarkListImpact(100_000);

        Console.WriteLine($"1. Чистий LINQ (потоковий, ToList лише наприкінці): {timeNoList} ms");
        Console.WriteLine($"2. З раннім ToList() у середині ланцюжка          : {timeWithList} ms");

        if (timeWithList > timeNoList)
        {
            double ratio = (double)timeWithList / Math.Max(timeNoList, 1);
            Console.WriteLine($"Різниця: проміжний ToList() повільніший у {ratio:F2} разів.");
        }
        else if (timeWithList < timeNoList)
        {
            double ratio = (double)timeNoList / Math.Max(timeWithList, 1);
            Console.WriteLine($"Різниця: проміжний ToList() виявився швидшим у {ratio:F2} разів.");
        }
        else
        {
            Console.WriteLine("Різниця: обидва підходи показали однаковий час.");
        }
        break;

    default:
        Console.WriteLine("Доступні підкоманди: report-top | report-low | report-groups | report-sum | report-loans | report-benchmark");
        break;
}

static void Print(IReadOnlyList<StockLine> lines)
{
    foreach (var l in lines)
    {
        Console.WriteLine($"{l.Sku,-15} {l.Name,-30} {l.Quantity,4} {l.Unit}");
    }
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}