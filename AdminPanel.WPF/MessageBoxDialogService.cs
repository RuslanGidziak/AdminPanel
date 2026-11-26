using System.Windows;
using AdminPanel.ViewModels;

namespace AdminPanel.WPF;

// ЗАВДАННЯ v (MVVM): реалізація IDialogService для WPF.
// ViewModel викликає Confirm, а MessageBox показує саме цей клас
public class MessageBoxDialogService : IDialogService
{
    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}