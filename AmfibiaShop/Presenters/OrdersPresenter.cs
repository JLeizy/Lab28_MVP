using System;
using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>ПРЕДСТАВИТЕЛЬ «Мои заказы»: список, шкала статусов, отмена, возврат, повтор заказа.</summary>
public class OrdersPresenter : PagePresenter
{
    public static readonly string[] Filters = { "Все", "Активные", "Доставленные", "Отменённые и возвраты" };

    private readonly IOrdersView view;
    private int filter;
    private string? selected;

    public OrdersPresenter(IOrdersView view, ShopContext shop, INavigator navigator, string? orderNumber) : base(shop, navigator)
    {
        this.view = view;
        selected = orderNumber;

        view.OrderSelected += (_, number) => { selected = number; Render(); };
        view.FilterChanged += (_, index) => { filter = index; selected = null; Render(); };
        view.CancelRequested += OnCancelRequested;
        view.ReturnRequested += OnReturnRequested;
        view.RepeatRequested += OnRepeatRequested;
        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        Shop.Orders.Changed += OnModelChanged;
        Shop.Currency.Changed += OnModelChanged;

        view.SetFilters(Filters, filter);
        Render();
    }

    public override void Dispose()
    {
        Shop.Orders.Changed -= OnModelChanged;
        Shop.Currency.Changed -= OnModelChanged;
        base.Dispose();
    }

    public static string StatusKind(string status) => status switch
    {
        "delivered" => "done",
        "cancelled" or "returned" => "cancel",
        _ => "active"
    };

    private IReadOnlyList<Order> Filtered() =>
        Shop.Orders.ForUser(Shop.User.Email)
            .Where(o => filter switch
            {
                1 => StatusKind(o.Status) == "active",
                2 => o.Status == "delivered",
                3 => StatusKind(o.Status) == "cancel",
                _ => true
            })
            .ToList();

    private Order? Selected => selected == null ? null : Shop.Orders.Find(selected);

    private void Render()
    {
        IReadOnlyList<Order> orders = Filtered();
        if (selected == null || orders.All(o => o.Number != selected))
        {
            selected = orders.FirstOrDefault()?.Number;
        }

        view.ShowOrders(orders.Select(o => new OrderRowData(
                o.Number, Format.Date(o.Created), Shop.Orders.StatusName(o.Status), StatusKind(o.Status),
                Money(o.Total), Format.Plural(o.ItemsCount, "товар", "товара", "товаров"))).ToList(),
            selected,
            orders.Count == 0 ? "Заказов нет" : Format.Plural(orders.Count, "заказ", "заказа", "заказов"));
        view.ShowOrder(Selected == null ? null : Details(Selected));
    }

    private OrderDetailsData Details(Order o)
    {
        DeliveryMethod? delivery = Shop.Data.FindDelivery(o.Delivery);
        PaymentMethod? payment = Shop.Data.FindPayment(o.Payment);

        return new OrderDetailsData
        {
            Number = o.Number,
            Tracking = o.Tracking,
            Date = Format.DateTime(o.Created),
            Status = Shop.Orders.StatusName(o.Status),
            StatusKind = StatusKind(o.Status),
            StatusHelp = Shop.Orders.StatusHelp(o.Status),
            Steps = Steps(o, delivery?.Pickup ?? true),
            Items = o.Items.Select(i => new OrderItemData
            {
                Id = i.Product,
                Product = Shop.Catalog.Find(i.Product),
                Name = $"{i.Brand} — {i.Name}",
                Details = $"Размер {i.Size} · {i.Quantity} шт. × {Money(i.Price)}",
                Total = Money(i.Total)
            }).ToList(),
            Info = new[]
            {
                new SpecRow("Трек-номер", o.Tracking),
                new SpecRow("Получатель", $"{o.Customer}, {o.Phone}"),
                new SpecRow("Доставка", delivery?.Name ?? o.Delivery),
                new SpecRow("Адрес", $"{o.City}, {o.Address}"),
                new SpecRow("Оплата", $"{payment?.Name ?? o.Payment} · {(o.Paid ? "оплачен" : "ожидает оплаты")}"),
                new SpecRow("Промокод", o.Promo ?? "—")
            },
            Subtotal = Money(o.Subtotal),
            Discount = o.Discount > 0 ? $"−{Money(o.Discount)}" : null,
            Delivery = o.DeliveryPrice == 0 ? "Бесплатно" : Money(o.DeliveryPrice),
            Total = Money(o.Total),
            CanCancel = Shop.Orders.CanCancel(o),
            CanReturn = Shop.Orders.CanReturn(o, DateTime.Now)
        };
    }

    /// <summary>Шкала статусов: пройденные, текущий и будущие шаги.</summary>
    private IReadOnlyList<StepData> Steps(Order o, bool pickup)
    {
        string Time(string status) => o.StatusTime(status) is DateTime at ? Format.DateTime(at) : "";

        if (o.Status == "cancelled")
        {
            return o.History.Select(h => new StepData(Shop.Orders.StatusName(h.Status), Format.DateTime(h.At),
                h.Status == "cancelled" ? "cancelled" : "done")).ToList();
        }

        var steps = Shop.Orders.Flow
            .Where(s => pickup || s != "arrived")
            .Select(s => new StepData(Shop.Orders.StatusName(s), Time(s),
                s == o.Status ? "current" : o.StatusTime(s) != null || o.Status == "returned" ? "done" : "todo"))
            .ToList();

        if (o.Status == "returned")
        {
            steps.Add(new StepData(Shop.Orders.StatusName("returned"), Time("returned"), "cancelled"));
        }

        return steps;
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        if (Selected is not Order order) return;
        bool ok = Shop.Orders.Cancel(order, DateTime.Now);
        view.ShowMessage(Shop.Orders.Message, isError: !ok);
    }

    private void OnReturnRequested(object? sender, EventArgs e)
    {
        if (Selected is not Order order) return;
        bool ok = Shop.Orders.RequestReturn(order, DateTime.Now);
        view.ShowMessage(Shop.Orders.Message, isError: !ok);
    }

    /// <summary>Повторить заказ: те же товары и размеры снова в корзину (вещи с ресейла — нет).</summary>
    private void OnRepeatRequested(object? sender, EventArgs e)
    {
        if (Selected is not Order order) return;

        int added = 0, skipped = 0;
        foreach (OrderItem item in order.Items)
        {
            Product? product = Shop.Catalog.Find(item.Product);
            if (product == null || product.Preowned || !Shop.Cart.Add(product, item.Size, item.Quantity))
            {
                skipped++;
                continue;
            }

            added++;
        }

        string message = added == 0
            ? "Эти вещи нельзя заказать повторно (ресейл в единственном экземпляре или лимит количества)."
            : $"В корзину добавлено {Format.Plural(added, "позиция", "позиции", "позиций")}" + (skipped > 0 ? $", пропущено: {skipped} (ресейл или лимит)." : ".");
        view.ShowMessage(message, isError: added == 0);
        Navigator.Notify(message, isError: added == 0);
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();
}
