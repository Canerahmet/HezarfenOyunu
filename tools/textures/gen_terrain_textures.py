"""
Hezarfen: 1632 — Arazi örtüsü dokuları (prosedürel, KENDİ TELİFİMİZ).

## Neden

Faz 1'de arazi DEM'den geldi ve **hiç katman atanmadı**: `TerrainLit` malzemesi
katmansız kalınca Unity tek düz bir renk çizer. Bu, ışık gelene kadar
görünmedi — ADR 0023 geçici aydınlatmayı kurunca yaya seviyesinde zeminin
"düz kum rengi bir zemin, çıplak bir yamaç kütlesi" olduğu ortaya çıktı ve
en zayıf halka oldu.

## Neden dört katman, beş değil

Unity splatmap'i RGBA dokularda taşır: **4 katman = 1 doku**. Beşinci katman
ikinci bir splat dokusu açar; bellek iki katına çıkar ve arazi kabuğu her
karede bir kez daha örneklenir. Dört, bedava olan sınırdır ve İstanbul yamacı
için yeter:

    toprak  — işlenmiş/çiğnenmiş düzlük        (Earth)
    ot      — yamacın varsayılan örtüsü        (Grass)
    kaya    — dik yamaçta çıkan anakaya        (Rock)
    kıyı    — deniz seviyesi bandı ve tabanı   (Shore)

## Mevsim: İLKBAHAR (ADR 0025)

Palet ilkbahara göre kurulur — taze ot, nemli toprak, çatlakta yosun. Sebep
zevk değil tutarlılık: oyunun birinci tasarım direği **lodos**tur ve lodos
yılın soğuk yarısının rüzgârıdır. "Yaz sonu" paleti, uçuşu taşıyan rüzgâr
sistemiyle çelişiyordu.

## Bu dokular Blender'a GİTMEZ

`gen_foliage` ve `gen_lead` çıktıları `art/textures/generated/` altına yazılır,
çünkü onları Blender tarafındaki `materials.py` okur. Arazi katmanlarını okuyan
hiçbir .blend yok: tek tüketici `TerrainLit`. Kanonik bir kopya, kimsenin
açmadığı 40 MB olurdu. Bu yüzden çıktı doğrudan Unity'ye ve yanında bir
bildirim dosyasına yazılır.

## Maske düzeni TerrainLit'te FARKLIDIR

    HDRP/Lit         maske B = detay maskesi (kullanılmıyor)
    HDRP/TerrainLit  maske B = **YÜKSEKLİK** — katmanlar arası yükseklik
                     harmanlaması bu kanalı okur

Yani `build_unity_maps.py`'nin `build_mask`'ı burada kullanılamaz: B'ye 0
yazsaydık yükseklik harmanı açıkken **hiçbir katman diğerini yenemez** ve
geçişler düz doğrusal solmaya döner. Maske burada üretilir.

## Telif

Girdi yalnızca tohumlanmış sayı üretecidir; üçüncü taraf verisi yoktur.

Kullanım:
  python tools/textures/gen_terrain_textures.py [--res 1024]
"""

import argparse
import json
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl          # noqa: E402

OUT_DIR = os.path.join("unity", "HezarfenGame", "Assets", "_Project",
                       "Art", "Textures", "Terrain")

# Karo ölçüsü: yerde bir karonun kaç metre olduğu.
#
# Küçük karo yaya seviyesinde keskin, ama HAVADAN tekrarı görünür kılar — bu
# bir uçuş oyunu, arazi çoğunlukla yukarıdan görülür. Büyük karo tersi.
# Seçilen değerler 1024 px'te 4–9 mm/texel verir (yaya için fazlasıyla) ve
# tekrarın dalga boyunu 5–9 m'ye çıkarır. Tekrarın asıl çözümü doku bombalama
# (stokastik örnekleme) — gölgelendirici işi, burada değil (bkz. ADR 0024 §7).
# Katmanlarin ORTALAMA renkleri birbirinden ayrilmak ZORUNDA. Ilk uretimde
# dordunun de ton acisi 32-43 derece arasindaydi (hepsi turuncu-kahve) ve
# Kaya ile Kiyi'nin ortalamalari arasinda yalnizca 5 seviye vardi: yakindan
# dordu de dogru gorunuyordu ama HAVADAN manzara tek renk bir col oluyordu,
# cunku uzakta doku mip ortalamasina iner ve geriye yalnizca ortalama renk
# kalir. CIE76 farki 12'nin altindaki iki katman, uzaktan tek katmandir.
PALETTE_DE_MIN = 12.0

