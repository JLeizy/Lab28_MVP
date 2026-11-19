using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CurrencyConverter.Models;

namespace CurrencyConverter.Views;

/// <summary>
/// ВИД (View).
/// Только показывает данные и передаёт действия пользователя Представителю.
/// Никаких расчётов и проверок здесь нет.
/// </summary>
public partial class MainWindow : Window, IConverterView
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public event EventHandler? ConvertRequested;
    public event EventHandler? SwapRequested;
    public event EventHandler? RefreshRatesRequested;

    public string AmountText => AmountTextBox.Text;

    public string? FromCurrencyCode => FromComboBox.SelectedValue as string;

    public string? ToCurrencyCode => ToComboBox.SelectedValue as string;

    // ----- Обработчики событий: перехватываем и делегируем Представителю -----

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        ConvertRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SwapButton_Click(object sender, RoutedEventArgs e)
    {
        SwapRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshRatesRequested?.Invoke(this, EventArgs.Empty);
    }

    // ----- Методы, через которые Представитель обновляет интерфейс -----

    public void SetCurrencies(IEnumerable<Currency> currencies)
    {
        List<Currency> list = currencies.ToList();
        // У каждого списка своя копия, чтобы выбор в одном не менял другой
        FromComboBox.ItemsSource = list;
        ToComboBox.ItemsSource = list.ToList();
    }

    public void SelectCurrencies(string fromCode, string toCode)
    {
        FromComboBox.SelectedValue = fromCode;
        ToComboBox.SelectedValue = toCode;
    }

    public void ShowRates(IEnumerable<Currency> rates)
    {
        RatesListView.ItemsSource = rates;
    }

    public void ShowResult(string resultText, string rateText)
    {
        SetText(ResultTextBlock, resultText);
        SetText(RateTextBlock, rateText);
        SetText(ErrorTextBlock, string.Empty);
    }

    public void ShowError(string message)
    {
        SetText(ResultTextBlock, string.Empty);
        SetText(RateTextBlock, string.Empty);
        SetText(ErrorTextBlock, message);
    }

    public void ShowStatus(string text)
    {
        StatusTextBlock.Text = text;
    }

    public void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;

        if (isBusy)
        {
            Cursor = Cursors.AppStarting;
        }
        else
        {
            ClearValue(CursorProperty);
        }
    }

    private static void SetText(TextBlock textBlock, string text)
    {
        textBlock.Text = text;
        textBlock.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }
}
