# AdminPanel — індивідуальна робота IW2. WPF


У цій роботі я зробив десктопний застосунок на **WPF (.NET 8)** з входом у систему та розподілом доступу за ролями. Я реалізовував роль **Administrator**. Проєкт розбив на кілька шарів, як ми розбирали на парах: [`AdminPanel.DTO`](./AdminPanel.DTO), [`AdminPanel.DAL`](./AdminPanel.DAL), [`AdminPanel.BL`](./AdminPanel.BL), [`AdminPanel.ViewModels`](./AdminPanel.ViewModels), [`AdminPanel.WPF`](./AdminPanel.WPF), а також два проєкти з тестами.

## 1. Встановлення та налаштування проєкту

Для роботи проєкту я підключив такі NuGet-пакети:

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.8" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="Moq" Version="4.21.0" />
```

- **Microsoft.Data.SqlClient** — через нього DAL підключається до SQL Server.
- **Microsoft.Extensions.DependencyInjection** — контейнер для Dependency Injection.
- **Microsoft.Extensions.Configuration.Json** — щоб рядок підключення брався з `appsettings.json`, а не був вшитий у код.
- **Moq** — щоб у тестах не звертатися до справжньої бази, а підставляти «фейкові» об'єкти.

Щоб запустити проєкт у себе, потрібно:
1. Відкрити SQL Server Management Studio і виконати скрипт [`Database/01-CreateDatabase.sql`](./Database/01-CreateDatabase.sql) — він створить базу `AdminPanelDb` і таблицю `tblUsers`.
2. У файлі `AdminPanel.WPF/appsettings.json` вписати свій пароль замість `***`:

```json
{
  "ConnectionStrings": {
    "AdminPanel": "Server=.;Database=AdminPanelDb;User Id=sa;Password=***;TrustServerCertificate=True"
  }
}
```

3. Зробити `AdminPanel.WPF` стартовим проєктом і натиснути **F5**.

При першому запуску програма сама створює адміністратора, тому увійти можна одразу: логін `admin`, пароль `Admin123!`.

## 2. Що вміє роль Administrator

| Що потрібно було зробити | Як це виглядає в програмі |
|---|---|
| Залогінитися | вікно «Вхід» |
| Потрапити на стартову сторінку | після входу відкривається головне вікно з привітанням |
| Перейти на сторінку своєї ролі через меню | **Адміністрування → Керування користувачами** |
| Бачити таблицю користувачів | таблиця з логіном, роллю, станом і датою реєстрації |
| Активувати або блокувати акаунти | кнопки **Активувати** та **Заблокувати** |
| Робити пошук і сортувати | поле **Пошук за логіном** і список **Сортувати за** |
| Розлогуватися | **Обліковий запис → Вийти** |

За умовою в системі має бути хоча б один адмін. Я зробив це так: адміністратор не може заблокувати сам себе (кнопка для власного акаунта просто неактивна), а при кожному запуску клас `AdminInitializer` перевіряє, чи є активний адмін, і якщо немає — створює або розблоковує акаунт `admin`.

Звичайний користувач може сам зареєструватися, але увійти зможе тільки після того, як адмін його активує. Пункту меню «Адміністрування» він не бачить.

## 3. Як я виконав вимоги завдання

| № | Вимога | Де це в коді |
|---|---|---|
| i | Автентифікація і всі функції ролі | `AuthService`, `UserService`, вікна `LoginWindow`, `MainWindow`, `UsersWindow` |
| ii | Без потрібної ролі доступу немає | меню приховане для не-адмінів, а `UserService.RequireAdmin()` ще й перевіряє права в BL |
| iii | BL покритий unit-тестами | `AdminPanel.BL.Tests`, `AdminPanel.ViewModels.Tests` |
| iv | Dependency Injection для BL | `App.xaml.cs` → метод `ConfigureServices` |
| v | MVVM і команди | `ViewModelBase`, `RelayCommand` (`ICommand`) |
| vi | Валідація даних | `ValidatableViewModelBase` (`INotifyDataErrorInfo`), `AuthService.ValidateLogin` |
| vii | Дизайн зі стилями та шаблонами | `Themes/Styles.xaml` |

Права я перевіряю саме в BL, а не тільки ховаю кнопки. Тобто навіть якщо якось відкрити вікно адміністратора без прав, сервіс однаково не дасть нічого зробити.

## 4. Dependency Injection

Усі залежності я реєструю в одному місці — в `App.xaml.cs`:

```csharp
services.AddSingleton<IUserDal>(_ => new UserDal(connectionString));
services.AddSingleton<IAuthService, AuthService>();
services.AddSingleton<IUserService, UserService>();
services.AddTransient<LoginViewModel>();
services.AddTransient<LoginWindow>();
```

- **Singleton** — для DAL, сервісів BL і сесії користувача, бо вони потрібні в одному екземплярі на весь застосунок.
- **Transient** — для ViewModel-ів і вікон, бо кожне нове вікно має отримати свій новий об'єкт.

Класи BL працюють через інтерфейси (`IUserDal`, `IPasswordHasher`, `IUserSession`), тому в тестах я легко підміняю базу даних моками.

## 5. MVVM, валідація та стилі

- **`ViewModelBase`** — реалізує `INotifyPropertyChanged`, завдяки цьому екран сам оновлюється, коли змінюються дані.
- **`RelayCommand`** — моя реалізація `ICommand`. Через `CanExecute` кнопка стає неактивною, коли дію виконати не можна (наприклад, «Активувати» для вже активного користувача).
- **`ValidatableViewModelBase`** — реалізує `INotifyDataErrorInfo`. Якщо логін введено неправильно, поле підсвічується червоним, а при наведенні видно текст помилки.
- **`Styles.xaml`** — тут зібране все оформлення: кольори, стилі кнопок через `BasedOn`, власний `ControlTemplate` для кнопок із тригерами (наведення, натискання, неактивна) і `DataTemplate` з `DataTrigger`, який фарбує стан користувача в таблиці (зелений — активний, жовтий — очікує, червоний — заблокований).

Паролі в базі я не зберігаю у відкритому вигляді — тільки хеш (PBKDF2) і сіль.

## 6. Як запустити тести

У Visual Studio:
1. Відкрити **Test Explorer** (`Test` → `Test Explorer`).
2. Побудувати рішення (`Build Solution`).
3. Натиснути **Run All Tests**.

Або в терміналі (PowerShell):

```powershell
dotnet test
```

Що саме я тестував:

| Тестовий клас | Що перевіряє |
|---|---|
| `PasswordHasherTests` | що хешування пароля працює правильно |
| `AuthServiceTests` | вхід з правильним і неправильним паролем, вхід неактивованого та заблокованого користувача, реєстрацію |
| `UserServiceTests` | пошук, сортування, активацію, блокування і те, що звичайний користувач не має доступу |
| `AdminInitializerTests` | що в системі завжди є хоча б один адмін |
| `LoginViewModelTests`, `RegisterViewModelTests`, `UsersViewModelTests` | валідацію полів і роботу команд |

У тестах я використовував `[SetUp]`, `[TestCase]` для параметризованих тестів, `Assert.That`, `Assert.Throws` і `Assert.Multiple`.

