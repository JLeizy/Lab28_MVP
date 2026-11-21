using System;
using System.Collections.Generic;

namespace AmfibiaShop.Models;

/// <summary>Состояние покупателя, которое сохраняется между запусками.</summary>
public class UserState
{
    public List<string> Wishlist { get; set; } = new();
    public List<string> Compare { get; set; } = new();
    public List<CartLineState> Cart { get; set; } = new();
    public string? Promo { get; set; }
    public string? Delivery { get; set; }
}

/// <summary>
/// Все модели магазина вместе. Общие модели создаются один раз,
/// личные (избранное, сравнение, корзина) — при входе пользователя.
/// </summary>
public class ShopContext
{
    public ShopContext(ShopData data, Storage storage)
    {
        Data = data;
        Storage = storage;
        Auth = new AuthModel(data, storage);
        Catalog = new CatalogModel(data);
        Currency = new CurrencyModel(data.Rates);
        Orders = new OrderModel(data, storage);
        Reviews = new ReviewModel(data, storage, Orders);
        Wishlist = new WishlistModel(data);
        Compare = new CompareModel(data);
        Cart = new CartModel(data);
    }

    public ShopData Data { get; }
    public Storage Storage { get; }
    public AuthModel Auth { get; }
    public CatalogModel Catalog { get; }
    public CurrencyModel Currency { get; }
    public OrderModel Orders { get; }
    public ReviewModel Reviews { get; }

    public WishlistModel Wishlist { get; private set; }
    public CompareModel Compare { get; private set; }
    public CartModel Cart { get; private set; }

    public User User => Auth.CurrentUser ?? throw new InvalidOperationException("Пользователь не вошёл в аккаунт.");

    /// <summary>Загружает личные данные вошедшего пользователя.</summary>
    public void StartSession()
    {
        UserState state = Storage.Load<UserState>(Storage.UserFile(User.Email)) ?? new UserState();

        Wishlist = new WishlistModel(Data, state.Wishlist);
        Compare = new CompareModel(Data, state.Compare);
        Cart = new CartModel(Data);
        Cart.Restore(state.Cart, state.Promo, state.Delivery);

        // Любое изменение сразу сохраняется
        Wishlist.Changed += (_, _) => SaveState();
        Compare.Changed += (_, _) => SaveState();
        Cart.Changed += (_, _) => SaveState();
    }

    public void EndSession()
    {
        if (Auth.CurrentUser != null)
        {
            SaveState();
        }

        Auth.Logout();
        Wishlist = new WishlistModel(Data);
        Compare = new CompareModel(Data);
        Cart = new CartModel(Data);
    }

    private void SaveState()
    {
        if (Auth.CurrentUser == null)
        {
            return;
        }

        Storage.Save(Storage.UserFile(User.Email), new UserState
        {
            Wishlist = new List<string>(Wishlist.Ids),
            Compare = new List<string>(Compare.Ids),
            Cart = Cart.ToState(),
            Promo = Cart.AppliedPromo?.Code,
            Delivery = Cart.Delivery.Code
        });
    }
}
