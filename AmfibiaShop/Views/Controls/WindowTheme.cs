using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AmfibiaShop.Views.Controls;

/// <summary>Тёмный заголовок окна Windows 10/11 — в тон тёмной теме приложения.</summary>
public static class WindowTheme
{
    private const int DwmUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void UseDarkTitleBar(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                int enabled = 1;
                DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
            }
            catch (DllNotFoundException)
            {
                // Старая Windows без DWM — оставляем стандартный заголовок
            }
        };
    }
}
