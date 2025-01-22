namespace RadioModBackend.NET.Helpers;

public static class DataFormattingExtensions
{
    public static string ToTimestampString(this double totalSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        return time.Hours > 0 ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}" : $"{time.Minutes}:{time.Seconds:D2}";
    }

    public static string? ToCompactNumberString(this long viewCount)
    {
        return viewCount >= 1_000_000_000
            ? $"{viewCount / 1_000_000_000D:0.#}B"
            : viewCount >= 1_000_000
            ? $"{viewCount / 1_000_000D:0.#}M"
            : viewCount >= 1_000 ? $"{viewCount / 1_000D:0.#}K" : viewCount.ToString();
    }

    public static string? ToCompactNumberString(this long? viewCount)
    {
        return viewCount >= 1_000_000_000
            ? $"{viewCount / 1_000_000_000D:0.#}B"
            : viewCount >= 1_000_000
            ? $"{viewCount / 1_000_000D:0.#}M"
            : viewCount >= 1_000 ? $"{viewCount / 1_000D:0.#}K" : viewCount.ToString();
    }
}
