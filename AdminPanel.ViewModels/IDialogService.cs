namespace AdminPanel.ViewModels;

// ЗАВДАННЯ v (MVVM): ViewModel не знає про MessageBox (це View).
// Реалізація — у WPF-проєкті, у тестах — мок
public interface IDialogService
{
    bool Confirm(string message, string title);
}