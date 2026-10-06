using System;
using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    private bool _isIssued;

    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public bool IsIssued => _isIssued;

    private BookCopy(string id, string isbn, string title, bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        _isIssued = isIssued;
    }

    public static BookCopy Create(string id, string isbn, string title, bool isIssued = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор примірника не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва книги не може бути порожньою", nameof(title));

        return new BookCopy(id.Trim(), isbn.Trim().ToUpperInvariant(), title.Trim(), isIssued);
    }

    public void Issue()
    {
        if (_isIssued)
            throw new InvalidOperationException($"Примірник {Id} ('{Title}') вже виданий, повторна видача неможлива");

        _isIssued = true;
    }

    public void Return()
    {
        if (!_isIssued)
            throw new InvalidOperationException($"Примірник {Id} ('{Title}') перебуває в бібліотеці та не був виданий");

        _isIssued = false;
    }

    public BookDto ToDto() => new(Id, Isbn, Title, DateTime.Now.Year);

    public static BookCopy FromDto(BookDto dto) =>
        Create(dto.Id, dto.Isbn, dto.Title);

    public override string ToString() =>
        $"[{Id}] {Title} (ISBN: {Isbn}) — Статус: {(IsIssued ? "Виданий" : "В наявності")}";
}