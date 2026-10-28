namespace Cli;

using Core.Abstractions;
using Core.Events;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(LibraryChangedEventArgs e)
    {
        string dueDateStr = e.DueDate?.ToString("yyyy-MM-dd") ?? "N/A";
        Console.WriteLine($"[ConsoleSink] {e.At:HH:mm:ss} {e.Kind,-8} {e.BookId} | Читач: {e.ReaderId} | До дати: {dueDateStr}");
    }
}