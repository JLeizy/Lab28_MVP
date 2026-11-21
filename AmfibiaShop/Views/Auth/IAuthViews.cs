using System;
using System.Collections.Generic;

namespace AmfibiaShop.Views;

// Файл назван IAuthViews.cs, интерфейсы — ILoginView и IRegisterView.

/// <summary>Вид формы входа.</summary>
public interface ILoginView
{
    string Email { get; }
    string Password { get; }

    /// <summary>Нажата кнопка «Войти» или Enter.</summary>
    event EventHandler? LoginRequested;

    /// <summary>Нажата ссылка «Создать аккаунт».</summary>
    event EventHandler? RegisterRequested;

    void ShowError(string message);
    void ClearPassword();
}

/// <summary>Вид формы регистрации.</summary>
public interface IRegisterView
{
    string FirstName { get; }
    string LastName { get; }
    string Email { get; }
    string Phone { get; }
    string Password { get; }
    string PasswordRepeat { get; }
    bool Agreed { get; }

    event EventHandler? RegisterRequested;
    event EventHandler? LoginRequested;

    /// <summary>Ошибки под полями: ключ — имя поля (FirstName, Email…).</summary>
    void ShowFieldErrors(IReadOnlyDictionary<string, string> errors);

    void ShowError(string message);
}
