using System.Text.Encodings.Web;
using System.Text.Json;
using Core;
using Core.Dto;
using Core.Import;
using System;
using Core.Domain;

Console.OutputEncoding = System.Text.Encoding.UTF8;
EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
    return 0;
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

string? customPath = args.FirstOrDefault(a => !a.StartsWith("--"));
string path = !string.IsNullOrEmpty(customPath) ? customPath : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();
ImportResult<BookDto> result = extension switch
{
    ".csv" => BookCsvImporter.Load(path),
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

TryDo("Порожній ISBN примірника", () => 
    BookCopy.Create("BC-002", "   ", "1984"));

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

TryDo("Дата видачі у майбутньому", () => 
    Loan.Open("L-102", "BC-001", "READER-01", DateTime.Now.AddDays(10)));

TryDo("Дата повернення раніше дати видачі", () =>
{
    Loan testLoan = Loan.Open("L-103", "BC-001", "READER-01", DateTime.Now.AddDays(-2));
    testLoan.Close(DateTime.Now.AddDays(-5));
});

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

return 0;