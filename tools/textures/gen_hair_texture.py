"""
Hezarfen: 1632 — Saç/sakal kartı dokusu (prosedürel, KENDİ TELİFİMİZ).

## Neden üretiliyor, indirilmiyor

Saç kartı bir **alfa atlasıdır** ve Poly Haven onu vermiyor. CLAUDE.md
kuralı net: lisansı `refs/LICENSES.md`'de belgelenmemiş hiçbir görsel
indirilemez. Prosedürel üretim bu kısıtın etrafından dolaşmaz,
**kaldırır** — çıktı bizim eserimizdir.

## Ne üretiyor

Dört şeritlik bir **kart atlası**. Her şerit bir saç tutamıdır: kökten
uca giden teller, aralarında boşluk, uçlarda incelme. Kartın U ekseni
tutam genişliği, V ekseni kök→uç.

Şeritler birbirinden farklı: biri sık ve düz (kafa üstü), biri seyrek ve
dağınık (kenar), biri kısa ve kalın (sakal), biri uzun ve ince (uç
tutamları). Tek bir şerit atlas olsaydı bütün saç aynı desende tekrar
ederdi ve o tekrar uzaktan bile okunur.

## Alfa neden ayrı dosya

BC'nin dördüncü kanalına gömmek cazip ama yanlış: Blender BC'yi **sRGB**,
alfayı **Non-Color** okumalı. Aynı dosyada iki farklı renk uzayı
isteyemezsin; birini seçersen öbürü sessizce yanlış olur.

Kullanım:
  python tools/textures/gen_hair_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import proclib as pl   # noqa: E402

TEX_ID = "hair_card"

#: Kartın gerçek dünya ölçüsü (m). Bir tutam ~7 cm genişliğinde,
#: ~22 cm uzunluğunda; atlas dört tutam taşıdığı için U ekseni 4 katı.
SIZE_M = 0.28

#: Şerit tarifleri: (tel sayısı, tel kalınlığı px, uç incelmesi,
#: dağınıklık, kök koyuluğu)
SERITLER = [
    dict(tel=54, kalinlik=2.6, incelme=0.72, dagimik=0.10, kok=0.55),  # sık düz
    dict(tel=30, kalinlik=3.4, incelme=0.55, dagimik=0.34, kok=0.62),  # seyrek
    dict(tel=46, kalinlik=3.0, incelme=0.40, dagimik=0.22, kok=0.70),  # sakal
    dict(tel=22, kalinlik=2.0, incelme=0.88, dagimik=0.46, kok=0.50),  # uç
]

#: Saç rengi — 17. yy Anadolu erkeği için koyu kestane. Siyah değil:
#: tam siyah saç ışığı hiç kırmaz ve oyunda plastik görünür.
KOK_RENK = np.array([0.106, 0.070, 0.048])
UC_RENK = np.array([0.196, 0.138, 0.092])


def _tutam(res, w, tarif, rng):
    """Bir şeridin (alfa, yükseklik, tel kimliği) alanları."""
    alfa = np.zeros((res, w), dtype=np.float32)
    yuk = np.zeros((res, w), dtype=np.float32)
    kimlik = np.zeros((res, w), dtype=np.float32)

    v = np.linspace(0.0, 1.0, res)[:, None]      # 0 = kök, 1 = uç
    for i in range(tarif["tel"]):
        u0 = rng.uniform(0.04, 0.96) * w
        # Tel kökten uca hafifce kayar; dagimik olan daha cok kayar.
        kay = rng.normal(0.0, 1.0) * tarif["dagimik"] * w * 0.5
        merkez = u0 + kay * v[:, 0]
        # Uca dogru incelir; incelme 1'e yakinsa tel ucta yok olur.
        kal = tarif["kalinlik"] * (1.0 - tarif["incelme"] * v[:, 0])
        kal = np.maximum(kal, 0.35)
        # Telin uzunlugu: hepsi ayni yerde bitmez, yoksa uc duz kesilmis
        # gorunur — sac makasla kesilmis gibi durur.
        boy = rng.uniform(0.55, 1.0)
        us = np.arange(w)[None, :]
        d = np.abs(us - merkez[:, None])
        maske = np.clip(1.0 - d / np.maximum(kal[:, None], 1e-3), 0.0, 1.0)
        maske = maske ** 0.7
        maske[v[:, 0] > boy] = 0.0
        alfa = np.maximum(alfa, maske)
        # Yukseklik: telin ortasi yuksek (silindir) — normal haritasi
        # bunu okuyunca teller ayri ayri isik alir.
        yuk = np.maximum(yuk, maske ** 2.0)
        kimlik = np.where(maske > 0.35, rng.uniform(0.0, 1.0), kimlik)
    return alfa, yuk, kimlik


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--seed", type=int, default=1632)
    args = ap.parse_args()

    res = args.res
    rng = np.random.default_rng(args.seed)
    n = len(SERITLER)
    w = res // n

    alfa = np.zeros((res, res), dtype=np.float32)
    yuk = np.zeros((res, res), dtype=np.float32)
    kimlik = np.zeros((res, res), dtype=np.float32)
    kokluk = np.zeros((res, res), dtype=np.float32)

    v = np.linspace(0.0, 1.0, res)[:, None]
    for i, tarif in enumerate(SERITLER):
        a, y, k = _tutam(res, w, tarif, rng)
        x0 = i * w
        alfa[:, x0:x0 + w] = a
        yuk[:, x0:x0 + w] = y
        kimlik[:, x0:x0 + w] = k
        kokluk[:, x0:x0 + w] = np.repeat(
            (1.0 - v) * tarif["kok"] + 0.15, w, axis=1)

    # --- BC ---------------------------------------------------------------
    # Renk kökten uca açılır (uç güneşte solar) ve tel kimliğine göre
    # oynar (her tel aynı tonda değildir).
    t = np.clip(v * 0.85, 0.0, 1.0)
    col = (KOK_RENK[None, None, :] * (1.0 - t[..., None])
           + UC_RENK[None, None, :] * t[..., None])
    # TEL FARKI: her tel ayni tonda degildir ve fark GENIS olmali.
    # Ilk yazimda 0,82-1,18 arasi oynatiyordum ve BC neredeyse duz
    # kahve cikti: alfada apacik okunan teller renkte kayboluyordu.
    # Sac kutlesinin okunmasi tam da o ton farkindan gelir.
    col = col * (0.55 + 0.95 * kimlik[..., None])
    # Kökte koyulaşma: tutamın dibi gölgededir.
    col = col * (1.0 - 0.45 * kokluk[..., None])
    bc = pl._u8(pl.linear_to_srgb(np.clip(col, 0.0, 1.0)))

    # --- N ----------------------------------------------------------------
    nrm = pl._u8(pl.normal_from_height(yuk, strength=1.6))

    # --- R / AO / ARM ------------------------------------------------------
    # Saç PÜRÜZLÜ değildir; ıslak değilse bile ipeksi bir parlaklığı
    # vardır ve tam mat saç peruk gibi görünür.
    rough = np.clip(0.42 + 0.22 * (1.0 - yuk) + 0.08 * kimlik, 0.0, 1.0)
    ao = np.clip(0.35 + 0.65 * yuk, 0.0, 1.0)
    arm = pl._u8(np.stack([ao, rough, np.zeros_like(rough)], axis=-1))

    d = pl.write_texture_set(
        TEX_ID, res, SIZE_M, bc, nrm, arm,
        meta_extra=dict(
            kind="hair_card",
            strips=n,
            note=("Sac/sakal karti alfa atlasi. Alfa AYRI dosyada "
                  "(T_hair_card_A.png): BC sRGB, alfa Non-Color okunmali "
                  "ve ayni dosya iki renk uzayi tasiyamaz."),
            strip_meters=[SIZE_M / n, SIZE_M],
        ),
        rough=rough, ao=ao,
        extra={"A": alfa})

    kapsama = float((alfa > 0.5).mean())
    print(f"[HZ] sac dokusu -> {d}")
    print(f"[HZ] {n} serit, alfa kapsamasi %{kapsama * 100:.1f}")
    # Kapsama denetimi: %20'nin altinda kart bos gorunur, %75'in ustunde
    # alfanin anlami kalmaz (duz levha olur).
    if not 0.20 <= kapsama <= 0.75:
        raise SystemExit(
            f"[HZ] HATA alfa kapsamasi %{kapsama*100:.1f} — kart ya bos "
            "ya duz levha. Tel sayisi/kalinligi ayarlanmali.")


if __name__ == "__main__":
    main()
