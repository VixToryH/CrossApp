using System.Globalization;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var rawItems = JsonSerializer.Deserialize<List<JsonElement>>(json, options) ?? [];

            for (int i = 0; i < rawItems.Count; i++)
            {
                int number = i + 1;

                switch (ParseJsonElement(rawItems[i]))
                {
                    case ParseOk ok:
                        items.Add(ok.Value);
                        break;
                    case ParseFailed failed:
                        errors.Add($"об'єкт {number}: {failed.Reason}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Помилка синтаксису JSON-файлу: {ex.Message}");
        }

        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome ParseJsonElement(JsonElement element)
    {
        string id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
        string isbn = element.TryGetProperty("isbn", out var isbnProp) ? isbnProp.GetString() ?? "" : "";
        string title = element.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
        string? author = element.TryGetProperty("author", out var authorProp) ? authorProp.GetString() : null;

        string yearStr = element.TryGetProperty("year", out var yearProp) ? yearProp.ToString() : "";

        return (id, isbn, title, yearStr) switch
        {
            ("", _, _, _) or (_, "", _, _) or (_, _, "", _) => 
                new ParseFailed("Id, ISBN або Назва порожні"),

            (_, _, _, var yStr) when !int.TryParse(yStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) || y < 0 || y > DateTime.Now.Year =>
                new ParseFailed($"рік '{yStr}' є некоректним або поза допустимими межами (0 - {DateTime.Now.Year})"),
            _ => new ParseOk(new BookDto(id, isbn, title, int.Parse(yearStr, CultureInfo.InvariantCulture), string.IsNullOrWhiteSpace(author) ? null : author))
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}