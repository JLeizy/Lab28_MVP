# Проверка окон AmfibiaShop через UI Automation (как будто пользователь нажимает кнопки)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

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
function El($parent, [string]$id) {
    $e = $parent.FindFirst($TS::Descendants, (Cond $AE::AutomationIdProperty $id))
    if (-not $e) { throw "Элемент $id не найден" }
    return $e
}
function TextOf($parent, [string]$id) { (El $parent $id).Current.Name.Replace($nb, ' ') }
function SetValue($el, [string]$text) { $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($text); Start-Sleep -Milliseconds 400 }
function Press($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 600 }
function SelectItem($item) { $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 500 }
function Items($el, $type) { $el.FindAll($TS::Children, (Cond $AE::ControlTypeProperty $type)) }
function ChooseInCombo($combo, [string]$text) {
    $ec = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $ec.Expand(); Start-Sleep -Milliseconds 500
    $item = $combo.FindFirst($TS::Descendants, (Cond $AE::NameProperty $text))
    if (-not $item) { throw "В списке нет '$text'" }
    SelectItem $item
    $ec.Collapse(); Start-Sleep -Milliseconds 400
}

try { Set-DisplayResolution -Width 1366 -Height 768 -Force -ErrorAction Stop } catch { Write-Host "Разрешение не изменено: $($_.Exception.Message)" }
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
Write-Host "Экран: $($b.Width)x$($b.Height)"

