namespace Core.Abstractions;

using Core.Events;

public interface INotificationSink
{
    void Notify(LibraryChangedEventArgs e);
}