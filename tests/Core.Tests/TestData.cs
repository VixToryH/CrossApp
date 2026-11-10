using Core.Domain;

namespace Core.Tests;

public static class TestData
{
    public static IReadOnlyList<BookCopy> Items() => new List<BookCopy>
    {
        BookCopy.Create("BC-1", "978-0000000001", "SKU-BIG", isIssued: false),
        BookCopy.Create("BC-2", "978-0000000001", "SKU-BIG", isIssued: false),
        BookCopy.Create("BC-3", "978-0000000002", "SKU-MID", isIssued: false),
        BookCopy.Create("BC-4", "978-0000000003", "SKU-LOW", isIssued: true)
    };
}