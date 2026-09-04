# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — **Yan yana iki ozdes ev var mi?**

## Neden bu sayi

Ev cesitliligi isinin kabul kriteri sudur: *"ayni mahallede yan yana
iki ozdes ev sifir."* Varyant havuzu 26'dan 200'e cikarildi ve 10.900
ev yerlestirildi — ama kriter bugune kadar **hic olculmedi**. Havuzun
buyuk olmasi, komsu iki evin farkli olmasini garanti etmez: rastgele
secimde ayni varyantin yan yana dusme olasiligi sifir degildir ve
gozun yakaladigi sey tam da odur.

Sayi karede degil **sahnede** okunur: kare bir bakis acisidir, sahne
butun sehirdir.

## Nasil olcer

Semt sahnelerinden her ev orneginin varyant adi ve konumu okunur
(`PrefabInstance` bloklarindaki `m_LocalPosition`). Her ev icin en
yakin komsu bulunur; komsu ayni varyantsa **cift** sayilir.

`--mesafe` esigi: iki ev "yan yana" sayilmak icin en cok kac metre
otede olabilir. Varsayilan 12 m — Osmanli sokaginda cephe genisligi
5,6-9 m (varyant katalogu), yani 12 m bir ev boyu kadar otedir.

## Ilk olcumun sonucu (2026-09-04)

```
semt                       ev  komsulu  ozdes cift    oran  ort komsu (m)
D_Eyup                    722      671          56   8.35%           8.98
D_Galata                 2651     2490           0   0.00%           8.97
D_Surici_Bati            2026     1939         131   6.76%           9.11
D_Surici_Dogu            3173     3033         261   8.61%           8.98
D_Uskudar                2328     2218         180   8.12%           9.09
TOPLAM                  10900    10351         628   6.07%
```

Kriter **karsilanmiyor**: 628 ozdes komsu cifti. Ustelik 200 varyantla
rastgele secimde beklenen oran 1/200 = %0,5'tir; olculen %6-8,6 bunun
**on iki-on yedi kati**. Yani secim rastgele bile degil, kumelenmis.

Ama asil bilgi dagilimda: **D_Galata tam sifir**, otekiler %6,8-8,6.
`OttomanStreetBuilder` icinde tekrar engeli var (`KomsudaAyniVar`,
`TekrarYaricapi = 15 m`) ve Galata'nin sifiri tam olarak o kuralin
imzasidir — 15 m yaricapinda ayni varyant yoksa 12 m esiginde de
olmaz. Yani kural calisiyor; oteki dort semt ONUNLA KURULMAMIS.
Kural 2026-08-31'de eklendi, o semtlerin sahneleri sonra commit'lendi
ama yeniden KURULMADI — commit tarihi ile uretim tarihi ayni sey degil.

Yapilacak: dort semt `OttomanStreetBuilder` ile yeniden kurulacak ve
bu alet tekrar kosulacak. Beklenti: hepsi sifir.

## Kullanim

    python tools/olcum/ev_tekrari.py
    python tools/olcum/ev_tekrari.py --mesafe 15 --semt D_Galata
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
                          "Scenes", "Districts")

#: Iki evin "yan yana" sayildigi en buyuk mesafe (m).
VARSAYILAN_MESAFE = 12.0


def _ornekleri_oku(yol):
    """`(varyant, x, z)` listesi — bir semt sahnesinden."""
    s = io.open(yol, encoding="utf-8", errors="replace").read()
    cikti = []
    for blok in s.split("--- !u!1001 &")[1:]:
        ad = re.search(r"PF_(House_\w+)", blok)
        if not ad:
            continue
        x = re.search(r"propertyPath: m_LocalPosition\.x\n      value: "
                      r"([-\d.eE]+)", blok)
        z = re.search(r"propertyPath: m_LocalPosition\.z\n      value: "
                      r"([-\d.eE]+)", blok)
        if x and z:
            cikti.append((ad.group(1), float(x.group(1)), float(z.group(1))))
    return cikti


def _izgara(ornekler, hucre):
    kova = collections.defaultdict(list)
    for i, (_, x, z) in enumerate(ornekler):
        kova[(int(x // hucre), int(z // hucre))].append(i)
    return kova


def olc(ornekler, mesafe):
    """Donus: `(cift_sayisi, komsusu_olan, en_yakin_ortalama)`."""
    if len(ornekler) < 2:
        return 0, 0, 0.0
    kova = _izgara(ornekler, mesafe)
    cift = 0
    komsulu = 0
    toplam_d = 0.0
    for i, (ad, x, z) in enumerate(ornekler):
        cx, cz = int(x // mesafe), int(z // mesafe)
        en_iyi, en_ad = None, None
        for dx in (-1, 0, 1):
            for dz in (-1, 0, 1):
                for j in kova.get((cx + dx, cz + dz), ()):
                    if j == i:
                        continue
                    b, bx, bz = ornekler[j]
                    d = math.hypot(bx - x, bz - z)
                    if en_iyi is None or d < en_iyi:
                        en_iyi, en_ad = d, b
        if en_iyi is None or en_iyi > mesafe:
            continue
        komsulu += 1
        toplam_d += en_iyi
        if en_ad == ad:
            cift += 1
    ort = toplam_d / komsulu if komsulu else 0.0
    return cift, komsulu, ort


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--mesafe", type=float, default=VARSAYILAN_MESAFE)
    ap.add_argument("--semt", default=None, help="Yalniz bu semt")
    a = ap.parse_args()

    yollar = sorted(glob.glob(os.path.join(SAHNE_KOKU, "*.unity")))
    if a.semt:
        yollar = [y for y in yollar if a.semt in os.path.basename(y)]
    if not yollar:
        sys.exit(f"[HZ] HATA: {SAHNE_KOKU} altinda sahne yok")

    t_cift = t_komsulu = t_ev = 0
    print(f"{'semt':22} {'ev':>6} {'komsulu':>8} {'ozdes cift':>11} "
          f"{'oran':>7} {'ort komsu (m)':>14}")
    for y in yollar:
        o = _ornekleri_oku(y)
        cift, komsulu, ort = olc(o, a.mesafe)
        t_cift += cift
        t_komsulu += komsulu
        t_ev += len(o)
        oran = (cift / komsulu * 100.0) if komsulu else 0.0
        print(f"{os.path.basename(y)[:-6]:22} {len(o):6d} {komsulu:8d} "
              f"{cift:11d} {oran:6.2f}% {ort:14.2f}")
    oran = (t_cift / t_komsulu * 100.0) if t_komsulu else 0.0
    print(f"{'TOPLAM':22} {t_ev:6d} {t_komsulu:8d} {t_cift:11d} "
          f"{oran:6.2f}%")
    # Rastgele secimde beklenen oran: 1/varyant_sayisi
    print(f"\n[HZ] Esik {a.mesafe:.0f} m. Kriter: yan yana ozdes ev SIFIR.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
