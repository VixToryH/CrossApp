using System.Text.Encodings.Web;
using System.Text.Json;
using Core;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
}
else
{
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
}