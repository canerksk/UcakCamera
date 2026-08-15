# Kamera İzleme

Marka bağımsız IP kamera / NVR izleme masaüstü uygulaması. .NET 8 WinForms, tek exe, gömülü SQLite; harici veritabanı sunucusu yok, web arayüzü yok.

- Kamera kesildiğinde e-posta bildirimi (MailKit; ardışık hata eşiği, tekrar aralığı, düzelme maili, toplu kesinti maili).
- Üç katmanlı sağlayıcı mimarisi — kod değiştirmeden yeni marka eklenebilir.
- Canlı durum kartları + olay geçmişi + uptime yüzdeleri + gecikme grafiği (ScottPlot).
- Deneme modu: sahte HTTP + ONVIF sunucusu ve seed veri ile gerçek kamera olmadan denenebilir.

## Mimari (üç katman)

```
                ┌──────────────────────────────────────────┐
                │  IzlemeServisi (PeriodicTimer, semafor)  │
                └───────────────┬──────────────────────────┘
                                │
                                ▼
                ┌──────────────────────────────────────────┐
                │             SaglayiciSecici              │
                │  parmak izi ile seç, 3 hatada alt katman │
                │  saat başı üst katmanı yeniden dene      │
                └──┬──────────┬────────────┬───────────────┘
                   │          │            │
                   ▼          ▼            ▼
             ┌──────────┐┌─────────┐┌─────────────────┐
             │ Marka    ││ ONVIF   ││ Evrensel        │
             │ (JSON)   ││ Profile ││ Ping + RTSP OPT │
             │ öncelik  ││ S       ││ öncelik 0       │
             │ 100+     ││ 50      ││ Ping başarısız  │
             │ HTTP+XML ││ SOAP+   ││ ise RTSP        │
             │ /JSON/dm ││ Digest  ││ denenmez        │
             └──────────┘└─────────┘└─────────────────┘
```

- **Katman 1 (Evrensel)** her cihazda çalışır: `System.Net.NetworkInformation.Ping` → başarılıysa `TcpClient` ile RTSP OPTIONS. Video decode edilmez, FFmpeg yok. 200 veya 401 cevabı servisin ayakta olduğunu gösterir.
- **Katman 2 (ONVIF)** Profile S için elle SOAP + WS-Security UsernameToken digest (`Saglayicilar/Onvif/*`). WS-Discovery ile ağdaki cihazları bulur, `GetStreamUri` ile RTSP adresini kameradan alır. `PullPoint` aboneliğiyle `VideoSource/SignalLoss`, `VideoSource/ImageTooDark`, `Device/HardwareFailure` topic'lerini dinler; abonelik yenilenir, kopunca exponential backoff ile yeniden kurulur.
- **Katman 3 (Marka)** `TanimliSaglayici` sınıfı, `Saglayicilar/Tanimlar/` klasöründeki her `.json` dosyasını okur ve JSON'da tarif edilen HTTP isteklerini/olay akışını uygular. XML → XPath, JSON → dotted path, düz metin → `key=value` desteklenir. Digest veya Basic auth.

Kademeli düşüş: bir sağlayıcı üst üste 3 kez hata verirse otomatik olarak bir alt katmana geçilir; UI ve log'a yazılır. Saatte bir üst katman yeniden denenir. Kullanıcı sağlayıcıyı elle sabitlerse (dropdown) bu otomatik düşüş devre dışı kalır.

## Klasör düzeni

```
src/KameraIzleme/
  Program.cs
  Arayuz/                Windows Forms
    AnaForm.*
    KameraKarti.*
    Sekmeler/           4 sekme (Canlı, Kameralar, Olaylar, Ayarlar)
    OnvifKesifForm.*
    IpTaramaForm.*
    KameraDuzenleForm.*
    SistemTepsisi.cs
    UygulamaKapsami.cs
  Deneme/                Sahte HTTP + ONVIF sunucusu, seed veri
  Izleme/                Yoklama motoru, alarm motoru
  Loglama/               Serilog kurulumu
  Mail/                  MailKit gönderim
  Modeller/              Kamera, KameraDurumu, KameraOlayi, CihazBilgisi
  Saglayicilar/          Üç katman + JSON tanım motoru
    Onvif/               WS-Discovery, SOAP, WS-Security
    Tanimlar/            hikvision.json, dahua.json, axis.json, uniview.json
  Veri/                  SQLite şema + Dapper depoları + DPAPI

tests/KameraIzleme.Tests/  xUnit testleri
```

