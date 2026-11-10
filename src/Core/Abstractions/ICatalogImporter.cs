using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogImporter
{
    Task<IReadOnlyList<BookCopy>> LoadAsync(
        string path, 
        CancellationToken ct = default, 
        IProgress<int>? progress = null);

    IAsyncEnumerable<BookCopy> LoadStreamAsync(
        string path, 
        CancellationToken ct = default);
}