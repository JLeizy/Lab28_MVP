using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AmfibiaShop.Presenters;

/// <summary>
/// Проверка формата введённых данных. Это работа Представителя:
/// Модель получает уже правильно оформленные значения.
/// </summary>
public static class Validation
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$");
    private static readonly Regex NamePattern = new(@"^[\p{L}][\p{L}\-' ]{0,39}$");

    public static string? Email(string value)
    {
        if (value.Length == 0) return "Введите email.";
        return EmailPattern.IsMatch(value) ? null : "Email указан неверно. Пример: buyer@amfibia.by";
    }

    public static string? Name(string value, string field)
    {
        if (value.Length == 0) return $"Введите {field}.";
        return NamePattern.IsMatch(value) ? null : $"Поле «{field}» может содержать только буквы.";
    }

    /// <summary>Телефон убирает пробелы, скобки и дефисы и приводит к виду +375291234567.</summary>
    public static string NormalizePhone(string value) =>
        new string(value.Where(c => char.IsDigit(c) || c == '+').ToArray());

    public static string? Phone(string value)
    {
        string phone = NormalizePhone(value);
        if (phone.Length == 0) return "Введите номер телефона.";
        if (phone.StartsWith("+375"))
        {
            return Regex.IsMatch(phone, @"^\+375(25|29|33|44|17)\d{7}$")
                ? null
                : "Белорусский номер: +375 (29) 123-45-67 — код 25, 29, 33, 44 или 17.";
        }

        return Regex.IsMatch(phone, @"^\+\d{10,14}$") ? null : "Номер в международном формате: +375 29 123-45-67.";
    }

    public static string? Required(string value, string message) => string.IsNullOrWhiteSpace(value) ? message : null;

    /// <summary>Номер карты: 16 цифр и верная контрольная сумма (алгоритм Луна).</summary>
    public static string? CardNumber(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return "Введите номер карты.";
        if (digits.Length != 16 || value.Any(c => !char.IsDigit(c) && c != ' ')) return "Номер карты — 16 цифр.";
        return Luhn(digits) ? null : "Номер карты с ошибкой. Тестовая карта: 4242 4242 4242 4242.";
    }

    public static bool Luhn(string digits)
    {
        int sum = 0;
        bool doubleIt = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (doubleIt)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }

            sum += d;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }

    /// <summary>Срок действия ММ/ГГ, карта не просрочена.</summary>
    public static string? CardExpiry(string value, DateTime today)
    {
        if (!DateTime.TryParseExact(value.Trim(), "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return "Срок действия в формате ММ/ГГ, например 08/28.";
        }

        DateTime lastDay = new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);
        return lastDay < today.Date ? "Срок действия карты истёк." : null;
    }

    public static string? CardCvc(string value) =>
        Regex.IsMatch(value.Trim(), @"^\d{3}$") ? null : "CVC — 3 цифры с обратной стороны карты.";

    public static string? CardHolder(string value) =>
        Regex.IsMatch(value.Trim(), @"^[A-Za-z]+(?: [A-Za-z]+)+$") ? null : "Имя и фамилия латиницей, как на карте: EVA LISOVSKAYA.";

    /// <summary>Цена «от/до» в фильтре: пусто или неотрицательное число.</summary>
    public static bool TryPrice(string text, out decimal? value)
    {
        value = null;
        string clean = text.Replace(" ", "").Replace(" ", "").Replace(',', '.');
        if (clean.Length == 0) return true;
        if (decimal.TryParse(clean, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal v) && v >= 0)
        {
            value = v;
            return true;
        }

        return false;
    }
}
