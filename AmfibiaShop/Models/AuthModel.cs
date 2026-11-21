using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>Данные регистрации (уже проверенные Представителем на формат).</summary>
public class RegistrationData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Изменяемые данные профиля.</summary>
public class ProfileData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string House { get; set; } = string.Empty;
    public string Apartment { get; set; } = string.Empty;
    public string Postal { get; set; } = string.Empty;
}

/// <summary>
/// МОДЕЛЬ аккаунтов: вход, регистрация, профиль, смена пароля.
/// Как модель из методички: методы обновляют состояние, результат — в свойстве Message.
/// </summary>
public class AuthModel
{
    private const string FileName = "users.json";

    private readonly Storage storage;
    private readonly List<User> users;

    public AuthModel(ShopData data, Storage storage)
    {
        this.storage = storage;
        users = storage.Load<List<User>>(FileName) ?? data.Users.Select(u => u.Clone()).ToList();
    }

    /// <summary>Данные вошедшего пользователя изменились (профиль).</summary>
    public event EventHandler? UserChanged;

    /// <summary>Пользователь, который вошёл (null — вход не выполнен).</summary>
    public User? CurrentUser { get; private set; }

    /// <summary>Сообщение о результате последнего действия.</summary>
    public string Message { get; private set; } = string.Empty;

    public IReadOnlyList<User> Users => users;

    /// <summary>Правило пароля магазина: не короче 8 символов, есть буквы и цифры.</summary>
    public static string? CheckPasswordPolicy(string password)
    {
        if (password.Length < 8) return "Пароль должен быть не короче 8 символов.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return "Пароль должен содержать буквы и цифры.";
        return null;
    }

    public bool Login(string email, string password)
    {
        CurrentUser = null;
        User? user = Find(email);

        if (user == null || user.Password != password)
        {
            Message = "Неверный email или пароль.";
            return false;
        }

        if (user.Blocked)
        {
            Message = "Аккаунт заблокирован. Обратитесь в поддержку: support@amfibia.by";
            return false;
        }

        CurrentUser = user;
        Message = $"Добро пожаловать, {user.FirstName}!";
        return true;
    }

    public bool Register(RegistrationData data)
    {
        if (Find(data.Email) != null)
        {
            Message = "Пользователь с таким email уже зарегистрирован.";
            return false;
        }

        string? policy = CheckPasswordPolicy(data.Password);
        if (policy != null)
        {
            Message = policy;
            return false;
        }

        var user = new User
        {
            Email = data.Email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            Phone = data.Phone,
            Password = data.Password,
            Role = "buyer",
            RoleName = "Покупатель",
            City = "Минск",
            Registered = DateTime.Now
        };
        users.Add(user);
        Save();

        CurrentUser = user;
        Message = $"Аккаунт создан. Добро пожаловать, {user.FirstName}!";
        return true;
    }

    public bool UpdateProfile(ProfileData data)
    {
        if (CurrentUser == null)
        {
            Message = "Сначала войдите в аккаунт.";
            return false;
        }

        CurrentUser.FirstName = data.FirstName;
        CurrentUser.LastName = data.LastName;
        CurrentUser.Phone = data.Phone;
        CurrentUser.City = data.City;
        CurrentUser.Street = data.Street;
        CurrentUser.House = data.House;
        CurrentUser.Apartment = data.Apartment;
        CurrentUser.Postal = data.Postal;
        Save();
        UserChanged?.Invoke(this, EventArgs.Empty);

        Message = "Данные профиля сохранены.";
        return true;
    }

    public bool ChangePassword(string currentPassword, string newPassword)
    {
        if (CurrentUser == null)
        {
            Message = "Сначала войдите в аккаунт.";
            return false;
        }

        if (CurrentUser.Password != currentPassword)
        {
            Message = "Текущий пароль указан неверно.";
            return false;
        }

        if (newPassword == currentPassword)
        {
            Message = "Новый пароль должен отличаться от текущего.";
            return false;
        }

        string? policy = CheckPasswordPolicy(newPassword);
        if (policy != null)
        {
            Message = policy;
            return false;
        }

        CurrentUser.Password = newPassword;
        Save();
        Message = "Пароль изменён.";
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        Message = string.Empty;
    }

    private User? Find(string email) =>
        users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    private void Save() => storage.Save(FileName, users);
}
