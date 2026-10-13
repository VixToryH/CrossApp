namespace Core.Storage;

using Core.Abstractions;
using Core.Domain;

public sealed class CachingLibraryStore(ILibraryStore innerStore) : ILibraryStore
{
    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) 
    => EnsureCache().Values.Where(predicate).ToList();
    private readonly ILibraryStore _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
    private Dictionary<string, BookCopy>? _cache;

    public Type InnerStoreType => _innerStore.GetType();

    private Dictionary<string, BookCopy> EnsureCache()
    {
        if (_cache is null)
        {
            _cache = _innerStore.List().ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);
        }
        return _cache;
    }

    public IReadOnlyList<BookCopy> List()
    {
        return EnsureCache().Values.ToList();
    }

    public BookCopy? GetById(string id)
    {
        return EnsureCache().GetValueOrDefault(id);
    }

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        _innerStore.Add(item);
        
        EnsureCache()[item.Id] = item;
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        _innerStore.Update(item);
        EnsureCache()[item.Id] = item;
    }

    public bool Remove(string id)
    {
        var removed = _innerStore.Remove(id);
        if (removed)
        {
            EnsureCache().Remove(id);
        }
        return removed;
    }
}