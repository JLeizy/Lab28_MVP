using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД оформления заказа: 4 шага и экран «Заказ оформлен».</summary>
public partial class CheckoutView : UserControl, ICheckoutView
{
    private readonly Dictionary<string, TextBlock> errorBlocks;
    private readonly StackPanel[] steps;

    public CheckoutView()
    {
        InitializeComponent();
        steps = new[] { Step1, Step2, Step3, Step4 };
        errorBlocks = new Dictionary<string, TextBlock>
        {
            ["Customer"] = CustomerError, ["Phone"] = PhoneError, ["Email"] = EmailError,
            ["Delivery"] = DeliveryError, ["City"] = CityError, ["Street"] = StreetError, ["House"] = HouseError,
            ["PickupPoint"] = PickupPointError, ["Payment"] = PaymentError, ["CardNumber"] = CardNumberError,
            ["CardHolder"] = CardHolderError, ["CardExpiry"] = CardExpiryError, ["CardCvc"] = CardCvcError
        };
    }

    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? DeliveryChanged;
    public event EventHandler? CityChanged;
    public event EventHandler? PaymentChanged;
    public event EventHandler? ConfirmRequested;
    public event EventHandler? OrdersRequested;
    public event EventHandler? CatalogRequested;

    public string Customer => CustomerBox.Text;
    public string Phone => PhoneBox.Text;
    public string Email => EmailBox.Text;
    public string? SelectedDelivery => OptionButtons.Selected(DeliveryPanel);
    public string City => CityBox.Text;
    public string Street => StreetBox.Text;
    public string House => HouseBox.Text;
    public string Apartment => ApartmentBox.Text;
    public string? SelectedPickupPoint => PickupBox.SelectedItem as string;
    public string? SelectedPayment => OptionButtons.Selected(PaymentPanel);
    public string CardNumber => CardNumberBox.Text;
    public string CardHolder => CardHolderBox.Text;
    public string CardExpiry => CardExpiryBox.Text;
    public string CardCvc => CardCvcBox.Text;

    private void NextButton_Click(object sender, RoutedEventArgs e) => NextRequested?.Invoke(this, EventArgs.Empty);
    private void PrevButton_Click(object sender, RoutedEventArgs e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => ConfirmRequested?.Invoke(this, EventArgs.Empty);
    private void OrdersButton_Click(object sender, RoutedEventArgs e) => OrdersRequested?.Invoke(this, EventArgs.Empty);
    private void CatalogButton_Click(object sender, RoutedEventArgs e) => CatalogRequested?.Invoke(this, EventArgs.Empty);
    private void CityBox_TextChanged(object sender, TextChangedEventArgs e) => CityChanged?.Invoke(this, EventArgs.Empty);

    public void ShowStep(int step, IReadOnlyList<string> titles)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].Visibility = i + 1 == step ? Visibility.Visible : Visibility.Collapsed;
        }

        PrevButton.Visibility = step > 1 ? Visibility.Visible : Visibility.Hidden;
        NextButton.Visibility = step < titles.Count ? Visibility.Visible : Visibility.Collapsed;
        ConfirmButton.Visibility = step == titles.Count ? Visibility.Visible : Visibility.Collapsed;
        BuildStepper(step, titles);
    }

    /// <summary>Шкала шагов: пройденные — с галочкой, текущий — светлый, следующие — приглушённые.</summary>
    private void BuildStepper(int step, IReadOnlyList<string> titles)
    {
        StepsPanel.Children.Clear();
        for (int i = 1; i <= titles.Count; i++)
        {
            bool done = i < step, current = i == step;
            var circle = new Border
            {
                Width = 30, Height = 30, CornerRadius = new CornerRadius(15),
                Background = (Brush)FindResource(done ? "Accent" : current ? "Text" : "Surface3"),
                Child = new TextBlock
                {
                    Text = done ? "✓" : i.ToString(),
                    Foreground = (Brush)FindResource(done || current ? "Bg" : "Muted"),
                    FontFamily = (FontFamily)FindResource("FontSemi"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            var title = new TextBlock
            {
                Text = titles[i - 1], Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)FindResource(current ? "FontSemi" : "FontBody"),
                Foreground = (Brush)FindResource(current || done ? "Text" : "Muted")
            };
            var item = new StackPanel { Orientation = Orientation.Horizontal };
            item.Children.Add(circle);
            item.Children.Add(title);
            StepsPanel.Children.Add(item);
            if (i < titles.Count)
            {
                StepsPanel.Children.Add(new Border
                {
                    Width = 64, Height = 1, Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center,
                    Background = (Brush)FindResource(done ? "Accent" : "LineStrong")
                });
            }
        }
    }

    public void FillRecipient(string customer, string phone, string email)
    {
        CustomerBox.Text = customer;
        PhoneBox.Text = phone;
        EmailBox.Text = email;
    }

    public void FillAddress(string city, string street, string house, string apartment)
    {
        CityBox.Text = city;
        StreetBox.Text = street;
        HouseBox.Text = house;
        ApartmentBox.Text = apartment;
    }

    public void ShowDeliveryOptions(IReadOnlyList<OptionData> options, string? selected) =>
        OptionButtons.Build(DeliveryPanel, options, selected, "CheckoutDelivery", _ => DeliveryChanged?.Invoke(this, EventArgs.Empty));

    public void ShowAddressMode(bool pickup, IReadOnlyList<string> pickupPoints)
    {
        CourierPanel.Visibility = pickup ? Visibility.Collapsed : Visibility.Visible;
        PickupPanel.Visibility = pickup ? Visibility.Visible : Visibility.Collapsed;
        object? previous = PickupBox.SelectedItem;
        PickupBox.ItemsSource = pickupPoints;
        PickupBox.SelectedItem = previous is string p && pickupPoints.Contains(p) ? p : null;
    }

    public void ShowPaymentOptions(IReadOnlyList<OptionData> options, string? selected) =>
        OptionButtons.Build(PaymentPanel, options, selected, "CheckoutPayment", _ => PaymentChanged?.Invoke(this, EventArgs.Empty));

    public void ShowCardForm(bool visible) => CardPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    public void ShowSummary(CheckoutSummaryData summary)
    {
        SummaryRows.ItemsSource = summary.Rows;
        SummaryItems.ItemsSource = summary.Items;
        SumSubtotal.Text = summary.Subtotal;
        SumDiscount.Text = summary.Discount ?? "—";
        SumDelivery.Text = summary.Delivery;
        SumTotal.Text = summary.Total;
    }

    public void ShowFieldErrors(IReadOnlyDictionary<string, string> errors)
    {
        foreach (var (field, block) in errorBlocks)
        {
            block.Text = errors.TryGetValue(field, out string? message) ? message : string.Empty;
        }
    }

    public void ShowError(string? message)
    {
        ErrorText.Text = message ?? string.Empty;
        ErrorPanel.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ShowSuccess(OrderSuccessData success)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        SuccessPanel.Visibility = Visibility.Visible;
        SuccessNumber.Text = $"Заказ {success.Number} · {success.StatusText}";
        SuccessDetails.Text = $"Сумма {success.Total}. Трек-номер {success.Tracking} — по нему можно следить за доставкой.";
        SuccessNote.Text = success.PaymentNote;
    }
}
