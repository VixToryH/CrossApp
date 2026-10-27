namespace Core.Services;

using Core.Abstractions;
using Core.Events;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(LibraryChangedEventArgs e)
    {
        string dueDateStr = e.DueDate.HasValue ? e.DueDate.Value.ToString("yyyy-MM-dd") : "N/A";
        Console.WriteLine($"[ConsoleSink]: {e.At:HH:mm:ss} {e.Kind,-8} Книга ID: {e.BookId} | Читач: {e.ReaderId} | До дати: {dueDateStr}");
    }
}