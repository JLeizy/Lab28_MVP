using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>ПРЕДСТАВИТЕЛЬ профиля: личные данные, адрес, смена пароля, статистика, выход.</summary>
public class ProfilePresenter : PagePresenter
{
    private readonly IProfileView view;

    public ProfilePresenter(IProfileView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        this.view = view;

        view.SaveRequested += OnSaveRequested;
        view.ChangePasswordRequested += OnChangePasswordRequested;
        view.LogoutRequested += (_, _) => Navigator.Logout();
        Shop.Currency.Changed += OnModelChanged;

        Render();
    }

    public override void Dispose()
    {
        Shop.Currency.Changed -= OnModelChanged;
        base.Dispose();
    }

    private void Render()
    {
        User u = Shop.User;
        var orders = Shop.Orders.ForUser(u.Email);
        decimal spent = orders.Where(o => o.Status is not ("cancelled" or "returned")).Sum(o => o.Total);

        view.ShowProfile(new ProfileViewData
        {
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Phone = u.Phone,
            City = u.City,
            Street = u.Street,
            House = u.House,
            Apartment = u.Apartment,
            Postal = u.Postal,
            Role = u.RoleName,
            Since = u.Registered == default ? "" : $"С нами с {Format.Date(u.Registered)}",
            Initials = u.Initials,
            Stats = new[]
            {
                new SpecRow(orders.Count.ToString(), "заказов"),
                new SpecRow(Money(spent), "потрачено"),
                new SpecRow(Shop.Wishlist.Count.ToString(), "в избранном"),
                new SpecRow(Shop.Reviews.CountByUser(u.Email).ToString(), "отзывов")
            }
        });
    }

    /// <summary>Проверка полей профиля. Пустой словарь — всё верно.</summary>
    public static Dictionary<string, string> ValidateProfile(string firstName, string lastName, string phone, string city, string postal)
    {
        var errors = new Dictionary<string, string>();
        void Add(string field, string? error)
        {
            if (error != null) errors[field] = error;
        }

        Add("FirstName", Validation.Name(firstName, "имя"));
        Add("LastName", Validation.Name(lastName, "фамилию"));
        Add("Phone", Validation.Phone(phone));
        Add("City", Validation.Required(city, "Укажите город."));
        Add("Postal", postal.Length == 0 || Regex.IsMatch(postal, @"^\d{5,6}$") ? null : "Индекс — 5 или 6 цифр.");
        return errors;
    }

    private void OnSaveRequested(object? sender, EventArgs e)
    {
        var errors = ValidateProfile(view.FirstName.Trim(), view.LastName.Trim(), view.Phone, view.City.Trim(), view.Postal.Trim());
        view.ShowProfileErrors(errors);
        if (errors.Count > 0)
        {
            view.ShowMessage("Проверьте отмеченные поля.", isError: true);
            return;
        }

        bool ok = Shop.Auth.UpdateProfile(new ProfileData
        {
            FirstName = view.FirstName.Trim(),
            LastName = view.LastName.Trim(),
            Phone = Validation.NormalizePhone(view.Phone),
            City = view.City.Trim(),
            Street = view.Street.Trim(),
            House = view.House.Trim(),
            Apartment = view.Apartment.Trim(),
            Postal = view.Postal.Trim()
        });
        view.ShowMessage(Shop.Auth.Message, isError: !ok);
        if (ok)
        {
            Render();
            Navigator.Notify(Shop.Auth.Message);
        }
    }

    private void OnChangePasswordRequested(object? sender, EventArgs e)
    {
        var errors = new Dictionary<string, string>();
        if (view.CurrentPassword.Length == 0) errors["CurrentPassword"] = "Введите текущий пароль.";
        if (AuthModel.CheckPasswordPolicy(view.NewPassword) is string policy) errors["NewPassword"] = policy;
        if (view.RepeatPassword != view.NewPassword) errors["RepeatPassword"] = "Пароли не совпадают.";
        view.ShowPasswordErrors(errors);
        if (errors.Count > 0)
        {
            return;
        }

        bool ok = Shop.Auth.ChangePassword(view.CurrentPassword, view.NewPassword);
        view.ShowMessage(Shop.Auth.Message, isError: !ok);
        if (ok)
        {
            view.ClearPasswords();
            Navigator.Notify(Shop.Auth.Message);
        }
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();
}
