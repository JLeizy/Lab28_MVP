using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CurrencyConverter.Models;
using CurrencyConverter.Views;

namespace CurrencyConverter.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ (Presenter).
/// Получает события от Вида, проверяет введённые данные,
/// передаёт их Модели и выводит обновлённые данные обратно в Вид.
/// </summary>
public class ConverterPresenter
{
    private const decimal MaxAmount = 1_000_000_000m;
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly IConverterView view;   // связь с видом
    private readonly ConverterModel model;  // связь с моделью

    private bool hasResult; // был ли уже успешный пересчёт

    public ConverterPresenter(IConverterView view, ConverterModel model)
    {
        this.view = view;
        this.model = model;

        // Подписываемся на события вида
        view.ConvertRequested += OnConvertRequested;
        view.SwapRequested += OnSwapRequested;
        view.RefreshRatesRequested += OnRefreshRatesRequested;

        // Первоначальное заполнение интерфейса данными модели
        view.SetCurrencies(model.Currencies);
        view.SelectCurrencies("USD", ConverterModel.BaseCurrencyCode);
        ShowRates();

        // При запуске пробуем получить свежие курсы. Без интернета
        // программа просто продолжит работать со встроенными курсами.
        _ = LoadRatesAsync(showErrors: false);
    }

    /// <summary>
    /// Проверка введённой суммы. Принимает и запятую, и точку: "99,50", "99.50", "1 500".
    /// </summary>
    public static bool TryParseAmount(string text, out decimal amount, out string error)
    {
        amount = 0;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Введите сумму.";
            return false;
        }

        string normalized = text.Trim()
            .Replace(" ", string.Empty)
            .Replace(" ", string.Empty) // неразрывный пробел
            .Replace(',', '.');

        if (!decimal.TryParse(normalized,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out amount))
        {
            error = "Сумма должна быть числом, например 150 или 99,50.";
            return false;
        }

        if (amount <= 0)
        {
            error = "Сумма должна быть больше нуля.";
            return false;
        }

        if (amount > MaxAmount)
        {
            error = "Слишком большая сумма: максимум 1 000 000 000.";
            return false;
        }

        if (decimal.Round(amount, 2) != amount)
        {
            error = "Укажите не больше двух знаков после запятой.";
            return false;
        }

        return true;
    }

    private void OnConvertRequested(object? sender, EventArgs e)
    {
        ConvertAmount();
    }

    private void OnSwapRequested(object? sender, EventArgs e)
    {
        string? from = view.FromCurrencyCode;
        string? to = view.ToCurrencyCode;
        if (from == null || to == null)
        {
            return;
        }

        view.SelectCurrencies(to, from);

        if (hasResult)
        {
            ConvertAmount(); // сразу показываем результат для новой пары
        }
    }

    private async void OnRefreshRatesRequested(object? sender, EventArgs e)
    {
        await LoadRatesAsync(showErrors: true);
    }

    /// <summary>Основной сценарий: Вид → Представитель → Модель → Представитель → Вид.</summary>
    private void ConvertAmount()
    {
        // 1. Получаем данные от вида и проверяем их
        if (!TryParseAmount(view.AmountText, out decimal amount, out string error))
        {
            hasResult = false;
            view.ShowError(error);
            return;
        }

        string? from = view.FromCurrencyCode;
        string? to = view.ToCurrencyCode;
        if (from == null || to == null)
        {
            hasResult = false;
            view.ShowError("Выберите обе валюты.");
            return;
        }

        // 2. Передаём проверенные данные модели, модель обновляется
        model.Convert(amount, from, to);

        // 3. Берём обновлённые данные модели и передаём их виду
        view.ShowResult(
            $"{Format(model.Amount, "N2")} {model.FromCode} = {Format(model.Result, "N2")} {model.ToCode}",
            $"1 {model.FromCode} = {Format(model.CrossRate, "N4")} {model.ToCode}");
        hasResult = true;
    }

    private async Task LoadRatesAsync(bool showErrors)
    {
        view.SetBusy(true);
        view.ShowStatus("Загрузка курсов с сайта НБРБ…");

        try
        {
            await model.LoadRatesFromNbrbAsync();
            ShowRates();

            if (hasResult)
            {
                ConvertAmount(); // пересчитываем по новым курсам
            }
        }
        catch (Exception ex)
        {
            string reason = DescribeError(ex);
            view.ShowStatus($"Курсы не обновлены ({reason}). {RatesStatusText()}");

            if (showErrors)
            {
                view.ShowError($"Не удалось загрузить курсы НБРБ: {reason}.");
                hasResult = false;
            }
        }
        finally
        {
            view.SetBusy(false);
        }
    }

    private void ShowRates()
    {
        // В таблице показываем только иностранные валюты: курс BYN всегда 1
        view.ShowRates(model.Currencies
            .Where(c => c.Code != ConverterModel.BaseCurrencyCode)
            .ToList());
        view.ShowStatus(RatesStatusText());
    }

    private string RatesStatusText()
    {
        return $"Курсы НБРБ на {model.RatesDate:dd.MM.yyyy} · источник: {model.RatesSource}";
    }

    private static string Format(decimal value, string format)
    {
        return value.ToString(format, RuCulture);
    }

    private static string DescribeError(Exception ex)
    {
        return ex switch
        {
            TaskCanceledException => "сайт НБРБ не ответил вовремя",
            HttpRequestException => "нет интернета или сайт НБРБ недоступен",
            JsonException => "сайт НБРБ вернул данные в неожиданном формате",
            _ => ex.Message
        };
    }
}
