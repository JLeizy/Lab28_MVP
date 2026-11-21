using System;
using System.Globalization;
using AmfibiaShop.Models;

namespace AmfibiaShop.Presenters;

/// <summary>Форматирование цен, дат и чисел для показа (по-русски).</summary>
public static class Format
{
    public static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>Цена в выбранной валюте: «3 550 BYN», «$1 107,62», «€1 022,40».</summary>
    public static string Money(decimal amountByn, CurrencyModel currency)
    {
        decimal value = currency.Convert(amountByn);
        string number = value.ToString(value == Math.Truncate(value) ? "N0" : "N2", Ru);
        return currency.Current switch
        {
            "USD" => "$" + number,
            "EUR" => "€" + number,
            _ => number + " BYN"
        };
    }

    public static string Date(DateTime value) => value.ToString("d MMMM yyyy", Ru);

    public static string DateTime(DateTime value) => value.ToString("dd.MM.yyyy, HH:mm", Ru);

    /// <summary>Звёзды рейтинга: «★★★★☆».</summary>
    public static string Stars(int rating) => new string('★', Math.Clamp(rating, 0, 5)) + new string('☆', 5 - Math.Clamp(rating, 0, 5));

    /// <summary>Склонение: 1 товар, 2 товара, 5 товаров.</summary>
    public static string Plural(int n, string one, string few, string many)
    {
        int mod100 = Math.Abs(n) % 100, mod10 = mod100 % 10;
        string word = mod100 is >= 11 and <= 14 ? many : mod10 == 1 ? one : mod10 is >= 2 and <= 4 ? few : many;
        return $"{n} {word}";
    }
}
