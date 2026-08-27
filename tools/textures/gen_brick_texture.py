"""
Hezarfen: 1632 — Tuğla dokusu üreticisi (prosedürel, KENDİ TELİFİMİZ).

## Bu doku bir KUSURDAN doğdu

Galata Kulesi'nin gövdesinde **13,20 ve 17,17 m** kotlarında iki tuğla kuşak
var (TDV) ve ilki 1509 depremi onarımının **dikişidir** — o kottan yukarısı
Mimar Murad bin Hayreddin'in işi. Yani kuşak bir süs değil, iki yapım
evresinin sınırı.

Kuşaklar `cutstone` (kumtaşı) malzemesiyle üretildi ve v2 render'ı kusuru
gösterdi: kuşak **tuğla olarak değil, gövdeye dolanmış ince bir gölge
çizgisi** olarak okunuyordu. Sebep açık — kuşağın anlamı **rengindedir** ve
kumtaşı ile moloz taş neredeyse aynı renkte.

Poly Haven'da tuğla dokusu indirilmedi ve lisanssız görsel indirmek yasak
(CLAUDE.md). Mermer, kurşun ve yaprakta olduğu gibi: üretmek.

## Ölçüler KAYNAKLIDIR — karo boyu onlardan TÜREDİ

Osmanlı almaşık duvarında tuğla **35 × 35 × 4,5 cm**; şaşırtmalı örgü için
yarısı (17,5 × 35). Tuğlalar arası **derz 2,5–3 cm**.
*(Kaynak: DergiPark, "XIV–XV. Yüzyıllara Ait Osmanlı Camilerinde Görülen
Tuğla-Taş Almaşıklığı Üzerine Gözlemler"; Bizans almaşık duvar literatürü.)*

Karo boyu seçilmedi, **hesaplandı**: sıra adımı 4,5 + 3,0 = **7,5 cm**, tuğla
adımı 35 + 2,5 = **37,5 cm**. 0,75 m'lik kare hem 10 sıraya hem 2 tuğlaya tam
bölünür ve iki derz de belgeli 2,5–3,0 cm aralığında kalır. Başka bir boy ya
dikişi gösterirdi ya da derzi kaynağın dışına çıkarırdı.

## Ölçüt: KUSURUN KENDİSİ

Asıl eşik parlaklık ya da tanecik değil, düzeltilen kusurdur: **kuşak,
yanındaki moloz taştan ayırt edilebilmeli.** Bu yüzden ana ölçü, dokunun
ortalama renginin `old_stone_wall`ın ortalama renginden **CIELAB ΔE**
uzaklığıdır — yani "bu iki yüzey yan yana durduğunda göz onları ayırır mı".

İkinci ölçü, iddia edilen düzenin gerçekten orada olduğudur: satır
ortalamalarının baskın dikey periyodu **7,5 cm** çıkmalı. Bu genel bir
"periyodiklik ölçer" değil (mermerde öyle bir aletin ölçemediği yazılıydı);
"benim koyduğum sıra aralığı dokuda var mı" sorusudur ve ölçülebilir.

Kullanım:
  python tools/textures/gen_brick_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl          # noqa: E402

# --- belgeli ölçüler (m) ---------------------------------------------------

BRICK_T = 0.045           # tugla kalinligi 4,5 cm
BRICK_L = 0.350           # tugla boyu 35 cm
JOINT_V = 0.030           # sira arasi derz 3,0 cm  (belgeli aralik 2,5-3,0)
JOINT_H = 0.025           # dusey derz 2,5 cm       (ayni aralik)

COURSE = BRICK_T + JOINT_V        # 0,075 m
PITCH = BRICK_L + JOINT_H         # 0,375 m
TILE = 0.75                       # 10 sira x 2 tugla — ikisine de tam bolunur

# --- ölçütler --------------------------------------------------------------

#: Kuşağın yanındaki taştan CIELAB uzaklığı. Kusur "kuşak taştan ayırt
#: edilemiyor"du; eşik bu yüzden buraya konuldu. 20, "bakan biri iki ayrı
#: malzeme görür" için rahat bir sınır (ΔE 2-3 zar zor seçilir, 10 belirgin).
DELTA_E_MIN = 20.0

#: Sıra aralığı dokuda gerçekten olmalı. Ölçülen baskın dikey periyot
#: 7,5 cm'den %15'ten fazla sapamaz.
COURSE_TOL = 0.15

#: Yakın planda yüzey plastik olmasın — mermerdeki eşiğin aynısı.
FINE_MIN = 1.2

#: Derzli bir yüzey KABA olmalı. Mermerde bu bir TAVANDI (<=4,0, "derzli
#: olma"); tuğlada TABANDIR. Kıyas ölçümleri aynı tablodan:
#: sıva 1,04 · moloz taş 2,99 · derzli kumtaşı 4,79 · arnavut kaldırımı 5,05.
COARSE_MIN = 3.0

STONE_BC = os.path.join("art", "textures", "polyhaven", "old_stone_wall",
                        "T_old_stone_wall_BC.jpg")

SPECS = [
    # Osmanli tuglasi pisirilmis kildir: turuncu-kirmizi, firindan cikisa gore
    # degisken. Horasan harci kirec + KIRIK TUGLA tozudur, yani pembemsi —
    # duz beyaz-gri bir derz Bizans/Osmanli duvarini yanlis gosterirdi.
    #
    # Renkler OLCUMLE koyulastirildi. Ilk deger (tugla 146,74,52 / harc
    # 196,180,160) ayri ayri gayet farkliydi — tugla dE 30,8, harc dE 23,3 —
    # ama KARISIMIN ortalamasi tasin tam ustune dustu (dE 12,3): koyu kirmizi
    # ile acik harcin ortalamasi, tasin sicak grisinin ta kendisi. Uzaktan
    # bakildiginda kusak yine kaybolurdu, yani duzeltilen kusurun aynisi.
    #
    # Harc bu yuzden yaslandirildi (horasan zaten pembedir, kirec beyazi
    # degil) ve tugla doygunlastirildi; ortalama artik kirmiziya kayiyor.
    dict(id="brick_band", size=TILE, seed=1509,
         brick=(158, 66, 44), brick_alt=(112, 48, 32), brick_hot=(198, 110, 72),
         mortar=(168, 132, 108)),
]


def _bond(res, size):
    """
    Şaşırtmalı (running bond) örgünün maskeleri.

    Döner: `(mortar, brick_id, u, v)` — sırasıyla derz maskesi (1 = derz),
    her tuğlaya ayrı bir tam sayı (renk değişkenliği için), ve tuğla
    içindeki yerel koordinatlar (kenar aşınması için).
    """
    y = (np.arange(res) + 0.5) / res * size          # metre
    x = (np.arange(res) + 0.5) / res * size

    row = np.floor(y / COURSE).astype(int)           # sira numarasi
    y_in = y - row * COURSE                          # sira icindeki kot

    # Sira icinde: once tugla, sonra derz.
    mort_v = (y_in > BRICK_T).astype(float)

    # Tek siralar YARIM TUGLA kayar — sasirtmali orgunun tanimi.
    off = np.where(row % 2 == 1, PITCH * 0.5, 0.0)
    xg = (x[None, :] + off[:, None])
    col = np.floor(xg / PITCH).astype(int)
    x_in = xg - col * PITCH
    mort_h = (x_in > BRICK_L).astype(float)

    mortar = np.clip(mort_v[:, None] + mort_h, 0.0, 1.0)
    brick_id = (row[:, None] * 7919 + col) % 9973
    u = np.clip(x_in / BRICK_L, 0.0, 1.0)
    v = np.clip((y_in / BRICK_T)[:, None] * np.ones_like(x_in), 0.0, 1.0)
    return mortar, brick_id, u, v


def _stone_rgb():
    """Kuşağın yanında duracak taşın ortalama sRGB rengi (0-255)."""
    path = os.path.join(os.getcwd(), STONE_BC)
    if not os.path.exists(path):
        return None
    img = np.asarray(Image.open(path).convert("RGB").resize((256, 256)),
                     dtype=np.float64)
    return img.reshape(-1, 3).mean(axis=0)


def course_period_m(gray, size):
    """Satır ortalamalarının baskın dikey periyodu (metre)."""
    prof = gray.mean(axis=1)
    f = np.abs(np.fft.rfft(prof - prof.mean()))
    k = int(np.argmax(f[1:]) + 1)
    return size / k


def fine_energy(gray):
    k = np.zeros_like(gray)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            k += np.roll(np.roll(gray, dy, 0), dx, 1)
    return float(np.abs(gray - k / 9.0).mean())


def coarse_energy(gray, block):
    n = gray.shape[0] // block
    r = gray[:n * block, :n * block].reshape(n, block, n, block).mean(axis=(1, 3))
    k = np.zeros_like(r)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            k += np.roll(np.roll(r, dy, 0), dx, 1)
    return float(np.abs(r - k / 9.0).mean())


def build(spec, res):
    rng = np.random.default_rng(spec["seed"])
    mortar, bid, u, v = _bond(res, spec["size"])

    # Tugla renk degiskenligi: firinda her tugla ayni pismez. Tugla BASINA
    # tek bir cekilis — icinde gradyan olmamali, yoksa tugla degil leke olur.
    #
    # Yayilim OLCUYLE genisletildi: dar bir yelpazede (132..180) kaba
    # enerji 2,93 cikiyordu (esik 3,0), yani 20 cm olcekte duvar
    # duzlesiyordu. Orta mesafede tuglayi tugla yapan sey tam da
    # tugladan tuglaya degisen pisme rengidir.
    hues = rng.random(9973)
    t = hues[bid][..., None]
    b0 = pl.srgb_to_linear(spec["brick"])
    b1 = pl.srgb_to_linear(spec["brick_alt"])
    b2 = pl.srgb_to_linear(spec["brick_hot"])
    brick = np.where(t < 0.5,
                     b1[None, None, :] * (1.0 - t * 2.0) + b0[None, None, :] * (t * 2.0),
                     b0[None, None, :] * (2.0 - t * 2.0) + b2[None, None, :] * (t * 2.0 - 1.0))

    # KENAR ASINMASI: tuglanin kosesi yuvarlanir ve harc oraya tasar.
    edge = np.minimum(np.minimum(u, 1.0 - u) / 0.06,
                      np.minimum(v, 1.0 - v) / 0.22)
    edge = np.clip(edge, 0.0, 1.0)

    grain = pl.fine_grain(res, rng)
    # Harc kaba ve gozeneklidir; horasan icinde kirik tugla taneleri vardir.
    speck = pl.blob_field(res, 260, res / 26.0, 1.0, rng, exponent=1.4)

    mort_c = pl.srgb_to_linear(spec["mortar"])[None, None, :]
    mort_c = mort_c * (0.88 + 0.24 * speck[..., None])

    # Kenar asinmasinin harc payi 0,75'ten 0,45'e indi: olculdu, 0,75'te
    # harc tugla yuzunun ustune tasiyor ve efektif harc orani %43 yerine
    # cok daha yuksek cikiyor — dokunun ortalamasi acilip tasa yaklasiyordu.
    m = np.clip(mortar + (1.0 - edge) * 0.45, 0.0, 1.0)[..., None]
    col = brick * (1.0 - m) + mort_c * m
    # Ince tane genligi OLCUYLE secildi: 0,30 ile ince enerji 0,80 cikiyordu
    # (esik 1,2) — yakin planda yuzey plastikti. Pismis kil ve horasan ikisi
    # de gozeneklidir; parlak duz bir yuzey ikisini de yanlis gosterir.
    col *= (1.0 + 0.70 * (grain - 0.5))[..., None]

    bc = (np.clip(pl.linear_to_srgb(col), 0.0, 1.0) * 255.0).round().astype(np.uint8)

    # Kabartma: DERZ GERI CEKILIR. Almasik duvarda tugla sirasi harctan
    # disari durur; golge cizgisini veren budur.
    h_m = (-0.012 * np.clip(mortar + (1.0 - edge) * 0.6, 0.0, 1.0)
           + 0.0006 * (grain - 0.5) + 0.0015 * (speck - 0.5) * mortar)
    amp = float(h_m.max() - h_m.min())
    nrm = (pl.normal_from_height(pl.normalize(h_m),
                                 strength=amp * res / spec["size"]) * 255.0
           ).round().astype(np.uint8)

    # Pismis kil mat; harc daha da mat ve gozenekli.
    rough = np.clip(0.72 + 0.16 * mortar.astype(float) + 0.05 * (1.0 - grain),
                    0.0, 1.0)
    ao = np.clip(1.0 - 0.42 * np.clip(mortar + (1.0 - edge) * 0.5, 0.0, 1.0),
                 0.0, 1.0)
    metal = np.zeros_like(rough)
    arm = (np.stack([ao, rough, metal], axis=-1) * 255.0).round().astype(np.uint8)
    return bc, nrm, arm, rough, ao


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    stone = _stone_rgb()
    bad = 0
    for spec in SPECS:
        bc, nrm, arm, rough, ao = build(spec, a.res)
        gray = (0.2126 * bc[:, :, 0] + 0.7152 * bc[:, :, 1]
                + 0.0722 * bc[:, :, 2]).astype(float)

        mean_rgb = bc.reshape(-1, 3).mean(axis=0)
        de = pl.delta_e(mean_rgb, stone) if stone is not None else float("nan")

        period = course_period_m(gray, spec["size"])
        fine = fine_energy(gray)
        coarse = coarse_energy(gray, max(2, int(round(0.20 / spec["size"] * a.res))))

        ok_de = (stone is None) or de >= DELTA_E_MIN
        ok_p = abs(period - COURSE) / COURSE <= COURSE_TOL
        ok = ok_de and ok_p and fine >= FINE_MIN and coarse >= COARSE_MIN
        bad += 0 if ok else 1

        d = pl.write_texture_set(
            spec["id"], a.res, spec["size"], bc, nrm, arm,
            meta_extra=dict(
                generated_by="tools/textures/gen_brick_texture.py",
                role="brick",
                use="Tugla kusagi — Galata Kulesi govdesi 13,20 ve 17,17 m "
                    "(M_Brick_Band); tugla-tas almasik orgu",
                why="Kusaklar cutstone ile uretilmisti ve render'da tugla "
                    "olarak degil govdeye dolanmis ince bir GOLGE CIZGISI "
                    "olarak okunuyordu — kusagin anlami rengindedir. Poly "
                    "Haven'da tugla indirilmedi, lisanssiz gorsel indirmek "
                    "yasak (CLAUDE.md).",
                dimensions_note=(
                    "Karo boyu SECILMEDI, hesaplandi: tugla 35x35x4,5 cm, "
                    "derz 2,5-3,0 cm (Osmanli almasik orgu literaturu). Sira "
                    "adimi 7,5 cm, tugla adimi 37,5 cm; 0,75 m ikisine de tam "
                    "bolunur ve iki derz de belgeli araliktadir."),
                measured=dict(delta_e_vs_old_stone_wall=round(de, 1),
                              course_period_m=round(period, 4),
                              fine=round(fine, 2), coarse=round(coarse, 2)),
            ),
            rough=rough, ao=ao, out_root=a.out)

        print(f"[HZ] {spec['id']}: {a.res}x{a.res}, {spec['size']} m -> {d}")
        print(f"[HZ]   tastan ayrim dE {de:5.1f} (>={DELTA_E_MIN})   "
              f"{'OK' if ok_de else 'KALDI'}")
        print(f"[HZ]   sira araligi {period * 100:5.2f} cm "
              f"(hedef {COURSE * 100:.2f} +/- %{COURSE_TOL * 100:.0f})   "
              f"{'OK' if ok_p else 'KALDI'}")
        print(f"[HZ]   ince {fine:5.2f} (>={FINE_MIN})   "
              f"kaba {coarse:5.2f} (>={COARSE_MIN})")

    print(f"[HZ] {len(SPECS)} prosedurel tugla dokusu uretildi, {bad} olcut disi")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
