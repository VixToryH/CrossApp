using System.Runtime.InteropServices;
using System.Text.Json;

var info = new
{
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    SubjectArea = "Бібліотека — облік видач примірників книг читачам."
};

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Гвоздевич Вікторія, група ФЕІ-36");
    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"ОС (OSDescription) : {info.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {info.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {info.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {info.DotNetVersion}");
    Console.WriteLine($"Runtime : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку : {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог : {info.CurrentDirectory}");

    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {info.SubjectArea}");
}