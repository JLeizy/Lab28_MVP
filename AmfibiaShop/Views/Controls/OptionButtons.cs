using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views.Controls;

/// <summary>Набор радиокнопок-карточек (доставка, оплата) из готовых вариантов Представителя.</summary>
public static class OptionButtons
{
    public static void Build(Panel panel, IReadOnlyList<OptionData> options, string? selected, string group, Action<string> onChecked)
    {
        panel.Children.Clear();
        foreach (OptionData option in options)
        {
            var title = new TextBlock
            {
                Text = option.Title,
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.FindResource("FontSemi"),
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("Text")
            };
            var subtitle = new TextBlock
            {
                Text = option.Subtitle,
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("Muted")
            };
            var extra = new TextBlock
            {
                Text = option.Extra,
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.FindResource("FontSemi"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("Text")
            };

            var content = new DockPanel();
            DockPanel.SetDock(extra, Dock.Right);
            content.Children.Add(extra);
            var texts = new StackPanel();
            texts.Children.Add(title);
            if (option.Subtitle.Length > 0) texts.Children.Add(subtitle);
            content.Children.Add(texts);

            var button = new RadioButton
            {
                Style = (Style)Application.Current.FindResource("Radio.Option"),
                GroupName = group + panel.GetHashCode(),
                Tag = option.Key,
                Content = content,
                IsChecked = option.Key == selected
            };
            button.Checked += (_, _) => onChecked(option.Key);
            panel.Children.Add(button);
        }
    }

    /// <summary>Код выбранного варианта (null — ничего не выбрано).</summary>
    public static string? Selected(Panel panel)
    {
        foreach (object child in panel.Children)
        {
            if (child is RadioButton { IsChecked: true, Tag: string key }) return key;
        }

        return null;
    }
}
