using System;
using System.Windows;

namespace AmfibiaShop.Views;

/// <summary>
/// ВИД окна входа.
/// Только показывает форму и передаёт нажатие кнопки Представителю.
/// </summary>
public partial class LoginWindow : Window, ILoginView
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    public event EventHandler? LoginRequested;

    public string Email => EmailTextBox.Text;

    public string Password => PassBox.Password;

    // Обработчик нажатия на кнопку входа: передаём событие Представителю
    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowError(string message)
    {
        MessageBlock.Text = message;
        MessageBlock.Visibility = Visibility.Visible;
    }

    public void ClearPassword()
    {
        PassBox.Clear();
        PassBox.Focus();
    }
}
