using System.Collections.Generic;
using AmfibiaShop.Models;

namespace AmfibiaShop.Presenters.Display;

// Готовые к показу данные: Представитель заполняет их из Модели
// (форматирует цены, даты, статусы), а Вид только выводит.

/// <summary>Карточка товара в сетке.</summary>
public class ProductCardData
{
    public string Id { get; init; } = string.Empty;
    public Product Product { get; init; } = null!; // для рисунка товара
    public string Brand { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string? OldPrice { get; init; }
    public string? Badge { get; init; }
    public string BadgeKind { get; init; } = string.Empty; // new / sale / resale / iconic
    public bool IsFavorite { get; init; }
    public string Rating { get; init; } = string.Empty;
}

/// <summary>Плитка бренда.</summary>
public class BrandCardData
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string ProductsText { get; init; } = string.Empty;
    public string Bg { get; init; } = "#111112";
    public string Bg2 { get; init; } = "#1D1D20";
    public string Fg { get; init; } = "#EDE6DA";
    public string Accent { get; init; } = "#D9707F";
    public string FontKey { get; init; } = "serif";
}

/// <summary>Строка «название — значение».</summary>
public record SpecRow(string Label, string Value);

/// <summary>Вариант с выбором (доставка, оплата, фильтр).</summary>
public record OptionData(string Key, string Title, string Subtitle = "", string Extra = "");

/// <summary>Другой цвет той же модели.</summary>
public record VariantData(string Id, string Color, string Hex, bool IsCurrent);

/// <summary>Отзыв.</summary>
public record ReviewData(string Author, string Stars, string Date, string Text, string Note);

/// <summary>Страница товара.</summary>
public class ProductDetailsData
{
    public string Id { get; init; } = string.Empty;
    public Product Product { get; init; } = null!;
    public string Brand { get; init; } = string.Empty;
    public string BrandSlug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string? OldPrice { get; init; }
    public string? DiscountText { get; init; }
    public IReadOnlyList<string> Badges { get; init; } = new List<string>();
    public string Description { get; init; } = string.Empty;
    public string Composition { get; init; } = string.Empty;
    public string Care { get; init; } = string.Empty;
    public IReadOnlyList<SpecRow> Specs { get; init; } = new List<SpecRow>();
    public string RatingText { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
}

/// <summary>Креативный директор, событие истории, коллекция.</summary>
public record DirectorData(string Name, string Period, string Description, string KeyWorks);
public record TimelineData(string Year, string Title, string Description);
public record CollectionData(string Title, string Subtitle, bool IsNew);

/// <summary>Страница бренда.</summary>
public class BrandDetailsData
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Tagline { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<SpecRow> Facts { get; init; } = new List<SpecRow>();
    public IReadOnlyList<DirectorData> Directors { get; init; } = new List<DirectorData>();
    public IReadOnlyList<TimelineData> Timeline { get; init; } = new List<TimelineData>();
    public IReadOnlyList<CollectionData> Collections { get; init; } = new List<CollectionData>();
    public string Bg { get; init; } = "#111112";
    public string Bg2 { get; init; } = "#1D1D20";
    public string Fg { get; init; } = "#EDE6DA";
    public string Accent { get; init; } = "#D9707F";
    public string FontKey { get; init; } = "serif";
    public string ProductsText { get; init; } = string.Empty;
}

/// <summary>Столбец таблицы сравнения.</summary>
public class CompareColumnData
{
    public string Id { get; init; } = string.Empty;
    public Product Product { get; init; } = null!;
    public string Brand { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Values { get; init; } = new List<string>();
    public bool IsBestPrice { get; init; }
}

/// <summary>Строка корзины.</summary>
public class CartLineData
{
    public int Index { get; init; }
    public string Id { get; init; } = string.Empty;
    public Product Product { get; init; } = null!;
    public string Brand { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Quantity { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public bool CanIncrease { get; init; }
    public bool CanDecrease { get; init; }
    public string Note { get; init; } = string.Empty;
}

/// <summary>Итоги корзины.</summary>
public class CartSummaryData
{
    public string CountText { get; init; } = string.Empty;
    public string Subtotal { get; init; } = string.Empty;
    public string? Discount { get; init; }
    public string Delivery { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public string FreeDeliveryText { get; init; } = string.Empty;
    public double FreeDeliveryProgress { get; init; }
    public string? PromoApplied { get; init; }
}

/// <summary>Итог перед подтверждением заказа.</summary>
public class CheckoutSummaryData
{
    public IReadOnlyList<SpecRow> Rows { get; init; } = new List<SpecRow>();
    public IReadOnlyList<string> Items { get; init; } = new List<string>();
    public string Subtotal { get; init; } = string.Empty;
    public string? Discount { get; init; }
    public string Delivery { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
}

/// <summary>Сообщение об успешном заказе.</summary>
public record OrderSuccessData(string Number, string Tracking, string Total, string StatusText, string PaymentNote);

/// <summary>Заказ в списке.</summary>
public record OrderRowData(string Number, string Date, string Status, string StatusKind, string Total, string ItemsText);

/// <summary>Шаг на шкале статусов.</summary>
public record StepData(string Title, string Time, string State); // done / current / todo / cancelled

/// <summary>Товар в заказе.</summary>
public class OrderItemData
{
    public string Id { get; init; } = string.Empty;
    public Product? Product { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
}

/// <summary>Подробности заказа.</summary>
public class OrderDetailsData
{
    public string Number { get; init; } = string.Empty;
    public string Tracking { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusKind { get; init; } = string.Empty;
    public string StatusHelp { get; init; } = string.Empty;
    public IReadOnlyList<StepData> Steps { get; init; } = new List<StepData>();
    public IReadOnlyList<OrderItemData> Items { get; init; } = new List<OrderItemData>();
    public IReadOnlyList<SpecRow> Info { get; init; } = new List<SpecRow>();
    public string Subtotal { get; init; } = string.Empty;
    public string? Discount { get; init; }
    public string Delivery { get; init; } = string.Empty;
    public string Total { get; init; } = string.Empty;
    public bool CanCancel { get; init; }
    public bool CanReturn { get; init; }
}

/// <summary>Профиль и статистика покупателя.</summary>
public class ProfileViewData
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string House { get; init; } = string.Empty;
    public string Apartment { get; init; } = string.Empty;
    public string Postal { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Since { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public IReadOnlyList<SpecRow> Stats { get; init; } = new List<SpecRow>();
}
