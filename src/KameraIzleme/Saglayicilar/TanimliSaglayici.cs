using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using KameraIzleme.Modeller;
using KameraIzleme.Veri;
using Serilog;

namespace KameraIzleme.Saglayicilar;

/// <summary>
/// JSON tanım dosyasından yönlendirilen marka adaptörü. Tek bir sınıf tüm markalara hizmet eder;
/// yeni marka eklemek için sadece <see cref="SaglayiciTanimYukleyici.VarsayilanDizin"/>
/// klasörüne bir .json dosyası koymak yeterlidir.
/// </summary>
public sealed class TanimliSaglayici : IKameraSaglayici
{
    private static readonly HttpClient PaylasilanIstemci = HttpIstemciKur();

    private readonly SaglayiciTanimi _tanim;
    private readonly AyarlarDeposu _ayarlar;

    public TanimliSaglayici(SaglayiciTanimi tanim, AyarlarDeposu ayarlar)
    {
        _tanim = tanim;
        _ayarlar = ayarlar;
    }

    public string Marka => _tanim.Marka;

    public int Oncelik => _tanim.Oncelik;

    public SaglayiciTanimi Tanim => _tanim;

    public async Task<bool> DesteklerMiAsync(CihazBilgisi cihaz, CancellationToken ct)
    {
        var pi = _tanim.Parmakizi;

        // 1) ONVIF üretici alanı en güvenilir işaret.
        if (!string.IsNullOrEmpty(cihaz.OnvifUretici))
        {
            foreach (var anahtar in pi.OnvifUreticiIcerir)
            {
                if (cihaz.OnvifUretici.Contains(anahtar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // 2) HTTP Server başlığı.
        if (!string.IsNullOrEmpty(cihaz.HttpServerBasligi))
        {
            foreach (var anahtar in pi.HttpServerBasligiIcerir)
            {
                if (cihaz.HttpServerBasligi.Contains(anahtar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // 3) Yol kontrolü.
        foreach (var kontrol in pi.HttpYolKontrolu)
        {
            if (cihaz.HttpYolDurumlari.TryGetValue(kontrol.Yol, out int durum)
                && durum == kontrol.BeklenenDurum)
            {
                return true;
            }

            // Sözlükte yoksa canlı deneyelim.
            try
            {
                using var istek = new HttpRequestMessage(HttpMethod.Get,
                    $"http://{cihaz.Ip}:{cihaz.HttpPort}{kontrol.Yol}");
                using var cevap = await PaylasilanIstemci.SendAsync(
                    istek, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                if ((int)cevap.StatusCode == kontrol.BeklenenDurum)
                {
                    return true;
                }
            }
            catch
            {
                // sessizce geç
            }
        }

        return false;
    }

    public async Task<KontrolSonucu> DurumKontrolAsync(Kamera kamera, CancellationToken ct)
    {
        if (_tanim.DurumKontrolu is null)
        {
            return new KontrolSonucu
            {
                Basarili = true,
                AktifKatman = Marka,
                Mesaj = "Marka tanımında durum uç noktası yok — evrensel ile birlikte kullan.",
            };
        }

        int timeout = _ayarlar.Al("izleme.http_timeout_ms", 5000);
        using var iptalKaynagi = CancellationTokenSource.CreateLinkedTokenSource(ct);
        iptalKaynagi.CancelAfter(timeout);

        string url = $"http://{kamera.Ip}:{kamera.HttpPort}" +
            YerTutucu.Coz(_tanim.DurumKontrolu.Yol, YerTutucu.KameraSozlugu(kamera));

        try
        {
            HttpResponseMessage cevap = await DigestAuth.KimlikliGonderAsync(
                PaylasilanIstemci,
                () => new HttpRequestMessage(HttpMethod.Get, url),
                kamera.Kullanici,
                SifreKorumasi.Coz(kamera.SifreSifreli),
                _tanim.KimlikDogrulama,
                iptalKaynagi.Token).ConfigureAwait(false);

            using (cevap)
            {
                string govde = await cevap.Content.ReadAsStringAsync(iptalKaynagi.Token).ConfigureAwait(false);
                if (cevap.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return Basarisiz(OlayTipleri.YetkisizErisim, "HTTP 401 — kimlik doğrulama başarısız");
                }

                if (!cevap.IsSuccessStatusCode)
                {
                    return Basarisiz(OlayTipleri.Dondu, $"HTTP {(int)cevap.StatusCode}");
                }

                bool basariliMi = BasariKosuluDogrula(_tanim.DurumKontrolu, govde);
                return basariliMi
                    ? new KontrolSonucu { Basarili = true, AktifKatman = Marka }
                    : Basarisiz(OlayTipleri.Dondu, "Marka durum kontrolü başarısız yanıt döndü");
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Basarisiz(OlayTipleri.Dondu, "Marka HTTP zaman aşımı");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Marka HTTP hatası: {Ip}", kamera.Ip);
            return Basarisiz(OlayTipleri.Dondu, $"Marka HTTP hatası: {ex.Message}");
        }

        KontrolSonucu Basarisiz(string tip, string mesaj) => new()
        {
            Basarili = false,
            AktifKatman = Marka,
            Mesaj = mesaj,
            OlayTipi = tip,
        };
    }

    public async Task<IReadOnlyList<KanalBilgisi>> KanallariGetirAsync(Kamera kamera, CancellationToken ct)
    {
        if (_tanim.KanalListesi is null)
        {
            return Array.Empty<KanalBilgisi>();
        }

        string url = $"http://{kamera.Ip}:{kamera.HttpPort}" +
            YerTutucu.Coz(_tanim.KanalListesi.Yol, YerTutucu.KameraSozlugu(kamera));

        try
        {
            HttpResponseMessage cevap = await DigestAuth.KimlikliGonderAsync(
                PaylasilanIstemci,
                () => new HttpRequestMessage(HttpMethod.Get, url),
                kamera.Kullanici,
                SifreKorumasi.Coz(kamera.SifreSifreli),
                _tanim.KimlikDogrulama,
                ct).ConfigureAwait(false);

            using (cevap)
            {
                if (!cevap.IsSuccessStatusCode)
                {
                    return Array.Empty<KanalBilgisi>();
                }

                string govde = await cevap.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return KanallariAyrisir(_tanim.KanalListesi, govde);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Kanal listesi alınamadı: {Ip}", kamera.Ip);
            return Array.Empty<KanalBilgisi>();
        }
    }

    public async IAsyncEnumerable<CihazOlayi> OlaylariDinleAsync(
        Kamera kamera,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (_tanim.OlayAkisi is null)
        {
            yield break;
        }

        string url = $"http://{kamera.Ip}:{kamera.HttpPort}" +
            YerTutucu.Coz(_tanim.OlayAkisi.Yol, YerTutucu.KameraSozlugu(kamera));

        // Uzun süreli akış — kendi HttpClient'ı, timeout kapalı.
        using var akisIstemci = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        HttpResponseMessage cevap;
        try
        {
            cevap = await DigestAuth.KimlikliGonderAsync(
                akisIstemci,
                () => new HttpRequestMessage(HttpMethod.Get, url),
                kamera.Kullanici,
                SifreKorumasi.Coz(kamera.SifreSifreli),
                _tanim.KimlikDogrulama,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Marka olay akışı bağlantısı başarısız: {Ip}", kamera.Ip);
            yield break;
        }

        if (!cevap.IsSuccessStatusCode)
        {
            cevap.Dispose();
            yield break;
        }

        // Multipart akışı satır satır tarayıp XML/JSON blok başlangıçlarını yakalar.
        using (cevap)
        {
            using var akis = await cevap.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var okuyucu = new StreamReader(akis);
            var tampon = new StringBuilder();

            while (!ct.IsCancellationRequested)
            {
                string? satir;
                try
                {
                    satir = await okuyucu.ReadLineAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Marka olay akışı okuma hatası: {Ip}", kamera.Ip);
                    yield break;
                }

                if (satir is null)
                {
                    yield break;
                }

                if (satir.StartsWith("--", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(satir))
                {
                    if (tampon.Length > 0)
                    {
                        foreach (var olay in OlaylariAyrisir(_tanim.OlayAkisi, tampon.ToString()))
                        {
                            yield return olay;
                        }

                        tampon.Clear();
                    }

                    continue;
                }

                if (satir.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // header satırlarını atla
                }

                tampon.AppendLine(satir);
            }
        }
    }

    // ---- Yardımcılar ----

    private static bool BasariKosuluDogrula(UcTanimi uc, string govde)
    {
        if (string.IsNullOrEmpty(uc.BasariKosulu))
        {
            return true;
        }

        return uc.Format switch
        {
            "xml" => XPathVar(govde, uc.BasariKosulu),
            "json" => JsonYolVar(govde, uc.BasariKosulu),
            _ => govde.Contains(uc.BasariKosulu, StringComparison.OrdinalIgnoreCase),
        };
    }

    private static bool XPathVar(string xml, string xpath)
    {
        try
        {
            var belge = new XPathDocument(new StringReader(xml));
            var gezici = belge.CreateNavigator();
            var yonetici = new System.Xml.XmlNamespaceManager(gezici.NameTable!);
            // Basit XPath'ler için ad alanı yönetimini kısa geçiyoruz.
            var eslesme = gezici.Select(xpath, yonetici);
            return eslesme.MoveNext();
        }
        catch
        {
            return false;
        }
    }

    private static bool JsonYolVar(string json, string yol)
    {
        // Çok basit bir dotted-path: "a.b[0].c".
        try
        {
            using var belge = JsonDocument.Parse(json);
            JsonElement mevcut = belge.RootElement;
            foreach (var parca in AyristirYol(yol))
            {
                if (parca.Endeks is int i)
                {
                    if (mevcut.ValueKind != JsonValueKind.Array || i >= mevcut.GetArrayLength())
                    {
                        return false;
                    }

                    mevcut = mevcut[i];
                }
                else if (parca.Ad is string ad)
                {
                    if (mevcut.ValueKind != JsonValueKind.Object || !mevcut.TryGetProperty(ad, out mevcut))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<(string? Ad, int? Endeks)> AyristirYol(string yol)
    {
        foreach (var parca in yol.TrimStart('$').TrimStart('.').Split('.'))
        {
            var m = Regex.Match(parca, @"^([a-zA-Z_][a-zA-Z0-9_]*)(?:\[(\d+)\])?$");
            if (!m.Success)
            {
                continue;
            }

            yield return (m.Groups[1].Value, null);
            if (m.Groups[2].Success)
            {
                yield return (null, int.Parse(m.Groups[2].Value));
            }
        }
    }

    private static IReadOnlyList<KanalBilgisi> KanallariAyrisir(UcTanimi uc, string govde)
    {
        var sonuc = new List<KanalBilgisi>();

        if (uc.Format == "xml" && !string.IsNullOrEmpty(uc.ListeYolu))
        {
            try
            {
                var belge = new XPathDocument(new StringReader(govde));
                var gezici = belge.CreateNavigator();
                var yonetici = new System.Xml.XmlNamespaceManager(gezici.NameTable!);
                var eslesmeler = gezici.Select(uc.ListeYolu, yonetici);
                while (eslesmeler.MoveNext())
                {
                    var elem = eslesmeler.Current!;
                    sonuc.Add(new KanalBilgisi
                    {
                        KanalNo = OkuXml(elem, uc.AlanEslemesi.GetValueOrDefault("kanal_no", "id")),
                        Ad = OkuXmlNull(elem, uc.AlanEslemesi.GetValueOrDefault("ad", "name")),
                        Ip = OkuXmlNull(elem, uc.AlanEslemesi.GetValueOrDefault("ip", "ip")),
                        Cevrimici = string.Equals(
                            OkuXmlNull(elem, uc.AlanEslemesi.GetValueOrDefault("cevrimici", "online")),
                            "true",
                            StringComparison.OrdinalIgnoreCase),
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Kanal XML ayrıştırma hatası");
            }
        }
        else if (uc.Format == "json" && !string.IsNullOrEmpty(uc.ListeYolu))
        {
            // Sadece kök liste veya basit dotted-path desteği.
            try
            {
                using var belge = JsonDocument.Parse(govde);
                var liste = belge.RootElement;
                foreach (var parca in AyristirYol(uc.ListeYolu))
                {
                    if (parca.Ad is string ad && liste.TryGetProperty(ad, out var alt))
                    {
                        liste = alt;
                    }
                }

                if (liste.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in liste.EnumerateArray())
                    {
                        sonuc.Add(new KanalBilgisi
                        {
                            KanalNo = OkuJson(elem, uc.AlanEslemesi.GetValueOrDefault("kanal_no", "id")) ?? string.Empty,
                            Ad = OkuJson(elem, uc.AlanEslemesi.GetValueOrDefault("ad", "name")),
                            Ip = OkuJson(elem, uc.AlanEslemesi.GetValueOrDefault("ip", "ip")),
                            Cevrimici = string.Equals(
                                OkuJson(elem, uc.AlanEslemesi.GetValueOrDefault("cevrimici", "online")),
                                "true",
                                StringComparison.OrdinalIgnoreCase),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Kanal JSON ayrıştırma hatası");
            }
        }

        return sonuc;
    }

    private IEnumerable<CihazOlayi> OlaylariAyrisir(OlayAkisTanimi tanim, string metin)
    {
        if (tanim.Format == "xml" && !string.IsNullOrEmpty(tanim.OlayYolu))
        {
            List<CihazOlayi> sonuc = new();
            try
            {
                int ilkNoktaVirgul = metin.IndexOf('<');
                if (ilkNoktaVirgul < 0)
                {
                    return sonuc;
                }

                var belge = new XPathDocument(new StringReader(metin[ilkNoktaVirgul..]));
                var gezici = belge.CreateNavigator();
                var yonetici = new System.Xml.XmlNamespaceManager(gezici.NameTable!);
                var eslesmeler = gezici.Select(tanim.OlayYolu, yonetici);
                while (eslesmeler.MoveNext())
                {
                    var elem = eslesmeler.Current!;
                    string tip = OkuXml(elem, tanim.AlanEslemesi.GetValueOrDefault("olay_tipi", "eventType"));
                    string? kanal = OkuXmlNull(elem, tanim.AlanEslemesi.GetValueOrDefault("kanal_no", "channelID"));
                    string tipCevrilmis = tanim.OlayEslemesi.GetValueOrDefault(tip.ToLowerInvariant(), tip);
                    sonuc.Add(new CihazOlayi
                    {
                        Tip = tipCevrilmis,
                        KanalNo = kanal,
                        Mesaj = $"{Marka}: {tip}",
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Marka olay XML ayrıştırma hatası");
            }

            return sonuc;
        }

        // Düz metin: "key=value" satırları
        var duzList = new List<CihazOlayi>();
        var codeMatch = Regex.Match(metin, @"Code=(\w+)", RegexOptions.IgnoreCase);
        if (codeMatch.Success)
        {
            string tip = codeMatch.Groups[1].Value;
            string tipCevrilmis = tanim.OlayEslemesi.GetValueOrDefault(tip.ToLowerInvariant(), tip);
            duzList.Add(new CihazOlayi { Tip = tipCevrilmis, Mesaj = metin.Trim() });
        }

        return duzList;
    }

    private static string OkuXml(XPathNavigator elem, string yol)
    {
        var iter = elem.SelectSingleNode(yol);
        return iter?.Value ?? string.Empty;
    }

    private static string? OkuXmlNull(XPathNavigator elem, string yol)
    {
        var iter = elem.SelectSingleNode(yol);
        return iter?.Value;
    }

    private static string? OkuJson(JsonElement elem, string yol)
    {
        JsonElement mevcut = elem;
        foreach (var parca in AyristirYol(yol))
        {
            if (parca.Ad is string ad)
            {
                if (mevcut.ValueKind != JsonValueKind.Object || !mevcut.TryGetProperty(ad, out mevcut))
                {
                    return null;
                }
            }
            else if (parca.Endeks is int i)
            {
                if (mevcut.ValueKind != JsonValueKind.Array || i >= mevcut.GetArrayLength())
                {
                    return null;
                }

                mevcut = mevcut[i];
            }
        }

        return mevcut.ValueKind switch
        {
            JsonValueKind.String => mevcut.GetString(),
            JsonValueKind.Number => mevcut.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => mevcut.GetRawText(),
        };
    }

    private static HttpClient HttpIstemciKur()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        var istemci = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };
        istemci.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("KameraIzleme", "1.0"));
        return istemci;
    }
}