## Kurulum

Windows'ta:

```powershell
dotnet build -c Release
dotnet run --project src/KameraIzleme
```

İlk açılışta `kamera.db` exe'nin yanına oluşur. Şifreler DPAPI (kullanıcı kapsamı) ile şifrelenir.

### İlk kez açış — deneme modu ile

1. Uygulamayı aç → **Ayarlar** sekmesi.
2. "Deneme modu (maili loga bas, sahte sunucular)" onay kutusunu işaretle → "Ayarları kaydet".
3. Uygulamayı yeniden başlat.
4. Deneme kameraları (127.0.0.1 dahil) otomatik eklenir; sahte HTTP (127.0.0.1:18080) ve sahte ONVIF (127.0.0.1:18081) sunucuları ayağa kalkar.

### Gerçek kameralar

1. **Kameralar** sekmesi → "ONVIF Keşif" → ağdaki cihazlar taranır, `GetStreamUri` ile RTSP adresi alınır, seçilenler tek tıkla eklenir. Bu, 40 kamerayı elle girmekten kurtarır.
2. Alternatif: "IP Aralığı Tara" → 554/80/8000 portu açık cihazları bulur.
3. Alternatif: "Yeni" → tek kamera formu; "Bağlantıyı test et" ile her katmanın (`ping`/`RTSP`/`ONVIF`/`marka API`) tek tek durumu raporlanır.

## Ayarların anlamı

| Anahtar | Varsayılan | Anlamı |
| --- | --- | --- |
| `izleme.aralik_saniye` | 30 | Kontrol turları arası bekleme |
| `izleme.ping_timeout_ms` | 2000 | ICMP ping zaman aşımı |
| `izleme.rtsp_timeout_ms` | 3000 | RTSP OPTIONS zaman aşımı |
| `alarm.ardisik_hata_esigi` | 3 | Bu sayıya ulaşmadan mail atılmaz, olay açılmaz |
| `alarm.tekrar_bildirim_dakika` | 60 | Aynı kamera için tekrar mail göndermeden önce bekleme |
| `alarm.toplu_kesinti_yuzde` | 50 | Bu oranın üstü tek tek yerine tek "TOPLU KESİNTİ" maili gider |
| `uygulama.deneme_modu` | false | Mail yerine log, sahte sunucular ve seed veri |
| `mail.host` / `.port` / `.ssl` | — | SMTP sunucusu |
| `mail.aliciler` | — | Virgülle ayrılmış alıcı listesi |

Ayarlar UI'daki "Ayarlar" sekmesinden değiştirilebilir; anahtarlar `ayarlar` tablosunda yaşar.

## Yeni marka nasıl eklenir

Çoğu markada tek satır C# yazmaya gerek yok. Adımlar:

### 1. `Saglayicilar/Tanimlar/marka-adi.json` dosyası oluştur

Yayınlanan `hikvision.json` (Hikvision), `dahua.json`, `axis.json`, `uniview.json` dosyaları başlangıç şablonu olarak kullanılabilir. Alanların anlamı:

