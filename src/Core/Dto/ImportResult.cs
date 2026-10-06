namespace Core.Dto;

public sealed record ImportResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyList<string> Errors)
{
    public int TotalCount => Items.Count + Errors.Count;

    public double ErrorPercentage => TotalCount > 0 
        ? (double)Errors.Count / TotalCount * 100 
        : 0;
}
