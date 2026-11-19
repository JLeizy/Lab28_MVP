using System;
using System.Text.RegularExpressions;
using AmfibiaShop.Models;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ окна входа.
/// Получает email и пароль от вида, проверяет их и передаёт модели.
/// </summary>
public class LoginPresenter
{
    private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private readonly ILoginView view;  // связь с видом
    private readonly AuthModel model;  // связь с моделью

    public LoginPresenter(ILoginView view, AuthModel model)
    {
        this.view = view;
        this.model = model;

        view.LoginRequested += OnLoginRequested;
    }

    /// <summary>Вход выполнен: приложение открывает окно магазина.</summary>
    public event EventHandler<User>? LoginSucceeded;

    /// <summary>
    /// Проверка введённых данных до обращения к модели.
    /// Возвращает текст ошибки или null, если всё верно.
    /// </summary>
    public static string? Validate(string email, string password)
    {
        if (email.Length == 0)
        {
            return "Введите email.";
        }

        if (!EmailPattern.IsMatch(email))
        {
            return "Email указан неверно. Пример: buyer@amfibia.by";
        }

        if (password.Length == 0)
        {
            return "Введите пароль.";
        }

        return null;
    }

    private void OnLoginRequested(object? sender, EventArgs e)
    {
        // 1. Получаем данные от вида и обрабатываем их
        string email = view.Email.Trim();
        string password = view.Password; // пароль не обрезаем: пробел может быть его частью

        string? error = Validate(email, password);
        if (error != null)
        {
            view.ShowError(error);
            return;
        }

        // 2. Передаём данные модели, модель обновляется
        model.Login(email, password);

        // 3. Берём результат из модели
        if (model.LoggedUser == null)
        {
            view.ShowError(model.Message);
            view.ClearPassword();
            return;
        }

        LoginSucceeded?.Invoke(this, model.LoggedUser);
    }
}
