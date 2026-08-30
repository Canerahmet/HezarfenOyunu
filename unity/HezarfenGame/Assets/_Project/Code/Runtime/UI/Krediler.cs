namespace Hezarfen.Arayuz
{
    /// <summary>
    /// <b>Krediler ve kaynakça.</b>
    ///
    /// ## Atıf bir nezaket değil, YÜKÜMLÜLÜK
    ///
    /// İki kaynak atıf ŞART koşuyor ve ikisinin de metni
    /// <c>refs/LICENSES.md</c>'de yazılı: <b>Copernicus DEM GLO-30</b>
    /// (arazinin tamamı) ve <b>OpenStreetMap</b> (Ayasofya/Sultanahmet
    /// plan ölçüleri). Atıf eksikse bu bir incelik kusuru değil,
    /// <b>lisans ihlali</b> — ve oyun ticari yayınlanacak.
    ///
    /// Bu yüzden metin bir sabit ve bir test onun içeriğini sınıyor:
    /// krediler ekranı bir gün yeniden yazılırsa atıf sessizce düşmesin.
    /// Nitekim düşmüştü — Copernicus hiç girmemişti.
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
        /// <summary>
        /// Yasal olarak zorunlu atıflar — testle korunuyor.
        ///
        /// <b>Copernicus bu listede YOKTU ve bu bir kusurdu.</b> Oyun
        /// Steam'de ticari yayınlanacak; `refs/LICENSES.md` Copernicus DEM
        /// GLO-30'u <i>"serbest kullanım, atıf zorunlu"</i> diye kaydetmiş
        /// ve şart koşulan metni <c>tools/gis/dem_fetch.py</c> içine
        /// yazmıştı — ama krediler ekranında yalnızca <i>"kamu erişimli
        /// DEM kaynakları"</i> yazıyordu. Test de yanlış şeyi koruyordu:
        /// kullandığımızı sormuyor, listede olanı tutuyordu.
        ///
        /// Arazinin TAMAMI o veriden türetildi; yani eksik olan atıf,
        /// oyunun en büyük tek varlığınınkiydi.
        /// </summary>
        public static readonly string[] ZorunluAtif =
        {
            "OpenStreetMap",
            "Open Database License (ODbL)",
            "Copernicus",
            "Airbus Defence and Space",
        };

        public const string Metin =
@"HEZARFEN · 1632

— VERİ VE VARLIK ATIFLARI —

Yükseklik verisi: Copernicus DEM GLO-30.
Produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014
and (c) Airbus Defence and Space GmbH 2014-2018 provided under
COPERNICUS by the European Union and ESA; all rights reserved.
Arazi, kıyı çizgisi ve topografya bu veriden türetilmiştir.

Yapı planı ölçüleri (Ayasofya, Sultanahmet):
Contains information from OpenStreetMap and OpenStreetMap
Foundation, which is made available under the
Open Database License (ODbL).
Geometri kopyalanmadı; plandan yalnızca ölçü okundu.

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
