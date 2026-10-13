namespace Core.Services;

using Core.Abstractions;
using Core.Domain;

public sealed class LendingService(ILibraryStore store)
{
    public IReadOnlyList<BookCopy> Search(Func<BookCopy, bool> predicate)
{
    ArgumentNullException.ThrowIfNull(predicate);
    return _store.Find(predicate);
}
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public BookCopy AddBook(string id, string isbn, string title)
    {
        var book = BookCopy.Create(id, isbn, title);
        _store.Add(book);
        return book;
    }

    public void ChangeBookStatus(string id, BookCopyStatus newStatus)
    {
        var book = _store.GetById(id) 
            ?? throw new InvalidOperationException($"Немає книги з id={id}.");

        book.ChangeStatus(newStatus);
        _store.Update(book);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);
}