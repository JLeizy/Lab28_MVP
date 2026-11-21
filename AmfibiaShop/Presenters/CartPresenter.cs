using System;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>ПРЕДСТАВИТЕЛЬ корзины: количество, удаление, промокод, доставка, итоги.</summary>
public class CartPresenter : PagePresenter
{
    private readonly ICartView view;

    public CartPresenter(ICartView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        this.view = view;

        view.IncreaseRequested += (_, index) => ChangeQuantity(index, +1);
        view.DecreaseRequested += (_, index) => ChangeQuantity(index, -1);
        view.RemoveRequested += OnRemoveRequested;
        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        view.ApplyPromoRequested += OnApplyPromoRequested;
        view.RemovePromoRequested += (_, _) => Shop.Cart.RemovePromo();
        view.DeliverySelected += (_, code) => Shop.Cart.SelectDelivery(code);
        view.CheckoutRequested += (_, _) => Navigator.GoCheckout();
        view.ClearRequested += (_, _) => Shop.Cart.Clear();
        view.CatalogRequested += (_, _) => Navigator.GoCatalog();
        Shop.Cart.Changed += OnModelChanged;
        Shop.Currency.Changed += OnModelChanged;

        Render();
    }

    public override void Dispose()
    {
        Shop.Cart.Changed -= OnModelChanged;
        Shop.Currency.Changed -= OnModelChanged;
        base.Dispose();
    }

    private void Render()
    {
        CartModel cart = Shop.Cart;

        view.ShowLines(cart.Lines.Select((l, i) => new CartLineData
        {
            Index = i,
            Id = l.Product.Id,
            Product = l.Product,
            Brand = l.Product.Brand,
            Name = l.Product.Name,
            Details = $"Размер {l.Size} · {l.Product.Color}",
            Quantity = l.Quantity.ToString(),
            Price = Money(l.Product.Price),
            Total = Money(l.Total),
            CanDecrease = l.Quantity > 1,
            CanIncrease = !l.Product.Preowned && l.Quantity < CartModel.MaxQuantity,
            Note = l.Product.Preowned ? "Ресейл · единственный экземпляр" : ""
        }).ToList());

        view.ShowDeliveryOptions(Shop.Data.Delivery.Select(d => new OptionData(
            d.Code,
            d.Name,
            d.Pickup ? $"Пункт выдачи · {d.DaysMin}–{d.DaysMax} дн." : $"До двери · {d.DaysMin}–{d.DaysMax} дн.",
            cart.FreeDelivery ? "Бесплатно" : Money(d.Price))).ToList(), cart.Delivery.Code);

        view.ShowSummary(new CartSummaryData
        {
            CountText = Format.Plural(cart.Count, "товар", "товара", "товаров"),
            Subtotal = Money(cart.Subtotal),
            Discount = cart.AppliedPromo == null ? null : $"−{Money(cart.Discount)}",
            Delivery = cart.IsEmpty ? "—" : cart.DeliveryPrice == 0 ? "Бесплатно" : Money(cart.DeliveryPrice),
            Total = Money(cart.Total),
            FreeDeliveryText = cart.IsEmpty
                ? $"Бесплатная доставка — от {Money(cart.FreeDeliveryFrom)}"
                : cart.FreeDelivery
                    ? "Доставка бесплатная"
                    : $"До бесплатной доставки осталось {Money(cart.LeftToFreeDelivery)}",
            FreeDeliveryProgress = (double)Math.Min(1m, cart.AfterDiscount / cart.FreeDeliveryFrom),
            PromoApplied = cart.AppliedPromo == null ? null : $"{cart.AppliedPromo.Code} · −{cart.AppliedPromo.Percent}%"
        });
    }

    private void ChangeQuantity(int index, int delta)
    {
        if (index < 0 || index >= Shop.Cart.Lines.Count) return;
        if (!Shop.Cart.ChangeQuantity(Shop.Cart.Lines[index], delta))
        {
            view.ShowMessage(Shop.Cart.Message, isError: true);
        }
    }

    private void OnRemoveRequested(object? sender, int index)
    {
        if (index < 0 || index >= Shop.Cart.Lines.Count) return;
        Shop.Cart.Remove(Shop.Cart.Lines[index]);
        Navigator.Notify(Shop.Cart.Message);
    }

    private void OnApplyPromoRequested(object? sender, EventArgs e)
    {
        string code = view.PromoText.Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            view.ShowMessage("Введите промокод.", isError: true);
            return;
        }

        bool applied = Shop.Cart.ApplyPromo(code);
        view.ShowMessage(Shop.Cart.Message, isError: !applied);
        if (applied)
        {
            view.ClearPromoText();
        }
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();
}
