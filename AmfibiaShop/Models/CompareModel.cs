using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>
/// МОДЕЛЬ сравнения товаров. Как на сайте, сравнивать можно не больше 4 вещей
/// (в базе сайта это ограничение проверяет триггер check_comparisons_limit).
/// </summary>
public class CompareModel
{
    public const int MaxItems = 4;

    private readonly ShopData data;
    private readonly List<string> ids;

    public CompareModel(ShopData data, IEnumerable<string>? ids = null)
    {
        this.data = data;
        this.ids = (ids ?? Enumerable.Empty<string>()).Where(id => data.FindProduct(id) != null).Distinct().Take(MaxItems).ToList();
    }

    public event EventHandler? Changed;

    public string Message { get; private set; } = string.Empty;

    public int Count => ids.Count;

    public IReadOnlyList<string> Ids => ids;

    public IReadOnlyList<Product> Items => ids.Select(id => data.FindProduct(id)!).ToList();

    public bool Contains(string id) => ids.Contains(id);

    public bool Add(string id)
    {
        Product? product = data.FindProduct(id);
        if (product == null)
        {
            Message = "Товар не найден.";
            return false;
        }

        if (ids.Contains(id))
        {
            Message = "Этот товар уже в сравнении.";
            return false;
        }

        if (ids.Count >= MaxItems)
        {
            Message = $"Сравнивать можно не больше {MaxItems} товаров. Уберите один из списка сравнения.";
            return false;
        }

        ids.Add(id);
        Message = $"«{product.Name}» добавлен к сравнению.";
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Remove(string id)
    {
        if (ids.Remove(id))
        {
            Message = "Товар убран из сравнения.";
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Clear()
    {
        ids.Clear();
        Message = "Сравнение очищено.";
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
