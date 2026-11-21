using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AmfibiaShop.Models;

/// <summary>Данные оформления заказа (формат уже проверен Представителем).</summary>
public class CheckoutRequest
{
    public string Customer { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DeliveryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string House { get; set; } = string.Empty;
    public string Apartment { get; set; } = string.Empty;
    public string PickupPoint { get; set; } = string.Empty;
    public string PaymentCode { get; set; } = string.Empty;
}

/// <summary>
/// МОДЕЛЬ заказов: оформление, история, статусы, отмена и возврат.
/// Статусы и их порядок — как в orderService.js сайта.
/// </summary>
public class OrderModel
{
    public const int ReturnDays = 14;
    private const string FileName = "orders.json";
    private static readonly int[] TrackingWeights = { 8, 6, 4, 2, 3, 5, 9, 7 };

    private readonly ShopData data;
    private readonly Storage storage;
    private readonly List<Order> orders;
    private readonly Random random = new();

    public OrderModel(ShopData data, Storage storage)
    {
        this.data = data;
        this.storage = storage;
        orders = storage.Load<List<Order>>(FileName) ?? data.Orders;
    }

    public event EventHandler? Changed;

    public string Message { get; private set; } = string.Empty;

    public IReadOnlyList<string> Flow => data.Flow;

    public IReadOnlyList<Order> All => orders;

    public IReadOnlyList<Order> ForUser(string email) =>
        orders.Where(o => string.Equals(o.User, email, StringComparison.OrdinalIgnoreCase))
              .OrderByDescending(o => o.Created)
              .ToList();

    public Order? Find(string number) => orders.FirstOrDefault(o => o.Number == number);

    public string StatusName(string code) => data.StatusName(code);

    public string StatusHelp(string code) => data.StatusHelp(code);

    /// <summary>Самовывоз: отдельные пункты выдачи для каждой службы и города.</summary>
    public IReadOnlyList<string> PickupPoints(string deliveryCode, string city)
    {
        DeliveryMethod? method = data.FindDelivery(deliveryCode);
        if (method == null || !method.Pickup || string.IsNullOrWhiteSpace(city))
        {
            return Array.Empty<string>();
        }

        string c = city.Trim();
        return new[]
        {
            $"{method.Name}, {c} — отделение №1, центр города",
            $"{method.Name}, {c} — отделение №7, ТЦ «Галерея»",
            $"{method.Name}, {c} — постамат у метро, круглосуточно"
        };
    }

    public bool CanCancel(Order order) => order.Status is "created" or "confirmed";

    public bool CanReturn(Order order, DateTime now) =>
        order.Status == "delivered" && order.StatusTime("delivered") is DateTime at && (now - at).TotalDays <= ReturnDays;

    public Order? PlaceOrder(CheckoutRequest request, CartModel cart, User user, DateTime now)
    {
        if (cart.IsEmpty)
        {
            Message = "Корзина пуста.";
            return null;
        }

        DeliveryMethod? delivery = data.FindDelivery(request.DeliveryCode);
        PaymentMethod? payment = data.FindPayment(request.PaymentCode);
        if (delivery == null || payment == null)
        {
            Message = "Выберите способ доставки и оплаты.";
            return null;
        }

        if (payment.Code == "cash" && delivery.Pickup)
        {
            Message = "Оплата наличными возможна только при доставке курьером.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            Message = "Укажите город.";
            return null;
        }

        if (!delivery.Pickup && (string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.House)))
        {
            Message = "Для курьерской доставки укажите улицу и дом.";
            return null;
        }

        if (delivery.Pickup && string.IsNullOrWhiteSpace(request.PickupPoint))
        {
            Message = "Выберите пункт выдачи.";
            return null;
        }

        cart.SelectDelivery(delivery.Code);

