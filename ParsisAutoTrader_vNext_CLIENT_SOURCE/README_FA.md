# Parsis AutoTrader vNext

بازنویسی از صفر با C#، WinUI 3، Windows App SDK و خروجی Setup واقعی ویندوز.

## آنچه پیاده‌سازی شده
- رابط جدید WinUI 3، ناوبری مدرن، فارسی/انگلیسی، Dark/Light
- ورود Telegram با شماره یا QR، دریافت کانال‌ها و انتخاب چند کانال
- Parser سیگنال فارسی/انگلیسی برای BUY/SELL، Market/Limit/Stop، Entry، SL و سه TP
- فیلتر سیگنال تکراری
- کشف خودکار ترمینال‌های MT5
- دریافت Login/Password/Server در برنامه و رمزنگاری رمز با DPAPI
- نصب و Compile خودکار EA توسط MetaEditor
- اجرای MT5 با فایل config، ورود خودکار، فعال‌کردن EA، بازکردن چارت و شروع Bridge
- مدیریت TP1/TP2/TP3 و Break-even داخل EA
- توقف معمولی و توقف اضطراری
- تست سه‌روزه و لایسنس آفلاین امضاشده RSA
- برنامه خصوصی License Manager
- Inno Setup برای آیکون Desktop، Start Menu و Uninstall
- پوشه pishniaz و GitHub Actions برای ساخت Setup.exe روی Windows

## نکته امنیتی مهم
فایل `src/Parsis.LicenseManager/admin_private/license_private.pem` کاملاً خصوصی است. آن را در Git عمومی یا نسخه مشتری قرار ندهید.

## Telegram API
Telegram Client بدون API ID و API Hash متعلق به سازنده کار نمی‌کند. مشتری این اطلاعات را وارد نمی‌کند. در GitHub Repository دو Secret بسازید:
- `TELEGRAM_API_ID`
- `TELEGRAM_API_HASH`

Workflow هنگام Build آن‌ها را وارد نسخه نهایی می‌کند.

## ساخت Setup روی Windows
1. .NET 10 SDK و Inno Setup 6 را نصب کنید.
2. `build\Build-Windows.cmd` را اجرا کنید.
3. خروجی‌ها:
   - `installer\output\ParsisAutoTraderSetup.exe`
   - `installer\output\ParsisLicenseManagerSetup_PRIVATE.exe`

## هشدار
قبل از حساب Real، Parser، ورود خودکار، سفارش‌های Market/Pending، حد ضرر و بستن مرحله‌ای را روی حساب Demo همان بروکر تست کنید. نام نماد و قوانین Volume Step بین بروکرها متفاوت است.
