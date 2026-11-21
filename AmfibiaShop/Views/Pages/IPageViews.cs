using System;
using System.Collections.Generic;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views;

// Интерфейсы страниц. Каждая страница — Вид (UserControl), с которым работает свой Представитель.

/// <summary>События карточек товара, общие для страниц с сеткой товаров.</summary>
public interface IProductGridEvents
{
    event EventHandler<string>? ProductOpened;
    event EventHandler<string>? FavoriteToggled;
}

/// <summary>Главная.</summary>
public interface IHomeView : IProductGridEvents
{
    event EventHandler<string>? BrandOpened;

    /// <summary>Кнопки «Смотреть все»: new, iconic, sale, all.</summary>
    event EventHandler<string>? CollectionRequested;

    void ShowHero(string title, string subtitle, IReadOnlyList<SpecRow> stats);
    void ShowNew(IReadOnlyList<ProductCardData> items);
    void ShowIconic(IReadOnlyList<ProductCardData> items);
    void ShowSale(IReadOnlyList<ProductCardData> items);
    void ShowBrands(IReadOnlyList<BrandCardData> brands);
}

/// <summary>Каталог с фильтрами.</summary>
public interface ICatalogView : IProductGridEvents
{
    string SearchText { get; }
    string? SelectedGroup { get; }
    string? SelectedCategory { get; }
    IReadOnlyList<string> SelectedBrands { get; }
    string? SelectedGender { get; }
    IReadOnlyList<string> SelectedColors { get; }
    string MinPriceText { get; }
    string MaxPriceText { get; }
    bool OnlyNew { get; }
    bool OnlySale { get; }
    bool OnlyPreowned { get; }
    bool OnlyIconic { get; }
    int SortIndex { get; }

    /// <summary>Изменился любой фильтр или сортировка.</summary>
    event EventHandler? FiltersChanged;

    /// <summary>Выбран другой раздел — нужно обновить список категорий.</summary>
    event EventHandler? GroupChanged;

    event EventHandler? ResetRequested;

    event EventHandler<int>? PageRequested;

    void SetOptions(IReadOnlyList<OptionData> groups, IReadOnlyList<OptionData> brands, IReadOnlyList<OptionData> genders,
                    IReadOnlyList<OptionData> colors, IReadOnlyList<string> sorts);

    void SetCategories(IReadOnlyList<OptionData> categories);

    /// <summary>Выставляет фильтры (сброс или переход с другой страницы).</summary>
    void ApplyState(string search, string? group, string? category, IReadOnlyCollection<string> brands, string? gender,
                    IReadOnlyCollection<string> colors, bool onlyNew, bool onlySale, bool onlyPreowned, bool onlyIconic, int sort);

    void ShowProducts(IReadOnlyList<ProductCardData> items, string countText, int page, int pageCount);

    void ShowPriceError(string? message);
}

/// <summary>Страница товара.</summary>
public interface IProductView : IProductGridEvents
{
    string? SelectedSize { get; }
    int ReviewRating { get; }
    string ReviewText { get; }

    event EventHandler? BrandOpened;
    event EventHandler<string>? VariantOpened;
    event EventHandler<int>? QuantityChangeRequested;
    event EventHandler? AddToCartRequested;
    event EventHandler? BuyNowRequested;
    event EventHandler? FavoriteRequested;
    event EventHandler? CompareRequested;
    event EventHandler? ReviewSubmitted;

    void ShowProduct(ProductDetailsData details);
    void ShowVariants(IReadOnlyList<VariantData> variants);
    void ShowSizes(IReadOnlyList<string> sizes, string? selected, string hint);
    void ShowQuantity(string quantity, bool canDecrease, bool canIncrease);
    void SetFavorite(bool isFavorite);
    void SetCompared(bool isCompared);
    void ShowReviews(IReadOnlyList<ReviewData> reviews, string summary);
    void ShowReviewForm(bool enabled, string hint);
    void ClearReviewForm();
    void ShowSimilar(IReadOnlyList<ProductCardData> items);
    void ShowMessage(string message, bool isError);
}

/// <summary>Список брендов.</summary>
public interface IBrandsView
{
    event EventHandler<string>? BrandOpened;

    void ShowBrands(IReadOnlyList<BrandCardData> brands, string countText);
}

/// <summary>Страница бренда.</summary>
public interface IBrandView : IProductGridEvents
{
    event EventHandler? AllProductsRequested;

    void ShowBrand(BrandDetailsData brand);
    void ShowProducts(IReadOnlyList<ProductCardData> items);
}

