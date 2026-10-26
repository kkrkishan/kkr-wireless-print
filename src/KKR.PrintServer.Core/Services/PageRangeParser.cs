namespace KKR.PrintServer.Core.Services;

public static class PageRangeParser
{
    public static List<int> Parse(string? pageRange, int totalPages)
    {
        if (totalPages <= 0)
        {
            return new List<int>();
        }

        if (string.IsNullOrWhiteSpace(pageRange) || pageRange.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return Enumerable.Range(1, totalPages).ToList();
        }

        var result = new HashSet<int>();
        var parts = pageRange.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var subParts = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (subParts.Length == 2 &&
                    int.TryParse(subParts[0], out int start) &&
                    int.TryParse(subParts[1], out int end))
                {
                    if (start > end)
                    {
                        (start, end) = (end, start);
                    }

                    for (int p = start; p <= end; p++)
                    {
                        if (p >= 1 && p <= totalPages)
                        {
                            result.Add(p);
                        }
                    }
                }
            }
            else if (int.TryParse(part, out int singlePage))
            {
                if (singlePage >= 1 && singlePage <= totalPages)
                {
                    result.Add(singlePage);
                }
            }
        }

        if (result.Count == 0)
        {
            // Fallback to all pages if filter matched nothing valid
            return Enumerable.Range(1, totalPages).ToList();
        }

        return result.OrderBy(x => x).ToList();
    }
}
