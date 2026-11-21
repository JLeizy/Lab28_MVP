using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>МОДЕЛЬ избранного: список понравившихся товаров.</summary>
public class WishlistModel
{
    private readonly ShopData data;
    private readonly List<string> ids;

    public WishlistModel(ShopData data, IEnumerable<string>? ids = null)
    {
        this.data = data;
        this.ids = (ids ?? Enumerable.Empty<string>()).Where(id => data.FindProduct(id) != null).Distinct().ToList();
    }

    public event EventHandler? Changed;

    public string Message { get; private set; } = string.Empty;

    public int Count => ids.Count;

    public IReadOnlyList<string> Ids => ids;

    /// <summary>Товары в порядке добавления — новые сверху.</summary>
    public IReadOnlyList<Product> Items => ids.AsEnumerable().Reverse().Select(id => data.FindProduct(id)!).ToList();

    public bool Contains(string id) => ids.Contains(id);

    /// <summary>Добавляет или убирает товар. Возвращает true, если товар теперь в избранном.</summary>
    public bool Toggle(string id)
    {
        Product? product = data.FindProduct(id);
        if (product == null)
        {
            Message = "Товар не найден.";
            return false;
        }

        if (ids.Remove(id))
        {
            Message = $"«{product.Name}» удалён из избранного.";
            Changed?.Invoke(this, EventArgs.Empty);
            return false;
        }

        ids.Add(id);
        Message = $"«{product.Name}» добавлен в избранное.";
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        ids.Clear();
        Message = "Избранное очищено.";
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
