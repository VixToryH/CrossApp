namespace Core;

using System.Collections.Generic;
using Core.Domain;

public static class SampleData
{
    public static List<BookCopy> Books() =>
    [
        // Доступні класичні книги (по кілька примірників для популярних назв)
        BookCopy.Create("B-001", "978-0131103627", "The C Programming Language"),
        BookCopy.Create("B-002", "978-0131103627", "The C Programming Language"),
        BookCopy.Create("B-003", "978-0131103627", "The C Programming Language"),

        BookCopy.Create("B-004", "978-0201633610", "Design Patterns"),
        BookCopy.Create("B-005", "978-0201633610", "Design Patterns"),

        BookCopy.Create("B-006", "978-0132350884", "Clean Code"),
        BookCopy.Create("B-007", "978-0132350884", "Clean Code"),
        BookCopy.Create("B-008", "978-0132350884", "Clean Code"),
        BookCopy.Create("B-009", "978-0132350884", "Clean Code"),

        // Видані примірники (isIssued: true)
        BookCopy.Create("B-010", "978-0321356680", "Effective C#", isIssued: true),
        BookCopy.Create("B-011", "978-0321356680", "Effective C#", isIssued: true),
        BookCopy.Create("B-012", "978-0596007126", "Head First Design Patterns", isIssued: true),

        // Позиції з малим залишком (по 1 примірнику) для report-low
        BookCopy.Create("B-013", "978-0134494166", "The Art of Computer Programming"),
        BookCopy.Create("B-014", "978-0201835953", "Mythical Man-Month"),
        BookCopy.Create("B-015", "978-0135957059", "Refactoring"),

        // Додаткові примірники для об'єму
        BookCopy.Create("B-016", "978-0137081073", "The Clean Coder"),
        BookCopy.Create("B-017", "978-0137081073", "The Clean Coder"),
        BookCopy.Create("B-018", "978-0134685991", "Effective Java")
    ];
}