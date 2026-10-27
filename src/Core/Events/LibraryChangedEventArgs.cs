namespace Core.Events;

public sealed class LibraryChangedEventArgs(
    string bookId,
    LibraryChangeKind kind,
    string readerId,
    DateTimeOffset? dueDate) : EventArgs
{
    public string BookId { get; } = bookId;
    public LibraryChangeKind Kind { get; } = kind;
    public string ReaderId { get; } = readerId;
    public DateTimeOffset? DueDate { get; } = dueDate;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}