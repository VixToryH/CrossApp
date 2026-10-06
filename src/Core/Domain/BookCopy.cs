using System;
using Core.Dto;
using System.Collections.Generic;
using Core.Import;

namespace Core.Domain;

public enum BookCopyStatus
{
    Available, 
    Issued,    
    Archived   
}

public record DomainImportResult<T>(IReadOnlyList<T> Entities, IReadOnlyList<string> DomainErrors);

public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }

    public BookCopyStatus Status { get; private set; }
    public bool IsIssued => Status == BookCopyStatus.Issued;

    private BookCopy(string id, string isbn, string title, BookCopyStatus status)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        Status = status;
    }

    public static BookCopy Create(string id, string isbn, string title, bool isIssued = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор примірника не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва книги не може бути порожньою", nameof(title));

        var initialStatus = isIssued ? BookCopyStatus.Issued : BookCopyStatus.Available;
        return new BookCopy(id.Trim(), isbn.Trim().ToUpperInvariant(), title.Trim(), initialStatus);
    }

    public void ChangeStatus(BookCopyStatus newStatus)
    {
        Status = (Status, newStatus) switch
        {
            (BookCopyStatus.Available, BookCopyStatus.Issued) => BookCopyStatus.Issued,
            (BookCopyStatus.Issued, BookCopyStatus.Available) => BookCopyStatus.Available,
            (BookCopyStatus.Available, BookCopyStatus.Archived) => BookCopyStatus.Archived,

            _ => throw new InvalidOperationException(
                $"Неприпустимий перехід стану для примірника '{Id}': з {Status} у {newStatus}")
        };
    }

    public void Issue() => ChangeStatus(BookCopyStatus.Issued);

    public void Return() => ChangeStatus(BookCopyStatus.Available);

    public void Archive() => ChangeStatus(BookCopyStatus.Archived);

    public BookDto ToDto() => new(Id, Isbn, Title, DateTime.Now.Year);

    public static BookCopy FromDto(BookDto dto) =>
        Create(dto.Id, dto.Isbn, dto.Title);

    public override string ToString() =>
        $"[{Id}] {Title} (ISBN: {Isbn}) — Статус: {Status}";

    public static DomainImportResult<BookCopy> FromImportResult(ImportResult<BookDto> importResult)
    {
        var entities = new List<BookCopy>();
        var domainErrors = new List<string>(importResult.Errors); 

        foreach (var dto in importResult.Items)
        {
            try
            {
                var entity = FromDto(dto);
                entities.Add(entity);
            }
            catch (Exception ex)
            {
                domainErrors.Add($"[Інваріант] Книга '{dto.Id}': {ex.Message}");
            }
        }

        return new DomainImportResult<BookCopy>(entities.AsReadOnly(), domainErrors.AsReadOnly());
    }
}