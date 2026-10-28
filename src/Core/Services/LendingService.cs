namespace Core.Services;

using Core.Abstractions;
using Core.Domain;
using Core.Events;

public sealed class LendingService(ILibraryStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<INotificationSink> _sinks = sinks?.ToList() ?? [];

    public event EventHandler<LibraryChangedEventArgs>? Changed;

    private void OnChanged(string bookId, LibraryChangeKind kind, string readerId = "", DateTimeOffset? dueDate = null)
    {
        var args = new LibraryChangedEventArgs(bookId, kind, readerId, dueDate);

        Changed?.Invoke(this, args);

        foreach (var sink in _sinks)
        {
            try
            {
                sink.Notify(args);
            }
            catch
            {

            }
        }
    }

    public BookCopy AddBook(string id, string isbn, string title)
    {
        var book = BookCopy.Create(id, isbn, title);
        _store.Add(book);

        OnChanged(book.Id, LibraryChangeKind.Added); 

        return book;
    }

    public void ChangeBookStatus(string id, BookCopyStatus newStatus, string readerId = "")
    {
        var book = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає книги з id={id}.");

        book.ChangeStatus(newStatus); 
        _store.Update(book);

        var kind = newStatus == BookCopyStatus.Issued
            ? LibraryChangeKind.Issued
            : LibraryChangeKind.Returned;

        var dueDate = newStatus == BookCopyStatus.Issued
            ? DateTimeOffset.Now.AddDays(14)
            : (DateTimeOffset?)null;

        OnChanged(book.Id, kind, readerId, dueDate);
    }

    public IReadOnlyList<BookCopy> Search(Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.Find(predicate);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);
}