namespace Hezarfen.Arayuz
{
    /// <summary>
    /// <b>Krediler ve kaynakça.</b>
    ///
    /// ## Atıf bir nezaket değil, YÜKÜMLÜLÜK
    ///
    /// OpenStreetMap verisi <b>ODbL</b> altında. Atıf eksikse bu bir
    /// incelik kusuru değil, <b>lisans ihlali</b>. Bu yüzden metin bir
    /// sabit ve bir test onun içeriğini sınıyor — krediler ekranı bir gün
    /// yeniden yazılırsa atıf sessizce düşmesin.
    ///
    /// ## Kaynakça oyunun PAZARLAMA GÜCÜ
    ///
    /// PLAN Bölüm 13 bunu açıkça söylüyor: *"tarih meraklısı oyuncuya
    /// kaynakça vermek bu oyunun pazarlama gücüdür."* Oyunda gördüğü Arap
    /// Camii'nin neden öyle olduğunu merak eden oyuncuya kaynağı
    /// verebilmek, üç fazdır dipnot tutmanın karşılığı.
    ///
    /// Kademe işaretleri (T1/T2/T3) burada da duruyor, çünkü oyunun
    /// dürüstlüğü tam olarak orada: neyin belgeli, neyin yeniden kurgu
    /// olduğunu söylemek.
    /// </summary>
    public static class Krediler
    {
        /// <summary>Yasal olarak zorunlu atıflar — testle korunuyor.</summary>
        public static readonly string[] ZorunluAtif =
        {
            "OpenStreetMap",
            "ODbL",
        };

        public const string Metin =
@"HEZARFEN · 1632

— VERİ VE VARLIK ATIFLARI —

Harita verisi: © OpenStreetMap katkıcıları.
Open Database License (ODbL) altında kullanılmıştır.
Kıyı çizgisi ve topografya bu veriden türetilmiştir.

Yükseklik verisi: kamu erişimli DEM kaynakları.

HDRI ve doku: Poly Haven (CC0).
Taban insan geometrisi: Blender Studio, Human Base Meshes (CC0).
Kıyafet referansı: Claes Rålamb, Rålambska dräktboken (1657), kamu malı.

Motor: Unity (HDRP). Modelleme: Blender.

— TARİHSEL KAYNAKÇA —

Şehir 1632 İstanbul'una göre kuruldu ve her öge bir
KADEME taşır:

  T1  belgeli — kaynakta doğrudan yazıyor
  T2  yeniden kurgu — tipoloji belgeli, ölçü değil
  T3  tını — dönem duygusu, belge değil

Başlıca kaynaklar:

  Evliya Çelebi, Seyahatnâme 1. cilt
    (1638 Esnaf Alayı; Hezarfen ve Lagari anlatıları)
  Kâtip Çelebi
  TDV İslâm Ansiklopedisi
  İstanbul Kadı Sicilleri (İSAM/İSAR)
  Mühimme ve narh defterleri
  BOA, A.DVN nr. 25/47 — kahvehane fermanı, 2 Eylül 1633
  Koç Üniversitesi, İstanbul Surları
  Louis Mitler, The Genoese in Galata 1453–1682
  Robert Dankoff, An Ottoman Mentality

Ayrıntılı kaynakça ve her ögenin dayanağı için:
docs/RESEARCH.md

— DÜRÜSTLÜK NOTU —

Hezarfen Ahmed Çelebi'nin uçuşu YALNIZ Evliya Çelebi'de
geçer; başka kayıtla doğrulanmaz. Kese altın ihsanının
mali kayıtlarda izi yoktur. Gereken süzülme oranı (~55:1)
modern delta kanadın (~15:1) çok üstündedir. Kaynakların
çoğu 1632 der, bazıları 1638.

Bu oyun o rivayeti oynatır; belge diye sunmaz.";
    }
}
