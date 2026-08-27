"""
Hezarfen: 1632 — Kurşun örtü dokusu üreticisi (prosedürel, KENDİ TELİFİMİZ).

## Neden bu doku eksikti ve neden önemli

Kubbe kurşunla örtülür: hamamın, hanın, mescidin ve türbenin **üstü** budur.
Yani şehre yukarıdan bakıldığında görülen yüzeylerin çoğu kurşundur — bir uçuş
oyununda en çok bakılan yüzey. ADR 0017'de "uygun CC0 dokusu yok" diye boş
bırakılmıştı; palet düz gri bir renk veriyordu ve bütün kubbeler tek parça
plastik gibi okunuyordu.

Poly Haven'da kurşun örtü yok (metal dokuları sac levha, paslı çelik, dövme
demir). Yaprakta olduğu gibi çözüm indirmek değil **üretmek**: çıktı bizim
eserimizdir.

## Kurşun örtüyü kurşun yapan şey: DİKİŞ

Kurşun tek parça dökülmez; el ile açılmış levhalar hâlinde serilir ve
birleşimler **katlanarak** kapatılır. Eğim yönündeki birleşim ahşap bir çıta
üstüne kıvrılır — *rulo dikiş*, belirgin bir sırt yapar. Eğime dik birleşim
daha alçak katlanır (*enine kat*). Levhalar arası düzlem hafifçe çöker, çünkü
kurşun ağırdır ve akar.

Bu üç şey olmadan doku "gri metal levha" olur; bunlarla birlikte kubbe
**ölçek kazanır**: dikiş aralığı gözün kubbenin büyüklüğünü okuduğu cetveldir.

## Metaliklik neden 1,0 değil

Havaya açık kurşun birkaç ayda bazik kurşun karbonatla kaplanır ve o tabaka
**metal değildir** (dielektrik). Yüzey bu yüzden karışımdır: yıkanan sırtlar
çıplak metale yakın, düzlükler oksitle örtülü. Maske R kanalı bu **örtü
oranını** taşır (0,18–0,78 arası ölçülür), malzeme tarafındaki `_Metallic`
çarpanı 1,0'dır — HDRP ikisini çarpar.

Üst sınırın 1,0'a dayanmaması ayrıca bilinçli: sahnede henüz dolaylı
aydınlatma pişmemiş (ADR 0019 §11). Tam metal bir yüzeyin taban rengi yoktur,
yalnızca yansıması vardır; yansıtacak bir şey olmadığında **siyah** çıkar.
Aydınlatma fazında GI pişince bu tavan yeniden ölçülmeli.

Kullanım:
  python tools/textures/gen_lead_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl          # noqa: E402

# Levha ölçüsü DÖNEMİN ölçüsüdür, keyfi değil: el ile dövülen kurşun levha
# taşınabilir olmak zorundadır (~30 kg). 0,50 × 1,00 m bu sınırın içindedir ve
# 2 m'lik döşenebilir karede tam 4 × 2 levha eder — kesirli levha, dikişi
# karonun kenarında kırardı.
SPECS = [
    dict(id="lead_sheet", size=2.0, sheet_w=0.50, sheet_h=1.00, seed=29,
         base=(124, 127, 131), dark=(84, 88, 94), oxide=(176, 175, 168)),
]

ROLL_SIGMA = 0.030      # egim yonundeki rulo dikis yariciapi (m)
WELT_SIGMA = 0.018      # enine kat (m)
ROLL_H = 0.032          # rulo sirt yuksekligi (m)
WELT_H = 0.013
DISH_H = 0.007          # levha ortasindaki cokme (m)


def _seam_fields(res, size_m, sheet_w, sheet_h):
    """Levha ızgarası: sırt yükseklikleri ve levha içi konum (döşenebilir)."""
    cols = size_m / sheet_w
    rows = size_m / sheet_h
    if abs(cols - round(cols)) > 1e-6 or abs(rows - round(rows)) > 1e-6:
        raise ValueError(f"levha olcusu {sheet_w}x{sheet_h} m, {size_m} m kareye "
                         f"TAM bolunmuyor — dikis karo kenarinda kirilirdi")

    x = (np.arange(res) + 0.5) / res
    uu = (x * cols) % 1.0
    vv = (x * rows) % 1.0
    du = np.minimum(uu, 1.0 - uu) * sheet_w        # en yakin dusey dikise m
    dv = np.minimum(vv, 1.0 - vv) * sheet_h

    roll = np.exp(-(du / ROLL_SIGMA) ** 2)[None, :] * np.ones((res, 1))
    welt = np.exp(-(dv / WELT_SIGMA) ** 2)[:, None] * np.ones((1, res))
    dish = ((1.0 - (2.0 * uu - 1.0) ** 2)[None, :]
            * (1.0 - (2.0 * vv - 1.0) ** 2)[:, None])
    return roll, welt, dish


def build(spec, res):
    rng = np.random.default_rng(spec["seed"])
    roll, welt, dish = _seam_fields(res, spec["size"], spec["sheet_w"],
                                    spec["sheet_h"])
    # Oksit lekeleri: yumusak, yonsuz, ve KUCUK.
    #
    # Ilk denemede 420 adet ve res/20 yaricapindaydi; olcum lekelerin taban
    # tonun 50 seviye ustune ciktigini gosterdi (p99 = 164, std 12,2) ve doku
    # "sicramis boya" gibi okundu. Patina bir sicrama degil **pus**tur: cok,
    # kucuk ve az kontrastli.
    mottle = pl.blob_field(res, 950, res / 34.0, 1.7, rng, exponent=1.1)
    grain = pl.fine_grain(res, rng)     # proclib'e tasindi (arazi de kullaniyor)

    # --- yukseklik, METRE cinsinden kurulur (normal gucu buradan cikar)
    h_m = (ROLL_H * roll + WELT_H * welt - DISH_H * dish
           + 0.0035 * (mottle - 0.5) + 0.0012 * (grain - 0.5))
    amp = float(h_m.max() - h_m.min())
    h = pl.normalize(h_m)

    # --- taban renk
    base = pl.srgb_to_linear(spec["base"])
    dark = pl.srgb_to_linear(spec["dark"])
    oxide = pl.srgb_to_linear(spec["oxide"])

    # Oksit ORTUSU: lekelerin ustunde yogun, aralarinda seyrek.
    ox = np.clip((mottle - 0.40) / 0.45, 0.0, 1.0) ** 1.15
    tone = np.clip(0.30 + 0.85 * mottle + 0.25 * (grain - 0.5), 0.0, 1.0)
    OX_ALBEDO = 0.42          # olculerek secildi — dosya sonundaki nota bak

    col = dark[None, None, :] * (1.0 - tone[..., None]) + base[None, None, :] * tone[..., None]
    col = (col * (1.0 - OX_ALBEDO * ox[..., None])
           + oxide[None, None, :] * (OX_ALBEDO * ox[..., None]))
    # Rulo ve kat sirtlari yagmurla YIKANIR: oksit orada tutunmaz, metal cikar.
    wash = np.clip(0.55 * roll + 0.30 * welt, 0.0, 0.75)
    col = col * (1.0 - wash[..., None]) + base[None, None, :] * wash[..., None]
    col *= (1.0 + 0.09 * roll + 0.05 * welt)[..., None]
    bc = (pl.linear_to_srgb(col) * 255.0).round().astype(np.uint8)

    nrm = (pl.normal_from_height(h, strength=amp * res / spec["size"]) * 255.0
           ).round().astype(np.uint8)

    # --- purzuluk ve metaliklik ORTUSU (gerekce dosya basliginda)
    rough = np.clip(0.50 - 0.16 * roll + 0.24 * ox - 0.05 * (grain - 0.5),
                    0.0, 1.0)
    metal = np.clip(0.62 - 0.50 * ox + 0.16 * roll, 0.0, 1.0)

    # --- AO: ISIK GORMEYEN yer CUKUR olan degil, ICBUKEY olandir.
    #
    # Levha ortasi cukurdur ama gogu bol gorur; karanlik olan yer dikisin
    # DIBIDIR. Olcut bu yuzden yukseklik degil egrilik (Laplace): pozitif
    # deger vadi tabani demektir.
    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    valley = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    ao = np.clip(1.0 - 0.50 * valley ** 0.6, 0.30, 1.0)

    arm = ((np.stack([ao, rough, metal], axis=-1)) * 255.0).round().astype(np.uint8)
    return bc, nrm, arm, rough, ao, metal


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    for spec in SPECS:
        bc, nrm, arm, rough, ao, metal = build(spec, a.res)
        d = pl.write_texture_set(
            spec["id"], a.res, spec["size"], bc, nrm, arm,
            meta_extra=dict(
                generated_by="tools/textures/gen_lead_texture.py",
                role="lead",
                use="Kubbe ve konik kulah kursun ortusu (M_Lead_Sheet)",
                why="Poly Haven'da kursun ortu yok; lisanssiz gorsel indirmek "
                    "yasak (CLAUDE.md). Kubbe ustu, ucus oyununda en cok "
                    "bakilan yuzeydir — duz gri renk birakilamazdi.",
                sheet_meters=[spec["sheet_w"], spec["sheet_h"]],
                metallic_note="Maske R kanali OKSIT ORTUSUNU tasir; malzemede "
                              "_Metallic = 1,0 carpan olarak durur.",
            ),
            rough=rough, ao=ao, out_root=a.out)
        print(f"[HZ] {spec['id']}: {a.res}x{a.res}, {spec['size']} m, "
              f"levha {spec['sheet_w']}x{spec['sheet_h']} m -> {d}")
        print(f"[HZ]   metaliklik ortusu {metal.min():.2f}-{metal.max():.2f}, "
              f"purzuluk {rough.min():.2f}-{rough.max():.2f}")

    print(f"[HZ] {len(SPECS)} prosedurel kursun dokusu uretildi")


if __name__ == "__main__":
    sys.exit(main())
