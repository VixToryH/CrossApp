using System;

namespace Core.Domain;
public sealed class Loan
{
    private DateTime? _returnedOn;

    public string Id { get; }
    public string BookCopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }
    public DateTime? ReturnedOn => _returnedOn;
    public bool IsOpen => _returnedOn == null;
    private Loan(string id, string bookCopyId, string readerId, DateTime issuedOn, DateTime? returnedOn)
    {
        Id = id;
        BookCopyId = bookCopyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        _returnedOn = returnedOn;
    }

    public static Loan Open(string id, string bookCopyId, string readerId, DateTime issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор видачі не може бути порожнім", nameof(id));

        if (string.IsNullOrWhiteSpace(bookCopyId))
            throw new ArgumentException("Ідентифікатор примірника обов'язковий", nameof(bookCopyId));

        if (string.IsNullOrWhiteSpace(readerId))
            throw new ArgumentException("Ідентифікатор читача обов'язковий", nameof(readerId));

        if (issuedOn > DateTime.Now)
            throw new ArgumentOutOfRangeException(nameof(issuedOn), issuedOn, 
                "Дата видачі не може бути в майбутньому");

        return new Loan(id.Trim(), bookCopyId.Trim(), readerId.Trim(), issuedOn, null);
    }

    public void Close(DateTime returnedOn)
    {
        if (!IsOpen)
            throw new InvalidOperationException($"Видача {Id} вже закрита {ReturnedOn:yyyy-MM-dd}");

        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn,
                $"Дата повернення ({returnedOn:yyyy-MM-dd}) не може бути раніше дати видачі ({IssuedOn:yyyy-MM-dd})");

        if (returnedOn > DateTime.Now)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), returnedOn, "Дата повернення не може бути в майбутньому");

        _returnedOn = returnedOn;
    }
}


