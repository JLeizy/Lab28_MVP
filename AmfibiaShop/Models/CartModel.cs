using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>
/// МОДЕЛЬ корзины.
/// Добавляет и удаляет товары, применяет промокод, считает суммы.
/// </summary>
public class CartModel
{
    /// <summary>Сколько штук одного товара одного размера можно положить в корзину.</summary>
    public const int MaxQuantity = 10;

    private readonly List<CartItem> items = new List<CartItem>();

    // Промокоды сайта (db/seed.js): код, скидка %, начало и конец действия
    private readonly List<PromoCode> promoCodes = new List<PromoCode>
    {
        new PromoCode("AMFIBIA10", 10, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)),
        new PromoCode("GOTHIC15", 15, new DateTime(2026, 5, 1), new DateTime(2026, 12, 31)),
        new PromoCode("WELCOME20", 20, new DateTime(2026, 1, 1), null)
    };

    private readonly DateTime? fixedToday;

    /// <param name="today">Дата для проверки промокодов. Не указана — берётся сегодняшняя.</param>
    public CartModel(DateTime? today = null)
    {
        fixedToday = today;
    }

    public IReadOnlyList<CartItem> Items => items;

    /// <summary>Применённый промокод (null — нет).</summary>
    public PromoCode? AppliedPromo { get; private set; }

    /// <summary>Сообщение о результате последнего действия.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Сколько всего штук товаров в корзине.</summary>
    public int Count => items.Sum(i => i.Quantity);

    /// <summary>Сумма без скидки.</summary>
    public decimal Subtotal => items.Sum(i => i.Total);

    /// <summary>Скидка по промокоду, округлённая до копеек.</summary>
    public decimal Discount => AppliedPromo == null
        ? 0
        : Math.Round(Subtotal * AppliedPromo.Percent / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Итого к оплате.</summary>
    public decimal Total => Subtotal - Discount;

    private DateTime Today => fixedToday ?? DateTime.Today;

    public bool Add(Product product, string size)
    {
        // Вещь с ресейла существует в одном экземпляре
        if (product.Preowned && items.Any(i => i.Product == product))
        {
            Message = "Эта вещь с ресейла есть в единственном экземпляре и уже лежит в корзине.";
            return false;
        }

        CartItem? existing = items.FirstOrDefault(i => i.Product == product && i.Size == size);
        if (existing != null)
        {
            if (existing.Quantity >= MaxQuantity)
            {
                Message = $"Нельзя добавить больше {MaxQuantity} штук одного товара.";
                return false;
            }
            existing.Quantity++;
        }
        else
        {
            items.Add(new CartItem(product, size));
        }

        Message = $"Добавлено в корзину: {product.Name}, размер {size}.";
        return true;
    }

    public void Remove(CartItem item)
    {
        items.Remove(item);
        Message = $"Удалено из корзины: {item.Product.Name}.";
    }

    public bool ApplyPromoCode(string code)
    {
        PromoCode? promo = promoCodes.FirstOrDefault(p => p.Code == code);

        if (promo == null)
        {
            Message = $"Промокода {code} не существует.";
            return false;
        }

        if (Today < promo.ValidFrom)
        {
            Message = $"Промокод {code} начнёт действовать {promo.ValidFrom:dd.MM.yyyy}.";
            return false;
        }

        if (promo.ValidTo.HasValue && Today > promo.ValidTo.Value)
        {
            Message = $"Срок действия промокода {code} закончился {promo.ValidTo.Value:dd.MM.yyyy}.";
            return false;
        }

        AppliedPromo = promo; // обновление модели
        Message = $"Промокод {code} применён: скидка {promo.Percent}%.";
        return true;
    }
}
