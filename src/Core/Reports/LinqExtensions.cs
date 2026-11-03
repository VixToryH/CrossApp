using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Reports;

public static class LinqExtensions
{
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source, 
        Func<T, TKey> keySelector, 
        int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        if (count <= 0) return Enumerable.Empty<T>();

        return source
            .OrderByDescending(keySelector)
            .Take(count);
    }
}