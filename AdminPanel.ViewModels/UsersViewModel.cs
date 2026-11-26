using System.Collections.ObjectModel;
using AdminPanel.BL;
using AdminPanel.DTO;
using AdminPanel.ViewModels.Basic;

namespace AdminPanel.ViewModels;

// ЗАВДАННЯ i: сторінка ролі Administrator — таблиця, пошук, сортування, активація, блокування.
// ЗАВДАННЯ v (MVVM + команди): кнопки прив'язані до LoadCommand, ActivateCommand, BlockCommand
public class UsersViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IUserSession _userSession;
    private readonly IDialogService _dialogService;

    private string _searchText = string.Empty;
    private SortOption _selectedSort;
    private bool _sortDescending;
    private UserListItem? _selectedUser;
    private string? _errorMessage;
    private string? _statusMessage;
    private int _totalCount;
    private int _activeCount;
    private int _pendingCount;
    private int _blockedCount;

    // ЗАВДАННЯ iv (DI): залежності приходять через конструктор
    public UsersViewModel(IUserService userService, IUserSession userSession, IDialogService dialogService)
    {
        _userService = userService;
        _userSession = userSession;
        _dialogService = dialogService;

        SortOptions =
        [
            new SortOption("Id", UserSortField.Id),
            new SortOption("Логін", UserSortField.Login),
            new SortOption("Роль", UserSortField.Role),
            new SortOption("Стан", UserSortField.Status),
            new SortOption("Дата реєстрації", UserSortField.CreatedAt)
        ];
        _selectedSort = SortOptions[0];

        // ЗАВДАННЯ v (команди): другий параметр — CanExecute, тобто коли кнопка активна
        LoadCommand = new RelayCommand(Load);
        ActivateCommand = new RelayCommand(Activate,
            () => SelectedUser is not null && SelectedUser.Status != UserStatus.Active);
        BlockCommand = new RelayCommand(Block,
            () => SelectedUser is not null && SelectedUser.Status != UserStatus.Blocked && !SelectedUser.IsCurrentUser);
    }

    // ObservableCollection сама повідомляє DataGrid про додані чи видалені рядки
    public ObservableCollection<UserListItem> Users { get; } = new();

    public IReadOnlyList<SortOption> SortOptions { get; }

    // Роль: «робити пошук». Зміна тексту одразу перезавантажує список
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Load();
            }
        }
    }

    // Роль: «сортувати»
    public SortOption SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (value is not null && SetProperty(ref _selectedSort, value))
            {
                Load();
            }
        }
    }

    public bool SortDescending
    {
        get => _sortDescending;
        set
        {
            if (SetProperty(ref _sortDescending, value))
            {
                Load();
            }
        }
    }

    public UserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                // Без цього кнопки «Активувати» / «Заблокувати» не оновлять доступність
                ActivateCommand.RaiseCanExecuteChanged();
                BlockCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    // Лічильники для рядка стану внизу вікна
    public int TotalCount { get => _totalCount; private set => SetProperty(ref _totalCount, value); }
    public int ActiveCount { get => _activeCount; private set => SetProperty(ref _activeCount, value); }
    public int PendingCount { get => _pendingCount; private set => SetProperty(ref _pendingCount, value); }
    public int BlockedCount { get => _blockedCount; private set => SetProperty(ref _blockedCount, value); }

    public RelayCommand LoadCommand { get; }
    public RelayCommand ActivateCommand { get; }
    public RelayCommand BlockCommand { get; }

    // Роль: «бачить таблицю користувачів»
    public void Load()
    {
        try
        {
            var found = _userService.Search(SearchText, SelectedSort.Field, SortDescending);
            var all = _userService.Search(null);

            var selectedId = SelectedUser?.UserId;
            var currentId = _userSession.CurrentUser?.UserId;

            Users.Clear();
            foreach (var user in found)
            {
                Users.Add(new UserListItem(user, user.UserId == currentId));
            }

            // Після оновлення зберігаємо виділений рядок
            SelectedUser = Users.FirstOrDefault(u => u.UserId == selectedId);

            TotalCount = all.Count;
            ActiveCount = all.Count(u => u.Status == UserStatus.Active);
            PendingCount = all.Count(u => u.Status == UserStatus.Pending);
            BlockedCount = all.Count(u => u.Status == UserStatus.Blocked);
            ErrorMessage = null;
        }
        catch (BusinessRuleException ex)
        {
            // ЗАВДАННЯ ii: не адмін — BL відмовляє, показуємо причину
            ErrorMessage = ex.Message;
        }
    }

    // Роль: «активувати accounts»
    private void Activate()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        try
        {
            _userService.Activate(user.UserId);
            Load();
            StatusMessage = $"Акаунт \"{user.Login}\" активовано.";
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // Роль: «блокувати accounts», з підтвердженням
    private void Block()
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        if (!_dialogService.Confirm($"Заблокувати акаунт \"{user.Login}\"? Користувач не зможе увійти.", "Підтвердження"))
        {
            return;
        }

        try
        {
            _userService.Block(user.UserId);
            Load();
            StatusMessage = $"Акаунт \"{user.Login}\" заблоковано.";
        }
        catch (BusinessRuleException ex)
        {
            // Наприклад: «має залишитися хоча б один адміністратор»
            ErrorMessage = ex.Message;
        }
    }
}