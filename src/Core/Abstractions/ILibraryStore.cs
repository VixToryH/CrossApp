namespace Core.Abstractions;

using Core.Domain;

public interface ILibraryStore
{
    IReadOnlyList<BookCopy> List();
    IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate);
    BookCopy? GetById(string id);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
}