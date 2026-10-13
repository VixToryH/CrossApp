namespace Core.Storage;

using Core.Abstractions;
using Core.Domain;



public sealed class InMemoryLibraryStore(IEnumerable<BookCopy>? seed = null) : ILibraryStore
{
    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) 
    => _items.Values.Where(predicate).ToList();
    private readonly Dictionary<string, BookCopy> _items = 
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BookCopy> List() => _items.Values.ToList();

    public BookCopy? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} вже існує.");

        _items.Add(item.Id, item);
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}