"""
Hezarfen: 1632 — **Pişmiş probların içinde ışık var mı?**

## Neden bu ölçü

Fırın "başarılı" diyebilir, diske 157 MB yazabilir, çalışma zamanı
`kurulu/kume var` diyebilir — ve sokağın gölgesi yine siyah kalabilir.
Bu turda tam olarak bu oldu. Sorunun hangi yarıda olduğunu ancak
**verinin kendisi** söyler.

APV dört dosya yazar ve her biri ayrı bir soruyu cevaplar:

| dosya | içerik | ne söyler |
|---|---|---|
| `CellData` | L0 — ışınım | **ışık var mı** |
| `CellOptionalData` | L1 — yön | ışık yönlü mü |
| `CellSharedData` | geçerlilik | prob geçerli mi |
| `CellSupportData` | konum | prob nerede |

Bu betik L0'a bakar: baytların kaçı sıfır ve dosyada kaç ayrı değer
var. Işıksız pişmiş bir kümede L0 sıfırdır ve dosya tek bir desenin
tekrarıdır (`00 00 00 00 00 00 00 38`, yani half olarak 0/0/0/0,5).

Kullanım:
  python tools/olcum/prob_isigi.py
  python tools/olcum/prob_isigi.py --kok "unity/.../Faz1_Terrain"
"""

import argparse
import collections
import glob
import os
import sys


def olc(yol):
    with open(yol, "rb") as fh:
        b = fh.read()
    if not b:
        return None
    sifir = b.count(0)
    # Sekizli desen sayimi: L0 half dortluleri bu hizada duruyor.
    desen = collections.Counter(bytes(b[i:i + 8])
                                for i in range(0, min(len(b), 1 << 20), 8))
    enCok, adet = desen.most_common(1)[0]
    return dict(bayt=len(b), sifir_orani=sifir / len(b),
                farkli_desen=len(desen),
                en_cok=enCok.hex(), en_cok_orani=adet / max(1, sum(desen.values())))


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--kok", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Project", "Scenes",
        "Faz1_Terrain"))
    a = ap.parse_args()

    yollar = sorted(glob.glob(os.path.join(a.kok, "*.bytes")))
    if not yollar:
        print(f"[HZ] {a.kok} altinda .bytes yok — firin hic kosmamis.")
        return 1
    for y in yollar:
        d = olc(y)
        ad = os.path.basename(y).split("Set")[-1]
        print(f"{ad:34s} {d['bayt']:10d} bayt  sifir %{d['sifir_orani']*100:5.1f}  "
              f"{d['farkli_desen']:6d} farkli desen  "
              f"en cok {d['en_cok']} (%{d['en_cok_orani']*100:.1f})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
