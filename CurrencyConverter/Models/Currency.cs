namespace CurrencyConverter.Models;

/// <summary>
/// Валюта и её официальный курс НБРБ.
/// Класс данных, с которым работает Модель (как класс User в методичке).
/// </summary>
public class Currency
{
    public Currency(string code, string name, int scale, decimal officialRate)
    {
        Code = code;
        Name = name;
        Scale = scale;
        OfficialRate = officialRate;
    }

    /// <summary>Буквенный код валюты: USD, EUR, RUB...</summary>
    public string Code { get; }

    /// <summary>Название валюты.</summary>
    public string Name { get; }

    /// <summary>За сколько единиц валюты указан курс (например, RUB — за 100 рублей).</summary>
    public int Scale { get; }

    /// <summary>Сколько белорусских рублей стоят Scale единиц валюты.</summary>
    public decimal OfficialRate { get; }

    /// <summary>Сколько BYN стоит одна единица валюты.</summary>
    public decimal RatePerUnit => OfficialRate / Scale;

    /// <summary>Текст для выпадающего списка: "USD — Доллар США".</summary>
    public string DisplayName => $"{Code} — {Name}";
}
