using System;
using System.IO;
using UnityEngine;

namespace Hezarfen.Sehir
{
    /// <summary>
    /// <b>Kayıt dosyasının içeriği.</b>
    ///
    /// ## Neden bu kadar KISA
    ///
    /// Şehirde iki bin sakin var ve hiçbiri burada yok. Rutin saf bir
    /// işlev — `(vakit, tohum) → hedef` (ADR 0070) — ve sakinlerin
    /// tohumları tek bir <see cref="sehirTohumu"/>'ndan üretiliyor.
    /// Yani kimin nerede olduğunu saklamak gerekmiyor: tarih ve tohum
    /// verilince şehir <b>kendini yeniden kurar</b>.
    ///
    /// Bu, Faz 6'da "rutin saf olsun" kararının bugün ödediği bedel.
    /// Durum tutan bir rutin, kayıt dosyasına iki bin satır yazdırırdı ve
    /// her sürümde o satırların göçünü de yazmak gerekirdi.
    ///
    /// ## Sürüm alanı ilk günden var
    ///
    /// Kayıt biçimi değişecek — hep değişir. Sürümsüz bir dosya, ilk
    /// güncellemede sessizce yanlış okunur: alanlar kayar ve oyuncu
    /// kesesinde başka bir sayı bulur. <see cref="surum"/> okuma anında
    /// denetleniyor.
    /// </summary>
    [Serializable]
    public class KayitVerisi
    {
        /// <summary>Kayıt biçimi sürümü. Artmadan alan silinmez.</summary>
        public int surum = 1;

        /// <summary>Kaydın alındığı gerçek zaman (görüntüleme için).</summary>
        public string damga = "";

        // --- ZAMAN ---
        public int yil = 1632;
        public int yilinGunu = 122;
        public float saat = 9f;

        // --- OYUNCU ---
        public float x, y, z;
        public float bakisYaw;

        // --- KESE ---
        public int akce;

        /// <summary>Yasak mal taşınıyor mu (aranma riski).</summary>
        public bool yasakMal;

        // --- GOREV ---
        /// <summary>Etkin görev arketipi; -1 = görev yok.</summary>
        public int gorevArketip = -1;
        public int gorevTohum;
        public int gorevSiradaki;

        // --- ARANMA ---
        public float aranmaSeviyesi;

        // --- PERDE 2 ---
        public int perde2Asama;
        public int talimSayisi;

        // --- SEHIR ---
        /// <summary>
        /// Sakinlerin dağıtıldığı tohum. <b>Şehrin tamamı bu sayıdan
        /// doğuyor</b> — kim nerede oturuyor, kim hangi meslekten.
        /// </summary>
        public int sehirTohumu = 1632;
    }

    /// <summary>
    /// <b>Kaydı diske yazar ve okur.</b>
    ///
    /// Dosya <see cref="Application.persistentDataPath"/> altında; depo
    /// içinde değil, oyuncunun makinesinde.
    ///
    /// ## Yazma ATOMİKtir
    ///
    /// Önce geçici bir dosyaya yazılır, sonra yerine taşınır. Doğrudan
    /// üstüne yazarken oyun kapanırsa (ya da elektrik giderse) oyuncu
    /// hem eski hem yeni kaydı kaybederdi — yarım bir JSON hiçbir işe
    /// yaramaz. Taşıma işletim sistemi düzeyinde tek adımdır.
    /// </summary>
    public static class Kayit
    {
        public const string DosyaAdi = "hezarfen_kayit.json";

        /// <summary>Kayıt biçiminin bu sürümde okuyabildiği en eski hâli.</summary>
        public const int EnEskiOkunabilirSurum = 1;

        public static string Yol =>
            Path.Combine(Application.persistentDataPath, DosyaAdi);

        public static bool Var => File.Exists(Yol);

        /// <summary>Kaydı yazar. Başarısızsa <c>false</c> ve konsolda sebep.</summary>
        public static bool Yaz(KayitVerisi v)
        {
            if (v == null) return false;
            v.damga = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string gecici = Yol + ".tmp";
                File.WriteAllText(gecici, JsonUtility.ToJson(v, true));
                // ATOMIK: once gecici, sonra yerine.
                if (File.Exists(Yol)) File.Delete(Yol);
                File.Move(gecici, Yol);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Hezarfen] Kayit yazilamadi: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kaydı okur. Yoksa ya da okunamıyorsa <c>null</c>.
        ///
        /// <b>Bozuk bir kayıt sessizce yeni oyun başlatmaz:</b> oyuncu
        /// kaydının gittiğini bilmeli. Sebep konsola yazılır ve çağıran
        /// karar verir.
        /// </summary>
        public static KayitVerisi Oku()
        {
            if (!Var) return null;
            try
            {
                var v = JsonUtility.FromJson<KayitVerisi>(File.ReadAllText(Yol));
                if (v == null)
                {
                    Debug.LogError("[Hezarfen] Kayit okunamadi: bicim bozuk.");
                    return null;
                }
                if (v.surum < EnEskiOkunabilirSurum)
                {
                    Debug.LogError($"[Hezarfen] Kayit surumu {v.surum} cok "
                                   + $"eski (en az {EnEskiOkunabilirSurum}).");
                    return null;
                }
                return v;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Hezarfen] Kayit okunamadi: {e.Message}");
                return null;
            }
        }

        /// <summary>Kaydı siler.</summary>
        public static void Sil()
        {
            try { if (Var) File.Delete(Yol); }
            catch (Exception e)
            {
                Debug.LogError($"[Hezarfen] Kayit silinemedi: {e.Message}");
            }
        }
    }
}
