using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;
using System.Text.Json;
using Core.Configuration;

namespace Core.Tests;

public sealed class InMemoryLibraryStore : ILibraryStore
{
    private readonly List<BookCopy> _books = [];

    public void Add(BookCopy book) => _books.Add(book);

    public BookCopy? GetById(string id) => _books.FirstOrDefault(b => b.Id == id);

    public IReadOnlyList<BookCopy> List() => _books;

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) => _books.Where(predicate).ToList();

    public void Update(BookCopy book)
    {
        var index = _books.FindIndex(b => b.Id == book.Id);
        if (index >= 0)
        {
            _books[index] = book;
        }
    }

    public bool Remove(string id)
    {
        var book = GetById(id);
        return book != null && _books.Remove(book);
    }
}

public class CatalogServiceTests
{
    [Fact]

    public void ChangeBookStatus_WhenAlreadyIssued_ThrowsDomainException()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);
        var book = service.AddBook("BC-100", "978-1111111111", "Тестова книга");
        
        service.ChangeBookStatus(book.Id, BookCopyStatus.Issued);

        Assert.Throws<DomainException>(() => service.ChangeBookStatus(book.Id, BookCopyStatus.Issued));
    }

    [Fact]
    public void ChangeBookStatus_ReturnIssuedBook_UpdatesStatus()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);
        var book = service.AddBook("BC-101", "978-2222222222", "Тестова книга 2");
        
        service.ChangeBookStatus(book.Id, BookCopyStatus.Issued);
        service.ChangeBookStatus(book.Id, BookCopyStatus.Available);

        Assert.Equal(BookCopyStatus.Available, store.GetById(book.Id)!.Status);
    }

    [Fact]
    public void AddBook_RaisesChangedExactlyOnce()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var events = new List<LibraryChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.AddBook("BC-102", "978-3333333333", "Книга для події");

        Assert.Single(events);
        Assert.Equal(LibraryChangeKind.Added, events[0].Kind);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected)
    {
        var result = ReportService.LowStock(TestData.Items(), threshold);
        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void TopByQuantity_IsDeterministic()
    {
        var top = ReportService.TopByQuantity(TestData.Items(), 2).Select(x => x.Name).ToArray();
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" }, top);
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        var service = new LendingService(new InMemoryLibraryStore());
        
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact]
    public async Task ImportAsync_WithInvalidJson_ThrowsDomainException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "[{ \"Id\": \"BC-1\", \"Status\": \"InvalidStatus\" }]");
            var service = new LendingService(new InMemoryLibraryStore());

            await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
public async Task ImportStreamAsync_ValidJson_ImportsSuccessfully()
{
    var tempFile = Path.GetTempFileName();
    try
    {
        var json = "[{\"Id\":\"BC-STREAM-1\",\"Isbn\":\"978-0000000000\",\"Title\":\"Stream Book\",\"Status\":\"Available\"}]";
        await File.WriteAllTextAsync(tempFile, json);

        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);

        var imported = await service.ImportStreamAsync(new JsonCatalogImporter(), tempFile);

        Assert.Equal(1, imported);
        Assert.NotNull(store.GetById("BC-STREAM-1"));
    }
    finally
    {
        if (File.Exists(tempFile)) File.Delete(tempFile);
    }
}

[Fact]

public async Task ImportWithTimeoutAsync_WhenTimeoutExpires_ThrowsTimeoutException()
{
    var tempFile = Path.GetTempFileName();
    try
    {
        var items = Enumerable.Range(1, 100)
            .Select(i => new { Id = $"BC-TIMEOUT-{i}", Isbn = "978-0000000000", Title = $"Book {i}", Status = "Available" });
        await File.WriteAllTextAsync(tempFile, JsonSerializer.Serialize(items));

        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);

        using var userCts = new CancellationTokenSource();
        
        var timeout = TimeSpan.Zero;

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportWithTimeoutAsync(new JsonCatalogImporter(), tempFile, timeout, userCts.Token));

        Assert.Contains("таймаутом", exception.Message);
    }
    finally
    {
        if (File.Exists(tempFile)) File.Delete(tempFile);
    }
}
private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}

