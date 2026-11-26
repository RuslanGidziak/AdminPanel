using AdminPanel.DAL.Interfaces;
using AdminPanel.DTO;
using Moq;
using NUnit.Framework;

namespace AdminPanel.BL.Tests;

// ЗАВДАННЯ iii: тести функцій адміністратора (пошук, сортування, активація, блокування)
[TestFixture]
public class UserServiceTests
{
    private Mock<IUserDal> _dal = null!;
    private Mock<IUserSession> _session = null!;
    private List<UserDto> _users = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        // «База» з чотирьох користувачів
        _users =
        [
            new UserDto { UserId = 1, Login = "admin", IsAdmin = true, Status = UserStatus.Active, CreatedAt = new DateTime(2025, 12, 1) },
            new UserDto { UserId = 2, Login = "ivan", Status = UserStatus.Active, CreatedAt = new DateTime(2026, 1, 10) },
            new UserDto { UserId = 3, Login = "maria", Status = UserStatus.Pending, CreatedAt = new DateTime(2026, 3, 1) },
            new UserDto { UserId = 4, Login = "taras", Status = UserStatus.Blocked, CreatedAt = new DateTime(2026, 2, 1) }
        ];

        // Мок повертає копії, як справжня база (кожне читання — новий об'єкт)
        _dal = new Mock<IUserDal>();
        _dal.Setup(d => d.GetAll()).Returns(() => _users.Select(Copy).ToList());
        _dal.Setup(d => d.GetById(It.IsAny<int>()))
            .Returns((int id) => _users.Where(u => u.UserId == id).Select(Copy).FirstOrDefault());
        _dal.Setup(d => d.Update(It.IsAny<UserDto>())).Returns(true);

        // За замовчуванням увійшов адміністратор
        _session = new Mock<IUserSession>();
        _session.Setup(s => s.CurrentUser).Returns(() => Copy(_users[0]));

        _service = new UserService(_dal.Object, _session.Object);
    }

    private static UserDto Copy(UserDto u) => new()
    {
        UserId = u.UserId,
        Login = u.Login,
        IsAdmin = u.IsAdmin,
        Status = u.Status,
        CreatedAt = u.CreatedAt
    };

    private static string[] Logins(IEnumerable<UserDto> users) => users.Select(u => u.Login).ToArray();

    // ЗАВДАННЯ ii: без входу і без ролі адміністратора доступу немає
    [Test]
    public void Search_NotSignedIn_Throws()
    {
        _session.Setup(s => s.CurrentUser).Returns((UserDto?)null);

        Assert.Throws<BusinessRuleException>(() => _service.Search(null));
    }

    [Test]
    public void Search_RegularUser_ThrowsNotAdmin()
    {
        _session.Setup(s => s.CurrentUser).Returns(Copy(_users[1]));

        var ex = Assert.Throws<BusinessRuleException>(() => _service.Search(null));

        Assert.That(ex!.Message, Is.EqualTo(UserService.NotAdminMessage));
    }

    [Test]
    public void Search_AdminBlockedWhileSignedIn_Throws()
    {
        _users[0].Status = UserStatus.Blocked;

        var ex = Assert.Throws<BusinessRuleException>(() => _service.Search(null));

        Assert.That(ex!.Message, Is.EqualTo(UserService.NotAdminMessage));
    }

    // Роль: «робити пошук»
    [TestCase("IV", new[] { "ivan" })]
    [TestCase("ar", new[] { "maria", "taras" })]
    [TestCase("   ", new[] { "admin", "ivan", "maria", "taras" })]
    [TestCase("zzz", new string[0])]
    public void Search_ByLoginPart_IgnoresCase(string text, string[] expected)
    {
        Assert.That(Logins(_service.Search(text)), Is.EqualTo(expected));
    }

    // Роль: «сортувати»
    [Test]
    public void Search_SortByLoginDescending()
    {
        var result = _service.Search(null, UserSortField.Login, true);

        Assert.That(Logins(result), Is.EqualTo(new[] { "taras", "maria", "ivan", "admin" }));
    }

    [Test]
    public void Search_SortByStatus_PendingFirst()
    {
        var result = _service.Search(null, UserSortField.Status);

        Assert.That(Logins(result), Is.EqualTo(new[] { "maria", "admin", "ivan", "taras" }));
    }

    [Test]
    public void Search_SortByCreatedAt()
    {
        var result = _service.Search(null, UserSortField.CreatedAt);

        Assert.That(Logins(result), Is.EqualTo(new[] { "admin", "ivan", "taras", "maria" }));
    }

    // Роль: «активувати accounts»
    [TestCase(3)]
    [TestCase(4)]
    public void Activate_InactiveUser_SavesActiveStatus(int userId)
    {
        _service.Activate(userId);

        _dal.Verify(d => d.Update(It.Is<UserDto>(u => u.UserId == userId && u.Status == UserStatus.Active)), Times.Once);
    }

    [Test]
    public void Activate_AlreadyActive_ThrowsAndDoesNotSave()
    {
        Assert.Throws<BusinessRuleException>(() => _service.Activate(2));
        _dal.Verify(d => d.Update(It.IsAny<UserDto>()), Times.Never);
    }

    [Test]
    public void Activate_UnknownUser_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => _service.Activate(99));
    }

    [Test]
    public void Activate_ByRegularUser_ThrowsAndDoesNotSave()
    {
        _session.Setup(s => s.CurrentUser).Returns(Copy(_users[1]));

        Assert.Throws<BusinessRuleException>(() => _service.Activate(3));
        _dal.Verify(d => d.Update(It.IsAny<UserDto>()), Times.Never);
    }

    // Роль: «блокувати accounts»
    [TestCase(2)]
    [TestCase(3)]
    public void Block_User_SavesBlockedStatus(int userId)
    {
        _service.Block(userId);

        _dal.Verify(d => d.Update(It.Is<UserDto>(u => u.UserId == userId && u.Status == UserStatus.Blocked)), Times.Once);
    }

    // Роль: «в системі хоча б один адмін повинен бути»
    [Test]
    public void Block_Self_ThrowsAndDoesNotSave()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => _service.Block(1));

        Assert.That(ex!.Message, Is.EqualTo(UserService.SelfBlockMessage));
        _dal.Verify(d => d.Update(It.IsAny<UserDto>()), Times.Never);
    }

    [Test]
    public void Block_AlreadyBlocked_Throws()
    {
        Assert.Throws<BusinessRuleException>(() => _service.Block(4));
    }

    [Test]
    public void Block_AnotherActiveAdmin_IsAllowed()
    {
        _users.Add(new UserDto { UserId = 5, Login = "olena", IsAdmin = true, Status = UserStatus.Active });

        _service.Block(5);

        _dal.Verify(d => d.Update(It.Is<UserDto>(u => u.UserId == 5 && u.Status == UserStatus.Blocked)), Times.Once);
    }

    [Test]
    public void Block_DalDidNotUpdate_Throws()
    {
        _dal.Setup(d => d.Update(It.IsAny<UserDto>())).Returns(false);

        Assert.Throws<BusinessRuleException>(() => _service.Block(2));
    }
}