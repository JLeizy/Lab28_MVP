using System.Windows;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters;
using AmfibiaShop.Views;

namespace AmfibiaShop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // MVP окна входа: Модель + Вид + Представитель
        var loginView = new LoginWindow();
        var loginPresenter = new LoginPresenter(loginView, new AuthModel());

        // Когда вход выполнен — открываем магазин и закрываем окно входа
        loginPresenter.LoginSucceeded += (sender, user) => OpenShop(loginView, user);

        loginView.Show();
    }

    private static void OpenShop(Window loginView, User user)
    {
        // MVP окна магазина: две модели (каталог и корзина) + Вид + Представитель
        var shopView = new ShopWindow();
        var shopPresenter = new ShopPresenter(shopView, new CatalogModel(), new CartModel(), user);

        shopView.Show();
        loginView.Close();
    }
}
