using System;

namespace AmfibiaShop.Views;

/// <summary>
/// Интерфейс ВИДА окна входа.
/// Представитель работает с окном только через него.
/// </summary>
public interface ILoginView
{
    /// <summary>Текст из поля Email.</summary>
    string Email { get; }

    /// <summary>Текст из поля Пароль.</summary>
    string Password { get; }

    /// <summary>Нажата кнопка «Войти» или Enter.</summary>
    event EventHandler? LoginRequested;

    void ShowError(string message);

    void ClearPassword();
}
