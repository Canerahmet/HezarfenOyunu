"""
Hezarfen: 1632 — Kanat **tüyü** dokusu (prosedürel, kendi telifimiz).

## Neden gerekti

Ölçüldü: `M_Feather` — kanadın bütün yüzeyi — dokusu olarak
`weathered_planks` kullanıyor. Yani **kereste**. Bu, deride bir kez
görülüp düzeltilen kusurun aynısı (`gen_kosele_texture.py`): *bir
yüzeye ait olmadığı bir doku giydirmek, dokusuz bırakmaktan daha az
görünür ama daha yanlıştır — yüzey yalan söyler.*

İnceleme karesinde ne olduğu görülüyor: 9,71 m açıklığındaki kanat,
tahta kaplama bir güverte gibi okunuyor. Oysa varlığın kendi kaynak
notu ne olduğunu yazıyor: *"ahşap çıta iskelet + **kartal tüyü yüzey**
+ deri kayış"* (`gen_kanat.py`, `SOURCE`).

## Ne üretiyor

Tek set, `tuy`: **bindirmeli** tüy yüzeyi.

Kaynak notun kendi cümlesi biçimi veriyor: *"Tüyler UCA DOĞRU üst üste
biner, çünkü bindirme yönü havayı tutan şeydir."* Yani yüzey bir
kiremit dizilimidir — her sıranın ucu bir sonrakinin üstünde durur ve
her bindirme çizgisi kendi gölgesini yapar.

Her tüyde üç katman:

* **Bindirme** — sıra sıra, uca doğru. Uç kenarı düz değil **taraklı**:
  her tüyün ucu bir yay çizer.
* **Omurga (rachis)** — tüyün ortasından geçen kabarık çizgi.
* **Tel (barb)** — omurgadan iki yana ~35° açılan ince çizgiler. Kartal
  telinin aralığı 0,5-1 mm; ortası alındı.

## Ölçüler nereden

Kartal birincil tüyü 35-50 cm boy, 5-8 cm en. Doku 40 cm'lik bir alanı
kaplıyor: bir tüy boyu ve altı tüy eni. Böylece kanatta doku bir kez
döşendiğinde tüy ölçüsü gerçek ölçüsünde okunur.

## Albedo neden nötr

Kumaş, sakal ve köseleyle aynı sözleşme: renk paletten gelir
(`FEATHER`, koyu kahve) ve malzeme onu `_BaseColor` ile çarpar.

Kullanım:
  python tools/textures/gen_tuy_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import proclib as pl   # noqa: E402


#: Dokunun kapladigi gercek alan (m).
SIZE_M = 0.40

#: Tuy eni (m) — kartal birincil tuyu 5-8 cm; ortasi.
TUY_EN = 0.065

#: Gorunen bindirme bandinin boyu (m). Tuy bundan uzundur; gerisi bir
#: sonraki siranin altinda kalir. Kartal kanadinda gorunen band tuy
#: boyunun kabaca ucte biri.
BAND_BOY = 0.115

#: Tel (barb) araligi (m). Kartal telinde 0,5-1 mm.
TEL_ARALIK = 0.00075

#: Tellerin omurgayla yaptigi aci (derece).
TEL_ACI = 35.0

#: Yuzeyin kabartma yuksekligi (m). Bindirme basamagi olculebilir bir
#: sey: ust uste binen iki tuy kabaca tuy kalinligi kadar, ~0,4 mm.
KABARTMA = 0.0004


def _alanlar(res, rng):
    """Yükseklik, omurga maskesi ve bindirme basamağı — hepsi döşenebilir."""
    # Izgara: doku boyu tam sayida tuy ve band tasimali, yoksa dosenmez.
    nx = max(2, int(round(SIZE_M / TUY_EN)))
    ny = max(2, int(round(SIZE_M / BAND_BOY)))
    px = res / float(nx)
    py = res / float(ny)

    yy, xx = np.meshgrid(np.arange(res), np.arange(res), indexing="ij")
    x = xx.astype(np.float64)
    y = yy.astype(np.float64)

    # SIRA KAYMASI: tuyler duvar orgusu gibi sasirtmali dizilir; alt
    # alta gelen bir dikis kanatta cizgi olarak okunurdu.
    r = np.floor(y / py)
    kayma = (r % 2.0) * (px * 0.5)
    u_ham = (x + kayma) / px
    c = np.floor(u_ham)
    u = u_ham - c                      # tuy icinde 0..1

    # UCUN YAYI: tuyun ucu duz degil, ortasi ileri cikan bir yay.
    # `d` bandin ust kenarini o yay kadar kaydirir.
    yay = 1.0 - (2.0 * u - 1.0) ** 2   # 0..1, ortada 1
    d = yay * py * 0.22

    s = (y - (r * py - d)) / py        # 0 = uc kenari, 1 = gizlenen dip
    s = np.clip(s, 0.0, 1.0)

    # BINDIRME: uc kabarik, dip cukur. Basamak band sinirinda.
    h = 1.0 - 0.62 * s ** 0.75

    # OMURGA: tuyun ortasindan gecen kabarik cizgi.
    omurga = np.exp(-((2.0 * u - 1.0) / 0.17) ** 2)
    h = h + 0.22 * omurga * (1.0 - 0.35 * s)

    # TEL: omurgadan iki yana acilan ince cizgiler. Aci isarete gore
    # aynalanir, yoksa teller omurgayi kesip gecerdi.
    aci = np.radians(TEL_ACI) * np.sign(2.0 * u - 1.0)
    # TEL ARALIGI COZUNURLUGE CARPAR.
    #
    # Kartal telinde aralik 0,5-1 mm. 1024 piksellik bir dokuda 40
    # cm'lik alan piksel basina 0,39 mm eder, yani 0,75 mm'lik bir tel
    # 1,9 piksele duser — Nyquist'in altinda ve sonuc moire olur, tel
    # olmaz. Alt sinir dort piksel: 1024'te 1,56 mm. Gercek olcuden
    # kaba, ama moire'dan durust; cozunurluk artarsa kendiliginden
    # gercek olcuye iner.
    adim = max(4.0, TEL_ARALIK * res / SIZE_M)
    faz = (x * np.cos(aci) + y * np.sin(aci)) / adim
    tel = 0.5 + 0.5 * np.cos(2.0 * np.pi * faz)

    # INCE KATMAN ALBEDOYU TASIR, MAKRO KATMAN NORMALI.
    #
    # Ilk denemede hepsi tek bir yukseklik alanindaydi ve normalize
    # edilince sonuc olculdu: albedo neredeyse duz beyaz. Sebep
    # yapisal — bindirme rampasi (0,62) tellerin (0,055) on kati, yani
    # normalize etme tel yapisini ezip geciyor. Iki katman ayri
    # tutulur: goz teli ALBEDODA gorur, bindirmeyi GOLGEDE.
    ince = 0.86 * tel * (1.0 - omurga) + 0.14 * pl.fine_grain(res, rng)
    h = h + 0.055 * tel * (1.0 - omurga)
    h = h + pl.fine_grain(res, rng) * 0.05
    h = np.clip((h - h.min()) / max(1e-6, h.max() - h.min()), 0.0, 1.0)
    ince = np.clip((ince - ince.min())
                   / max(1e-6, ince.max() - ince.min()), 0.0, 1.0)

    # Bindirme cizgisi AO icin ayri tutulur: s'nin sifira yakin oldugu
    # yerde bir onceki tuyun golgesi vardir.
    basamak = np.exp(-(s / 0.06) ** 2)
    return h, ince, omurga, basamak


def build(res, tohum=1632):
    rng = np.random.default_rng(tohum)
    h, ince, omurga, basamak = _alanlar(res, rng)

    # ALBEDO NOTR — renk paletten (`FEATHER`) gelir.
    #
    # Degeri INCE katman tasiyor (tel + tane), makro rampa degil:
    # tellerin arasi tuyun kendi golgesidir ve albedoda gorunur.
    v = np.clip(0.74 + (ince - float(ince.mean())) * 0.34
                - 0.10 * (h - float(h.mean())), 0.32, 0.95)
    # Bindirme cizgisi albedoda da hafifce koyu: tuyun ucu kendi
    # golgesini tasir ve bu, normal haritasinin veremedigi bir sey.
    v = v * (1.0 - 0.16 * basamak)
    bc = (pl.linear_to_srgb(np.stack([v, v, v], axis=-1)) * 255.0)
    bc = bc.round().astype(np.uint8)

    nrm = (pl.normal_from_height(h, KABARTMA * res / SIZE_M) * 255.0
           ).round().astype(np.uint8)

    # PURUZLULUK: tel boyunca duzgun, omurgada daha parlak, bindirme
    # cizgisinde mat (toz orada birikir).
    puru = np.clip(0.58 - 0.14 * omurga + 0.22 * basamak, 0.34, 0.86)

    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    cukur = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    ao = np.clip(1.0 - 0.34 * cukur ** 0.7 - 0.30 * basamak, 0.42, 1.0)

    metal = np.zeros_like(h)
    arm = (np.stack([ao, puru, metal], axis=-1) * 255.0)
    arm = arm.round().astype(np.uint8)
    return bc, nrm, arm, puru, ao


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    bc, nrm, arm, puru, ao = build(a.res)
    d = pl.write_texture_set(
        "tuy", a.res, SIZE_M, bc, nrm, arm,
        meta_extra=dict(
            generated_by="tools/textures/gen_tuy_texture.py",
            role="tuy",
            use="kanat yuzeyi (M_Feather)",
            why="M_Feather dokusu olarak KERESTE kullaniyordu "
                "(weathered_planks) ve 9,71 m'lik kanat inceleme "
                "karesinde tahta kaplama bir guverte gibi okunuyordu. "
                "Varligin kendi kaynak notu yuzeyi zaten yaziyor: "
                "'ahsap cita iskelet + kartal tuyu yuzey + deri kayis'.",
            base_color_note="BC bilerek notr. Renk paletten gelir "
                            "(FEATHER) ve malzeme onu _BaseColor ile "
                            "carpar.",
            feather_width_mm=TUY_EN * 1000.0,
            barb_pitch_mm=TEL_ARALIK * 1000.0,
        ),
        rough=puru, ao=ao, out_root=a.out)
    print(f"[HZ] tuy: {a.res}x{a.res}, {SIZE_M} m -> {d}")
    print(f"[HZ]   tuy eni {TUY_EN * 1000:.0f} mm, band {BAND_BOY * 1000:.0f} mm, "
          f"tel {TEL_ARALIK * 1000:.2f} mm, "
          f"purzuluk {puru.min():.2f}-{puru.max():.2f}, "
          f"AO {ao.min():.2f}-{ao.max():.2f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
