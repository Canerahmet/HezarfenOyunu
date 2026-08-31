# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — Dönem mobilyası (plan Faz II.5).

## Neden kutu, neden bu kadar az üçgen

Bu parçalar evin **LOD0'ında** yaşıyor, yani yalnız yakındaki evde
çiziliyorlar (bkz. `_ic_bolme_geometri`). Buna rağmen 10.900 evin
yakınındaki her biri sayılıyor: parça başına yüz üçgen, sokakta yüz
bin eder. Osmanlı iç mekânının biçim dili zaten kutuya yakın — sedir
bir seki, sandık bir sandık, yüklük duvarda bir niş.

## Ölçüler insandan türer

Hiçbiri elle yazılmış bir sayı değil; hepsi 1,70 m'lik gövdeden ya da
oturma/uzanma ölçüsünden geliyor ve gerekçesi satırında yazılı.
RESEARCH.md §4.1 konutun *hayatlı, sedirli* olduğunu kaydediyor ama
mobilya ölçüsü vermiyor — bu yüzden **T2** (makul rekonstrüksiyon).

## Yerleşim kuralı: duvar boyu

Osmanlı odasında mobilya odanın ortasında durmaz; **duvara yaslanır**
ve orta boş kalır. Oda yerine geçen şey minderdir, masa yerine geçen
şey sinidir. Bu yüzden yerleştirici duvar hattı boyunca ilerler.
"""

import hz_blender as hz

#: Sedir yüksekliği (m) — oturunca diz 90°'de olsun diye. Sandalyenin
#: oturma yüksekliği 0,42-0,45; sedir ondan biraz alçaktır.
SEDIR_YUKSEK = 0.38
#: Sedir derinliği (m): oturan adamın uyluğu ~0,45, arkasına minder
#: gelir; toplam 0,70.
SEDIR_DERIN = 0.70
#: Minder kalınlığı (m).
MINDER_KALIN = 0.12
#: Sandık yüksekliği (m) — üstüne oturulabilir, yani sedirden alçak.
SANDIK_YUKSEK = 0.45
#: Yüklük derinliği (m): katlanmış yatak takımı ~0,55 tutar.
YUKLUK_DERIN = 0.55


def _kutu(ad, boyut, merkez, col, mat):
    o = hz.make_box(ad, boyut, merkez, col)
    hz.assign(o, mat)
    return o


def sedir(ad, col, mats, uzunluk, merkez, yon):
    """Duvar boyu sabit sedir. `yon`: (dx, dy) duvarın dışa normali."""
    nx, ny = yon
    # Gövde duvara yaslanır; derinlik normal yönünde içeri uzanır.
    if abs(nx) > abs(ny):
        boyut = (SEDIR_DERIN, uzunluk, SEDIR_YUKSEK)
        min_boyut = (MINDER_KALIN, uzunluk - 0.10, MINDER_KALIN * 1.6)
        min_ofset = (-nx * (SEDIR_DERIN * 0.5 - MINDER_KALIN * 0.5), 0.0)
    else:
        boyut = (uzunluk, SEDIR_DERIN, SEDIR_YUKSEK)
        min_boyut = (uzunluk - 0.10, MINDER_KALIN, MINDER_KALIN * 1.6)
        min_ofset = (0.0, -ny * (SEDIR_DERIN * 0.5 - MINDER_KALIN * 0.5))

    out = [_kutu(f"{ad}_Govde", boyut,
                 (merkez[0], merkez[1], merkez[2] + SEDIR_YUKSEK * 0.5),
                 col, mats["timber"])]
    # Arkalık minderi: sırt duvara gelmesin diye sedirin arka kenarında.
    out.append(_kutu(f"{ad}_Sirt", min_boyut,
                     (merkez[0] + min_ofset[0], merkez[1] + min_ofset[1],
                      merkez[2] + SEDIR_YUKSEK + MINDER_KALIN * 0.8),
                     col, mats["cloth"]))
    # Oturma minderi: sedirin üstünü kaplar.
    ust = (boyut[0] - 0.06, boyut[1] - 0.06, MINDER_KALIN)
    out.append(_kutu(f"{ad}_Minder", ust,
                     (merkez[0], merkez[1],
                      merkez[2] + SEDIR_YUKSEK + MINDER_KALIN * 0.5),
                     col, mats["cloth"]))
    return out


def sandik(ad, col, mats, merkez):
    """Sandık: giysi ve yatak takımı. Üstüne oturulur."""
    return [
        _kutu(f"{ad}_Govde", (0.95, 0.48, SANDIK_YUKSEK),
              (merkez[0], merkez[1], merkez[2] + SANDIK_YUKSEK * 0.5),
              col, mats["timber"]),
        # Kapak kenarı: gölge çizgisi. Sandığı kutudan ayıran şey bu.
        _kutu(f"{ad}_Kapak", (1.0, 0.53, 0.05),
              (merkez[0], merkez[1], merkez[2] + SANDIK_YUKSEK + 0.02),
              col, mats["timber"]),
    ]


def rahle(ad, col, mats, merkez):
    """Rahle: yerde oturan kişinin önünde açılan X ayaklı okuma sehpası."""
    return [
        _kutu(f"{ad}_Tabla", (0.42, 0.30, 0.03),
              (merkez[0], merkez[1], merkez[2] + 0.33), col, mats["timber"]),
        _kutu(f"{ad}_Ayak", (0.34, 0.06, 0.33),
              (merkez[0], merkez[1], merkez[2] + 0.165), col, mats["timber"]),
    ]


def mangal(ad, col, mats, merkez):
    """Mangal: odanın tek ısı kaynağı. Bakır tas, üç ayak."""
    out = [_kutu(f"{ad}_Tas", (0.46, 0.46, 0.16),
                 (merkez[0], merkez[1], merkez[2] + 0.30), col, mats["metal"])]
    for dx, dy in ((-0.16, -0.16), (0.16, -0.16), (0.0, 0.18)):
        out.append(_kutu(f"{ad}_Ayak", (0.05, 0.05, 0.22),
                         (merkez[0] + dx, merkez[1] + dy, merkez[2] + 0.11),
                         col, mats["metal"]))
    return out


def kilim(ad, col, mats, merkez, boyut):
    """Kilim: zemine serilir. 1 cm — yerden ayırt edilsin yeter."""
    return [_kutu(ad, (boyut[0], boyut[1], 0.012),
                  (merkez[0], merkez[1], merkez[2] + 0.006),
                  col, mats["cloth"])]


def yukluk(ad, col, mats, merkez, genislik, yon, kat_yuksek):
    """Yüklük: duvarda gömme dolap. Yatak takımı gündüz buraya girer."""
    nx, ny = yon
    yuk = min(2.05, kat_yuksek - 0.35)
    if abs(nx) > abs(ny):
        boyut = (YUKLUK_DERIN, genislik, yuk)
    else:
        boyut = (genislik, YUKLUK_DERIN, yuk)
    return [
        _kutu(f"{ad}_Govde", boyut,
              (merkez[0], merkez[1], merkez[2] + yuk * 0.5),
              col, mats["timber"]),
        # Orta kayıt: iki kapaklı olduğunu okutan çizgi.
        _kutu(f"{ad}_Kayit",
              (boyut[0] + 0.02, boyut[1] + 0.02, 0.05)
              if abs(nx) <= abs(ny) else
              (boyut[0] + 0.02, boyut[1] + 0.02, 0.05),
              (merkez[0], merkez[1], merkez[2] + yuk * 0.55),
              col, mats["trim"]),
    ]


def ocak(ad, col, mats, merkez, yon, kat_yuksek):
    """Ocak: duvara gömme pişirme nişi ve davlumbazı."""
    nx, ny = yon
    derin = 0.45
    en = 1.10
    if abs(nx) > abs(ny):
        nis = (derin, en, 1.30)
        davlumbaz = (derin + 0.10, en + 0.20, 0.45)
    else:
        nis = (en, derin, 1.30)
        davlumbaz = (en + 0.20, derin + 0.10, 0.45)
    return [
        _kutu(f"{ad}_Nis", nis, (merkez[0], merkez[1], merkez[2] + 0.65),
              col, mats["stone"]),
        _kutu(f"{ad}_Davlumbaz", davlumbaz,
              (merkez[0], merkez[1], merkez[2] + 1.30 + 0.225),
              col, mats["plaster"]),
    ]
