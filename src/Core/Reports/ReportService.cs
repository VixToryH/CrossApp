using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<BookCopy> source, int n)
    {
        return source
            .GroupBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => new StockLine(
                Sku: g.First().Isbn,
                Name: g.Key,
                Quantity: g.Count(),
                Unit: "шт"
            ))
            .TopBy(x => x.Quantity, n)
            .ToList();
    }

    public static IReadOnlyList<StockLine> LowStock(IEnumerable<BookCopy> source, int threshold)
    {
        return source
            .GroupBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() < threshold)
            .OrderBy(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new StockLine(g.First().Isbn, g.Key, g.Count(), "шт"))
            .ToList();
    }

    public static IReadOnlyList<GroupSummary> ByCategory(IEnumerable<BookCopy> source)
    {
        return source
            .GroupBy(b => b.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Count()))
            .OrderByDescending(s => s.TotalQuantity)
            .ToList();
    }

    public static IReadOnlyList<GroupSummary> ByCategoryQuerySyntax(IEnumerable<BookCopy> source)
    {
        var query = from book in source
                    group book by book.Status.ToString() into statusGroup
                    select new GroupSummary(
                        Key: statusGroup.Key,
                        Count: statusGroup.Count(),
                        TotalQuantity: statusGroup.Count()
                    ) into summary
                    orderby summary.TotalQuantity descending
                    select summary;

        return query.ToList();
    }

    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<BookCopy> source)
    {
        var list = source
            .GroupBy(b => b.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .ToList();

        return list.Count == 0 
            ? (0, 0, 0d, 0) 
            : (list.Count, list.Sum(), list.Average(), list.Max());
    }

    public static IReadOnlyList<BookLoanSummary> GetBookLoanSummariesGroupJoin(
        IEnumerable<BookCopy> books, 
        IEnumerable<Loan> loans)
    {
        return books
            .GroupJoin(
                loans,
                book => book.Id,
                loan => loan.BookCopyId,
                (book, bookLoans) => new BookLoanSummary(
                    BookId: book.Id,
                    Title: book.Title,
                    TotalLoansCount: bookLoans.Count(),
                    IsCurrentlyIssued: bookLoans.Any(l => l.IsOpen)
                )
            )
            .TopBy(s => s.TotalLoansCount, 10)
            .ToList();
    }

    public static IReadOnlyList<GroupSummary> TopCategories(IEnumerable<BookCopy> source, int topCount = 3)
    {
        return source
            .GroupBy(b => b.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Count()))
            .TopBy(g => g.TotalQuantity, topCount)
            .ToList();
    }

    public static (long ElapsedWithoutToListMs, long ElapsedWithToListMs) BenchmarkListImpact(int count = 100_000)
    {
        var largeCollection = Enumerable.Range(1, count)
            .Select(i => BookCopy.Create($"BC-{i}", $"978-{i:D9}", $"Book Title {i % 500}"))
            .ToList();

        _ = largeCollection.Where(b => b.Title.Contains("10")).Take(10).ToList();

        var swA = Stopwatch.StartNew();
        var resultA = largeCollection
            .Where(b => b.Title.Contains("Book Title 1"))
            .Select(b => b.Title)
            .Take(100)
            .ToList();
        swA.Stop();

        var swB = Stopwatch.StartNew();
        var resultB = largeCollection
            .Where(b => b.Title.Contains("Book Title 1"))
            .ToList()
            .Select(b => b.Title)
            .Take(100)
            .ToList();
        swB.Stop();

        return (swA.ElapsedMilliseconds, swB.ElapsedMilliseconds);
    }
}