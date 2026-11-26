using System.Windows;
using AdminPanel.BL;
using AdminPanel.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPanel.WPF;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IAuthService _authService;

    // ЗАВДАННЯ iv (DI)
    public MainWindow(MainViewModel viewModel, IAuthService authService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
        DataContext = viewModel;
    }

    // Роль: «доступний перехід на адмін сторінку»
    private void MenuUsers_Click(object sender, RoutedEventArgs e)
    {
        // ЗАВДАННЯ ii: подвійний захист — пункт меню прихований, а BL однаково перевіряє права
        if (!_viewModel.IsAdmin)
        {
            return;
        }

        var window = App.Services.GetRequiredService<UsersWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    // Роль: «може розлогуватися» — повернення до вікна входу
    private void MenuLogout_Click(object sender, RoutedEventArgs e)
    {
        _authService.Logout();

        var loginWindow = App.Services.GetRequiredService<LoginWindow>();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();
        Close();
    }
}