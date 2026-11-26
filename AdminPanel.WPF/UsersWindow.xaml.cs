using System.Windows;
using AdminPanel.ViewModels;

namespace AdminPanel.WPF;

public partial class UsersWindow : Window
{
    private readonly UsersViewModel _viewModel;

    // ЗАВДАННЯ iv (DI)
    public UsersWindow(UsersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    // Під час відкриття вікна завантажуємо таблицю
    private void Window_Loaded(object sender, RoutedEventArgs e) => _viewModel.Load();

    // Закриття вікна — суто віконна дія, тому вона в code-behind
    private void MenuClose_Click(object sender, RoutedEventArgs e) => Close();
}