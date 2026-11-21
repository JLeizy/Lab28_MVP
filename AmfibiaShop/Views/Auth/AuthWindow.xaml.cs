using System.Windows;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Auth;

/// <summary>Окно входа: слева витрина бренда, справа форма входа или регистрации.</summary>
public partial class AuthWindow : Window
{
    public AuthWindow()
    {
        InitializeComponent();
        WindowTheme.UseDarkTitleBar(this);
    }

    public ILoginView LoginView => LoginPanel;

    public IRegisterView RegisterView => RegisterPanel;

    public void ShowLogin()
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        Title = "AMFIBIA — вход";
        LoginPanel.FocusEmail();
    }

    public void ShowRegister()
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        RegisterPanel.Visibility = Visibility.Visible;
        Title = "AMFIBIA — регистрация";
    }
}
