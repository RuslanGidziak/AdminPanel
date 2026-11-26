using AdminPanel.BL;
using Moq;
using NUnit.Framework;

namespace AdminPanel.ViewModels.Tests;

// Тести ViewModel входу: валідація (ЗАВДАННЯ vi) і реакція на відповідь BL
[TestFixture]
public class LoginViewModelTests
{
    private Mock<IAuthService> _auth = null!;
    private Mock<IUserSession> _session = null!;
    private LoginViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _auth = new Mock<IAuthService>();
        _session = new Mock<IUserSession>();
        _vm = new LoginViewModel(_auth.Object, _session.Object);
    }

    [Test]
    public void SignIn_EmptyLogin_ShowsFieldErrorAndDoesNotCallService()
    {
        var result = _vm.SignIn("secret1");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(_vm.HasErrors, Is.True);
            Assert.That(_vm.GetErrors(nameof(LoginViewModel.Login)).Cast<string>(), Is.Not.Empty);
        });
        _auth.Verify(a => a.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SignIn_EmptyPassword_ShowsMessageAndDoesNotCallService()
    {
        _vm.Login = "ivan";

        Assert.That(_vm.SignIn(""), Is.False);
        Assert.That(_vm.ErrorMessage, Is.EqualTo("Введіть пароль."));
        _auth.Verify(a => a.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SignIn_WrongCredentials_ShowsCommonMessage()
    {
        _vm.Login = "ivan";
        _auth.Setup(a => a.Login("ivan", "bad")).Returns(false);

        Assert.That(_vm.SignIn("bad"), Is.False);
        Assert.That(_vm.ErrorMessage, Is.EqualTo(LoginViewModel.WrongCredentialsMessage));
    }

    // ЗАВДАННЯ ii: заблокований користувач бачить причину і не входить
    [Test]
    public void SignIn_BlockedAccount_ShowsMessageFromBl()
    {
        _vm.Login = "ivan";
        _auth.Setup(a => a.Login("ivan", "secret1")).Throws(new BusinessRuleException(AuthService.BlockedMessage));

        Assert.That(_vm.SignIn("secret1"), Is.False);
        Assert.That(_vm.ErrorMessage, Is.EqualTo(AuthService.BlockedMessage));
    }

    [Test]
    public void SignIn_Success_ReturnsTrueAndClearsError()
    {
        _vm.Login = "ivan";
        _auth.Setup(a => a.Login("ivan", "secret1")).Returns(true);
        _vm.SignIn(""); // спочатку з'являється помилка «Введіть пароль.»

        Assert.That(_vm.SignIn("secret1"), Is.True);
        Assert.That(_vm.ErrorMessage, Is.Null);
    }

    // ЗАВДАННЯ vi: WPF дізнається про помилку через подію ErrorsChanged
    [Test]
    public void ClearingLogin_RaisesErrorsChanged_FixingClearsError()
    {
        _vm.Login = "ivan";
        var changed = new List<string?>();
        _vm.ErrorsChanged += (_, e) => changed.Add(e.PropertyName);

        _vm.Login = "";
        Assert.That(_vm.HasErrors, Is.True);

        _vm.Login = "ivan2";
        Assert.That(_vm.HasErrors, Is.False);

        Assert.That(changed, Is.EqualTo(new[] { "Login", "Login" }));
    }

    [Test]
    public void IsAdmin_ComesFromSession()
    {
        _session.Setup(s => s.IsAdmin).Returns(true);

        Assert.That(_vm.IsAdmin, Is.True);
    }
}