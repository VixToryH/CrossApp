namespace Cli;

using Core.Abstractions;
using Core.Events;

public sealed class FileSink : INotificationSink
{
    private readonly string _logPath;

    public FileSink(string logPath)
    {
        _logPath = logPath;
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
    }

    public void Notify(LibraryChangedEventArgs e)
    {
        string dueDateStr = e.DueDate?.ToString("yyyy-MM-dd") ?? "N/A";
        string line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.BookId} | Читач: {e.ReaderId} | До дати: {dueDateStr}";
        File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}