[Fact]
public async Task ImportAsync_ReportsProgressCorrectly()
{
    var tempFile = Path.GetTempFileName();
    try
    {
        var items = Enumerable.Range(1, 4)
            .Select(i => new { Id = $"BC-PROG-{i}", Isbn = "978-0000000000", Title = $"Book {i}", Status = "Available" });
        await File.WriteAllTextAsync(tempFile, JsonSerializer.Serialize(items));

        var service = new LendingService(new InMemoryLibraryStore());
        var reportedValues = new List<int>();
        
        var progress = new SynchronousProgress<int>(reportedValues.Add);

        await service.ImportAsync(new JsonCatalogImporter(), tempFile, progress: progress);

        Assert.NotEmpty(reportedValues);
        Assert.Equal(100, reportedValues.Last());
    }
    finally
    {
        if (File.Exists(tempFile)) File.Delete(tempFile);
    }
}


[Fact]
public void StorageOptions_ResolveFullPath_RelativePath_CombinesWithBaseDir()
{
    var tempBase = Path.Combine(Path.GetTempPath(), "CrossAppTests_" + Guid.NewGuid().ToString("N"));
    try
    {
        var storage = new StorageOptions { DataPath = "subdata", FileName = "catalog.json" };
        var fullPath = storage.ResolveFullPath(tempBase);

        var expected = Path.GetFullPath(Path.Combine(tempBase, "subdata", "catalog.json"));
        Assert.Equal(expected, fullPath);
        Assert.True(Directory.Exists(Path.Combine(tempBase, "subdata")));
    }
    finally
    {
        if (Directory.Exists(tempBase))
            Directory.Delete(tempBase, recursive: true);
    }
}

[Fact]
public void StorageOptions_ResolveFullPath_RootedPath_IgnoresBaseDir()
{
    var rootedDir = Path.Combine(Path.GetTempPath(), "CrossAppRooted_" + Guid.NewGuid().ToString("N"));
    try
    {
        var storage = new StorageOptions { DataPath = rootedDir, FileName = "items.json" };
        var fullPath = storage.ResolveFullPath("C:\\SomeOtherBaseDir");

        var expected = Path.GetFullPath(Path.Combine(rootedDir, "items.json"));
        Assert.Equal(expected, fullPath);
    }
    finally
    {
        if (Directory.Exists(rootedDir))
            Directory.Delete(rootedDir, recursive: true);
    }
}

[Fact]
public void FileLibraryStore_SavesAtomicallyWithReadableCyrillic_AndReloads()
{
    var tempDir = Path.Combine(Path.GetTempPath(), "CrossAppStore_" + Guid.NewGuid().ToString("N"));
    var filePath = Path.Combine(tempDir, "books.json");

    try
    {
        var store = new FileLibraryStore(filePath);
        store.Add(BookCopy.Create("B-01", "978-966-01-0516-4", "Кобзар"));

        Assert.False(File.Exists(filePath + ".tmp"));
        Assert.True(File.Exists(filePath));

        var rawJson = File.ReadAllText(filePath);
        Assert.Contains("Кобзар", rawJson);
        Assert.DoesNotContain("\\u04", rawJson);

        var reloadedStore = new FileLibraryStore(filePath);
        var loaded = reloadedStore.GetById("B-01");
        Assert.NotNull(loaded);
        Assert.Equal("Кобзар", loaded.Title);
    }
    finally
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, recursive: true);
    }
}

[Fact]
public void FileLibraryStore_CorruptedJson_ThrowsDomainException()
{
    var tempDir = Path.Combine(Path.GetTempPath(), "CrossAppBadJson_" + Guid.NewGuid().ToString("N"));
    var filePath = Path.Combine(tempDir, "broken.json");

    try
    {
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(filePath, "{ invalid json [");

        var store = new FileLibraryStore(filePath);
        Assert.Throws<DomainException>(() => store.List());
    }
    finally
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, recursive: true);
    }
}

    
}


