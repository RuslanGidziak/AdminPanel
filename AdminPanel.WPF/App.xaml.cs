using System.Windows;
using System.Windows.Threading;
using AdminPanel.BL;
using AdminPanel.DAL;
using AdminPanel.DAL.Interfaces;
using AdminPanel.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPanel.WPF;

public partial class App : Application
{
    // ЗАВДАННЯ iv (DI): контейнер сервісів, доступний з усіх вікон
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Рядок підключення читаємо з appsettings.json, а не пишемо в коді
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("AdminPanel");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("У appsettings.json не задано ConnectionStrings:AdminPanel.", "Помилка конфігурації",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services, connectionString);

        // ValidateOnBuild: помилки реєстрації видно одразу під час старту, а не при відкритті вікна
        Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        try
        {
            // Роль: «в системі хоча б один адмін повинен бути» — за потреби створюється admin / Admin123!
            Services.GetRequiredService<IAdminInitializer>().EnsureAdminExists();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося підключитися до бази даних:\n{ex.Message}", "Помилка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // ЗАВДАННЯ i: застосунок починається з вікна входу
        var loginWindow = Services.GetRequiredService<LoginWindow>();
        MainWindow = loginWindow;
        loginWindow.Show();
    }

    // ЗАВДАННЯ iv (DI): корінь композиції — тут і тільки тут обираються реалізації інтерфейсів
    private static void ConfigureServices(IServiceCollection services, string connectionString)
    {
        // DAL: конструктору потрібен рядок підключення, тому реєструємо через фабрику
        services.AddSingleton<IUserDal>(_ => new UserDal(connectionString));

        // BL: сервіси без стану й сесія — один екземпляр на застосунок
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IAdminInitializer, AdminInitializer>();
        services.AddSingleton<IDialogService, MessageBoxDialogService>();

        // ViewModel і вікна: новий екземпляр щоразу, коли вікно відкривається
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<UsersViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<RegisterWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<UsersWindow>();
    }

    // Будь-яка неочікувана помилка показується повідомленням, а програма не падає
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}