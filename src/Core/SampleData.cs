namespace Core;

using Core.Domain;

public static class SampleData
{
    public static List<BookCopy> Books() =>
    [
        BookCopy.Create("B-001", "978-0131103627", "The C Programming Language"),
        BookCopy.Create("B-002", "978-0201633610", "Design Patterns"),
        BookCopy.Create("B-003", "978-0132350884", "Clean Code")
    ];
}