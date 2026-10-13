namespace Core.Abstractions;

using Core.Domain;

public interface ILibraryStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
}