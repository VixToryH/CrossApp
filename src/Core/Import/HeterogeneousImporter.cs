using Core.Dto;

namespace Core.Import;

public record HeterogeneousResult(
    IReadOnlyList<BookDto> Books,
    IReadOnlyList<ReaderDto> Readers,
    IReadOnlyList<string> Errors
);

public static class HeterogeneousImporter
{
    private const char Separator = ';';

    public static HeterogeneousResult Load(string path)
    {
        var books = new List<BookDto>();
        var readers = new List<ReaderDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                // Префікс B — Книга: B;Id;Isbn;Title;Year;Author
                case ["B", var id, var isbn, var title, var yearStr, var author]:
                    if (int.TryParse(yearStr, out int y))
                    {
                        string? authorOrNull = string.IsNullOrWhiteSpace(author) ? null : author;
                        books.Add(new BookDto(id, isbn, title, y, authorOrNull));
                    }
                    else
                    {
                        errors.Add($"рядок {number}: некоректний рік '{yearStr}'");
                    }
                    break;

                // Книга без автора: B;Id;Isbn;Title;Year
                case ["B", var id, var isbn, var title, var yearStr]:
                    if (int.TryParse(yearStr, out int yearVal))
                    {
                        books.Add(new BookDto(id, isbn, title, yearVal));
                    }
                    else
                    {
                        errors.Add($"рядок {number}: некоректний рік '{yearStr}'");
                    }
                    break;

                // Префікс R — Читач: R;Id;FullName;TicketNumber
                case ["R", var id, var name, var ticket]:
                    readers.Add(new ReaderDto(id, name, ticket));
                    break;

                default:
                    errors.Add($"рядок {number}: невідомий або некоректний формат префіксу");
                    break;
            }
        }

        return new HeterogeneousResult(books, readers, errors);
    }
}