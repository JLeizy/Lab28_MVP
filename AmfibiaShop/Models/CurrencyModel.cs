using System;
using System.Collections.Generic;

namespace AmfibiaShop.Models;

/// <summary>
/// МОДЕЛЬ валюты: цены хранятся в BYN, показываются в выбранной валюте
/// по курсам сайта (таблица currency_rates).
/// </summary>
public class CurrencyModel
{
    private readonly Dictionary<string, decimal> rates;

    public CurrencyModel(CurrencyRates source)
    {
        rates = new Dictionary<string, decimal>
        {
            ["BYN"] = 1m,
            ["USD"] = source.USD,
            ["EUR"] = source.EUR
        };
        Updated = source.Updated;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<string> Codes { get; } = new[] { "BYN", "USD", "EUR" };

    public string Current { get; private set; } = "BYN";

    public DateTime Updated { get; }

    public decimal Rate(string code) => rates[code];

    public bool Select(string code)
    {
        if (!rates.ContainsKey(code) || code == Current)
        {
            return false;
        }

        Current = code;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Перевод суммы из BYN в текущую валюту.</summary>
    public decimal Convert(decimal amountByn) =>
        Current == "BYN" ? amountByn : Math.Round(amountByn / rates[Current], 2, MidpointRounding.AwayFromZero);
}
