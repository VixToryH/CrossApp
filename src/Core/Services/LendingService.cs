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
            ?? throw new ArgumentException($"Немає книги з id={id}.");

        if (newStatus == BookCopyStatus.Issued && book.IsIssued)
        {
            throw new DomainException($"Примірник '{id}' уже видано.");
        }

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

    public async Task<int> ImportAsync(
        ICatalogImporter importer, 
        string path, 
        CancellationToken ct = default, 
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var items = await importer.LoadAsync(path, ct, progress);
        var added = 0;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            if (_store.GetById(item.Id) is not null) continue;

            _store.Add(item);
            OnChanged(item.Id, LibraryChangeKind.Added);
            added++;
        }

        return added;
    }

    // 1. Додаткове завдання: Потоковий імпорт через IAsyncEnumerable
    public async Task<int> ImportStreamAsync(ICatalogImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var added = 0;

        await foreach (var item in importer.LoadStreamAsync(path, ct))
        {
            if (_store.GetById(item.Id) is not null) continue;

            _store.Add(item);
            OnChanged(item.Id, LibraryChangeKind.Added);
            added++;
        }

        return added;
    }

    // 2. Додаткове завдання: Зв'язані токени та таймаут
    public async Task<int> ImportWithTimeoutAsync(
        ICatalogImporter importer, 
        string path, 
        TimeSpan timeout, 
        CancellationToken userToken = default,
        IProgress<int>? progress = null)
    {
        using var ctsTimeout = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userToken, ctsTimeout.Token);

        try
        {
            return await ImportAsync(importer, path, linkedCts.Token, progress);
        }
        catch (OperationCanceledException) when (ctsTimeout.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Операцію імпорту скасовано за таймаутом ({timeout.TotalMilliseconds} ms).");
        }
    }

    public IReadOnlyList<BookCopy> Search(Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.Find(predicate);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);
}