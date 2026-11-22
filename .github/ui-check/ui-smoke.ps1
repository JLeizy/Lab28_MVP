# Проверка окон AmfibiaShop через UI Automation: программа запускается и проходит сценарий покупателя
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class Mouse {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  public static void Click(int x, int y) { SetCursorPos(x, y); System.Threading.Thread.Sleep(80); mouse_event(2,0,0,0,UIntPtr.Zero); mouse_event(4,0,0,0,UIntPtr.Zero); }
}
"@

$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$script:fails = 0
$shots = Join-Path (Get-Location) 'shots'
New-Item -ItemType Directory -Force $shots | Out-Null
$nb = [char]0x00A0

function Check([string]$name, [bool]$ok, [string]$details = '') {
    if ($ok) { Write-Host "OK   $name  ->  $details" } else { Write-Host "FAIL $name  ->  $details"; $script:fails++ }
}
function Shot([string]$name) {
    Start-Sleep -Milliseconds 400
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $bmp.Save((Join-Path $shots "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}
function Cond($prop, $value) { New-Object System.Windows.Automation.PropertyCondition($prop, $value) }
function FindWindow([string]$title) { $AE::RootElement.FindFirst($TS::Children, (Cond $AE::NameProperty $title)) }
function WaitWindow([string]$title, [int]$seconds = 40) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $seconds) {
        if ($script:proc.HasExited) { throw "Программа завершилась с кодом $($script:proc.ExitCode)" }
        $w = FindWindow $title
        if ($w) { return $w }
        Start-Sleep -Milliseconds 300
    }
    throw "Окно '$title' не появилось"
}
function Wait([scriptblock]$cond, [int]$seconds = 10) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $seconds) { try { if (& $cond) { return $true } } catch {} ; Start-Sleep -Milliseconds 250 }
    return $false
}
function El($parent, [string]$id) {
    $e = $null
    $ok = Wait { $script:tmp = $parent.FindFirst($TS::Descendants, (Cond $AE::AutomationIdProperty $id)); $script:tmp -ne $null } 6
    if (-not $ok) { throw "Элемент $id не найден" }
    return $script:tmp
}
function ByName($parent, [string]$name) {
    $ok = Wait { $script:tmp = $parent.FindFirst($TS::Descendants, (Cond $AE::NameProperty $name)); $script:tmp -ne $null } 6
    if (-not $ok) { throw "Элемент с текстом '$name' не найден" }
    return $script:tmp
}
function TextOf($parent, [string]$id) { (El $parent $id).Current.Name.Replace($nb, ' ') }
function SetValue($el, [string]$text) { $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($text); Start-Sleep -Milliseconds 300 }
function Press($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 700 }
function SelectItem($item) { $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 700 }
function Toggle($el) { $el.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle(); Start-Sleep -Milliseconds 300 }
function ClickEl($el) {
    $r = $el.Current.BoundingRectangle
    [Mouse]::Click([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2))
    Start-Sleep -Milliseconds 900
}
function Items($el, $type) { $el.FindAll($TS::Descendants, (Cond $AE::ControlTypeProperty $type)) }
function ChooseInCombo($combo, [string]$text) {
    $ec = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $ec.Expand(); Start-Sleep -Milliseconds 500
    $item = $combo.FindFirst($TS::Descendants, (Cond $AE::NameProperty $text))
    if (-not $item) { throw "В списке нет '$text'" }
    SelectItem $item
    try { $ec.Collapse() } catch {}
    Start-Sleep -Milliseconds 400
}

try { Set-DisplayResolution -Width 1366 -Height 768 -Force -ErrorAction Stop } catch { Write-Host "Разрешение не изменено: $($_.Exception.Message)" }
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
Write-Host "Экран: $($b.Width)x$($b.Height)"

