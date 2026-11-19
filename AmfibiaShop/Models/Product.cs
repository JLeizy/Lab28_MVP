namespace AmfibiaShop.Models;

/// <summary>
/// Товар каталога AMFIBIA.
/// Заполняется из файла Data/catalog.json (выгрузка каталога сайта).
/// </summary>
public class Product
{
    /// <summary>Модный дом: Balenciaga, Prada, Chrome Hearts...</summary>
    public string Brand { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Подкатегория: Кроссовки, Худи, Кольца...</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Раздел: Одежда, Обувь, Сумки...</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Тип размерной сетки: clothing, shoes, ring, volume_ml, one_size.</summary>
    public string SizeType { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Material { get; set; } = string.Empty;

    /// <summary>Мужское, Женское или Унисекс.</summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>Цена в белорусских рублях.</summary>
    public decimal Price { get; set; }

    /// <summary>Вещь с ресейла: подлинная, с экспертизой, в единственном экземпляре.</summary>
    public bool Preowned { get; set; }

    /// <summary>Состояние для таблицы каталога.</summary>
    public string Condition => Preowned ? "ресейл" : "новое";
}
