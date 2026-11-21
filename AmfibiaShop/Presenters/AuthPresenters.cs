using System;
using System.Collections.Generic;
using AmfibiaShop.Models;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ формы входа — как в методичке:
/// получает данные от вида, обрабатывает их, передаёт модели и возвращает результат виду.
/// </summary>
public class LoginPresenter
{
    private readonly ILoginView view;
    private readonly AuthModel model;

    public LoginPresenter(ILoginView view, AuthModel model)
    {
        this.view = view;
        this.model = model;

        view.LoginRequested += OnLoginRequested;
        view.RegisterRequested += (_, _) => RegisterRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Вход выполнен — приложение открывает магазин.</summary>
    public event EventHandler<User>? LoginSucceeded;

    /// <summary>Пользователь хочет создать аккаунт.</summary>
    public event EventHandler? RegisterRequested;

    private void OnLoginRequested(object? sender, EventArgs e)
    {
        // 1. Получаем данные от вида и обрабатываем их
        string email = view.Email.Trim();
        string password = view.Password; // пароль не обрезаем: пробел может быть его частью

        string? error = Validation.Email(email) ?? (password.Length == 0 ? "Введите пароль." : null);
        if (error != null)
        {
            view.ShowError(error);
            return;
        }

        // 2. Передаём данные модели, модель обновляется
        if (!model.Login(email, password))
        {
            // 3. Модель сообщила об ошибке — показываем её
            view.ShowError(model.Message);
            view.ClearPassword();
            return;
        }

        LoginSucceeded?.Invoke(this, model.CurrentUser!);
    }
}

/// <summary>ПРЕДСТАВИТЕЛЬ регистрации нового покупателя.</summary>
public class RegisterPresenter
{
    private readonly IRegisterView view;
    private readonly AuthModel model;

    public RegisterPresenter(IRegisterView view, AuthModel model)
    {
        this.view = view;
        this.model = model;

        view.RegisterRequested += OnRegisterRequested;
        view.LoginRequested += (_, _) => LoginRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<User>? Registered;

    public event EventHandler? LoginRequested;

    /// <summary>Проверяет все поля формы. Пустой словарь — ошибок нет.</summary>
    public static Dictionary<string, string> Validate(string firstName, string lastName, string email, string phone,
                                                      string password, string repeat, bool agreed)
    {
        var errors = new Dictionary<string, string>();
        void Add(string field, string? error)
        {
            if (error != null) errors[field] = error;
        }

        Add("FirstName", Validation.Name(firstName, "имя"));
        Add("LastName", Validation.Name(lastName, "фамилию"));
        Add("Email", Validation.Email(email));
        Add("Phone", Validation.Phone(phone));
        Add("Password", AuthModel.CheckPasswordPolicy(password));
        Add("PasswordRepeat", repeat != password ? "Пароли не совпадают." : null);
        Add("Agreed", agreed ? null : "Нужно согласие с условиями магазина.");
        return errors;
    }

    private void OnRegisterRequested(object? sender, EventArgs e)
    {
        string firstName = view.FirstName.Trim();
        string lastName = view.LastName.Trim();
        string email = view.Email.Trim();

        Dictionary<string, string> errors = Validate(firstName, lastName, email, view.Phone, view.Password, view.PasswordRepeat, view.Agreed);
        view.ShowFieldErrors(errors);
        if (errors.Count > 0)
        {
            return;
        }

        bool ok = model.Register(new RegistrationData
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = Validation.NormalizePhone(view.Phone),
            Password = view.Password
        });

        if (!ok)
        {
            view.ShowError(model.Message);
            return;
        }

        Registered?.Invoke(this, model.CurrentUser!);
    }
}
