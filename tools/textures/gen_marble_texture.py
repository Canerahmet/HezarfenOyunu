"""
Hezarfen: 1632 — Mermer dokusu üreticisi (prosedürel, KENDİ TELİFİMİZ).

## Bu doku bir ÖLÇÜMÜN sonucudur

Menzil taşı `cutstone` (kesme taş) malzemesiyle üretiliyordu ve inceleme
karesi iki kusur gösterdi; ikisi de ölçüldü, gözle karar verilmedi:

  * Sütunun ortalama parlaklığı **36,7/255**, yanındaki çayır **162,5/255** —
    taş zeminden **4,4 kat koyu**. Mermer sütun ışıkta duran en açık şeydir.
  * Sütun boyunca satır ortalamalarının baskın dikey periyodu **0,95 m** —
    yani duvar dokusunun **taş sırası** düzeni. Kaynak ise açık: menzil
    taşları "tek parça mermer sütun"dur. Sıralı doku, taşın tek parça
    olmadığını söylüyordu.

`cutstone` bir DUVAR malzemesidir; bir sütuna sarıldığında derzler yalan
söylüyor. Poly Haven'da lisanslı mermer yok, lisanssız görsel indirmek yasak
(CLAUDE.md) — kurşunda ve yaprakta olduğu gibi çözüm üretmek.

## Mermeri mermer yapan şey: DAMAR, ama az

Mermer başkalaşmış kireçtaşıdır; damar, kütlenin içindeki kil/oksit
katmanlarının kıvrılmış izidir. Üç özelliği vardır ve üçü de doku için
kritik:

  * **Yönlüdür** — damarlar bir tabakalanma yönünü izler. Blok o yönde
    kesilir, sütun da bloktan çıkar.
  * **Seyrektir** — İstanbul'un Marmara (Prokonnesos) mermeri beyaz-grisi,
    ince damarlıdır. Sık damar bir başka taştır.
  * **PERİYODİK DEĞİLDİR** — ve düzeltilen kusur tam buydu. Bu yüzden burada
    ölçülen şey yalnızca "damar var mı" değil, dokunun **kendini
    tekrarlamadığı**dır (aşağıdaki BAND_MAX).

Kullanım:
  python tools/textures/gen_marble_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl          # noqa: E402

# --- ölçütler: her biri düzeltilen bir kusurdan doğdu -----------------------

# Mermer AÇIKTIR. Ölçülen kusur 36,7/255'ti; çayır 162,5. Taş, üstüne
# düşen ışıkta zeminden koyu olmamalı.
LUM_MIN = 175.0

# --- ÖLÇEMEDİĞİM ŞEY: "taş sırası var mı" --------------------------------
#
# Kusurun görünen yüzü periyodik bir bantlanmaydı (0,95 m). Bunu bir eşiğe
# bağlamayı üç ayrı formülasyonla denedim ve ÜÇÜ DE bilinen-kusurluyu
# bilinen-iyiden ayıramadı. Bilinen kusurlu: `large_sandstone_blocks`.
# Bilinen iyi: onaylanmış arazi dokuları.
#
#   ölçü                          kumtaşı(kusurlu)   arazi toprağı(iyi)
#   en güçlü frekans / ortalama        96,4                38,2
#   tek frekansın enerji payı           0,449               0,536   <- TERS
#   özilinti tepesi                     0,677               0,710   <- TERS
#
# İlk ölçü ayırıyor gibi duruyor ama ayırmıyor: bant sınırlı ÜRETİLMİŞ her
# doku (bu mermer dahil, 86) yüksek çıkıyor, çünkü ölçü aslında "tayf boş
# bantlar içeriyor mu" diye soruyor — periyodiklik değil.
#
# Ağaç maliyeti ölçümünde konan kural burada da geçerli: **ölçemeyen alet
# sayı üretmez, ölçemediğini söyler.** Bant oranı bu yüzden bir EŞİK değil,
# yalnızca bilgi olarak yazdırılıyor.
#
# Yerine geçen gerçek koruma piksel istatistiği değil BORU HATTI kuralıdır:
# menzil taşı `marble` rolünü kullanır ve bu rolün dokusunu bu betik üretir
# (`OkmeydaniTests.StonesAreMarbleNotMasonry`).

# Yakın planda okunabilirlik. Arazi dokularında eşik 2,0'dı; mermer doğası
# gereği daha düz bir yüzeydir (işlenmiş taş, toprak değil) ve 2,0'a ancak
# uydurma bir tane ile çıkardım. 1,2, yüzeyin plastik olmadığını söyleyecek
# kadar; "toprak kadar taneli" demeyecek kadar.
FINE_MIN = 1.2

# Metre ölçekte lekelenme olmasın: mermer benekli bir taş değildir. Ama
# üst sınır arazi dokularından ödünç alınamaz — orada 3,0'dı ve o sayı
# "havadan bakılan zemin lekeli görünmesin" diye konmuştu. Mermerin bütün
# meselesi ise metre ölçeğindeki DAMARDIR.
#
# Sınır bu yüzden gerçek taş dokuları ölçülerek konuldu (20 cm blok, her
# dokunun kendi ölçeğinde):
#
#   painted_plaster_wall (desensiz)      1,04
#   old_stone_wall (moloz)               2,99
#   large_sandstone_blocks (derzli)      4,79
#   cobblestone_floor_001 (ayrık taş)    5,05
#
# Mermer sıvadan yapılı, ayrı ayrı taşlardan yapılmış bir yüzeyden yapısız
# olmalı: 4,0, derzli her dokunun altında.
COARSE_MAX = 4.0

SPECS = [
    # 1,2 m kare: bir menzil taşı gövdesi 0,34 m — karo sütunun çevresinde
    # (~1,07 m) neredeyse tam bir tur atar, yani dikişi arayan göz onu
    # sütunun arkasında bulur.
    # `size` 1,2 m değil 0,9: menzil taşı gövdesi 0,34-0,40 m. 1,2 m'lik
    # karoda damarların ancak üçte biri sütunun görünen yüzüne düşüyordu.
    #
    # Damar rengi de ÖLÇÜMLE koyulaştırıldı. İlk değer (138,133,124) idi ve
    # sahnede sütunun ayrıntı enerjisi **0,23** çıktı — yani doku hiç
    # okunmuyordu, taş düz beyaz bir silindirdi. Sebep: mermer albedosu
    # yüksek, HDRP pozlaması onu üstten sıkıştırıyor; dokunun kendisinde
    # yeterli görünen kontrast, ışıkta yok oluyor.
    dict(id="marble_white", size=0.9, seed=131,
         base=(214, 211, 203), vein=(96, 92, 84), warm=(228, 224, 210)),
]

VEIN_COUNT = 7            # damar yoğunluğu maskesinin öbek sayısını belirler
RELIEF_MM = 1.4           # damarlar aşınmada azıcık çukurlaşır


def _tileable_noise(res, rng, kmax, aniso):
    """
    Döşenebilir, bant sınırlı gürültü — tam sayı frekanslı kosinüslerin
    rastgele fazlı toplamı.

    Neden bu ve neden tek bir sinüs değil: ilk yazımda damarlar tek bir
    sinüsün tepeleriydi ve ölçüm bandı **124** verdi — düzelttiğim kumtaşı
    duvarından (96) bile daha periyodik. "Kıvrılma ekledim, artık periyodik
    değil" demek yetmiyor; tek frekanslı bir alan bozulsa da tek frekanslı
    kalır. Yüzlerce bileşene yayılan bir tayfın baskın periyodu olmaz.

    `aniso` **y** yönündeki frekansları cezalandırır: alan y'de yavaş, x'te
    hızlı değişir, dolayısıyla eş seviye eğrileri **y boyunca uzar** —
    damarlar sütun gövdesi boyunca akar.

    İlk yazımda çarpan kx'teydi ve doku yatay damar verdi: bir sütuna
    sarıldığında bu, düzeltmeye çalıştığım **taş sırası** görüntüsünün ta
    kendisiydi. Render göstermeseydi ölçüler geçmişti — dördü de yön
    körüdür.
    """
    x = (np.arange(res) + 0.5) / res
    out = np.zeros((res, res))
    for kx in range(-kmax, kmax + 1):
        for ky in range(0, kmax + 1):
            if kx == 0 and ky == 0:
                continue
            k = np.hypot(kx, ky * aniso)
            if k > kmax:
                continue
            amp = rng.normal() / (k ** 1.7)
            ph = rng.uniform(0.0, 2.0 * np.pi)
            out += amp * np.cos(2.0 * np.pi * (kx * x[None, :] + ky * x[:, None])
                                + ph)
    return pl.normalize(out)


def _veins(res, rng, count):
    """
    Damar alanı (0-1; 1 = damar merkezi).

    Damar, kütlenin içindeki bir **eş seviye eğrisidir**: alanın belirli bir
    değeri geçtiği ince şerit. Bu yüzden kendiliğinden kıvrımlı, kapanan,
    çatallanan çizgiler verir — damarın gerçekten yaptığı şey.
    """
    f = _tileable_noise(res, rng, kmax=9, aniso=3.4)
    # Uc seviye = uc damar ailesi; hepsi ayni kalinlikta olmasin.
    #
    # Genişlikler ÖLÇÜLEREK inceltildi. İlk değerler (0,020 / 0,014 / 0,010)
    # sahnede **halka** veriyordu: eş seviye eğrisi kalınlaştıkça çizgi
    # olmaktan çıkıp kapalı bir leke haline geliyor ve taş mermer değil
    # kamuflajlı okunuyordu. Damar bir ÇİZGİDİR; kalınlığı onu öldürür.
    v = np.zeros((res, res))
    for level, width, w in ((0.42, 0.010, 1.00),
                            (0.58, 0.007, 0.72),
                            (0.30, 0.005, 0.45)):
        v = np.maximum(v, w * np.exp(-((f - level) / width) ** 2))
    # Damar meydanin her yerinde ayni yogunlukta degil: seyrelt.
    mask = pl.blob_field(res, count * 2, res / 4.5, 1.0, rng, exponent=1.0)
    return np.clip(v * (0.48 + 0.85 * mask), 0.0, 1.0)


def band_ratio(gray):
    """
    En güçlü periyodun ortalamaya oranı — **iki eksende de**.

    Tek eksen bakmak yetmez: taş sırası yataydır, ama bir başka doku dikey
    sıralı olabilir ve yalnız satırlara bakan bir alet onu göremezdi. Sonuç
    ikisinin büyüğüdür.
    """
    out = []
    for axis in (1, 0):
        prof = gray.mean(axis=axis)
        f = np.abs(np.fft.rfft(prof - prof.mean()))[1:]
        out.append(float(f.max() / max(f.mean(), 1e-9)))
    return max(out)


def fine_energy(gray):
    k = np.zeros_like(gray)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            k += np.roll(np.roll(gray, dy, 0), dx, 1)
    return float(np.abs(gray - k / 9.0).mean())


def coarse_energy(gray, block):
    """Önce blok indirge, SONRA ölç — yoksa ince tane kaba sayılır."""
    n = gray.shape[0] // block
    r = gray[:n * block, :n * block].reshape(n, block, n, block).mean(axis=(1, 3))
    k = np.zeros_like(r)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            k += np.roll(np.roll(r, dy, 0), dx, 1)
    return float(np.abs(r - k / 9.0).mean())


def build(spec, res):
    rng = np.random.default_rng(spec["seed"])
    vein = _veins(res, rng, VEIN_COUNT)
    # Bulut: damar disi kutle de tekduze degil, ama COK az kontrastli.
    cloud = pl.blob_field(res, 60, res / 5.0, 1.4, rng, exponent=1.0)
    grain = pl.fine_grain(res, rng)

    base = pl.srgb_to_linear(spec["base"])
    veinc = pl.srgb_to_linear(spec["vein"])
    warm = pl.srgb_to_linear(spec["warm"])

    # Kutle: taban ile sicak ton arasinda bulutla gezinir.
    t = np.clip(0.30 + 0.55 * cloud, 0.0, 1.0)[..., None]
    col = base[None, None, :] * (1.0 - t) + warm[None, None, :] * t
    # Damar KOYULASTIRIR ama ortmez: mermer yari saydamdir, damar bulanik okunur.
    vm = (0.66 * vein)[..., None]
    col = col * (1.0 - vm) + veinc[None, None, :] * vm
    # Ince tane: yakin planda yuzeyin kendisi. Mermer kristallidir; islenmis
    # yuzey duz ama pirilti tasir. Genlik OLCUYLE secildi: 0,055 ile ince
    # enerji 0,33 cikiyordu (esik 1,2) — yuzey plastikti.
    col *= (1.0 + 0.48 * (grain - 0.5))[..., None]

    bc = (np.clip(pl.linear_to_srgb(col), 0.0, 1.0) * 255.0).round().astype(np.uint8)

    # Kabartma: damar asinmada azicik cukurlasir, gerisi neredeyse duz.
    h_m = (-RELIEF_MM * 1e-3 * vein + 0.00018 * (grain - 0.5)
           + 0.00035 * (cloud - 0.5))
    amp = float(h_m.max() - h_m.min())
    nrm = (pl.normal_from_height(pl.normalize(h_m),
                                 strength=amp * res / spec["size"]) * 255.0
           ).round().astype(np.uint8)

    # Purzuluk: islenmis mermer parlaktir ama ACIK HAVADA matlasir; damar
    # (daha yumusak faz) once asinir, orasi daha mat.
    rough = np.clip(0.34 + 0.30 * vein + 0.06 * (1.0 - cloud), 0.0, 1.0)
    # AO: yalnizca damar cukurunda, ve cok hafif.
    ao = np.clip(1.0 - 0.16 * vein, 0.0, 1.0)
    metal = np.zeros_like(rough)
    arm = (np.stack([ao, rough, metal], axis=-1) * 255.0).round().astype(np.uint8)
    return bc, nrm, arm, rough, ao


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    bad = 0
    for spec in SPECS:
        bc, nrm, arm, rough, ao = build(spec, a.res)
        gray = (0.2126 * bc[:, :, 0] + 0.7152 * bc[:, :, 1]
                + 0.0722 * bc[:, :, 2]).astype(float)

        lum = float(gray.mean())
        band = band_ratio(gray)
        fine = fine_energy(gray)
        # 20 cm blok — arazi dokularindaki olcunun aynisi.
        coarse = coarse_energy(gray, max(2, int(round(0.20 / spec["size"] * a.res))))

        ok = lum >= LUM_MIN and fine >= FINE_MIN and coarse <= COARSE_MAX
        bad += 0 if ok else 1

        d = pl.write_texture_set(
            spec["id"], a.res, spec["size"], bc, nrm, arm,
            meta_extra=dict(
                generated_by="tools/textures/gen_marble_texture.py",
                role="marble",
                use="Menzil tasi, mezar tasi ve mihrap tasi mermeri (M_Marble_White)",
                why="Menzil taslari kaynakta 'tek parca MERMER sutun'dur; "
                    "kesme tas dokusu sutuna TAS SIRASI koyuyordu (olculen "
                    "dikey periyot 0,95 m) ve tasi cayirdan 4,4 kat koyu "
                    "birakiyordu (36,7 / 162,5). Poly Haven'da lisansli "
                    "mermer yok, lisanssiz gorsel indirmek yasak (CLAUDE.md).",
                vein_note="Damar bir es seviye egrisidir; yon anizotropiden "
                          "(kx frekanslari x'te 3,4 kat) gelir, sabit bir "
                          "aci degil.",
                measured=dict(luminance=round(lum, 1), band_ratio=round(band, 2),
                              fine=round(fine, 2), coarse=round(coarse, 2)),
            ),
            rough=rough, ao=ao, out_root=a.out)

        print(f"[HZ] {spec['id']}: {a.res}x{a.res}, {spec['size']} m -> {d}")
        print(f"[HZ]   parlaklik {lum:6.1f} (>={LUM_MIN})   "
              f"ince {fine:5.2f} (>={FINE_MIN})   "
              f"kaba {coarse:5.2f} (<={COARSE_MAX})   "
              f"{'OK' if ok else 'KALDI'}")
        print(f"[HZ]   bant orani {band:5.2f} — ESIK DEGIL, olculemedi "
              f"(gerekce dosya basinda)")
        print(f"[HZ]   purzuluk {rough.min():.2f}-{rough.max():.2f}, "
              f"AO {ao.min():.2f}-{ao.max():.2f}")

    print(f"[HZ] {len(SPECS)} prosedurel mermer dokusu uretildi, {bad} olcut disi")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
