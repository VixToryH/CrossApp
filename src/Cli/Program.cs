using System.Text.Encodings.Web;
using System.Text.Json;
using Core;
using Core.Dto;
using Core.Import;

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
    Console.WriteLine($"Примітка збірки    : {report.BuildNote}");
    Console.WriteLine($"Предметна область: {report.SubjectArea}");

string? customPath = args.FirstOrDefault(a => !a.StartsWith("--"));
string path = !string.IsNullOrEmpty(customPath) ? customPath : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<BookDto> result = BookCsvImporter.Load(path);

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
Console.WriteLine(new string('-', 60));

foreach (BookDto b in result.Items.Take(5))
{
    string authorInfo = string.IsNullOrEmpty(b.Author) ? "Автор невідомий" : b.Author;
    Console.WriteLine($" {b.Id,-6} {b.Isbn,-16} {b.Title,-25} {b.Year,5}  ({authorInfo})");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine(new string('-', 60));
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
}

return 0;