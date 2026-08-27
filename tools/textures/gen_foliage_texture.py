"""
Hezarfen: 1632 — Yaprak dokusu üreticisi (prosedürel, KENDİ TELİFİMİZ).

## Neden üretiliyor, indirilmiyor

Poly Haven'da kabuk dokusu var ama **yaprak** yok: yapraklı ağaç alfa atlası
ister ve kütüphane onu vermiyor. CLAUDE.md kuralı net — lisansı LICENSES.md'de
belgelenmemiş hiçbir görsel indirilemez. Prosedürel üretim bu kısıtın etrafından
dolaşmaz, **kaldırır**: çıktı bizim eserimizdir, kaydı da burada.

## Ne üretiyor

Kapalı geometriye giydirilen **döşenebilir yaprak kütlesi** dokusu — alfa
kartı değil. Ağacın tacı katı bir kabuktur (ADR 0019 §3); bu doku o kabuğu
düz bir yeşil blob olmaktan çıkarır: yaprak öbekleri, aralarındaki karanlık
boşluklar, ışığı kıran kabartı.

## Nasıl

Yükseklik alanı = rastgele konumlu, rastgele döndürülmüş elips "yaprak
öbekleri"nin toplamı (`proclib.blob_field`; toroidal mesafe → **döşenebilir**).
Sonra:
  * BC   : taban yeşil, öbek yüksekliğine göre açılır, çukurlar kararır
  * N    : yükseklik alanının gradyanı (OpenGL Y+, Poly Haven'la aynı)
  * R    : pürüzlülük (Blender inceleme render'ı bunu okur)
  * ARM  : R=AO (çukurlarda koyu), G=Roughness, B=Metallic(0)

**AO ayrı dosya olarak YAZILMAZ.** Sebebi ölçülebilir: taban renk zaten öbek
arası gölgeyle çarpılıyor (aşağıdaki `col *= 0.35 + 0.65·h`) — tacın katı
kabuk olmasını telafi eden şey o. Blender `AO` dosyasını görürse albedoyu bir
kez daha çarpar ve taç kararır. Kurşunda durum tersidir: orada gölge albedoya
işlenmez, AO kendi kanalında taşınır (bkz. `gen_lead_texture.py`).

Kullanım:
  python tools/textures/gen_foliage_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl          # noqa: E402

# (id, ölçü m, taban renk sRGB, öbek sayısı, öbek yarıçapı px, uzama, tohum)
#
# Servi PULSU yapraklıdır: çok küçük, sık, neredeyse siyaha çalan koyu yeşil.
# Çınar GENİŞ yapraklıdır: büyük loblar, açık ve sarıya çalan yeşil.
SPECS = [
    dict(id="foliage_servi", size=1.6, base=(52, 74, 46), tip=(96, 122, 74),
         count=2600, radius=11.0, stretch=2.4, seed=7, rough=0.62),
    dict(id="foliage_cinar", size=2.4, base=(74, 96, 44), tip=(140, 158, 78),
         count=900, radius=26.0, stretch=1.35, seed=13, rough=0.55),
]


def build(spec, res):
    rng = np.random.default_rng(spec["seed"])
    h = pl.blob_field(res, spec["count"], spec["radius"], spec["stretch"], rng)

    # Renk: öbeğin tepesi ışığa bakar ve AÇILIR, dipler kararır. Tek düz yeşil
    # ile fark: kütle derinlik kazanır, silueti taşıyan şey de o derinliktir.
    base = pl.srgb_to_linear(spec["base"])
    tip = pl.srgb_to_linear(spec["tip"])
    t = (h ** 1.25)[..., None]
    col = base[None, None, :] * (1.0 - t) + tip[None, None, :] * t
    # Öbek aralarındaki karanlık boşluk (yaprak arası gölge).
    col *= (0.35 + 0.65 * h[..., None] ** 0.6)
    bc = (pl.linear_to_srgb(col) * 255.0).round().astype(np.uint8)

    nrm = (pl.normal_from_height(h, strength=res / 55.0) * 255.0
           ).round().astype(np.uint8)

    # ARM: Poly Haven kanal duzeni (R=AO, G=Roughness, B=Metallic).
    ao = np.clip(0.30 + 0.70 * h ** 0.55, 0.0, 1.0)
    rough = np.clip(spec["rough"] + 0.18 * (1.0 - h), 0.0, 1.0)
    arm = np.stack([ao, rough, np.zeros_like(h)], axis=-1)
    arm = (arm * 255.0).round().astype(np.uint8)
    return bc, nrm, arm, rough


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    for spec in SPECS:
        bc, nrm, arm, rough = build(spec, a.res)
        d = pl.write_texture_set(
            spec["id"], a.res, spec["size"], bc, nrm, arm,
            meta_extra=dict(
                generated_by="tools/textures/gen_foliage_texture.py",
                role=spec["id"],
                use="Agac taci yaprak kutlesi",
                why="Poly Haven'da yaprak alfa atlasi yok; lisanssiz gorsel "
                    "indirmek yasak (CLAUDE.md). Prosedurel uretim kisiti kaldirir.",
            ),
            rough=rough, out_root=a.out)
        print(f"[HZ] {spec['id']}: {a.res}x{a.res}, {spec['size']} m -> {d}")

    print(f"[HZ] {len(SPECS)} prosedurel yaprak dokusu uretildi")


if __name__ == "__main__":
    sys.exit(main())