# Karo TEKRARININ gorunurlugu. Ilk maki denemesinde calilar 1,1 m capindaydi
# ve 5 m'lik karoya yalnizca ~4,5 tane sigiyordu; 3x3 dosendiginde yesil
# lekeler izgara gibi okunuyordu. Ne "ince" ne de "kaba" olcut bunu yakaladi:
# ikisi de PARLAKLIK olcuyor, tekrari ele veren sey ise RENK'ti (yesil-altin).
# Ucuncu kez ayni ders — bir bandi olcmeyen alet o bandin kusurunu gormez.
# Olcu artik Lab: karo 1/8'i bloklara indirgenir, komsulugundan dE sapmasi
# alinir.
MACRO_DE_MAX = 1.0

FINE_MIN = 2.0        # 3 px komsulukta ortalama sapma (0-255) — YAKIN okunabilirlik
# 5,0 ile basladi, 3,0'a cekildi: 3,39 olcen kuru ot 3x3 karo goruntusunde
# tekrari ACIKTAN okunuyordu, otekiler (2,4-3,0) okunmuyordu. Esik gozle
# degil, tekrarin gorunur oldugu yerden kalibre edildi.
COARSE_MAX = 3.0      # 20 cm blok — metre olcegindeki lekelenme (tekrar)

SPECS = [
    dict(id="Earth",    size=6.0, seed=71),
    dict(id="Grass",    size=5.0, seed=73),
    dict(id="Rock",     size=9.0, seed=79),
    dict(id="Shore",    size=4.0, seed=83),
]


def _u8(a):
    return (np.clip(a, 0.0, 1.0) * 255.0).round().astype(np.uint8)


def _finish(h_m, size_m, res, col_lin, rough, metal, ao):
    """Ortak son adım: metre cinsi yükseklikten normal + maske paketleme."""
    amp = float(h_m.max() - h_m.min())
    h = pl.normalize(h_m)

    bc = _u8(pl.linear_to_srgb(col_lin))
    nrm = _u8(pl.normal_from_height(h, strength=amp * res / size_m))

    # TerrainLit maskesi: R metaliklik, G AO, B YUKSEKLIK, A parlaklik.
    mask = _u8(np.stack([metal, ao, h, 1.0 - rough], axis=-1))
    return bc, nrm, mask, amp


