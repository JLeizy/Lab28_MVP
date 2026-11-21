using System;
using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ оформления заказа: 4 шага, как на сайте.
/// На каждом шаге проверяет поля и пускает дальше только с правильными данными.
/// </summary>
public class CheckoutPresenter : PagePresenter
{
    public static readonly string[] StepTitles = { "Получатель", "Доставка", "Оплата", "Подтверждение" };

    private readonly ICheckoutView view;
    private int step = 1;
    private bool finished;

    public CheckoutPresenter(ICheckoutView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        this.view = view;

        view.NextRequested += OnNextRequested;
        view.PreviousRequested += OnPreviousRequested;
        view.DeliveryChanged += (_, _) => ShowDeliveryState();
        view.CityChanged += (_, _) => ShowDeliveryState();
        view.PaymentChanged += (_, _) => view.ShowCardForm(view.SelectedPayment == "card");
        view.ConfirmRequested += OnConfirmRequested;
        view.OrdersRequested += (_, _) => Navigator.GoOrders(LastOrderNumber);
        view.CatalogRequested += (_, _) => Navigator.GoCatalog();
        Shop.Currency.Changed += OnCurrencyChanged;

        User user = Shop.User;
        view.FillRecipient(user.FullName, user.Phone, user.Email);
        view.FillAddress(user.City, user.Street, user.House, user.Apartment);
        view.ShowDeliveryOptions(DeliveryOptions(), Shop.Cart.Delivery.Code);
        view.ShowPaymentOptions(PaymentOptions(), "card");
        view.ShowCardForm(true);
        ShowDeliveryState();
        view.ShowStep(step, StepTitles);
    }

    public int Step => step;

    public string? LastOrderNumber { get; private set; }

    public override void Dispose()
    {
        Shop.Currency.Changed -= OnCurrencyChanged;
        base.Dispose();
    }

    private List<OptionData> DeliveryOptions() =>
        Shop.Data.Delivery.Select(d => new OptionData(
            d.Code, d.Name,
            d.Pickup ? $"Пункт выдачи · {d.DaysMin}–{d.DaysMax} дн." : $"Курьер до двери · {d.DaysMin}–{d.DaysMax} дн.",
            Shop.Cart.FreeDelivery ? "Бесплатно" : Money(d.Price))).ToList();

    private List<OptionData> PaymentOptions() => new()
    {
        new OptionData("card", "Банковская карта", "Оплата сразу, заказ подтверждается автоматически"),
        new OptionData("erip", "ЕРИП", "Оплата в течение суток через «Расчёт» (ЕРИП)"),
        new OptionData("cash", "Наличными при получении", "Только при доставке курьером")
    };

    private void ShowDeliveryState()
    {
        DeliveryMethod? method = view.SelectedDelivery == null ? null : Shop.Data.FindDelivery(view.SelectedDelivery);
        bool pickup = method?.Pickup ?? false;
        view.ShowAddressMode(pickup, pickup ? Shop.Orders.PickupPoints(method!.Code, view.City) : Array.Empty<string>());
    }

    /// <summary>Проверка полей текущего шага. Пустой словарь — можно идти дальше.</summary>
    public Dictionary<string, string> ValidateStep(int number)
    {
        var errors = new Dictionary<string, string>();
        void Add(string field, string? error)
        {
            if (error != null) errors[field] = error;
        }

        switch (number)
        {
            case 1:
                string customer = view.Customer.Trim();
                Add("Customer", customer.Length == 0 ? "Введите имя и фамилию получателя."
                    : !customer.Contains(' ') ? "Укажите и имя, и фамилию." : null);
                Add("Phone", Validation.Phone(view.Phone));
                Add("Email", Validation.Email(view.Email.Trim()));
                break;

            case 2:
                DeliveryMethod? method = view.SelectedDelivery == null ? null : Shop.Data.FindDelivery(view.SelectedDelivery);
                Add("Delivery", method == null ? "Выберите способ доставки." : null);
                Add("City", Validation.Required(view.City, "Укажите город."));
                if (method is { Pickup: false })
                {
                    Add("Street", Validation.Required(view.Street, "Укажите улицу."));
                    Add("House", Validation.Required(view.House, "Укажите номер дома."));
                }
                else if (method is { Pickup: true })
                {
                    Add("PickupPoint", view.SelectedPickupPoint == null ? "Выберите пункт выдачи." : null);
                }
                break;

            case 3:
                string? payment = view.SelectedPayment;
                bool pickupDelivery = Shop.Data.FindDelivery(view.SelectedDelivery ?? "")?.Pickup ?? false;
                Add("Payment", payment == null ? "Выберите способ оплаты."
                    : payment == "cash" && pickupDelivery ? "Наличными можно оплатить только курьеру. Выберите карту или ЕРИП." : null);
                if (payment == "card")
                {
                    Add("CardNumber", Validation.CardNumber(view.CardNumber));
                    Add("CardHolder", Validation.CardHolder(view.CardHolder));
                    Add("CardExpiry", Validation.CardExpiry(view.CardExpiry, DateTime.Today));
                    Add("CardCvc", Validation.CardCvc(view.CardCvc));
                }
                break;
        }

        return errors;
    }

