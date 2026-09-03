"""
Hezarfen: 1632 — **Üretilip Unity'ye inmemiş varlık var mı?**

## Neden bu ölçü

Bu deponun ikinci tekrar eden dersi: *yazıldı, diske geçti,
bağlanmadı.* Bu turda üç doku (`kosele`, `kumas_kilim`, `sac_yuzey`)
üretilmiş, palet onlara işaret ediyordu ve Unity tarafındaki dosyaları
**hiç yazılmamıştı** — malzemeler var olmayan dokuları gösteriyordu ve
hiçbir yerde hata yoktu.

Aynı boşluk geometride de olabilir: `art/blend/` altında kanonik bir
varlık durur, kimse `_Import`'a ihraç etmemiştir ya da iniş koşmamıştır,
ve oyunda o varlık yoktur. Bu betik onu sorar.

## Eşleştirme neden önek ile

Prefab adı blend adının aynısı olmak zorunda değil: `SM_House_N.blend`
oyunda `PF_House_N_GenisSacak` olarak yaşıyor — aynı kit, adlandırılmış
varyant. Bu yüzden eşleştirme **önek** ile yapılır; tam ad araması
sahte kusur üretirdi.

## Bilinen ve doğru istisnalar

* `SM_AxisCalibration` — ölçü aleti, sahneye girmez.
* `SK_Hezarfen_Govde` — çıplak taban gövde; ara ürün, giyinik
  varyantların girdisi.

Kullanım:
  python tools/olcum/inmemis_varlik.py
"""

import argparse
import glob
import os
import sys

#: Prefab'ı olmaması DOĞRU olan kanonik varlıklar.
ISTISNA = {
    "AxisCalibration": "olcu aleti — sahneye girmez",
    "Hezarfen_Govde": "ara urun — giyinik varyantlarin girdisi",
}


def _sade(ad):
    for on in ("SM_", "SK_", "PF_"):
        if ad.startswith(on):
            return ad[len(on):]
    return ad


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--blend", default=os.path.join("art", "blend"))
    ap.add_argument("--prefab", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Project", "Art", "Prefabs"))
    a = ap.parse_args()

    blends = sorted({_sade(os.path.basename(y)[:-6])
                     for y in glob.glob(os.path.join(a.blend, "**", "*.blend"),
                                        recursive=True)})
    pf = sorted({_sade(os.path.basename(y)[:-7])
                 for y in glob.glob(os.path.join(a.prefab, "**", "*.prefab"),
                                    recursive=True)})

    eksik = []
    for b in blends:
        if b in ISTISNA:
            continue
        # ONEK ESLESMESI: `House_N` -> `House_N_GenisSacak` sayilir.
        if any(p == b or p.startswith(b + "_") for p in pf):
            continue
        eksik.append(b)

    print(f"[HZ] {len(blends)} kanonik varlik, {len(pf)} prefab, "
          f"{len(ISTISNA)} bilinen istisna")
    for b in eksik:
        print(f"[HZ] INMEMIS  {b}")
    if not eksik:
        print("[HZ] Hepsi inmis.")
    return 1 if eksik else 0


if __name__ == "__main__":
    sys.exit(main())
