using System.Windows;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters;
using AmfibiaShop.Views;
using AmfibiaShop.Views.Auth;

namespace AmfibiaShop;

/// <summary>
/// Точка сборки MVP: здесь создаются Модели, Виды и Представители и связываются друг с другом.
/// </summary>
public partial class App : Application
{
    private ShopContext shop = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // МОДЕЛИ: данные сайта AMFIBIA + личные данные в %AppData%\AMFIBIA\Lab28_MVP
        shop = new ShopContext(ShopData.LoadEmbedded(), Storage.CreateDefault());

        ShowAuth();
    }

    /// <summary>Окно входа: два Вида (вход и регистрация) и два Представителя с общей моделью AuthModel.</summary>
    private void ShowAuth()
    {
        var window = new AuthWindow();
        var login = new LoginPresenter(window.LoginView, shop.Auth);
        var register = new RegisterPresenter(window.RegisterView, shop.Auth);

        login.RegisterRequested += (_, _) => window.ShowRegister();
        register.LoginRequested += (_, _) => window.ShowLogin();
        login.LoginSucceeded += (_, _) => OpenShop(window);
        register.Registered += (_, _) => OpenShop(window);

        window.Show();
    }

    /// <summary>Главное окно магазина: ShellPresenter управляет страницами.</summary>
    private void OpenShop(Window authWindow)
    {
        shop.StartSession();

        var window = new MainWindow();
        var shell = new ShellPresenter(window, shop, new ViewFactory());
        shell.LoggedOut += (_, _) =>
        {
            ShowAuth();
            window.Close();
        };

        window.Show();
        authWindow.Close();
    }
}