$exe = (Resolve-Path 'AmfibiaShop\bin\Release\net10.0-windows\AmfibiaShop.exe').Path
$script:proc = Start-Process -FilePath $exe -PassThru
try {
    # ===================== ВХОД И РЕГИСТРАЦИЯ =====================
    $auth = WaitWindow 'AMFIBIA — вход'
    Start-Sleep -Seconds 2
    Shot '01-login'
    Press (El $auth 'LoginButton')
    Check 'Вход: пустая форма' ((TextOf $auth 'MessageBlock') -eq 'Введите email.') (TextOf $auth 'MessageBlock')
    SetValue (El $auth 'EmailTextBox') 'blocked.user@example.com'
    SetValue (El $auth 'PassBox') 'Amfibia2026'
    Press (El $auth 'LoginButton')
    Check 'Вход: заблокированный аккаунт' ((TextOf $auth 'MessageBlock').StartsWith('Аккаунт заблокирован')) (TextOf $auth 'MessageBlock')
    Shot '02-login-blocked'

    Press (El $auth 'RegisterLink')
    $auth = WaitWindow 'AMFIBIA — регистрация'
    Press (El $auth 'CreateButton')
    Check 'Регистрация: ошибки под полями' ((TextOf $auth 'FirstNameError') -eq 'Введите имя.' -and (TextOf $auth 'AgreedError').StartsWith('Нужно согласие')) (TextOf $auth 'PhoneError')
    SetValue (El $auth 'FirstNameBox') 'Мария'
    SetValue (El $auth 'LastNameBox') 'Новик'
    SetValue (El $auth 'EmailBox') 'buyer@amfibia.by'
    SetValue (El $auth 'PhoneBox') '+375 44 765-43-21'
    SetValue (El $auth 'PasswordBox1') 'Mariya2026'
    SetValue (El $auth 'PasswordBox2') 'Mariya2026'
    Toggle (El $auth 'AgreeBox')
    Press (El $auth 'CreateButton')
    Check 'Регистрация: email уже занят' ((TextOf $auth 'MessageBlock').Contains('уже зарегистрирован')) (TextOf $auth 'MessageBlock')
    Shot '03-register'
    $regPanel = El $auth 'RegisterPanel'
    Press (ByName $regPanel 'Войти')
    $auth = WaitWindow 'AMFIBIA — вход'
    SetValue (El $auth 'EmailTextBox') 'buyer@amfibia.by'
    SetValue (El $auth 'PassBox') 'Amfibia2026'
    Press (El $auth 'LoginButton')

    # ===================== ГЛАВНАЯ =====================
    $main = WaitWindow 'AMFIBIA — маркетплейс брендовой одежды'
    Start-Sleep -Seconds 2
    Check 'Окно входа закрылось' (-not (FindWindow 'AMFIBIA — вход'))
    Check 'Инициалы пользователя в шапке' ((TextOf $main 'InitialsText') -eq 'ЕЛ') (TextOf $main 'InitialsText')
    Check 'Заголовок главной' ((TextOf $main 'HeroTitle').StartsWith('Ева, добро пожаловать')) (TextOf $main 'HeroTitle')
    Shot '04-home'

    # ===================== КАТАЛОГ =====================
    SelectItem (El $main 'NavCatalog')
    $page = $main
    Check 'Каталог: 714 вещей' ((TextOf $page 'CountText') -eq 'Найдено 714 вещей') (TextOf $page 'CountText')
    Shot '05-catalog'
    $groups = El $page 'GroupList'
    $groupItems = $groups.FindAll($TS::Children, (Cond $AE::ControlTypeProperty $CT::ListItem))
    Write-Host "     пунктов в списке разделов: $($groupItems.Count)"
    SelectItem $groupItems[5]
    Check 'Раздел «Ювелирные изделия»' ((TextOf $page 'CountText') -eq 'Найдено 85 вещей') (TextOf $page 'CountText')
    Toggle (El $page 'OnlyPreownedBox')
    Start-Sleep -Milliseconds 500
    Check 'Ювелирные + ресейл' ((TextOf $page 'CountText') -match '^Найдено \d+ ') (TextOf $page 'CountText')
    Shot '06-catalog-filters'
    Press (El $page 'ResetButton')
    SetValue (El $page 'SearchBox') 'Triple S'
    Start-Sleep -Milliseconds 900
    Check 'Поиск Triple S' ((TextOf $page 'CountText') -match '^Найдено \d+ вещ') (TextOf $page 'CountText')

    # ===================== ТОВАР =====================
    ClickEl (ByName $page 'Кроссовки Triple S')
    $page = $main
    Check 'Страница товара открыта' ((ByName $page 'Кроссовки Triple S') -ne $null)
    Press (El $page 'AddToCartButton')
    Check 'Без размера нельзя' ((TextOf $page 'MessageText') -eq 'Выберите размер.') (TextOf $page 'MessageText')
    $sizes = El $page 'SizeList'
    SelectItem ($sizes.FindFirst($TS::Children, (Cond $AE::NameProperty '38')))
    Press (El $page 'PlusButton')
    Press (El $page 'AddToCartButton')
    Check 'Добавлено 2 шт. в корзину' ((TextOf $page 'MessageText').StartsWith('Добавлено в корзину')) (TextOf $page 'MessageText')
    Check 'Счётчик корзины = 2' ((TextOf $main 'CartCount') -eq '2') (TextOf $main 'CartCount')
    Press (El $page 'FavoriteButton')
    Press (El $page 'CompareButton')
    Check 'Счётчики избранного и сравнения' ((TextOf $main 'WishlistCount') -eq '1' -and (TextOf $main 'CompareCount') -eq '1')
    Shot '07-product'

    Press (ByName $page 'Balenciaga')
    $page = $main
    Check 'Страница бренда Balenciaga' ((ByName $page 'Креативные директора') -ne $null)
    Start-Sleep -Milliseconds 600
    Shot '08-brand-balenciaga'

    # ===================== КОРЗИНА И ОФОРМЛЕНИЕ =====================
    SelectItem (El $main 'NavCart')
    $page = $main
    SetValue (El $page 'PromoBox') 'amfibia10'
    Press (El $page 'ApplyPromoButton')
    Check 'Промокод применён' ((TextOf $page 'PromoAppliedText') -eq 'Промокод AMFIBIA10 · −10%') (TextOf $page 'PromoAppliedText')
    Write-Host "     итог корзины: $(TextOf $page 'TotalText')"
    Shot '09-cart'
    Press (El $page 'CheckoutButton')
    $page = $main
    Shot '10-checkout-1'
    Press (El $page 'NextButton')
    Shot '11-checkout-2'
    Press (El $page 'NextButton')
    Press (El $page 'NextButton')
    Check 'Без данных карты дальше нельзя' ((TextOf $page 'CardNumberError') -eq 'Введите номер карты.') (TextOf $page 'CardNumberError')
    SetValue (El $page 'CardNumberBox') '4242 4242 4242 4242'
    SetValue (El $page 'CardHolderBox') 'EVA LISOVSKAYA'
    SetValue (El $page 'CardExpiryBox') '12/29'
    SetValue (El $page 'CardCvcBox') '123'
    Shot '12-checkout-3'
    Press (El $page 'NextButton')
    Shot '13-checkout-4'
    Press (El $page 'ConfirmButton')
    $num = TextOf $page 'SuccessNumber'
    Check 'Заказ оформлен' ($num -match '^Заказ AMF-\d{4}-\d{6} · Подтверждён$') $num
    Shot '14-order-success'

    # ===================== ЗАКАЗЫ =====================
    Press (El $page 'OrdersButton')
    $page = $main
    Check 'Мои заказы' ((TextOf $page 'CountText') -match 'заказ') (TextOf $page 'CountText')
    Shot '15-orders'

    # ===================== ОСТАЛЬНЫЕ СТРАНИЦЫ =====================
    SelectItem (El $main 'NavBrands')
    $page = $main
    Check 'Модные дома' ((TextOf $page 'CountText').StartsWith('15 модных домов')) (TextOf $page 'CountText')
    Shot '16-brands'
    SelectItem (El $main 'NavWishlist')
    Shot '17-wishlist'
    SelectItem (El $main 'NavCompare')
    Shot '18-compare'
    SelectItem (El $main 'NavProfile')
    $page = $main
    Check 'Профиль' ((TextOf $page 'EmailText') -eq 'buyer@amfibia.by') (TextOf $page 'EmailText')
    Shot '19-profile'

    ChooseInCombo (El $main 'CurrencyBox') 'USD'
    SelectItem (El $main 'NavHome')
    Start-Sleep -Milliseconds 800
    Shot '20-home-usd'

    Check 'Программа работает без сбоев' (-not $script:proc.HasExited)
}
catch {
    Write-Host "FAIL Сценарий прерван: $($_.Exception.Message)"
    $script:fails++
    try { Shot 'error' } catch {}
}
$crash = Join-Path $env:APPDATA 'AMFIBIA\Lab28_MVP\crash.log'
if (Test-Path $crash) { Write-Host '----- crash.log -----'; Get-Content $crash -Encoding UTF8 | ForEach-Object { Write-Host $_ }; $script:fails++ }
finally {
    if (-not $script:proc.HasExited) {
        $script:proc.CloseMainWindow() | Out-Null
        Start-Sleep -Seconds 2
        if (-not $script:proc.HasExited) { $script:proc.Kill() }
    }
}
if ($script:fails -gt 0) { Write-Host "ПРОВАЛЕНО: $($script:fails)"; exit 1 }
Write-Host 'ВСЕ ПРОВЕРКИ ОКОН ПРОЙДЕНЫ'