def detail_energy(gray, k=3):
    """Her pikselin k×k komşu ortalamasından ortalama sapması (sarmalı)."""
    from numpy.lib.stride_tricks import sliding_window_view
    p = np.pad(gray, k // 2, mode="wrap")
    w = sliding_window_view(p, (k, k))
    return float(np.abs(gray - w.mean(axis=(2, 3))).mean())


def coarse_energy(gray, k):
    """
    Metre ölçeğindeki **lekelenme**: önce k×k bloklara indirgenir, sonra o
    küçük görüntünün kendi 3×3 enerjisi ölçülür.

    İndirgeme şart. Önce "her pikselin 20 cm komşu ortalamasından sapması"
    yazılmıştı ve **ince taneyi de sayıyordu**: dokulara yakın ayrıntı
    eklendiğinde kaba sayı da yükseldi, yani ölçüt iki bandı ayıramıyordu.
    Bloklama ince bandı ortalamayla siler; geriye yalnızca lekelenme kalır.
    """
    n = gray.shape[0] // k * k
    g = gray[:n, :n].reshape(n // k, k, n // k, k).mean(axis=(1, 3))
    return detail_energy(g, 3)


def macro_energy(bc, res):
    """
    Karo ölçeğine yakın dalga boyundaki **renkli** yapı — tekrarı ele veren şey.

    Parlaklık ölçütleri bunu göremez: bir katman aynı parlaklıkta iki farklı
    renk taşıyabilir (yeşil maki / altın ot) ve karo döşendiğinde ızgara
    yalnızca renkten okunur. Ölçü bu yüzden Lab'da: görüntü karo 1/8'i
    bloklara indirgenir, her bloğun 3×3 komşuluğundan ΔE sapması alınır.
    """
    from numpy.lib.stride_tricks import sliding_window_view
    k = max(2, res // 8)
    lab = pl.lab_image(bc)
    n = lab.shape[0] // k * k
    small = lab[:n, :n].reshape(n // k, k, n // k, k, 3).mean(axis=(1, 3))
    p = np.pad(small, ((1, 1), (1, 1), (0, 0)), mode="wrap")
    w = sliding_window_view(p, (3, 3), axis=(0, 1)).mean(axis=(-2, -1))
    return float(np.linalg.norm(small - w, axis=-1).mean())


def report(bc, size_m, res):
    """
    Dokunun ayrıntısının HANGİ ÖLÇEKTE olduğunu ölçer.

    Bu ölçüt üretimin bir parçası, sonradan yapılan bir denetim değil — çünkü
    ilk yazımda dördü de gözle "iyi" görünüyordu ve ölçüm hepsinin **20 cm'nin
    altında hiçbir içeriği olmadığını** gösterdi (ince/kaba 0,10–0,21). Yerden
    1,70 m yükseklikte bakan bir yayanın gördüğü ölçek santimetredir; metre
    ölçeğindeki lekeler yakından "sis", uzaktan da **tekrar** olarak okunur.

    İki **bağımsız** eşik: ince ≥ 2,0 (yakından bakınca içerik var mı) ve
    kaba ≤ 5,0 (metre ölçeğinde lekelenme — havadan tekrarı ele veren şey).
    İlk yazımda tek bir `ince/kaba` oranı vardı ve iki ayrı derdi tek sayıya
    sıkıştırıyordu: kaya ince eşiğini rahat geçtiği hâlde oran yüzünden
    "zayıf" damgası yiyordu. Aynı hata ışık turunda da yapılmıştı (ADR 0023):
    karanlık ışıkla karanlık malzemeyi tek sayı ayırt edemez.
    """
    g = np.asarray(Image.fromarray(bc).convert("L"), dtype=np.float64)
    ppm = res / size_m
    fine = detail_energy(g, 3)                          # ~birkaç mm
    coarse = coarse_energy(g, max(2, int(round(ppm * 0.20))))       # 20 cm blok
    return fine, coarse, macro_energy(bc, res), float(g.mean()), float(g.std())


def _ao_from_curvature(h, depth=0.45, floor=0.35):
    """
    AO = ışık görmeyen yer. Ölçüt yükseklik DEĞİL eğriliktir (Laplace):
    pozitif değer içbükey, yani vadi tabanı demektir. Alçak ama açık bir
    düzlük gölgede değildir; dar bir çatlağın dibi gölgededir.
    """
    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    valley = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    return np.clip(1.0 - depth * valley ** 0.6, floor, 1.0)


# ------------------------------------------------------------------ toprak

def build_earth(res, size_m, rng):
    """
    Sıkışmış, çiğnenmiş ya da işlenmiş toprak: iri tonal lekeler + saçılmış
    çakıl + iz oyukları.

    İlkbahar toprağı **nemlidir**: koyu ve az çatlaklı. Oyuk deseni yine de
    gerekli, çünkü yaya karonun kaç metre olduğunu ondan okur. Öbek toplamıyla
    çatlak çıkmaz, hücre gürültüsü gerekir (proclib.worley).
    """
    coarse = pl.blob_field(res, 55, res / 8.0, 1.5, rng)
    # Çakıl SAYISI ölçüldü, seçilmedi: 4 200 taş / 36 m² ≈ 117 taş/m², yani
    # ~9 cm'de bir taş. Çiğnenmiş bir yol yüzeyi budur. 1 300'de metrekareye
    # 36 taş düşüyordu ve yüzey yakından bomboştu.
    # `mode="max"`: cakil AYRIK bir nesnedir. Toplandiginda 4 200 tas
    # birbirinin icinde eriyip duz bir alan oluyordu — yakin kare zimpara
    # kagidiydi, tek bir tas secilmiyordu. Ustelik sayi da azaldi (1 800):
    # ortusme orani ~0,17'ye dusunce taslar ayri ayri okunuyor.
    pebble = pl.blob_field(res, 1800, res / 190.0, 1.3, rng, exponent=0.45,
                           radius_jitter=(0.45, 1.70), window=True, mode="max")
    dust = pl.fine_grain(res, rng, passes=4)
    # 1-2 px tane: yayanın gerçekten gördüğü ölçek. Genlik `stretch` ile
    # kurulur, çünkü ham `fine_grain` normalize edilse bile kütlesi 0,5'te
    # sıkışıktır ve renge katkısı ölçüldüğünde ±%5'te kalıyordu.
    grit = pl.stretch(pl.fine_grain(res, rng, passes=1), 0.22)

    # Çatlak ölçek işaretidir ama İLKBAHARDA nemli toprak derin çatlamaz —
    # karartma 0,34'ten 0,16'ya indi. Kalanı iz ve tekerlek oyuğu olarak okunur.
    f1, f2 = pl.worley(res, 340, rng)
    crack = np.exp(-(((f2 - f1) / (res * 0.0026)) ** 2))

    h_m = (0.011 * pebble - 0.004 * crack
           + 0.005 * coarse + 0.0015 * (dust - 0.5) + 0.0022 * (grit - 0.5))

    base = pl.srgb_to_linear((104, 88, 68))      # nemli kahverengi toprak
    damp = pl.srgb_to_linear((62, 52, 42))       # oyuk dibi, ıslak
    stone = pl.srgb_to_linear((132, 124, 110))   # çakıl

    tone = np.clip(0.40 + 0.30 * coarse + 0.30 * (dust - 0.5), 0.0, 1.0)
    col = damp * (1.0 - tone[..., None]) + base * tone[..., None]
    p = np.clip((pebble - 0.30) / 0.55, 0.0, 1.0)
    # Cakil albedo'su tabana YAKIN olmali. 0,80 ile degistirildiginde taslar
    # yere serpilmis patlamis misir gibi okunuyordu; gercekte bir tasi ele
    # veren sey rengi degil BICIMIDIR ve bicim normal haritasindan gelir
    # (yukseklige zaten katiliyor). Albedo yalnizca hafifce griye kayar.
    col = col * (1.0 - 0.30 * p[..., None]) + stone * (0.30 * p[..., None])
    col = col * (1.0 - 0.16 * crack[..., None])
    col *= (0.55 + 0.90 * grit)[..., None]       # toz tanesi (ort 1,0)

    rough = np.clip(0.94 - 0.18 * p - 0.05 * (dust - 0.5), 0.0, 1.0)
    metal = np.zeros((res, res))
    return h_m, col, rough, metal


# --------------------------------------------------------------- yamaç otu

def build_grass(res, size_m, rng):
    """
    Yamacın varsayılan örtüsü — **ilkbahar**: taze ot + maki, aralarından
    toprak ve geçen yazdan kalmış seyrek kuru sap.

    ## Mevsim neden ilkbahar (ADR 0025)

    İlk üretim "yaz sonu kuru"ydu ve **oyunun kendi tasarımıyla çelişiyordu**:
    birinci tasarım direği lodostur (PLAN.md §0), lodos ise güneybatıdan eser
    ve İstanbul'da yılın soğuk yarısının rüzgârıdır; yaz sonuna hâkim olan
    poyraz kuzeydoğudandır. Uçuşu taşıyan rüzgârla manzaranın mevsimi aynı
    olmalı. Uçuşun günü kaynaklarda yok (RESEARCH.md §4.4(f)).

    ## Katman neden ÜÇ renk taşıyor

    Tek düze bir yeşil halı hem yanlış hem ölçeksizdir. Akdeniz yamacında
    ilkbaharda üç şey bir aradadır: **taze tek yıllık ot** (parlak sarı-yeşil),
    **maki** (funda/sakız/mersin — koyu, mavimsi yeşil, sert yapraklı) ve
    aralardan görünen toprak. Geçen yılın kuru sapları da seyrek durur.
    """
    # Serpme EŞİT ARALIKLI: rastgele serpme kendi başına kümelenir (Poisson) ve
    # o yoğunluk dalgalanması doğrudan makro banda düşer — maki bir kez böyle
    # kurulmuştu ve karo tekrarını ele veriyordu (makro ΔE 1,53; eşik 1,0).
    bush = pl.even_tufts(res, 620, res * 0.030, rng, falloff=1.3)
    clump = pl.even_tufts(res, 900, res * 0.026, rng, falloff=0.9)

    # Sap: 2,3 cm eninde, ×6 uzatmayla ~14 cm boyunda; `max` kipinde ayrı ayrı
    # kalır. Otu ot yapan şey budur, tutam yalnızca nerede bittiğini söyler.
    blade = pl.blob_field(res, 6000, res / 300.0, 6.0, rng, exponent=0.5,
                          window=True, mode="max")
    # Geçen yazdan kalan kuru sap: SEYREK. Tümüyle yeşil bir yamaç, tümüyle
    # sarı bir yamaç kadar tek düzedir.
    dead = pl.blob_field(res, 900, res / 320.0, 6.0, rng, exponent=0.6,
                         window=True, mode="max")
    dust = pl.fine_grain(res, rng, passes=3)
    grit = pl.stretch(pl.fine_grain(res, rng, passes=1), 0.20)

    h_m = (0.020 * clump + 0.009 * blade + 0.022 * bush
           + 0.001 * (dust - 0.5) + 0.0015 * (grit - 0.5))

    green = pl.srgb_to_linear((92, 118, 58))       # taze ot, gölgede
    green_lit = pl.srgb_to_linear((136, 162, 82))  # taze ot, ışıkta
    straw = pl.srgb_to_linear((162, 148, 100))     # geçen yıldan kalan sap
    soil = pl.srgb_to_linear((104, 88, 70))        # aradan görünen toprak
    scrub = pl.srgb_to_linear((70, 92, 60))        # maki — koyu, mavimsi yeşil
    scrub_lit = pl.srgb_to_linear((104, 126, 82))

    # Örtü ORANI: yamaç çoğunlukla otludur, toprak yalnızca ARALARDAN görünür.
    cover = 0.52 + 0.48 * np.clip(clump / 0.62, 0.0, 1.0)
    grass = green * (1.0 - blade[..., None]) + green_lit * blade[..., None]
    col = soil * (1.0 - cover[..., None]) + grass * cover[..., None]

    d = np.clip(dead / 0.70, 0.0, 1.0)
    col = col * (1.0 - 0.55 * d[..., None]) + straw * (0.55 * d[..., None])

    # Çalı otun ÜSTÜNE oturur; yaprak kütlesi kendi içinde ışık alır.
    b = np.clip(bush / 0.62, 0.0, 1.0) ** 0.8
    leaf = scrub * (1.0 - blade[..., None]) + scrub_lit * blade[..., None]
    col = col * (1.0 - b[..., None]) + leaf * b[..., None]

    col *= (0.90 + 0.20 * (dust - 0.5))[..., None]
    col *= (0.58 + 0.84 * grit)[..., None]

    # Taze ot kurudan daha az pürüzlüdür (mumsu yaprak) ama parlak değildir.
    rough = np.clip(0.84 - 0.10 * cover, 0.0, 1.0)
    metal = np.zeros((res, res))
    return h_m, col, rough, metal


# --------------------------------------------------------------------- kaya

def build_rock(res, size_m, rng):
    """
    Kırıklı anakaya: plakalar + aralarındaki derin çatlaklar.

    Renk gri-kahvedir, beyaz değil: İstanbul yamacının anakayası kireçtaşı
    kadar açık değildir. Ton **T3 sanatsal yorumdur** — belgeli bir kaynak
    ölçmedi (ADR 0024 §6).
    """
    # Kaya İKİ ölçekte kırılır: büyük plakalar (~0,8 m) ve onların üstündeki
    # ince kılcal ağ (~0,15 m). Tek ölçek bırakılırsa yüzey döşenmiş taş
    # kaplama gibi okunur — ölçüldü: bütün ayrıntı 20 cm üstündeydi.
    f1, f2 = pl.worley(res, 130, rng)
    plate = pl.normalize(f1)
    crack = np.exp(-(((f2 - f1) / (res * 0.004)) ** 2))

    g1, g2 = pl.worley(res, 1600, rng)
    hair = np.exp(-(((g2 - g1) / (res * 0.0010)) ** 2))

    chip = pl.blob_field(res, 900, res / 70.0, 1.6, rng, exponent=1.0,
                         window=True, mode="max")
    grit = pl.fine_grain(res, rng, passes=2)
    sharp = pl.stretch(pl.fine_grain(res, rng, passes=1), 0.20)

    h_m = (0.070 * plate - 0.060 * crack - 0.012 * hair
           + 0.012 * chip + 0.003 * (grit - 0.5) + 0.004 * (sharp - 0.5))

    # Kaya GRI olmali: ilk paletinde doygunlugu 0,13'tu ve kiyi kumundan
    # yalnizca 5 seviye ayriliyordu — uzaktan ikisi tek katmandi.
    light = pl.srgb_to_linear((162, 160, 156))
    dark = pl.srgb_to_linear((76, 76, 78))
    warm = pl.srgb_to_linear((132, 116, 96))     # demir lekesi
    moss = pl.srgb_to_linear((84, 100, 64))      # ilkbaharda çatlakta yosun

    # Plaka kontrasti 0,55 -> 0,38: kaba(20 cm) enerji 7,07 olculmustu ve
    # metre olcegindeki bu dalgalanma HAVADAN dokunun tekrarini ele veriyor.
    tone = np.clip(0.34 + 0.38 * plate + 0.25 * (grit - 0.5), 0.0, 1.0)
    col = dark * (1.0 - tone[..., None]) + light * tone[..., None]
    stain = np.clip((chip - 0.55) / 0.40, 0.0, 1.0)
    col = col * (1.0 - 0.22 * stain[..., None]) + warm * (0.22 * stain[..., None])
    col = col * (1.0 - 0.60 * crack[..., None]) * (1.0 - 0.30 * hair[..., None])
    # Yosun yalnız ÇATLAKTA tutunur: su orada durur. Kayayı yeşile boyamak
    # değil, çatlağı canlandırmak.
    mo = np.clip(crack * 0.40 + hair * 0.20, 0.0, 0.45)
    col = col * (1.0 - mo[..., None]) + moss * mo[..., None]
    col *= (0.60 + 0.80 * sharp)[..., None]      # kırık yüzeyin tanesi

    rough = np.clip(0.82 - 0.12 * plate + 0.10 * crack, 0.0, 1.0)
    metal = np.zeros((res, res))
    return h_m, col, rough, metal


# --------------------------------------------------------------------- kıyı

def build_shore(res, size_m, rng):
    """
    Kıyı ve deniz tabanı: ince kum + çakıl.

    Dalga izi (ripple) KOYULMADI: arazi UV'si dünya hizalıdır, yani izler
    15 km boyunca aynı yöne bakardı — kıyı çizgisi eğri olduğu için bu her
    yerde yanlış olurdu.
    """
    # Çakıl 800 → 3 000 (188 taş/m², ~7 cm arayla) ve iri lekeler 40 → 110'a
    # küçültüldü. İlk hâlinde 7 m'lik yumuşak lekeler yüzeyi kahverengi bir
    # sise çeviriyordu; kum tanesi ise renk karışımında neredeyse yoktu.
    pebble = pl.blob_field(res, 1200, res / 150.0, 1.35, rng, exponent=0.5,
                           radius_jitter=(0.45, 1.70), window=True, mode="max")
    patch = pl.blob_field(res, 110, res / 14.0, 1.6, rng, window=True)
    sand = pl.stretch(pl.fine_grain(res, rng, passes=1), 0.20)

    h_m = 0.014 * pebble + 0.003 * patch + 0.0035 * (sand - 0.5)

    # Kıyı, Kaya'dan PARLAKLIKLA değil TONLA ayrılır.
    #
    # İlk palet ayrımında kıyıyı açarak (186/196) ayırmıştım ve ölçüt geçti
    # ama sonuç yanlıştı: kıyı şeridi havadan bembeyaz bir kordon gibi
    # okunuyordu. İstanbul kıyısı beyaz kum değil **koyu çakıl ve taş**tır.
    # Ayrım artık sıcaklıktan geliyor — kaya nötr gri, kıyı kahverengiye
    # çalan çakıl — ve ΔE yine eşiğin üstünde.
    wet = pl.srgb_to_linear((86, 76, 62))        # ıslak, koyu
    dry = pl.srgb_to_linear((150, 128, 96))     # kurumuş çakıl
    stone = pl.srgb_to_linear((168, 150, 118))

    tone = np.clip(0.35 + 0.45 * patch + 0.30 * (sand - 0.5), 0.0, 1.0)
    col = wet * (1.0 - tone[..., None]) + dry * tone[..., None]
    p = np.clip((pebble - 0.32) / 0.52, 0.0, 1.0)
    col = col * (1.0 - 0.32 * p[..., None]) + stone * (0.32 * p[..., None])
    col *= (0.62 + 0.76 * sand)[..., None]       # kum tanesi

    rough = np.clip(0.78 - 0.14 * p - 0.08 * (1.0 - tone), 0.0, 1.0)
    metal = np.zeros((res, res))
    return h_m, col, rough, metal


BUILDERS = dict(Earth=build_earth, Grass=build_grass,
                Rock=build_rock, Shore=build_shore)

# Katmanın ne anlattığı — bildirime yazılır ve Unity tarafı bunu kullanıcıya
# gösterir. Kaynak niteliksel: örtünün DAĞILIMI eğim/kot kuralıdır, dokunun
# KENDİSİ sanatsal yorumdur.
NOTES = dict(
    Earth="Islenmis/cignenmis duzluk; mahalle ve bostan zemini.",
    Grass="Yamacin varsayilan ortusu; ilkbaharda taze ot + maki.",
    Rock="Dik yamacta cikan anakaya.",
    Shore="Deniz seviyesi bandi ve deniz tabani; kum-cakil.",
)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=OUT_DIR)
    a = ap.parse_args()

    os.makedirs(a.out, exist_ok=True)
    layers, total, failed, means = [], 0, [], {}

    for spec in SPECS:
        rng = np.random.default_rng(spec["seed"])
        h_m, col, rough, metal = BUILDERS[spec["id"]](a.res, spec["size"], rng)
        ao = _ao_from_curvature(pl.normalize(h_m))
        bc, nrm, mask, amp = _finish(h_m, spec["size"], a.res, col, rough,
                                     metal, ao)

        files = {}
        for key, arr in (("BC", bc), ("N", nrm), ("MASK", mask)):
            fn = f"T_Terrain{spec['id']}_{key}.png"
            Image.fromarray(arr).save(os.path.join(a.out, fn))
            files[key] = fn
            total += os.path.getsize(os.path.join(a.out, fn))

        layers.append(dict(
            name=spec["id"], sizeMeters=float(spec["size"]),
            baseColorFile=files["BC"], normalFile=files["N"],
            maskFile=files["MASK"], note=NOTES[spec["id"]],
        ))
        means[spec["id"]] = bc.reshape(-1, 3).mean(axis=0)
        fine, coarse, macro, mean, std = report(bc, spec["size"], a.res)
        ok = fine >= FINE_MIN and coarse <= COARSE_MAX and macro <= MACRO_DE_MAX
        if not ok:
            failed.append(f"{spec['id']} (ince {fine:.2f}, kaba {coarse:.2f}, "
                          f"makro {macro:.2f})")
        print(f"[HZ] Terrain{spec['id']}: {a.res}px / {spec['size']} m "
              f"({a.res / spec['size']:.0f} px/m), kabarti {amp * 1000:.0f} mm, "
              f"purzuluk {rough.min():.2f}-{rough.max():.2f}")
        print(f"[HZ]   ayrinti: ince {fine:5.2f} | kaba(20cm) {coarse:5.2f} | "
              f"makro-dE {macro:4.2f} | "
              f"ort {mean:5.1f} std {std:4.1f}  "
              f"{'OK' if ok else '<-- ZAYIF'}")

    # PALET AYRIMI: uzaktan katmanlar birbirinden ayirt edilebiliyor mu.
    print("[HZ] palet ayrimi (CIE76, esik %.0f):" % PALETTE_DE_MIN)
    ids = [sp["id"] for sp in SPECS]
    for i in range(len(ids)):
        for j in range(i + 1, len(ids)):
            de = pl.delta_e(means[ids[i]], means[ids[j]])
            mark = "OK" if de >= PALETTE_DE_MIN else "<-- COK YAKIN"
            print(f"[HZ]   {ids[i]:9s} <-> {ids[j]:9s}  dE {de:5.1f}  {mark}")
            if de < PALETTE_DE_MIN:
                failed.append(f"{ids[i]}~{ids[j]} (dE {de:.1f})")

    # Bildirim: ad-dosya-karo eslesmesi TEK yerde yasar. Unity tarafi
    # (TerrainCoverBuilder) katman varliklarini bundan uretir; iki tarafta
    # elle tekrarlanan bir liste, birinde degisip otekinde degismeyen bir
    # liste demektir.
    mpath = os.path.join(a.out, "terrain_layers.json")
    with open(mpath, "w", encoding="utf-8") as fh:
        json.dump(dict(
            generated_by="tools/textures/gen_terrain_textures.py",
            license="Kendi eserimiz — ucuncu taraf hakki yok",
            mask_layout="R=Metallic G=AO B=Height A=Smoothness (HDRP TerrainLit)",
            resolution=a.res, layers=layers,
        ), fh, ensure_ascii=False, indent=1)

    # Artik dosyalari sil: katman ADI degistiginde (DryGrass -> Grass) eski
    # dokular klasorde kalirsa Unity onlari ice aktarmaya ve bellekte tasimaya
    # devam eder — silinmis bir katmanin dokusu sessizce hayatta kalir.
    keep = {f for lyr in layers for f in (lyr["baseColorFile"], lyr["normalFile"],
                                          lyr["maskFile"])}
    keep.add("terrain_layers.json")
    removed = []
    for f in os.listdir(a.out):
        if f.endswith(".meta") or f in keep:
            continue                    # .meta'yi Unity kendi temizler
        os.remove(os.path.join(a.out, f))
        removed.append(f)
    if removed:
        print(f"[HZ] {len(removed)} artik doku silindi: {', '.join(sorted(removed))}")

    print(f"[HZ] {len(layers)} arazi katmani ({total / 1e6:.1f} MB) -> {mpath}")
    if failed:
        # Dosyalar yazildi ama esik tutmadi: sessizce gecmek, "iyi gorunuyordu"
        # ile "olculdu"yu yeniden karistirmak olurdu.
        print(f"[HZ] UYARI — yakin ayrinti esigi tutmayan katman: "
              f"{', '.join(failed)} "
              f"(ince >= {FINE_MIN}, kaba <= {COARSE_MAX}, "
              f"makro <= {MACRO_DE_MAX})")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
