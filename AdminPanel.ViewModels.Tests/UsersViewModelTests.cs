using AdminPanel.BL;
using AdminPanel.DTO;
using Moq;
using NUnit.Framework;

namespace AdminPanel.ViewModels.Tests;

// Тести адмін-сторінки: ЗАВДАННЯ v (команди, CanExecute) і функції ролі
[TestFixture]
public class UsersViewModelTests
{
    private Mock<IUserService> _service = null!;
    private Mock<IUserSession> _session = null!;
    private Mock<IDialogService> _dialog = null!;
    private UsersViewModel _vm = null!;

    private static List<UserDto> CreateUsers() =>
    [
        new UserDto { UserId = 1, Login = "admin", IsAdmin = true, Status = UserStatus.Active },
        new UserDto { UserId = 2, Login = "ivan", Status = UserStatus.Active },
        new UserDto { UserId = 3, Login = "maria", Status = UserStatus.Pending },
        new UserDto { UserId = 4, Login = "taras", Status = UserStatus.Blocked }
    ];

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IUserService>();
        _service.Setup(s => s.Search(It.IsAny<string>(), It.IsAny<UserSortField>(), It.IsAny<bool>()))
            .Returns(() => CreateUsers());

        // Увійшов admin (Id = 1)
        _session = new Mock<IUserSession>();
        _session.Setup(s => s.CurrentUser)
            .Returns(new UserDto { UserId = 1, Login = "admin", IsAdmin = true, Status = UserStatus.Active });

        _dialog = new Mock<IDialogService>();
        _vm = new UsersViewModel(_service.Object, _session.Object, _dialog.Object);
    }

    // Завантажити таблицю й виділити рядок
    private UserListItem LoadAndSelect(int userId)
    {
        _vm.Load();
        _vm.SelectedUser = _vm.Users.Single(u => u.UserId == userId);
        return _vm.SelectedUser;
    }

    [Test]
    public void Load_FillsUsersAndCounters()
    {
        _vm.Load();

        Assert.Multiple(() =>
        {
            Assert.That(_vm.Users, Has.Count.EqualTo(4));
            Assert.That(_vm.TotalCount, Is.EqualTo(4));
            Assert.That(_vm.ActiveCount, Is.EqualTo(2));
            Assert.That(_vm.PendingCount, Is.EqualTo(1));
            Assert.That(_vm.BlockedCount, Is.EqualTo(1));
            Assert.That(_vm.Users[0].IsCurrentUser, Is.True);
        });
    }

    [Test]
    public void Load_Twice_DoesNotDuplicateUsers()
    {
        _vm.Load();
        _vm.Load();

        Assert.That(_vm.Users, Has.Count.EqualTo(4));
    }

    // ЗАВДАННЯ ii: якщо BL відмовив, таблиця порожня і показується причина
    [Test]
    public void Load_NotAdmin_ShowsErrorFromBl()
    {
        _service.Setup(s => s.Search(It.IsAny<string>(), It.IsAny<UserSortField>(), It.IsAny<bool>()))
            .Throws(new BusinessRuleException(UserService.NotAdminMessage));

        _vm.Load();

        Assert.That(_vm.ErrorMessage, Is.EqualTo(UserService.NotAdminMessage));
        Assert.That(_vm.Users, Is.Empty);
    }

    [Test]
    public void SearchText_Change_SearchesWithThisText()
    {
        _vm.SearchText = "iv";

        _service.Verify(s => s.Search("iv", UserSortField.Id, false), Times.Once);
    }

    [Test]
    public void SortChange_SearchesWithSelectedFieldAndDirection()
    {
        _vm.SortDescending = true;
        _vm.SelectedSort = _vm.SortOptions.Single(o => o.Field == UserSortField.Login);

        _service.Verify(s => s.Search("", UserSortField.Login, true), Times.Once);
    }

    // ЗАВДАННЯ v: CanExecute команди залежить від вибраного рядка
    [Test]
    public void ActivateCommand_CanExecute_DependsOnSelectedUser()
    {
        _vm.Load();
        Assert.That(_vm.ActivateCommand.CanExecute(null), Is.False); // нічого не вибрано

        LoadAndSelect(3); // maria — очікує
        Assert.That(_vm.ActivateCommand.CanExecute(null), Is.True);

        LoadAndSelect(2); // ivan — уже активний
        Assert.That(_vm.ActivateCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void BlockCommand_CurrentUser_IsDisabled()
    {
        LoadAndSelect(1); // сам адмін
        Assert.That(_vm.BlockCommand.CanExecute(null), Is.False);

        LoadAndSelect(2);
        Assert.That(_vm.BlockCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void SelectedUser_Change_RaisesCanExecuteChanged()
    {
        _vm.Load();
        var raised = 0;
        _vm.ActivateCommand.CanExecuteChanged += (_, _) => raised++;

        _vm.SelectedUser = _vm.Users[2];

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void ActivateCommand_Execute_CallsServiceAndShowsStatus()
    {
        LoadAndSelect(3);

        _vm.ActivateCommand.Execute(null);

        _service.Verify(s => s.Activate(3), Times.Once);
        Assert.That(_vm.StatusMessage, Does.Contain("maria"));
    }

    [Test]
    public void BlockCommand_Confirmed_CallsService()
    {
        _dialog.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        LoadAndSelect(2);

        _vm.BlockCommand.Execute(null);

        _service.Verify(s => s.Block(2), Times.Once);
        Assert.That(_vm.StatusMessage, Does.Contain("ivan"));
    }

    [Test]
    public void BlockCommand_NotConfirmed_DoesNothing()
    {
        _dialog.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        LoadAndSelect(2);

        _vm.BlockCommand.Execute(null);

        _service.Verify(s => s.Block(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void BlockCommand_BusinessRuleViolation_ShowsError()
    {
        _dialog.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _service.Setup(s => s.Block(2)).Throws(new BusinessRuleException(UserService.LastAdminMessage));
        LoadAndSelect(2);

        _vm.BlockCommand.Execute(null);

        Assert.That(_vm.ErrorMessage, Is.EqualTo(UserService.LastAdminMessage));
    }
}