    private void OnNextRequested(object? sender, EventArgs e)
    {
        if (finished || step >= StepTitles.Length) return;

        Dictionary<string, string> errors = ValidateStep(step);
        view.ShowFieldErrors(errors);
        if (errors.Count > 0)
        {
            view.ShowError("Проверьте отмеченные поля.");
            return;
        }

        view.ShowError(null);
        step++;
        if (step == StepTitles.Length)
        {
            if (view.SelectedDelivery != null) Shop.Cart.SelectDelivery(view.SelectedDelivery);
            view.ShowSummary(Summary());
        }

        view.ShowStep(step, StepTitles);
    }

    private void OnPreviousRequested(object? sender, EventArgs e)
    {
        if (finished || step <= 1) return;
        step--;
        view.ShowError(null);
        view.ShowStep(step, StepTitles);
    }

    private CheckoutRequest Request() => new()
    {
        Customer = view.Customer.Trim(),
        Phone = Validation.NormalizePhone(view.Phone),
        Email = view.Email.Trim(),
        DeliveryCode = view.SelectedDelivery ?? string.Empty,
        City = view.City.Trim(),
        Street = view.Street.Trim(),
        House = view.House.Trim(),
        Apartment = view.Apartment.Trim(),
        PickupPoint = view.SelectedPickupPoint ?? string.Empty,
        PaymentCode = view.SelectedPayment ?? string.Empty
    };

    private CheckoutSummaryData Summary()
    {
        CartModel cart = Shop.Cart;
        CheckoutRequest r = Request();
        DeliveryMethod? delivery = Shop.Data.FindDelivery(r.DeliveryCode);
        PaymentMethod? payment = Shop.Data.FindPayment(r.PaymentCode);
        string digits = new string(view.CardNumber.Where(char.IsDigit).ToArray());

        return new CheckoutSummaryData
        {
            Rows = new[]
            {
                new SpecRow("Получатель", $"{r.Customer}, {r.Phone}"),
                new SpecRow("Email", r.Email),
                new SpecRow("Доставка", $"{delivery?.Name} · {delivery?.DaysMin}–{delivery?.DaysMax} дн."),
                new SpecRow("Адрес", delivery?.Pickup == true ? r.PickupPoint : $"{r.City}, {r.Street}, д. {r.House}" + (r.Apartment.Length > 0 ? $", кв. {r.Apartment}" : "")),
                new SpecRow("Оплата", payment?.Code == "card" && digits.Length >= 4 ? $"{payment.Name} •••• {digits[^4..]}" : payment?.Name ?? "")
            },
            Items = cart.Lines.Select(l => $"{l.Product.Brand} — {l.Product.Name}, размер {l.Size} × {l.Quantity} = {Money(l.Total)}").ToList(),
            Subtotal = Money(cart.Subtotal),
            Discount = cart.AppliedPromo == null ? null : $"−{Money(cart.Discount)} ({cart.AppliedPromo.Code})",
            Delivery = cart.DeliveryPrice == 0 ? "Бесплатно" : Money(cart.DeliveryPrice),
            Total = Money(cart.Total)
        };
    }

    private void OnConfirmRequested(object? sender, EventArgs e)
    {
        if (finished || step != StepTitles.Length) return;

        Order? order = Shop.Orders.PlaceOrder(Request(), Shop.Cart, Shop.User, DateTime.Now);
        if (order == null)
        {
            view.ShowError(Shop.Orders.Message);
            return;
        }

        finished = true;
        LastOrderNumber = order.Number;
        string digits = new string(view.CardNumber.Where(char.IsDigit).ToArray());
        string note = order.Payment switch
        {
            "card" => $"Оплачено картой •••• {(digits.Length >= 4 ? digits[^4..] : "")}. Заказ подтверждён и передан на склад.",
            "erip" => $"Оплатите заказ через ЕРИП: «Интернет-магазины» → AMFIBIA, номер {order.Number}.",
            _ => "Оплата наличными курьеру при получении. Заказ подтверждён."
        };
        view.ShowSuccess(new OrderSuccessData(order.Number, order.Tracking, Money(order.Total), Shop.Orders.StatusName(order.Status), note));
        Navigator.Notify(Shop.Orders.Message);
    }

    private void OnCurrencyChanged(object? sender, EventArgs e)
    {
        view.ShowDeliveryOptions(DeliveryOptions(), view.SelectedDelivery);
        if (step == StepTitles.Length && !finished)
        {
            view.ShowSummary(Summary());
        }
    }
}