/// <summary>Избранное.</summary>
public interface IWishlistView : IProductGridEvents
{
    event EventHandler? ClearRequested;
    event EventHandler? CatalogRequested;

    void ShowItems(IReadOnlyList<ProductCardData> items, string countText);
}

/// <summary>Сравнение.</summary>
public interface ICompareView
{
    event EventHandler<string>? ProductOpened;
    event EventHandler<string>? RemoveRequested;
    event EventHandler? ClearRequested;
    event EventHandler? CatalogRequested;

    void ShowComparison(IReadOnlyList<string> rowTitles, IReadOnlyList<CompareColumnData> columns, string countText);
}

/// <summary>Корзина.</summary>
public interface ICartView
{
    string PromoText { get; }

    event EventHandler<int>? IncreaseRequested;
    event EventHandler<int>? DecreaseRequested;
    event EventHandler<int>? RemoveRequested;
    event EventHandler<string>? ProductOpened;
    event EventHandler? ApplyPromoRequested;
    event EventHandler? RemovePromoRequested;
    event EventHandler<string>? DeliverySelected;
    event EventHandler? CheckoutRequested;
    event EventHandler? ClearRequested;
    event EventHandler? CatalogRequested;

    void ShowLines(IReadOnlyList<CartLineData> lines);
    void ShowDeliveryOptions(IReadOnlyList<OptionData> options, string selected);
    void ShowSummary(CartSummaryData summary);
    void ShowMessage(string message, bool isError);
    void ClearPromoText();
}

/// <summary>Оформление заказа в 4 шага.</summary>
public interface ICheckoutView
{
    string Customer { get; }
    string Phone { get; }
    string Email { get; }
    string? SelectedDelivery { get; }
    string City { get; }
    string Street { get; }
    string House { get; }
    string Apartment { get; }
    string? SelectedPickupPoint { get; }
    string? SelectedPayment { get; }
    string CardNumber { get; }
    string CardHolder { get; }
    string CardExpiry { get; }
    string CardCvc { get; }

    event EventHandler? NextRequested;
    event EventHandler? PreviousRequested;
    event EventHandler? DeliveryChanged;
    event EventHandler? CityChanged;
    event EventHandler? PaymentChanged;
    event EventHandler? ConfirmRequested;
    event EventHandler? OrdersRequested;
    event EventHandler? CatalogRequested;

    void ShowStep(int step, IReadOnlyList<string> titles);
    void FillRecipient(string customer, string phone, string email);
    void FillAddress(string city, string street, string house, string apartment);
    void ShowDeliveryOptions(IReadOnlyList<OptionData> options, string? selected);
    void ShowAddressMode(bool pickup, IReadOnlyList<string> pickupPoints);
    void ShowPaymentOptions(IReadOnlyList<OptionData> options, string? selected);
    void ShowCardForm(bool visible);
    void ShowSummary(CheckoutSummaryData summary);
    void ShowFieldErrors(IReadOnlyDictionary<string, string> errors);
    void ShowError(string? message);
    void ShowSuccess(OrderSuccessData success);
}

/// <summary>Мои заказы.</summary>
public interface IOrdersView
{
    event EventHandler<string>? OrderSelected;
    event EventHandler<int>? FilterChanged;
    event EventHandler? CancelRequested;
    event EventHandler? ReturnRequested;
    event EventHandler? RepeatRequested;
    event EventHandler<string>? ProductOpened;

    void SetFilters(IReadOnlyList<string> filters, int selected);
    void ShowOrders(IReadOnlyList<OrderRowData> orders, string? selectedNumber, string countText);
    void ShowOrder(OrderDetailsData? order);
    void ShowMessage(string message, bool isError);
}

/// <summary>Профиль покупателя.</summary>
public interface IProfileView
{
    string FirstName { get; }
    string LastName { get; }
    string Phone { get; }
    string City { get; }
    string Street { get; }
    string House { get; }
    string Apartment { get; }
    string Postal { get; }
    string CurrentPassword { get; }
    string NewPassword { get; }
    string RepeatPassword { get; }

    event EventHandler? SaveRequested;
    event EventHandler? ChangePasswordRequested;
    event EventHandler? LogoutRequested;

    void ShowProfile(ProfileViewData profile);
    void ShowProfileErrors(IReadOnlyDictionary<string, string> errors);
    void ShowPasswordErrors(IReadOnlyDictionary<string, string> errors);
    void ShowMessage(string message, bool isError);
    void ClearPasswords();
}