$exe = (Resolve-Path 'AmfibiaShop\bin\Release\net10.0-windows\AmfibiaShop.exe').Path
$script:proc = Start-Process -FilePath $exe -PassThru
try {
    # ===== Окно входа =====
    $login = WaitWindow 'AMFIBIA — вход'
    Start-Sleep -Seconds 2
    Shot '1-login'
    Press (El $login 'LoginButton')
    Check 'Вход: пустая форма' ((TextOf $login 'MessageBlock') -eq 'Введите email.') (TextOf $login 'MessageBlock')
    SetValue (El $login 'EmailTextBox') 'buyer'
    Press (El $login 'LoginButton')
    Check 'Вход: неверный email' ((TextOf $login 'MessageBlock').StartsWith('Email указан неверно')) (TextOf $login 'MessageBlock')
    SetValue (El $login 'EmailTextBox') 'blocked.user@example.com'
    SetValue (El $login 'PassBox') 'Amfibia2026'
    Press (El $login 'LoginButton')
    Check 'Вход: заблокированный аккаунт' ((TextOf $login 'MessageBlock').StartsWith('Аккаунт заблокирован')) (TextOf $login 'MessageBlock')
    Shot '2-login-blocked'
    SetValue (El $login 'EmailTextBox') 'buyer@amfibia.by'
    SetValue (El $login 'PassBox') 'wrong'
    Press (El $login 'LoginButton')
    Check 'Вход: неверный пароль' ((TextOf $login 'MessageBlock') -eq 'Неверный email или пароль.') (TextOf $login 'MessageBlock')
    SetValue (El $login 'PassBox') 'Amfibia2026'
    Press (El $login 'LoginButton')

    # ===== Окно магазина =====
    $shop = WaitWindow 'AMFIBIA — каталог и корзина'
    Start-Sleep -Seconds 2
    Check 'Окно входа закрылось' (-not (FindWindow 'AMFIBIA — вход'))
    $wr = $shop.Current.BoundingRectangle
    Check 'Окно магазина помещается на экран' (($wr.Width -le $b.Width + 16) -and ($wr.Height -le $b.Height + 16)) "окно $([int]$wr.Width)x$([int]$wr.Height)"
    Check 'Пользователь в шапке' ((TextOf $shop 'UserTextBlock') -eq 'Ева Лисовская · Покупатель') (TextOf $shop 'UserTextBlock')
    Check 'Весь каталог' ((TextOf $shop 'CountTextBlock') -eq 'Найдено товаров: 714') (TextOf $shop 'CountTextBlock')
    $rows = Items (El $shop 'ProductsListView') $CT::DataItem
    Check 'Строки в таблице товаров' ($rows.Count -gt 5) "видимых строк: $($rows.Count)"
    Shot '3-shop'

    SetValue (El $shop 'SearchTextBox') 'Triple S'
    Check 'Поиск Triple S' ((TextOf $shop 'CountTextBlock') -eq 'Найдено товаров: 3') (TextOf $shop 'CountTextBlock')
    $rows = Items (El $shop 'ProductsListView') $CT::DataItem
    SelectItem $rows[0]
    Check 'Карточка товара' ((TextOf $shop 'NameTextBlock') -eq 'Кроссовки Triple S') (TextOf $shop 'NameTextBlock')
    Check 'Цена в карточке' ((TextOf $shop 'PriceTextBlock') -match '^\d[\d ]* BYN$') (TextOf $shop 'PriceTextBlock')
    Write-Host "     описание: $((TextOf $shop 'DetailsTextBlock').Replace("`n", ' / '))"
    Press (El $shop 'AddToCartButton')
    Check 'Без размера нельзя' ((TextOf $shop 'CartMessageTextBlock') -eq 'Выберите размер.') (TextOf $shop 'CartMessageTextBlock')
    $sizes = Items (El $shop 'SizesListBox') $CT::ListItem
    Check 'Размеры обуви' ($sizes.Count -ge 7) ((@($sizes) | ForEach-Object { $_.Current.Name }) -join ',')
    SelectItem $sizes[2]
    Press (El $shop 'AddToCartButton')
    Check 'Добавлено в корзину' ((TextOf $shop 'CartMessageTextBlock').StartsWith('Добавлено в корзину')) (TextOf $shop 'CartMessageTextBlock')
    Check 'Счётчик корзины' ((TextOf $shop 'CartCountTextBlock') -eq 'товаров: 1') (TextOf $shop 'CartCountTextBlock')
    $cartRows = Items (El $shop 'CartListView') $CT::DataItem
    Check 'Строка в таблице корзины' ($cartRows.Count -eq 1) "строк: $($cartRows.Count)"

    SetValue (El $shop 'PromoTextBox') 'amfibia10'
    Press (El $shop 'ApplyPromoButton')
    Check 'Промокод AMFIBIA10' ((TextOf $shop 'DiscountTextBlock').Contains('AMFIBIA10, 10%')) (TextOf $shop 'DiscountTextBlock')
    Write-Host "     сумма: $(TextOf $shop 'SubtotalTextBlock'); итого: $(TextOf $shop 'TotalTextBlock')"
    $total = El $shop 'TotalTextBlock'
    $tr = $total.Current.BoundingRectangle
    Check 'Итог корзины виден на экране' ((-not $total.Current.IsOffscreen) -and ($tr.Bottom -le $b.Height)) "низ: $([int]$tr.Bottom) из $($b.Height)"
    Shot '4-cart'

    SetValue (El $shop 'SearchTextBox') ''
    ChooseInCombo (El $shop 'BrandComboBox') 'Chrome Hearts'
    Check 'Бренд Chrome Hearts' ((TextOf $shop 'CountTextBlock') -eq 'Найдено товаров: 60') (TextOf $shop 'CountTextBlock')
    ChooseInCombo (El $shop 'GroupComboBox') 'Ювелирные изделия'
    $c = TextOf $shop 'CountTextBlock'
    Check 'Chrome Hearts + Ювелирные изделия' (($c -match '^Найдено товаров: \d+$') -and ($c -ne 'Найдено товаров: 60')) $c
    ChooseInCombo (El $shop 'SortComboBox') 'Сначала дороже'
    $rows = Items (El $shop 'ProductsListView') $CT::DataItem
    SelectItem $rows[0]
    Write-Host "     самое дорогое: $(TextOf $shop 'NameTextBlock') — $(TextOf $shop 'PriceTextBlock')"
    $sizes = Items (El $shop 'SizesListBox') $CT::ListItem
    Write-Host "     размеры: $((@($sizes) | ForEach-Object { $_.Current.Name }) -join ',')"
    Shot '5-filters'

    $cartRows = Items (El $shop 'CartListView') $CT::DataItem
    SelectItem $cartRows[0]
    Press (El $shop 'RemoveButton')
    Check 'Удаление из корзины' ((TextOf $shop 'CartCountTextBlock') -eq 'пусто') (TextOf $shop 'CartCountTextBlock')
    Shot '6-removed'

    Check 'Программа работает без сбоев' (-not $script:proc.HasExited)
}
catch {
    Write-Host "FAIL Сценарий прерван: $($_.Exception.Message)"
    $script:fails++
    try { Shot 'error' } catch {}
}
finally {
    if (-not $script:proc.HasExited) {
        $script:proc.CloseMainWindow() | Out-Null
        Start-Sleep -Seconds 2
        if (-not $script:proc.HasExited) { $script:proc.Kill() }
    }
}
if ($script:fails -gt 0) { Write-Host "ПРОВАЛЕНО: $($script:fails)"; exit 1 }
Write-Host 'ВСЕ ПРОВЕРКИ ОКОН ПРОЙДЕНЫ'
