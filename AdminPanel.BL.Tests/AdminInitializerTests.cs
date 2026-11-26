using AdminPanel.DAL.Interfaces;
using AdminPanel.DTO;
using Moq;
using NUnit.Framework;

namespace AdminPanel.BL.Tests;

// ЗАВДАННЯ iii: тести правила «в системі хоча б один адмін»
[TestFixture]
public class AdminInitializerTests
{
    private Mock<IUserDal> _dal = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private AdminInitializer _initializer = null!;

    [SetUp]
    public void SetUp()
    {
        _dal = new Mock<IUserDal>();
        _hasher = new Mock<IPasswordHasher>();
        _hasher.Setup(h => h.Hash(It.IsAny<string>(), It.IsAny<Guid>())).Returns(new byte[] { 1 });
        _initializer = new AdminInitializer(_dal.Object, _hasher.Object);
    }

    [Test]
    public void EmptyDatabase_CreatesActiveAdmin()
    {
        _dal.Setup(d => d.GetAll()).Returns(new List<UserDto>());

        var changed = _initializer.EnsureAdminExists();

        Assert.That(changed, Is.True);
        _dal.Verify(d => d.Create(It.Is<UserDto>(u =>
            u.Login == AdminInitializer.DefaultLogin && u.IsAdmin && u.Status == UserStatus.Active)), Times.Once);
        _hasher.Verify(h => h.Hash(AdminInitializer.DefaultPassword, It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public void ActiveAdminExists_DoesNothing()
    {
        _dal.Setup(d => d.GetAll()).Returns(new List<UserDto>
        {
            new() { UserId = 1, Login = "boss", IsAdmin = true, Status = UserStatus.Active }
        });

        var changed = _initializer.EnsureAdminExists();

        Assert.That(changed, Is.False);
        _dal.Verify(d => d.Create(It.IsAny<UserDto>()), Times.Never);
        _dal.Verify(d => d.Update(It.IsAny<UserDto>()), Times.Never);
    }

    [Test]
    public void OnlyBlockedAdmin_ReactivatesAdminAccount()
    {
        var admin = new UserDto { UserId = 1, Login = "admin", IsAdmin = true, Status = UserStatus.Blocked };
        _dal.Setup(d => d.GetAll()).Returns(new List<UserDto> { admin });
        _dal.Setup(d => d.GetByLogin("admin")).Returns(admin);

        var changed = _initializer.EnsureAdminExists();

        Assert.That(changed, Is.True);
        _dal.Verify(d => d.Update(It.Is<UserDto>(u =>
            u.UserId == 1 && u.IsAdmin && u.Status == UserStatus.Active)), Times.Once);
        _dal.Verify(d => d.Create(It.IsAny<UserDto>()), Times.Never);
    }
}