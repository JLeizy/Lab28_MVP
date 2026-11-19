namespace AmfibiaShop.Models;

/// <summary>
/// Пользователь магазина (как класс User в методичке).
/// Данные — тестовые аккаунты сайта AMFIBIA.
/// </summary>
public class User
{
    public User(string firstName, string lastName, string email, string password, string role, bool isBlocked = false)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
        Role = role;
        IsBlocked = isBlocked;
    }

    public string FirstName { get; }

    public string LastName { get; }

    public string Email { get; }

    public string Password { get; }

    /// <summary>Роль на сайте: покупатель, продавец, модератор...</summary>
    public string Role { get; }

    /// <summary>Заблокированный пользователь не может войти.</summary>
    public bool IsBlocked { get; }

    public string FullName => $"{FirstName} {LastName}";
}
