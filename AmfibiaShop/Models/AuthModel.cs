using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>
/// МОДЕЛЬ входа в аккаунт.
/// Хранит пользователей и проверяет email и пароль.
/// Устроена как модель из примера в методичке: метод Login и свойство Message.
/// </summary>
public class AuthModel
{
    /// <summary>Пароль всех тестовых аккаунтов сайта AMFIBIA.</summary>
    public const string TestPassword = "Amfibia2026";

    private readonly List<User> users;
    private User? loggedUser;
    private string message = string.Empty;

    public AuthModel()
    {
        // Тестовые аккаунты сайта (db/seed-data/users.js)
        users = new List<User>
        {
            new User("Александра", "Вершинина", "owner@amfibia.by", TestPassword, "Создатель магазина"),
            new User("Олег", "Савицкий", "moderator@amfibia.by", TestPassword, "Модератор"),
            new User("Марина", "Ветрова", "support@amfibia.by", TestPassword, "Поддержка"),
            new User("Павел", "Кротов", "warehouse@amfibia.by", TestPassword, "Кладовщик"),
            new User("Денис", "Быков", "courier@amfibia.by", TestPassword, "Курьер"),
            new User("Илья", "Шевчук", "seller1@amfibia.by", TestPassword, "Продавец"),
            new User("Ева", "Лисовская", "buyer@amfibia.by", TestPassword, "Покупатель"),
            new User("Анна", "Ковалёва", "anna.kovaleva@example.com", TestPassword, "Покупатель"),
            new User("Дмитрий", "Соколов", "dmitry.sokolov@example.com", TestPassword, "Покупатель"),
            new User("Тимофей", "Заблокированный", "blocked.user@example.com", TestPassword, "Покупатель", isBlocked: true)
        };
    }

    /// <summary>Пользователь, который вошёл (null — вход не выполнен).</summary>
    public User? LoggedUser => loggedUser;

    /// <summary>Сообщение о результате последней попытки входа.</summary>
    public string Message => message;

    /// <summary>Метод авторизации: ищет пользователя и обновляет модель.</summary>
    public void Login(string email, string password)
    {
        loggedUser = null;

        // Email сравниваем без учёта регистра, пароль — точно
        User? user = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        if (user == null || user.Password != password)
        {
            message = "Неверный email или пароль.";
            return;
        }

        if (user.IsBlocked)
        {
            message = "Аккаунт заблокирован. Обратитесь в поддержку: support@amfibia.by";
            return;
        }

        loggedUser = user; // обновление модели
        message = $"Добро пожаловать, {user.FirstName}!";
    }
}
