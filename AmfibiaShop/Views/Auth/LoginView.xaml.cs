using System;
using System.Windows;
using System.Windows.Controls;

namespace AmfibiaShop.Views.Auth;

/// <summary>
/// ВИД формы входа (как MainWindow из методички): только показывает форму
/// и передаёт нажатие кнопки Представителю.
/// </summary>
public partial class LoginView : UserControl, ILoginView
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += (_, _) => FocusEmail();
    }

    public event EventHandler? LoginRequested;
    public event EventHandler? RegisterRequested;

    public string Email => EmailTextBox.Text;

    public string Password => PassBox.Password;

    public void FocusEmail() => EmailTextBox.Focus();

    // Обработчик нажатия на кнопку входа — передаём событие Представителю
    private void LoginButton_Click(object sender, RoutedEventArgs e) => LoginRequested?.Invoke(this, EventArgs.Empty);

    private void RegisterLink_Click(object sender, RoutedEventArgs e) => RegisterRequested?.Invoke(this, EventArgs.Empty);

    // Демо-кнопки только заполняют поля (это работа Вида, без логики)
    private void DemoButton_Click(object sender, RoutedEventArgs e)
    {
        EmailTextBox.Text = (string)((Button)sender).Tag;
        PassBox.Password = "Amfibia2026";
        ErrorPanel.Visibility = Visibility.Collapsed;
    }

    public void ShowError(string message)
    {
        MessageBlock.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    public void ClearPassword()
    {
        PassBox.Clear();
        PassBox.Focus();
    }
}
