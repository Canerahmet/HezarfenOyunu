# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — karedeki ince beneklenmenin LOD taramasi (dither) mi,
yoksa doku gurultusu mu oldugunu ayirir.

## Neden bir alete ihtiyac var

`LodGecisi` sert LOD sicramasini taramali gecisle yumusatti ve bant 0,25
secildi. Gerekce yaziliydi ama **gorsel bedeli hic olculmedi**: bandin
icindeki her yapi iki LOD'u satranc deseniyle karistirarak cizer. Oyun
turunda 03_galata_sokak karesinde bir catinin tamami kirmizi-beyaz benek
olarak cikti — yani bant, sicramayi gizlerken yerine kalici bir gurultu
koydu.

"Once olc, sonra degistir" kurali burada da gecerli: bandi daraltmadan
once bugunku gurultunun sayisi lazim, yoksa duzelmenin duzelme oldugu
bilinemez.

## Nasil olcer — ve ilk denemenin neden yetmedigi

Ilk yazim "3x3 ortalamadan sapma" olcuyordu. Kostu ve **yaprak dolu bir
bahce karesini (%4,3) taramali cati karesinden (%2,1) daha gurultulu
gosterdi**: o olcu yuksek frekansi olcuyordu, taramayi degil. Yaprak da
yuksek frekanslidir; kusur olmayan bir seyi kusur diye isaretleyen bir
alet, olcmuyor demektir.

Taramayi ayirt eden sey siddeti degil **duzenidir**: satranc deseni
piksel piksel isaret degistirir, yani (x+y) tekse bir renk, ciftse
oteki. Doku detayinin boyle sabit bir evresi yoktur. Bu yuzden olcu,
parlakligin satranc desenyle **yerel ilintisi**dir: gorüntü satranc
maskesiyle carpilir ve kucuk pencerelerde toplanir. Rastgele detayda bu
toplam sifira gider; taramada gitmez.

## Iki sayi birden yazilir

`yuksek frekans` karede ne kadar ince detay oldugunu soyler; `tarama`
o detayin duzenli olup olmadigini. Ikisi birden lazim:

| yuksek frekans | tarama | okunusu |
|---|---|---|
| yuksek | yuksek | LOD taramasi — bant daraltilir |
| yuksek | dusuk  | doku/geometri gurultusu — kaynagi baska |
| dusuk  | -      | temiz |

## Ilk kullanimin sonucu — bir hipotez elendi

03_galata_sokak karesindeki kirmizi benekli cati "LOD taramasi" diye
okunmustu ve bant 0,25'ten daraltilacakti. Olculdu:

    taramali sanilan bolge : yuksek frekans dx=0,039 dy=0,039
                             satranc ilintisi 0,0001
                             periyot-2/4/8 en guclu faz 0,0015/0,0051/0,0144

Satranc ilintisi sifir, hicbir periyotta duzen yok, parlaklik histogrami
**surekli** (iki kumeye ayrilmiyor). Taramali gecis iki kaynagi duzenli
bir maskeyle karistirir; bu bolgede oyle bir duzen yok. Yani beneklenme
LOD taramasi DEGIL, uzaktaki bir catinin doku gurultusudur.

Bu, bir sabiti bosuna degistirmekten alikoydu: `LodGecisi.GecisBandi`
daraltilsaydi Caner'in uc kez bildirdigi "titreme" geri gelirdi ve
benek yerinde kalirdi. Aletin ilk isi bir duzeltmeyi ENGELLEMEK oldu.

## Kullanim

    python tools/olcum/tarama_gurultusu.py renders/tur/*.png
"""

import glob
import os
import sys

try:
    from PIL import Image
except ImportError:                                    # pragma: no cover
    sys.exit("[HZ] HATA: Pillow gerekli — pip install pillow")

import numpy as np

#: Satranc ilintisinin esigi (0-1 parlaklik biriminde). Yaprakli bahce
#: karesi ile taramali cati karesi arasinda kalibre edildi.
ESIK = 0.022

#: Ilintinin toplandigi pencere — bir kac piksellik tarama lekesini
#: yakalayacak kadar kucuk, tek piksel gurultusune kanmayacak kadar buyuk.
PENCERE = 6

#: Bir lekenin "gurultu" sayilmasi icin en az kac piksel olmasi gerektigi.
EN_KUCUK_LEKE = 400


def _kutu_ortalama(a, n):
    """n x n kutu ortalamasi — kenarlar tekrarlanir."""
    k = n // 2
    p = np.pad(a, k, mode="edge")
    s = np.zeros_like(a)
    for dy in range(n):
        for dx in range(n):
            s += p[dy:dy + a.shape[0], dx:dx + a.shape[1]]
    return s / float(n * n)


def _en_buyuk_leke(maske):
    """Baglantili en buyuk bolgenin piksel sayisi (4-komsuluk, iteratif)."""
    h, w = maske.shape
    gorulen = np.zeros_like(maske, dtype=bool)
    en_buyuk = 0
    ys, xs = np.nonzero(maske)
    for y0, x0 in zip(ys, xs):
        if gorulen[y0, x0]:
            continue
        yigin = [(y0, x0)]
        gorulen[y0, x0] = True
        n = 0
        while yigin:
            y, x = yigin.pop()
            n += 1
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < h and 0 <= xx < w and maske[yy, xx] \
                        and not gorulen[yy, xx]:
                    gorulen[yy, xx] = True
                    yigin.append((yy, xx))
        en_buyuk = max(en_buyuk, n)
    return en_buyuk


def olc(yol):
    im = Image.open(yol).convert("RGB")
    a = np.asarray(im, dtype=np.float64) / 255.0
    l = a[:, :, 0] * 0.2126 + a[:, :, 1] * 0.7152 + a[:, :, 2] * 0.0722
    h, w = l.shape
    yy, xx = np.indices((h, w))
    satranc = np.where((yy + xx) % 2 == 0, 1.0, -1.0)
    # Yerel ortalama cikarilir ki genel parlaklik ilintiye karismasin.
    ilinti = np.abs(_kutu_ortalama((l - _kutu_ortalama(l, PENCERE)) * satranc,
                                   PENCERE))
    maske = ilinti > ESIK
    oran = float(maske.mean())
    leke = _en_buyuk_leke(maske) if maske.sum() else 0
    # Yuksek frekans: komsu piksel farkinin ortalamasi. Tarama olmadan da
    # yuksek olabilir (yaprak, kaldirim); ayirt eden sey ustteki ilintidir.
    yf = float((np.abs(np.diff(l, axis=1)).mean()
                + np.abs(np.diff(l, axis=0)).mean()) * 0.5)
    return dict(dosya=os.path.basename(yol), oran=oran, leke=leke, yf=yf,
                gurultulu=leke >= EN_KUCUK_LEKE)


def main(desenler):
    yollar = []
    for d in desenler:
        yollar.extend(sorted(glob.glob(d)))
    if not yollar:
        sys.exit("[HZ] HATA: kare bulunamadi")
    print(f"{'kare':28} {'yuksek frekans':>15} {'tarama':>9} "
          f"{'en buyuk leke':>14}  durum")
    for y in yollar:
        r = olc(y)
        durum = ("TARAMA" if r["gurultulu"]
                 else "doku gurultusu" if r["yf"] > 0.020 else "temiz")
        print(f"{r['dosya']:28} {r['yf']:15.4f} {r['oran'] * 100:8.2f}% "
              f"{r['leke']:14d}  {durum}")


if __name__ == "__main__":
    main(sys.argv[1:] or ["renders/tur/*.png"])
