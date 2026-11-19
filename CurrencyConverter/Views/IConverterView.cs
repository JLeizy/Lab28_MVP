using System;
using System.Collections.Generic;
using CurrencyConverter.Models;

namespace CurrencyConverter.Views;

/// <summary>
/// Интерфейс ВИДА (View).
/// Представитель работает с окном только через этот интерфейс,
/// поэтому не зависит от конкретных элементов WPF.
/// </summary>
public interface IConverterView
{
    // ----- Данные, которые ввёл пользователь -----

    /// <summary>Текст из поля "Сумма" как есть, без проверки.</summary>
    string AmountText { get; }

    /// <summary>Код валюты "Из" (null, если не выбрана).</summary>
    string? FromCurrencyCode { get; }

    /// <summary>Код валюты "В" (null, если не выбрана).</summary>
    string? ToCurrencyCode { get; }

    // ----- События, которые Вид передаёт (делегирует) Представителю -----

    /// <summary>Нажата кнопка "Конвертировать" или Enter.</summary>
    event EventHandler? ConvertRequested;

    /// <summary>Нажата кнопка "⇄" (поменять валюты местами).</summary>
    event EventHandler? SwapRequested;

    /// <summary>Нажата кнопка "Обновить с сайта НБРБ".</summary>
    event EventHandler? RefreshRatesRequested;

    // ----- Команды, которыми Представитель обновляет интерфейс -----

    void SetCurrencies(IEnumerable<Currency> currencies);

    void SelectCurrencies(string fromCode, string toCode);

    void ShowRates(IEnumerable<Currency> rates);

    void ShowResult(string resultText, string rateText);

    void ShowError(string message);

    void ShowStatus(string text);

    void SetBusy(bool isBusy);
}
