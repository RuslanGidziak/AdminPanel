using System.Windows;
using AdminPanel.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPanel.WPF;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    // ЗАВДАННЯ iv (DI): ViewModel передає контейнер DI, вікно саме її не створює
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        // Уся логіка входу — у ViewModel і BL; тут лише передаємо пароль і перемикаємо вікна
        if (!_viewModel.SignIn(pwdPassword.Password))
        {
            pwdPassword.Clear();
            pwdPassword.Focus();
            return;
        }

        // Роль: «попадає на стартову сторінку».
        // Спочатку показуємо нове вікно, потім закриваємо це: інакше застосунок завершиться
        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        var window = App.Services.GetRequiredService<RegisterWindow>();
        window.Owner = this;
        window.ShowDialog();
    }
}