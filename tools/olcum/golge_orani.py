"""
Hezarfen: 1632 — **Gölgenin rengini ölç.**

## Neden bu sayı

Bir turda Sûriçi sokağının üstten çekilen karesi simsiyahtı ve karanlık
bölge **her yerde tıpatıp aynı** rengi okuyordu. Gerçek bir gölge
yüzeye göre değişir ve gökyüzünden mavi alır; değişmeyen, mavisiz bir
karanlık gölge değil, **hiç dolaylı ışık almayan yüzey** demektir.

Ölçü tek bir sayıya iniyor: gölgedeki piksellerin **mavi/kırmızı**
oranı. Gökyüzü mavidir, yani gökten sıçrayan ışığı alan bir gölge
1'e yakın, hatta üstünde okur. Aynı yerin göz hizası karesinde
gölgeli kaldırım 0,63 ölçülmüştü; üstten çekilen karede 0,005.

## Nasıl ölçüyor

Parlaklığa göre iki küme: en karanlık %25 (gölge) ve en aydınlık %25
(güneş). İkisinin de mavi/kırmızı oranı ve **saçılımı** yazılıyor.
Saçılım da önemli — sabit bir renk, gölgenin değil sisin işaretidir.

## Fırın öncesi taban ölçüm (2026-09-03, `renders/tur/`, `--gok-yok`)

| kare | gölge mavi/kırmızı | saçılım |
|---|---:|---:|
| 01_dogum | 0,263 | 0,113 |
| 02_dogum_kosu | 0,263 | 0,103 |
| 03_galata_sokak | 0,298 | 0,171 |
| **04_surici** | **0,011** | 0,050 |
| **04_surici_kalabalik** | **0,000** | 0,006 |
| 05_ayasofya | 0,256 | 0,157 |
| **06_kara_surlari** | **0,000** | 0,006 |
| 06_kara_surlari_kalabalik | 0,012 | 0,092 |
| **07_kirsal** | 0,016 | 0,045 |
| 07_kirsal_kalabalik | 0,008 | 0,102 |
| 08_halic_basi | 0,296 | 0,174 |
| **09_marmara** | 0,013 | 0,058 |
| 10_uskudar | 0,271 | 0,112 |

Desen tek cümlede: **binanın olduğu her karede gölge siyah.** Çıplak
araziye bakan kareler 0,26-0,30 okuyor (gökyüzü zemine düşüyor); şehrin
içine bakan kareler 0,00-0,02. Yani kusur "sahne karanlık" değil,
*şehirde dolaylı ışık yok*.

0,26 da hedef değil, yalnızca **aynı sahnede ölçülen sağlıklı komşu**.
Fırın işini yaptıysa kapalı kareler o aileye katılır.

## Sabit bölge: fırın öncesi/sonrası aynı yeri okumak

`04_surici_kalabalik` karesinde sokağın kendisi (`--bolge
430,180,700,660`):

```
golge  parlaklik 0,0070  rgb 0,0176/0,0045/0,0000  mavi/kirmizi 0,000  sacilim 0,001
```

Saçılım **0,001**. O dikdörtgendeki her karanlık piksel aynı renk ve
mavisi tam sıfır. Kusurun en keskin hâli bu satır.

Fırından sonra aynı komut aynı dikdörtgeni okur; "en karanlık %25"
kendi kendini seçtiği için gölge aydınlanınca başka pikselleri
ölçerdi, sabit bölge ölçmez.

Kullanım:
  python tools/olcum/golge_orani.py --gok-yok renders/tur/*.png
  python tools/olcum/golge_orani.py --gok-yok --bolge 430,180,700,660       renders/tur/04_surici_kalabalik.png
"""

import argparse
import sys

import numpy as np
from PIL import Image


def _srgb_to_linear(x):
    x = x.astype(np.float64) / 255.0
    return np.where(x <= 0.04045, x / 12.92, ((x + 0.055) / 1.055) ** 2.4)


def olc(yol, dilim=0.25, gok_yok=False, bolge=None):
    im = np.asarray(Image.open(yol).convert("RGB"))
    # SABIT BOLGE: FIRIN ONCESI/SONRASI AYNI YERI OLCMEK ICIN.
    #
    # En karanlik %25 kendi kendini secer ve bu, tek bir kare icin
    # dogru olcudur. Ama fırın sonrasi kare yeniden cekilecek: golge
    # aydinlanirsa "en karanlik %25" ARTIK BASKA PIKSELLER olur ve iki
    # sayi ayni seyi olcmez. Sabit bir dikdortgen verildiginde olcu
    # ayni yeri okur.
    if bolge:
        x0, y0, x1, y1 = bolge
        im = im[y0:y1, x0:x1]
    lin = _srgb_to_linear(im)
    # Parlaklik: Rec.709.
    y = (lin[..., 0] * 0.2126 + lin[..., 1] * 0.7152
         + lin[..., 2] * 0.0722)
    duz = y.reshape(-1)
    # GOKYUZU OLCUMU BOZAR.
    #
    # Acik bir karede en karanlik %25 gokyuzunu de icerir ve gok
    # tanimi geregi mavidir: `03_galata_sokak` bos bir kum
    # duzlugunde 1,81 okudu, oysa orada olculecek golge yok. Gok
    # pikseli mavinin baskin ve karenin aydinlik oldugu yerdir.
    if gok_yok:
        rgbd = lin.reshape(-1, 3)
        gok = ((rgbd[:, 2] > rgbd[:, 0] * 1.15)
               & (rgbd[:, 2] > rgbd[:, 1] * 1.05)
               & (duz > 0.05))
        duz = np.where(gok, np.nan, duz)
    gecerli = np.flatnonzero(~np.isnan(duz))
    k = max(1, int(gecerli.size * dilim))
    sira = gecerli[np.argsort(duz[gecerli])]
    kume = {
        "golge": sira[:k],
        "gunes": sira[-k:],
    }
    rgb = lin.reshape(-1, 3)
    sonuc = {}
    for ad, idx in kume.items():
        p = rgb[idx]
        kirmizi = np.maximum(p[:, 0], 1e-6)
        oran = p[:, 2] / kirmizi
        sonuc[ad] = dict(
            n=int(idx.size),
            parlaklik=float(np.nanmean(duz[idx])),
            rgb=[float(p[:, i].mean()) for i in range(3)],
            mavi_kirmizi=float(np.median(oran)),
            sacilim=float(oran.std()),
        )
    return sonuc


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("kare", nargs="+")
    ap.add_argument("--dilim", type=float, default=0.25,
                    help="Golge/gunes kumesinin buyuklugu (oran)")
    ap.add_argument("--gok-yok", action="store_true",
                    help="Gokyuzu piksellerini disarida birak")
    ap.add_argument("--bolge", default=None,
                    help="Sabit dikdortgen: x0,y0,x1,y1 (piksel)")
    a = ap.parse_args()

    bolge = None
    if a.bolge:
        bolge = tuple(int(v) for v in a.bolge.split(","))
        if len(bolge) != 4:
            raise SystemExit("[HZ] --bolge x0,y0,x1,y1 bekler")

    for yol in a.kare:
        s = olc(yol, a.dilim, a.gok_yok, bolge)
        print(yol)
        for ad in ("golge", "gunes"):
            d = s[ad]
            print(f"  {ad:6s} parlaklik {d['parlaklik']:.4f}  "
                  f"rgb {d['rgb'][0]:.4f}/{d['rgb'][1]:.4f}/{d['rgb'][2]:.4f}  "
                  f"mavi/kirmizi {d['mavi_kirmizi']:.3f}  "
                  f"sacilim {d['sacilim']:.3f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
