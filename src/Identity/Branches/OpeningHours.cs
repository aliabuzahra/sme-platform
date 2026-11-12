using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sme.Identity.Branches;

/// <summary>One open interval in a day, as local wall-clock times "HH:mm".</summary>
public sealed record OpenInterval(string Open, string Close);

/// <summary>
/// Weekly opening hours: a list of intervals per day, keyed <c>mon</c> to <c>sun</c>.
/// A day that is missing or empty is closed. Stored as JSON on the branch row.
/// </summary>
public static class OpeningHours
{
    public static readonly string[] Days = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"];

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Returns the problems with <paramref name="hours"/>, keyed by field. Empty means valid.</summary>
    public static Dictionary<string, string[]> Validate(IReadOnlyDictionary<string, List<OpenInterval>>? hours)
    {
        var errors = new Dictionary<string, string[]>();
        if (hours is null) return errors;

        foreach (var (day, intervals) in hours)
        {
            if (!Days.Contains(day))
            {
                errors[$"hours.{day}"] = [$"Unknown day '{day}'. Use {string.Join(", ", Days)}."];
                continue;
            }
            var minutes = new List<(int Open, int Close)>();
            var problems = new List<string>();
            foreach (var interval in intervals ?? [])
            {
                if (!TryMinutes(interval.Open, out var open) || !TryMinutes(interval.Close, out var close))
                {
                    problems.Add($"'{interval.Open}'-'{interval.Close}' must use HH:mm.");
                    continue;
                }
                if (close <= open)
                {
                    problems.Add($"{interval.Open}-{interval.Close} must close after it opens.");
                    continue;
                }
                minutes.Add((open, close));
            }
            minutes.Sort();
            for (var i = 1; i < minutes.Count; i++)
            {
                if (minutes[i].Open < minutes[i - 1].Close) problems.Add("Intervals overlap.");
            }
            if (problems.Count > 0) errors[$"hours.{day}"] = problems.Distinct().ToArray();
        }
        return errors;
    }

    /// <summary>Canonical form: known days in week order, intervals sorted, closed days dropped.</summary>
    public static Dictionary<string, List<OpenInterval>> Normalize(IReadOnlyDictionary<string, List<OpenInterval>>? hours)
    {
        var result = new Dictionary<string, List<OpenInterval>>();
        if (hours is null) return result;
        foreach (var day in Days)
        {
            if (!hours.TryGetValue(day, out var intervals) || intervals is null || intervals.Count == 0) continue;
            result[day] = intervals.OrderBy(i => i.Open, StringComparer.Ordinal).ToList();
        }
        return result;
    }

    private static bool TryMinutes(string? text, out int minutes)
    {
        minutes = 0;
        if (text is null || text.Length != 5 || text[2] != ':') return false;
        if (!int.TryParse(text.AsSpan(0, 2), out var h) || !int.TryParse(text.AsSpan(3, 2), out var m)) return false;
        if (h is < 0 or > 23 || m is < 0 or > 59) return false;
        minutes = h * 60 + m;
        return true;
    }
}
