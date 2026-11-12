using Sme.Identity.Branches;

namespace Sme.Identity.Tests;

public sealed class OpeningHoursTests
{
    private static Dictionary<string, List<OpenInterval>> Hours(params (string Day, string Open, string Close)[] slots)
    {
        var hours = new Dictionary<string, List<OpenInterval>>();
        foreach (var (day, open, close) in slots)
        {
            if (!hours.TryGetValue(day, out var list)) hours[day] = list = [];
            list.Add(new OpenInterval(open, close));
        }
        return hours;
    }

    [Fact]
    public void Split_shifts_on_one_day_are_valid()
    {
        var errors = OpeningHours.Validate(Hours(("sat", "09:00", "13:00"), ("sat", "16:00", "22:00")));
        Assert.Empty(errors);
    }

    [Fact]
    public void Null_or_empty_hours_mean_closed_all_week()
    {
        Assert.Empty(OpeningHours.Validate(null));
        Assert.Empty(OpeningHours.Normalize(null));
        Assert.Empty(OpeningHours.Normalize(Hours()));
    }

    [Theory]
    [InlineData("mon", "9:00", "18:00")]
    [InlineData("mon", "09:00", "24:00")]
    [InlineData("mon", "18:00", "09:00")]
    [InlineData("mon", "09:00", "09:00")]
    public void Bad_times_are_reported_on_the_day(string day, string open, string close)
    {
        var errors = OpeningHours.Validate(Hours((day, open, close)));
        Assert.Contains($"hours.{day}", errors.Keys);
    }

    [Fact]
    public void Overlapping_intervals_and_unknown_days_are_rejected()
    {
        var errors = OpeningHours.Validate(Hours(("fri", "09:00", "14:00"), ("fri", "13:00", "18:00"), ("funday", "09:00", "10:00")));
        Assert.Contains("Intervals overlap.", errors["hours.fri"]);
        Assert.Contains("hours.funday", errors.Keys);
    }

    [Fact]
    public void Normalize_orders_days_and_intervals_and_drops_closed_days()
    {
        var normalized = OpeningHours.Normalize(Hours(("sun", "16:00", "20:00"), ("sun", "08:00", "12:00"), ("mon", "09:00", "17:00")));
        normalized["tue"] = [];

        var canonical = OpeningHours.Normalize(normalized);

        Assert.Equal(["mon", "sun"], canonical.Keys);
        Assert.Equal(["08:00", "16:00"], canonical["sun"].Select(i => i.Open));
    }
}
