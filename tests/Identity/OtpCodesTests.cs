using Sme.Identity.Auth;

namespace Sme.Identity.Tests;

public sealed class OtpCodesTests
{
    [Fact]
    public void Codes_have_the_requested_length_and_only_digits()
    {
        for (var i = 0; i < 50; i++)
        {
            var code = OtpCodes.Generate(6);
            Assert.Equal(6, code.Length);
            Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
        }
    }

    [Fact]
    public void Hash_depends_on_row_id_code_and_pepper()
    {
        var id = Guid.NewGuid();
        var hash = OtpCodes.Hash(id, "123456", "pepper");
        Assert.True(OtpCodes.Matches(id, "123456", "pepper", hash));
        Assert.False(OtpCodes.Matches(id, "123457", "pepper", hash));
        Assert.False(OtpCodes.Matches(Guid.NewGuid(), "123456", "pepper", hash));
        Assert.False(OtpCodes.Matches(id, "123456", "other", hash));
    }
}
