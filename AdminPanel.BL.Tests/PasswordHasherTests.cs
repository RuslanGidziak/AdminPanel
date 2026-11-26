using NUnit.Framework;

namespace AdminPanel.BL.Tests;

// ЗАВДАННЯ iii: тести хешування паролів
[TestFixture]
public class PasswordHasherTests
{
    private PasswordHasher _hasher = null!;

    [SetUp]
    public void SetUp() => _hasher = new PasswordHasher();

    [Test]
    public void Hash_SameSalt_ReturnsSameHash()
    {
        var salt = Guid.NewGuid();

        Assert.That(_hasher.Hash("secret1", salt), Is.EqualTo(_hasher.Hash("secret1", salt)));
    }

    [Test]
    public void Hash_DifferentSalt_ReturnsDifferentHash()
    {
        Assert.That(_hasher.Hash("secret1", Guid.NewGuid()), Is.Not.EqualTo(_hasher.Hash("secret1", Guid.NewGuid())));
    }

    [Test]
    public void Hash_HasExpectedLength()
    {
        Assert.That(_hasher.Hash("secret1", Guid.NewGuid()), Has.Length.EqualTo(PasswordHasher.HashSize));
    }

    [Test]
    public void Verify_CorrectAndWrongPassword()
    {
        var salt = Guid.NewGuid();
        var hash = _hasher.Hash("secret1", salt);

        Assert.Multiple(() =>
        {
            Assert.That(_hasher.Verify("secret1", salt, hash), Is.True);
            Assert.That(_hasher.Verify("wrong", salt, hash), Is.False);
        });
    }
}