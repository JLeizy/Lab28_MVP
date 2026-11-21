using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД корзины.</summary>
public partial class CartView : UserControl, ICartView
{
    public CartView()
    {
        InitializeComponent();
    }

    public event EventHandler<int>? IncreaseRequested;
    public event EventHandler<int>? DecreaseRequested;
    public event EventHandler<int>? RemoveRequested;
    public event EventHandler<string>? ProductOpened;
    public event EventHandler? ApplyPromoRequested;
    public event EventHandler? RemovePromoRequested;
    public event EventHandler<string>? DeliverySelected;
    public event EventHandler? CheckoutRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? CatalogRequested;

    public string PromoText => PromoBox.Text;

    private static int Index(object sender) => (int)((Button)sender).Tag;

    private void Increase_Click(object sender, RoutedEventArgs e) => IncreaseRequested?.Invoke(this, Index(sender));
    private void Decrease_Click(object sender, RoutedEventArgs e) => DecreaseRequested?.Invoke(this, Index(sender));
    private void Remove_Click(object sender, RoutedEventArgs e) => RemoveRequested?.Invoke(this, Index(sender));
    private void Open_Click(object sender, RoutedEventArgs e) => ProductOpened?.Invoke(this, (string)((Button)sender).Tag);
    private void ApplyPromo_Click(object sender, RoutedEventArgs e) => ApplyPromoRequested?.Invoke(this, EventArgs.Empty);
    private void RemovePromo_Click(object sender, RoutedEventArgs e) => RemovePromoRequested?.Invoke(this, EventArgs.Empty);
    private void Checkout_Click(object sender, RoutedEventArgs e) => CheckoutRequested?.Invoke(this, EventArgs.Empty);
    private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearRequested?.Invoke(this, EventArgs.Empty);
    private void Catalog_Click(object sender, RoutedEventArgs e) => CatalogRequested?.Invoke(this, EventArgs.Empty);

    private void PromoBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ApplyPromoRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowLines(IReadOnlyList<CartLineData> lines)
    {
        LineItems.ItemsSource = lines;
        bool empty = lines.Count == 0;
        EmptyPanel.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        CheckoutButton.IsEnabled = !empty;
    }

    public void ShowDeliveryOptions(IReadOnlyList<OptionData> options, string selected) =>
        OptionButtons.Build(DeliveryPanel, options, selected, "CartDelivery", code => DeliverySelected?.Invoke(this, code));

    public void ShowSummary(CartSummaryData summary)
    {
        CountText.Text = summary.CountText;
        SummaryCount.Text = $"Товары ({summary.CountText})";
        SubtotalText.Text = summary.Subtotal;
        DiscountText.Text = summary.Discount ?? "—";
        DeliveryText.Text = summary.Delivery;
        TotalText.Text = summary.Total;
        FreeDeliveryBar.Value = summary.FreeDeliveryProgress;
        FreeDeliveryText.Text = summary.FreeDeliveryText;
        PromoAppliedPanel.Visibility = summary.PromoApplied == null ? Visibility.Collapsed : Visibility.Visible;
        PromoAppliedText.Text = summary.PromoApplied == null ? string.Empty : $"Промокод {summary.PromoApplied}";
    }

    public void ShowMessage(string message, bool isError)
    {
        MessageText.Text = message;
        MessageText.Foreground = (Brush)FindResource(isError ? "Error" : "Success");
        MessageText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ClearPromoText() => PromoBox.Text = string.Empty;
}
