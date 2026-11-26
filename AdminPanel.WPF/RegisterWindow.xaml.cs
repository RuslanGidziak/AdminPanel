using System.Windows;
using AdminPanel.ViewModels;

namespace AdminPanel.WPF;

public partial class RegisterWindow : Window
{
    private readonly RegisterViewModel _viewModel;

    // ЗАВДАННЯ iv (DI)
    public RegisterWindow(RegisterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        // Паролі беремо з PasswordBox і передаємо у ViewModel
        if (!_viewModel.Register(pwdPassword.Password, pwdConfirm.Password))
        {
            return;
        }

        MessageBox.Show("Обліковий запис створено. Увійти можна після активації адміністратором.",
            "Реєстрація", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
    }
}