```jsonc
{
  "marka": "MarkaAdi",             // Görünen ad + saglayici alanına yazılan değer
  "oncelik": 100,                  // 0 en düşük (evrensel), 50 ONVIF, marka için >= 100

  "parmakizi": {
    // Cihaz tespit sırasında bu markanın olduğu nasıl anlaşılır?
    "onvif_uretici_icerir": ["hikvision"],
    "http_server_basligi_icerir": ["App-webs"],
    "http_yol_kontrolu": [
      { "yol": "/doc/page/login.asp", "beklenen_durum": 200 }
    ]
  },

  "kimlik_dogrulama": "digest",    // digest | basic | yok

  "rtsp": {
    // {kanal}, {ip}, {kullanici}, {rtsp_port}, {http_port} yer tutucuları
    "ana_akis": "/Streaming/Channels/{kanal}01",
    "alt_akis": "/Streaming/Channels/{kanal}02"
  },

  "durum_kontrolu": {
    "yol": "/ISAPI/System/status",
    "format": "xml",               // xml | json | duzmetin
    "basari_kosulu": "//DeviceStatus"   // XPath | JSONPath | metin içerir
  },

  "kanal_listesi": {
    "yol": "/ISAPI/ContentMgmt/InputProxy/channels",
    "format": "xml",
    "liste_yolu": "//InputProxyChannel",
    "alan_eslemesi": {
      "kanal_no": "id",
      "ad": "name",
      "ip": "sourceInputPortDescriptor/ipAddress",
      "cevrimici": "online"
    }
  },

  "olay_akisi": {
    "yol": "/ISAPI/Event/notification/alertStream",
    "tip": "multipart",            // uzun süreli çok parçalı bir bağlantı
    "format": "xml",
    "olay_yolu": "//EventNotificationAlert",
    "alan_eslemesi": {
      "olay_tipi": "eventType",
      "kanal_no": "channelID"
    },
    "olay_eslemesi": {             // markanın kendi ismi → uygulamanın standart adı
      "videoloss":  "sinyal_kaybi",
      "nodiskerror":"disk_hatasi",
      "illaccess":  "yetkisiz_erisim"
    }
  }
}
```

### 2. Dosyayı `Saglayicilar/Tanimlar/` klasörüne koy

İki yol:

- **Kaynak ağacından:** `src/KameraIzleme/Saglayicilar/Tanimlar/` altına koy → `dotnet build`. Dosya `CopyToOutputDirectory=PreserveNewest` ile exe'nin yanına kopyalanır.
- **Çalışan kurulumda:** UI'da **Ayarlar → Sağlayıcılar → "Tanım dosyası ekle"** düğmesiyle seç → uygulama JSON'u exe'nin yanındaki `Saglayicilar/Tanimlar/` klasörüne kopyalar → "Tanımları yeniden yükle" → uygulamayı yeniden başlat (motor yeni tanımı tam benimser).

### 3. Test — gerçek bir kamera olmadan

Uygulamayı deneme modunda başlat. Seed olarak eklenen 127.0.0.1 kamerası sahte HTTP sunucusu üzerinden yeni tanımının parmak izi/durum kontrolü kısımlarını doğrular. Tanımın syntax hatası varsa `logs/kamera-*.log` içinde satır satır loglanır.

### Standart olay tipleri

`olay_eslemesi` bloğunda markanın verdiği isimleri şunlardan birine çevir:

- `sinyal_kaybi` — kamera görüntüsü kayboldu / donuk
- `dustu` — cihaz erişilemez
- `dondu` — cihaz cevap veriyor ama servis çalışmıyor
- `donanim_hatasi` — cihazın kendisinden gelen donanım hatası
- `yetkisiz_erisim` — 401 / login başarısız
- `disk_hatasi` — NVR disk sorunu

Olay akışından gelen sinyal kaybı bildirimi eşik beklemeden anında mail atar; çünkü cihazın kendi teşhisi kesin bilgidir.

## Testler

```powershell
dotnet test
```

Kapsam: sağlayıcı seçici + fallback, `TanimliSaglayici` JSON yükleme (hatalı dosya toleransı dahil), yer tutucu çözümü, Digest meydan ayrıştırma.

## Bilinen sınırlar

- Uygulama açık değilken izleme durur — 7/24 gerekiyorsa izleme çekirdeği (`Izleme/`) Windows Service'e taşınmaya uygun tasarlandı, UI'dan bağımsız.
- ONVIF PullPoint gerektiren markalar için abonelik ilk turda birkaç saniye gecikebilir.
- WinForms + DPAPI olduğu için uygulama yalnızca Windows'ta çalışır.

## Lisans

Şirket içi kullanım.