        // Оплата картой проходит сразу, наличные — при получении: заказ подтверждается.
        // ЕРИП ждёт оплаты — заказ остаётся «Оформлен».
        bool confirmed = payment.Code != "erip";
        var order = new Order
        {
            Number = NextNumber(now),
            Tracking = MakeTracking(random.Next(0, 100_000_000)),
            User = user.Email,
            Customer = request.Customer,
            Phone = request.Phone,
            City = request.City.Trim(),
            Address = delivery.Pickup
                ? request.PickupPoint
                : $"{request.Street.Trim()}, д. {request.House.Trim()}" + (string.IsNullOrWhiteSpace(request.Apartment) ? "" : $", кв. {request.Apartment.Trim()}"),
            Created = now,
            Status = confirmed ? "confirmed" : "created",
            Delivery = delivery.Code,
            Payment = payment.Code,
            Paid = payment.Code == "card",
            Promo = cart.AppliedPromo?.Code,
            Items = cart.Lines.Select(l => new OrderItem
            {
                Product = l.Product.Id, Name = l.Product.Name, Brand = l.Product.Brand,
                Size = l.Size, Quantity = l.Quantity, Price = l.Product.Price
            }).ToList(),
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            DeliveryPrice = cart.DeliveryPrice,
            Total = cart.Total,
            History = new List<StatusChange> { new() { Status = "created", At = now } }
        };
        if (confirmed)
        {
            order.History.Add(new StatusChange { Status = "confirmed", At = now.AddMinutes(1) });
        }

        orders.Add(order);
        cart.Clear();
        Save();

        Message = $"Заказ {order.Number} оформлен.";
        Changed?.Invoke(this, EventArgs.Empty);
        return order;
    }

    public bool Cancel(Order order, DateTime now)
    {
        if (!CanCancel(order))
        {
            Message = "Отменить можно только заказ, который ещё не начали собирать.";
            return false;
        }

        SetStatus(order, "cancelled", now);
        Message = $"Заказ {order.Number} отменён. Резерв снят, оплата вернётся тем же способом.";
        return true;
    }

    public bool RequestReturn(Order order, DateTime now)
    {
        if (!CanReturn(order, now))
        {
            Message = $"Вернуть можно только доставленный заказ в течение {ReturnDays} дней.";
            return false;
        }

        SetStatus(order, "returned", now);
        Message = $"Возврат по заказу {order.Number} оформлен.";
        return true;
    }

    /// <summary>Покупал ли пользователь этот товар и получил ли его (для отзывов).</summary>
    public bool HasDelivered(string email, string productId) =>
        orders.Any(o => string.Equals(o.User, email, StringComparison.OrdinalIgnoreCase)
                        && o.Status is "delivered" or "returned"
                        && o.Items.Any(i => i.Product == productId));

    /// <summary>Трек-номер в формате UPU S10, как на сайте: AF + 8 цифр + контрольная + BY.</summary>
    public static string MakeTracking(int serial)
    {
        string digits = (serial % 100_000_000).ToString("D8");
        return $"AF{digits}{CheckDigit(digits)}BY";
    }

    public static bool IsValidTracking(string value)
    {
        Match m = Regex.Match(value.Trim().ToUpperInvariant(), @"^AF(\d{8})(\d)BY$");
        return m.Success && CheckDigit(m.Groups[1].Value) == m.Groups[2].Value[0] - '0';
    }

    private static int CheckDigit(string digits8)
    {
        int sum = 0;
        for (int i = 0; i < 8; i++) sum += (digits8[i] - '0') * TrackingWeights[i];
        int check = 11 - sum % 11;
        return check == 10 ? 0 : check == 11 ? 5 : check;
    }

    private string NextNumber(DateTime now)
    {
        int max = orders
            .Select(o => o.Number.Split('-'))
            .Where(p => p.Length == 3 && int.TryParse(p[2], out _))
            .Select(p => int.Parse(p[2]))
            .DefaultIfEmpty(0)
            .Max();
        return $"AMF-{now.Year}-{max + 1:D6}";
    }

    private void SetStatus(Order order, string status, DateTime now)
    {
        order.Status = status;
        order.History.Add(new StatusChange { Status = status, At = now });
        Save();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save() => storage.Save(FileName, orders);
}
