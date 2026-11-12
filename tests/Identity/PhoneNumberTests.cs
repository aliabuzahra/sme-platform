using Sme.Identity.Auth;

namespace Sme.Identity.Tests;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("+962791234567", "+962791234567")]
    [InlineData("+962 79 123 4567", "+962791234567")]
    [InlineData("00962-79-1234567", "+962791234567")]
    [InlineData("0791234567", "+962791234567")]
    [InlineData("+14155552671", "+14155552671")]
    public void Normalizes_to_e164(string input, string expected)
    {
        Assert.True(PhoneNumber.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("791234567")]
    [InlineData("+0791234567")]
    [InlineData("+962")]
    [InlineData("call me")]
    public void Rejects_anything_else(string input)
    {
        Assert.False(PhoneNumber.TryNormalize(input, out _));
    }
}
