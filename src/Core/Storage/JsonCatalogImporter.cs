using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private sealed record BookCopyDto(string Id, string Isbn, string Title, string Status);

    public async Task<IReadOnlyList<BookCopy>> LoadAsync(
        string path, 
        CancellationToken ct = default, 
        IProgress<int>? progress = null)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            path, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize: 4096, 
            useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<BookCopyDto>>(stream, cancellationToken: ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить даних.");

        var result = new List<BookCopy>(dtos.Count);

        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var status = Enum.Parse<BookCopyStatus>(dtos[i].Status, ignoreCase: true);
                var isIssued = status == BookCopyStatus.Issued;
                result.Add(BookCopy.Create(dtos[i].Id, dtos[i].Isbn, dtos[i].Title, isIssued));
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }

            var percent = (int)((i + 1.0) / dtos.Count * 100);
            progress?.Report(percent);
            progress?.Report(percent);
        }

        return result;
    }

    public async IAsyncEnumerable<BookCopy> LoadStreamAsync(
        string path, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            path, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize: 4096, 
            useAsync: true);

        var index = 0;
        var dtos = JsonSerializer.DeserializeAsyncEnumerable<BookCopyDto>(stream, cancellationToken: ct);

        await foreach (var dto in dtos.WithCancellation(ct))
        {
            index++;
            if (dto is null) continue;

            BookCopy book;
            try
            {
                var status = Enum.Parse<BookCopyStatus>(dto.Status, ignoreCase: true);
                var isIssued = status == BookCopyStatus.Issued;
                book = BookCopy.Create(dto.Id, dto.Isbn, dto.Title, isIssued);
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            {
                throw new DomainException($"Запис {index}: {ex.Message}", ex);
            }

            yield return book;
        }
    }
}