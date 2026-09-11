using LeanPortal.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeanPortal.Tests;

/// <summary>
/// The mail password is configured in the console, which means it is stored. These
/// cover the part that keeps it from being stored in the clear.
/// </summary>
public class SecretProtectorTests
{
    private static SecretProtector New(IDataProtectionProvider? provider = null) =>
        new(provider ?? new EphemeralDataProtectionProvider(), NullLogger<SecretProtector>.Instance);

    [Fact]
    public void Round_trips_a_password()
    {
        var protector = New();
        var stored = protector.Protect("s3cret-app-password");

        Assert.Equal("s3cret-app-password", protector.Unprotect(stored));
    }

    [Fact]
    public void Does_not_keep_the_password_in_what_it_stores()
    {
        var stored = New().Protect("s3cret-app-password");

        Assert.NotNull(stored);
        Assert.DoesNotContain("s3cret-app-password", stored);
        Assert.StartsWith("enc:v1:", stored);
    }

    [Fact]
    public void Protecting_twice_does_not_wrap_it_twice()
    {
        var protector = New();
        var once = protector.Protect("password");
        var twice = protector.Protect(once);

        // Saving the settings tab again must not re-encrypt what is already stored,
        // which would leave a value nothing can read back.
        Assert.Equal(once, twice);
        Assert.Equal("password", protector.Unprotect(twice));
    }

    [Fact]
    public void Reads_an_unencrypted_value_unchanged()
    {
        // A password set in appsettings, or one written before this existed.
        Assert.Equal("plain-from-the-file", New().Unprotect("plain-from-the-file"));
    }

    [Fact]
    public void Returns_null_when_the_key_ring_no_longer_opens_it()
    {
        var stored = New().Protect("password");

        // A different key ring: a restored database on another machine, or rotated
        // keys. Failing closed is right - the alternative is handing a relay a
        // password that is really ciphertext.
        Assert.Null(New().Unprotect(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Leaves_an_empty_value_alone(string? value)
    {
        var protector = New();

        Assert.Equal(value, protector.Protect(value));
        Assert.Equal(value, protector.Unprotect(value));
        Assert.False(protector.IsProtected(value));
    }
}
