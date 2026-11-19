using System.Windows;
using CurrencyConverter.Models;
using CurrencyConverter.Presenters;
using CurrencyConverter.Views;

namespace CurrencyConverter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Создаём три компонента MVP и связываем их
        var model = new ConverterModel(); // Модель: курсы и расчёт
        var view = new MainWindow();      // Вид: окно

        // Представитель сам подписывается на события вида,
        // поэтому вид держит на него ссылку, пока открыто окно
        var presenter = new ConverterPresenter(view, model);

        view.Show();
    }
}
