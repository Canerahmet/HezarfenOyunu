# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — Ortam sesi üreteci (plan Faz II.E).

## Neden sentez, neden indirme değil

CLAUDE.md: *"refs/ altına lisansı LICENSES.md'de belgelenmemiş HİÇBİR
görsel indirme"* ve ticari yayın koşulu her varlığı bağlar — ses dahil.
İndirilen bir dalga sesinin lisansını takip etmek, üretmekten pahalı.
Doku hattı zaten aynı kararı verdi (`tools/textures/gen_*`): **kendi
işimiz, CC0 eşdeğeri, izlenecek lisans yok.**

## Neden gürültüden ses çıkar

Ortam sesi melodi değil **doku**dur. Denizin sesi filtrelenmiş beyaz
gürültü artı yavaş bir zarf; rüzgârınki daha dar bantlı; cırcır
böceğininki dar bir bantta hızlı darbeler. Bunlar örneklenmiş bir
kayıttan ayırt edilmez çünkü kaynakları da fiziksel olarak budur.

Üretilen her yatak **döngüye kapanır** (baş ve son çapraz karışır);
yoksa her tekrarda duyulan bir tıklama olur.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/audio/gen_ortam.py
"""

import argparse
import math
import os
import random
import struct
import wave

FS = 22050          # örnekleme (Hz) — ortam yatağı için fazlası israf
SURE = 12.0         # saniye; döngü uzunluğu
KANAL = 2


def _bos(n):
    return [0.0] * n


def _beyaz(n, rng):
    return [rng.uniform(-1.0, 1.0) for _ in range(n)]


def _alcak_gecir(x, kesim, fs=FS):
    """Tek kutuplu alçak geçiren. `kesim` Hz."""
    a = math.exp(-2.0 * math.pi * kesim / fs)
    y = [0.0] * len(x)
    onceki = 0.0
    for i, v in enumerate(x):
        onceki = (1.0 - a) * v + a * onceki
        y[i] = onceki
    return y


def _yuksek_gecir(x, kesim, fs=FS):
    return [v - u for v, u in zip(x, _alcak_gecir(x, kesim, fs))]


def _bant(x, alt, ust):
    return _yuksek_gecir(_alcak_gecir(x, ust), alt)


def _dongu_kapat(x, pay=0.15):
    """Baş ve sonu çapraz karıştırır; döngüde tıklama kalmasın."""
    n = len(x)
    k = int(n * pay)
    y = list(x)
    for i in range(k):
        t = i / float(k)
        y[i] = x[i] * t + x[n - k + i] * (1.0 - t)
    return y[: n - k]


def _normalle(x, tepe=0.85):
    m = max(1e-9, max(abs(v) for v in x))
    return [v / m * tepe for v in x]


def deniz(n, rng):
    """Kıyıya vuran su: geniş bantlı gürültü + yavaş kabarma zarfı."""
    ham = _bant(_beyaz(n, rng), 90.0, 1800.0)
    out = [0.0] * n
    # Üç ayrı periyotta kabarma — dalgalar tek ritimde gelmez.
    for periyot, agirlik in ((7.3, 0.55), (4.1, 0.30), (11.7, 0.15)):
        faz = rng.uniform(0, math.tau)
        for i in range(n):
            t = i / float(FS)
            zarf = 0.35 + 0.65 * (0.5 + 0.5 * math.sin(
                math.tau * t / periyot + faz)) ** 2.2
            out[i] += ham[i] * zarf * agirlik
    return out


def ruzgar(n, rng):
    """Rüzgâr: dar bantlı, yavaş gezinen tepe frekansı."""
    ham = _beyaz(n, rng)
    out = [0.0] * n
    for kesim, agirlik in ((320.0, 0.5), (700.0, 0.3), (140.0, 0.2)):
        bant = _bant(ham, kesim * 0.55, kesim * 1.7)
        faz = rng.uniform(0, math.tau)
        for i in range(n):
            t = i / float(FS)
            zarf = 0.4 + 0.6 * (0.5 + 0.5 * math.sin(
                math.tau * t / 9.5 + faz))
            out[i] += bant[i] * zarf * agirlik
    return out


def circi(n, rng):
    """Gece cırcır böceği: dar bantta hızlı darbe treni."""
    out = [0.0] * n
    for _ in range(14):
        frek = rng.uniform(3800.0, 5200.0)
        hiz = rng.uniform(22.0, 34.0)          # darbe/saniye
        faz = rng.uniform(0, math.tau)
        genlik = rng.uniform(0.25, 0.6)
        # Böcek sürekli ötmez: 2-5 sn öter, susar.
        acik_bas = rng.uniform(0.0, SURE)
        acik_sure = rng.uniform(2.0, 5.0)
        for i in range(n):
            t = i / float(FS)
            if not (acik_bas <= (t % SURE) <= acik_bas + acik_sure):
                continue
            darbe = max(0.0, math.sin(math.tau * hiz * t + faz))
            out[i] += math.sin(math.tau * frek * t) * darbe ** 6 * genlik
    return out


def marti(n, rng):
    """Martı: seyrek, inişli çığlık. Kıyının imzası."""
    out = [0.0] * n
    for _ in range(5):
        bas = rng.uniform(0.0, SURE - 1.2)
        sure = rng.uniform(0.35, 0.75)
        f0 = rng.uniform(900.0, 1400.0)
        genlik = rng.uniform(0.20, 0.45)
        for i in range(n):
            t = i / float(FS) - bas
            if not (0.0 <= t <= sure):
                continue
            u = t / sure
            frek = f0 * (1.0 + 0.45 * math.sin(math.pi * u)) * (1.0 - 0.35 * u)
            zarf = math.sin(math.pi * u) ** 1.5
            # Sert bir ses: temel + üç harmonik.
            v = 0.0
            for h, a in ((1, 1.0), (2, 0.45), (3, 0.22)):
                v += math.sin(math.tau * frek * h * t) * a
            out[i] += v * zarf * genlik * 0.35
    return out


def kalabalik(n, rng):
    """Uzak pazar uğultusu: insan sesi bandında yavaş modülasyon."""
    ham = _bant(_beyaz(n, rng), 180.0, 1100.0)
    out = [0.0] * n
    for _ in range(6):
        periyot = rng.uniform(1.6, 4.5)
        faz = rng.uniform(0, math.tau)
        agirlik = rng.uniform(0.1, 0.22)
        for i in range(n):
            t = i / float(FS)
            zarf = 0.45 + 0.55 * (0.5 + 0.5 * math.sin(
                math.tau * t / periyot + faz))
            out[i] += ham[i] * zarf * agirlik
    return out


def adim(n, rng):
    """
    Tek bir ayak sesi: taş kaldırımda deri terlik.

    ## Neden gürültüden çıkar

    Bir adım sesi iki olaydan ibarettir: topuğun **darbesi** (geniş
    bantlı, çok kısa) ve tabanın **sürtünmesi** (daha yüksek frekanslı,
    biraz daha uzun). İkisi de gürültüdür; ayıran şey zarf.

    ## Neden dört varyant

    Tek örneği tekrar çalmak yürüyüşü bir **metronoma** çevirir ve kulak
    bunu bir saniyede yakalar. Gerçek adımlar birbirinin aynı değildir;
    dört ayrı tohumla üretilen dört örnek, rastgele seçildiğinde tekrarı
    duyulmaz kılar.
    """
    out = [0.0] * n
    # Darbe: 12 ms'lik keskin bir sönüm.
    darbe = int(FS * 0.012)
    ham = _bant(_beyaz(n, rng), 90.0, 900.0)
    for i in range(min(darbe, n)):
        sonum = math.exp(-i / (darbe * 0.35))
        out[i] += ham[i] * sonum

    # Surtunme: 60 ms, daha tiz, daha yumusak baslar.
    surt = int(FS * 0.060)
    tiz = _bant(_beyaz(n, rng), 1400.0, 6000.0)
    for i in range(min(surt, n)):
        t = i / float(surt)
        zarf = math.sin(math.pi * t) ** 1.6
        out[i] += tiz[i] * zarf * 0.35
    return out


YATAKLAR = {
    # ad: (üreten fonksiyonlar ve ağırlıkları, açıklama)
    "SFX_Ortam_Deniz": ([(deniz, 1.0), (marti, 0.5)],
                        "kiyi: su ve marti"),
    "SFX_Ortam_Ruzgar": ([(ruzgar, 1.0)],
                         "acik alan / yukseklik"),
    "SFX_Ortam_Gece": ([(circi, 1.0), (ruzgar, 0.25)],
                       "gece mahallesi: circi bocegi"),
    "SFX_Ortam_Carsi": ([(kalabalik, 1.0), (ruzgar, 0.15)],
                        "uzak pazar ugultusu"),

    # AYAK SESI — bir oyuncunun en cok ozledigi ses.
    "SFX_Adim_1": ([(adim, 1.0)], "tas kaldirimda adim (1)"),
    "SFX_Adim_2": ([(adim, 1.0)], "tas kaldirimda adim (2)"),
    "SFX_Adim_3": ([(adim, 1.0)], "tas kaldirimda adim (3)"),
    "SFX_Adim_4": ([(adim, 1.0)], "tas kaldirimda adim (4)"),
}

# Adim yataklari kisadir: 10 saniyelik bir dongu degil, tek bir vurus.
KISA = {"SFX_Adim_1", "SFX_Adim_2", "SFX_Adim_3", "SFX_Adim_4"}


def yaz(yol, sol, sag):
    with wave.open(yol, "wb") as w:
        w.setnchannels(KANAL)
        w.setsampwidth(2)
        w.setframerate(FS)
        cerceve = bytearray()
        for a, b in zip(sol, sag):
            cerceve += struct.pack("<hh",
                                   int(max(-1.0, min(1.0, a)) * 32000),
                                   int(max(-1.0, min(1.0, b)) * 32000))
        w.writeframes(bytes(cerceve))


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Project", "Audio", "Ortam"))
    ap.add_argument("--tohum", type=int, default=1632)
    args = ap.parse_args()

    os.makedirs(args.out, exist_ok=True)
    n = int(FS * SURE)

    for ad, (parcalar, neden) in YATAKLAR.items():
        # ADIM KISADIR VE DONGUYE KAPANMAZ.
        #
        # Ortam yataklari 10 saniyelik dongulerdir ve basi sonuyla
        # capraz karisir. Bir adim ise TEK BIR VURUSTUR: 0,2 saniye
        # ve dongu yok. Ayni islemden gecirmek onu bir ugultuya
        # cevirirdi.
        kisa = ad in KISA
        n = int(FS * (0.20 if kisa else SURE))
        # STEREO AMA AYNI DEĞİL: iki kanal ayrı tohumla üretilir.
        # Aynı sinyali iki kanala koymak sesi kafanın ortasında bir
        # noktaya sıkıştırır; ortam sesi çevreyi sarmalı.
        kanallar = []
        for k in range(KANAL):
            rng = random.Random(args.tohum + hash(ad) % 100000 + k * 7919)
            toplam = _bos(n)
            for fn, agirlik in parcalar:
                parca = fn(n, rng)
                for i in range(n):
                    toplam[i] += parca[i] * agirlik
            kanallar.append(_normalle(
                toplam if kisa else _dongu_kapat(toplam), 0.8))

        yol = os.path.join(args.out, ad + ".wav")
        yaz(yol, kanallar[0], kanallar[1])
        print(f"[SES] {ad:22s} {len(kanallar[0]) / FS:5.2f} sn  {neden}")

    print(f"[SES] {len(YATAKLAR)} yatak -> {args.out}")


if __name__ == "__main__":
    main()
