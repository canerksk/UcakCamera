# Kamera İzleme

Marka bağımsız IP kamera / NVR izleme masaüstü uygulaması. .NET 8 WinForms, tek exe, gömülü SQLite.

- **Kesinti bildirimi**: e-posta (MailKit), tekrar bildirim aralığı, düzelme bildirimi, toplu kesinti maili.
- **Üç katmanlı sağlayıcı mimarisi**: Evrensel (ping + RTSP OPTIONS) → ONVIF (WS-Discovery, Profile S) → JSON tanımlı marka adaptörü. Yeni marka eklemek çoğunlukla `Saglayicilar/Tanimlar/` klasörüne bir JSON dosyası atmaktan ibarettir.
- **Kademeli düşüş (fallback)**: üst katman art arda hata verirse otomatik olarak bir alt katmana geçer, saatte bir üst katmanı yeniden dener.
- **Dashboard**: canlı durum kartları, olay geçmişi, uptime yüzdeleri, gecikme grafiği (ScottPlot).
- **Deneme modu**: gerçek kamera olmadan denemek için sahte HTTP + ONVIF sunucusu.

Ayrıntılı kurulum ve "Yeni marka nasıl eklenir" başlıkları uygulama tamamlanınca genişletilir.

## Kurulum

```powershell
dotnet build -c Release
dotnet run --project src/KameraIzleme
```

İlk açılışta `kamera.db` exe'nin yanına oluşur.

## Yapı

```
src/KameraIzleme         WinForms uygulaması
tests/KameraIzleme.Tests xUnit testleri
```
