# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — **Bir noktanin cevresinde ne var?**

## Neden bu alet

Caner'in tekrar eden sikayeti "bos zemin". Kare bunu gosterir ama
tartisilir bir sey soyler: "bos gorunuyor". Sayi tartismayi bitirir —
bir noktanin 60 / 120 / 250 m yaricapinda kac yerlestirilmis nesne var.

Ayrica tur duraklarini degerlendirirken gerekli: bir durak "burada
kimse yok" diyorsa, sebep icerigin olmamasi da olabilir, kameranin
yanlis yone bakmasi da. Ikisi ayni kareden ayirt edilemez; bu alet
birincisini olcer.

## Ne sayar, ne saymaz

Sahne dosyalarindaki **konumu olan prefab ornekleri** sayilir. Tek
mesh'e birlestirilmis seyler (bahce/bostan duvarlari, yol seritleri,
kaideler) sayilmaz — onlarin konumu yoktur, birlesik mesh'in icindedir.
Yani sayi bir ALT SINIR: "en az bu kadar nesne var" der, "hicbir sey
yok" demez.

## Ilk olcum (2026-09-04) — tur duraklari

```
durak                60 m     120 m    250 m
07_kirsal               0         0        8
08_halic_basi           0         0        0
03_galata_sokak       121       388     1406
04_surici              54       235      807
```

Iki durak gercekten bos: kirsal duragin 120 m'sinde **hicbir sey** yok,
Halic basinin 250 m'sinde de. Sehir duraklari 54-121 nesne okuyor.
Yani o iki karedeki bosluk bir bakis acisi sorunu degil, icerik
eksikligi.

## Kullanim

    python tools/olcum/yakin_doku.py -2500 -600
    python tools/olcum/yakin_doku.py 300 100 --yaricap 40,80,160
"""

import argparse
import collections
import glob
import io
import math
import os
import re
import sys

SAHNE_KOKU = os.path.join("unity", "HezarfenGame", "Assets", "_Project",
                          "Scenes")


def ornekler():
    """Butun sahnelerdeki `(ad, x, z)` — konumu olan prefab ornekleri."""
    cikti = []
    for f in glob.glob(os.path.join(SAHNE_KOKU, "**", "*.unity"),
                       recursive=True):
        s = io.open(f, encoding="utf-8", errors="replace").read()
        for blok in s.split("--- !u!1001 &")[1:]:
            ad = re.search(r"PF_(\w+)", blok)
            x = re.search(r"propertyPath: m_LocalPosition\.x\n      value: "
                          r"([-\d.eE]+)", blok)
            z = re.search(r"propertyPath: m_LocalPosition\.z\n      value: "
                          r"([-\d.eE]+)", blok)
            if ad and x and z:
                cikti.append((ad.group(1), float(x.group(1)),
                              float(z.group(1))))
    return cikti


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("x", type=float)
    ap.add_argument("z", type=float)
    ap.add_argument("--yaricap", default="60,120,250",
                    help="Virgulle ayrik yaricaplar (m)")
    a = ap.parse_args()

    hepsi = ornekler()
    print(f"[HZ] sahnelerde konumlu prefab ornegi: {len(hepsi)}")
    for r in [float(v) for v in a.yaricap.split(",")]:
        yakin = [n for n in hepsi
                 if math.hypot(n[1] - a.x, n[2] - a.z) <= r]
        c = collections.Counter(n[0].split("_")[0] for n in yakin)
        ilk = ", ".join(f"{k}({v})" for k, v in c.most_common(5)) or "-"
        print(f"[HZ] ({a.x:.0f}, {a.z:.0f}) {r:6.0f} m icinde "
              f"{len(yakin):5d} nesne   [{ilk}]")
    return 0


if __name__ == "__main__":
    sys.exit(main())
