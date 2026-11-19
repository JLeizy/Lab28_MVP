using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyConverter.Models;

/// <summary>
/// МОДЕЛЬ (Model).
/// Хранит курсы валют, загружает их с сайта НБРБ и выполняет пересчёт.
/// Ничего не знает ни об окне (View), ни о представителе (Presenter).
/// </summary>
public class ConverterModel
{
    /// <summary>Белорусский рубль: все курсы НБРБ указаны в нём.</summary>
    public const string BaseCurrencyCode = "BYN";

    // Официальные курсы на сегодня (periodicity=0 означает ежедневные курсы)
    private const string NbrbRatesUrl = "https://api.nbrb.by/exrates/rates?periodicity=0";

    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private List<Currency> currencies = new List<Currency>();

    public ConverterModel()
    {
        LoadBuiltInRates();
    }

    /// <summary>Список валют с курсами.</summary>
    public IReadOnlyList<Currency> Currencies => currencies;

    /// <summary>Дата, на которую установлены курсы.</summary>
    public DateTime RatesDate { get; private set; }

    /// <summary>true — курсы получены с сайта НБРБ, false — встроенные в программу.</summary>
    public bool IsLoadedFromNbrb { get; private set; }

    /// <summary>Откуда взяты курсы (для строки состояния).</summary>
    public string RatesSource => IsLoadedFromNbrb ? "api.nbrb.by" : "встроенные в программу";

    // ----- Результат последнего пересчёта (состояние модели) -----

    /// <summary>Исходная сумма.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Из какой валюты переводили.</summary>
    public string FromCode { get; private set; } = string.Empty;

    /// <summary>В какую валюту переводили.</summary>
    public string ToCode { get; private set; } = string.Empty;

    /// <summary>Результат пересчёта, округлённый до копеек.</summary>
    public decimal Result { get; private set; }

    /// <summary>Сколько единиц валюты ToCode стоит 1 единица валюты FromCode.</summary>
    public decimal CrossRate { get; private set; }

    /// <summary>
    /// Пересчитывает сумму из одной валюты в другую через белорусский рубль:
    /// сумма → BYN → нужная валюта.
    /// </summary>
    public void Convert(decimal amount, string fromCode, string toCode)
    {
        Currency from = FindCurrency(fromCode);
        Currency to = FindCurrency(toCode);

        decimal amountInByn = amount * from.RatePerUnit; // сначала переводим в BYN
        decimal result = amountInByn / to.RatePerUnit;   // затем из BYN в нужную валюту

        // Обновление модели
        Amount = amount;
        FromCode = from.Code;
        ToCode = to.Code;
        Result = Math.Round(result, 2, MidpointRounding.AwayFromZero);
        CrossRate = Math.Round(from.RatePerUnit / to.RatePerUnit, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Загружает официальные курсы на сегодня с открытого API НБРБ.
    /// Если что-то пошло не так, выбрасывает исключение, а старые курсы остаются.
    /// </summary>
    public async Task LoadRatesFromNbrbAsync()
    {
        string json = await httpClient.GetStringAsync(NbrbRatesUrl);
        List<NbrbRate> nbrbRates = JsonSerializer.Deserialize<List<NbrbRate>>(json) ?? new List<NbrbRate>();

        var updated = new List<Currency>();
        DateTime date = RatesDate;

        foreach (Currency currency in currencies)
        {
            if (currency.Code == BaseCurrencyCode)
            {
                updated.Add(currency); // курс BYN к BYN всегда 1
                continue;
            }

            NbrbRate? rate = nbrbRates.FirstOrDefault(r => r.Abbreviation == currency.Code);
            if (rate == null || rate.Scale <= 0 || rate.OfficialRate == null || rate.OfficialRate <= 0)
            {
                throw new InvalidOperationException($"в ответе НБРБ нет курса {currency.Code}");
            }

            updated.Add(new Currency(currency.Code, currency.Name, rate.Scale, rate.OfficialRate.Value));
            date = rate.Date;
        }

        // Модель обновляется только когда все курсы получены успешно
        currencies = updated;
        RatesDate = date;
        IsLoadedFromNbrb = true;
    }

    /// <summary>
    /// Встроенные курсы (официальные курсы НБРБ на 19.11.2026).
    /// Используются, пока не загружены свежие курсы или если нет интернета.
    /// </summary>
    private void LoadBuiltInRates()
    {
        currencies = new List<Currency>
        {
            new Currency("BYN", "Белорусский рубль", 1, 1m),
            new Currency("USD", "Доллар США", 1, 2.9950m),
            new Currency("EUR", "Евро", 1, 3.2876m),
            new Currency("RUB", "Российский рубль", 100, 3.6533m),
            new Currency("PLN", "Польский злотый", 10, 7.4538m),
            new Currency("CNY", "Китайский юань", 10, 4.4896m)
        };
        RatesDate = new DateTime(2026, 11, 19);
        IsLoadedFromNbrb = false;
    }

    private Currency FindCurrency(string code)
    {
        Currency? currency = currencies.FirstOrDefault(c => c.Code == code);
        if (currency == null)
        {
            throw new ArgumentException($"Неизвестная валюта: {code}");
        }
        return currency;
    }
}
