using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>Строка корзины.</summary>
public class CartLine
{
    public CartLine(Product product, string size, int quantity)
    {
        Product = product;
        Size = size;
        Quantity = quantity;
    }

    public Product Product { get; }

    public string Size { get; }

    public int Quantity { get; internal set; }

    public decimal Total => Product.Price * Quantity;
}

/// <summary>Строка корзины для сохранения в файл.</summary>
public class CartLineState
{
    public string Product { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// МОДЕЛЬ корзины: товары, количество, промокод, способ доставки и суммы.
/// Правила — как на сайте: бесплатная доставка от 3000 BYN, вещь с ресейла в одном экземпляре.
/// </summary>
public class CartModel
{
    public const int MaxQuantity = 10;

    private readonly ShopData data;
    private readonly List<CartLine> lines = new();
    private readonly DateTime? fixedToday;

    /// <param name="today">Дата для проверки промокодов (для тестов). Не указана — сегодняшняя.</param>
    public CartModel(ShopData data, DateTime? today = null)
    {
        this.data = data;
        fixedToday = today;
        Delivery = data.Delivery[0];
    }

    public event EventHandler? Changed;

    public string Message { get; private set; } = string.Empty;

    public IReadOnlyList<CartLine> Lines => lines;

    public int Count => lines.Sum(l => l.Quantity);

    public bool IsEmpty => lines.Count == 0;

    public PromoCode? AppliedPromo { get; private set; }

    public DeliveryMethod Delivery { get; private set; }

    public decimal FreeDeliveryFrom => data.FreeDeliveryFrom;

    public decimal Subtotal => lines.Sum(l => l.Total);

    public decimal Discount => AppliedPromo == null
        ? 0
        : Math.Round(Subtotal * AppliedPromo.Percent / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Сумма после скидки — от неё считается бесплатная доставка.</summary>
    public decimal AfterDiscount => Subtotal - Discount;

    public bool FreeDelivery => AfterDiscount >= FreeDeliveryFrom;

    public decimal LeftToFreeDelivery => Math.Max(0, FreeDeliveryFrom - AfterDiscount);

    public decimal DeliveryPrice => IsEmpty || FreeDelivery ? 0 : Delivery.Price;

    public decimal Total => AfterDiscount + DeliveryPrice;

    private DateTime Today => (fixedToday ?? DateTime.Today).Date;

    public bool Add(Product product, string size, int quantity = 1)
    {
        if (!product.Sizes.Contains(size))
        {
            Message = $"У товара нет размера {size}.";
            return false;
        }

        if (quantity < 1)
        {
            Message = "Количество должно быть не меньше 1.";
            return false;
        }

        if (product.Preowned && (quantity > 1 || lines.Any(l => l.Product == product)))
        {
            Message = "Вещь с ресейла есть в единственном экземпляре — больше одной положить нельзя.";
            return false;
        }

        CartLine? line = lines.FirstOrDefault(l => l.Product == product && l.Size == size);
        int newQuantity = (line?.Quantity ?? 0) + quantity;
        if (newQuantity > MaxQuantity)
        {
            Message = $"Не больше {MaxQuantity} штук одного товара одного размера.";
            return false;
        }

        if (line == null)
        {
            lines.Add(new CartLine(product, size, quantity));
        }
        else
        {
            line.Quantity = newQuantity;
        }

        Message = $"Добавлено в корзину: {product.Name}, размер {size}.";
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool ChangeQuantity(CartLine line, int delta)
    {
        int quantity = line.Quantity + delta;
        if (quantity < 1)
        {
            Message = "Чтобы убрать товар, нажмите «Удалить».";
            return false;
        }

        if (line.Product.Preowned && quantity > 1)
        {
            Message = "Вещь с ресейла есть в единственном экземпляре.";
            return false;
        }

        if (quantity > MaxQuantity)
        {
            Message = $"Не больше {MaxQuantity} штук одного товара.";
            return false;
        }

        line.Quantity = quantity;
        Message = string.Empty;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Remove(CartLine line)
    {
        if (lines.Remove(line))
        {
            Message = $"Удалено из корзины: {line.Product.Name}.";
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Clear()
    {
        lines.Clear();
        AppliedPromo = null;
        Message = "Корзина очищена.";
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool ApplyPromo(string code)
    {
        PromoCode? promo = data.Promos.FirstOrDefault(p => p.Code == code);

        if (promo == null || !promo.Active)
        {
            Message = $"Промокода {code} не существует.";
            return false;
        }

        if (Today < promo.ValidFrom.Date)
        {
            Message = $"Промокод {code} начнёт действовать {promo.ValidFrom:dd.MM.yyyy}.";
            return false;
        }

        if (promo.ValidTo.HasValue && Today > promo.ValidTo.Value.Date)
        {
            Message = $"Срок действия промокода {code} закончился {promo.ValidTo.Value:dd.MM.yyyy}.";
            return false;
        }

        AppliedPromo = promo;
        Message = $"Промокод {code} применён: скидка {promo.Percent}%.";
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void RemovePromo()
    {
        if (AppliedPromo != null)
        {
            AppliedPromo = null;
            Message = "Промокод отменён.";
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool SelectDelivery(string code)
    {
        DeliveryMethod? method = data.FindDelivery(code);
        if (method == null)
        {
            Message = "Неизвестный способ доставки.";
            return false;
        }

        Delivery = method;
        Message = string.Empty;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    // ----- сохранение -----

    public List<CartLineState> ToState() =>
        lines.Select(l => new CartLineState { Product = l.Product.Id, Size = l.Size, Quantity = l.Quantity }).ToList();

    public void Restore(IEnumerable<CartLineState>? state, string? promo, string? delivery)
    {
        lines.Clear();
        foreach (CartLineState s in state ?? Enumerable.Empty<CartLineState>())
        {
            Product? product = data.FindProduct(s.Product);
            if (product != null && product.Sizes.Contains(s.Size) && s.Quantity > 0)
            {
                lines.Add(new CartLine(product, s.Size, Math.Min(s.Quantity, product.Preowned ? 1 : MaxQuantity)));
            }
        }

        AppliedPromo = data.Promos.FirstOrDefault(p => p.Code == promo);
        Delivery = data.FindDelivery(delivery ?? string.Empty) ?? data.Delivery[0];
    }
}
