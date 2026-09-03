"""
Hezarfen: 1632 — Kösele (deri) dokusu (prosedürel, kendi telifimiz).

## Neden gerekti

İki ölçüm:

* `M_Leather_Mest` — her sakinin ayağındaki mest — **dokusuz**
  (`_BaseColorMap: {fileID: 0}`). Kumaşta ve sakalda öğrenilen ders
  burada da geçerli: dokusuz albedo HDRP'de plastik okur.
* `M_Leather` dokuluydu ama dokusu **kereste**ydi
  (`weathered_planks`, boyanmış). Bir kayışa tahta damarı giydirmek,
  dokusuz bırakmaktan daha az görünür ama daha yanlış: yüzey yalan
  söyler.

## Ne üretiyor

Tek set, `kosele`:

* **Tane** — derinin gözenek dokusu. Worley hücreleri; boyut gerçek
  ölçüden türer (dana köselesinde gözenek 0,6–1,2 mm).
* **Kırışık** — az sayıda uzun kıvrım. Ayakkabı burnunda ve kayışın
  büküldüğü yerde deri kırışır; bunlar alçak frekanslıdır ve taneden
  ayrı bir katman olmalı, yoksa yüzey tek tip bir zımpara olur.
* **Aşınma** — kırışığın tepesi parlaktır (el ve toprak orayı
  cilalar), çukuru mattır. Pürüzlülüğün kendisi bu farkı taşır.

## Albedo neden nötr

Kumaş ve sakalla aynı sözleşme: renk paletten gelir
(`leather` koyu kahve, `mest` sarı) ve malzeme onu `_BaseColor` ile
çarpar. Renkli bir albedo sarı mesti kahveye çevirirdi.

Kullanım:
  python tools/textures/gen_kosele_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import proclib as pl   # noqa: E402


#: Doku 20 cm'lik bir alanı kaplar: bir mestin üstü kabaca bu kadar,
#: yani doku ayakkabıda bir kez döşenir ve tekrar okunmaz.
SIZE_M = 0.20

#: Gözenek boyu (m). Dana köselesinde 0,6-1,2 mm; ortası alındı.
GOZENEK_M = 0.0009

#: Kırık ağının hücre boyu (m). Köselede iri tane ("grain break")
#: kabaca 6-10 mm'lik adacıklar hâlinde ayrışır; ortası alındı.
KIRIK_M = 0.008

#: Tanenin yüzeyden yüksekliği (m). Ölçülebilir bir sayı: gözenek
#: derinliği çapının kabaca üçte biri.
KABARTMA = 0.00030


def build(res, tohum=1632):
    rng = np.random.default_rng(tohum)

    # --- TANE: worley hucreleri ---------------------------------------
    # Hucre sayisi gercek olcuden turer, gozle secilmez: 20 cm'lik
    # karede 0,9 mm'lik gozenek, kenarda ~222 hucre eder.
    hucre = max(16, int(round(SIZE_M / GOZENEK_M)))
    # `worley` (F1, F2) doner: F1 en yakin hucre merkezine mesafe.
    # Gozenek CUKURDUR ve merkezinde en derindir — yani F1 buyudukce
    # yuzey YUKSELIR; hucre siniri (F1 en buyuk) gozenegin kenari.
    f1, _f2 = pl.worley(res, hucre * hucre, rng)
    tane = (f1 - f1.min()) / max(1e-6, f1.max() - f1.min())

    # --- KIRIK AGI: WORLEY'IN KENDI TARIFI ----------------------------
    #
    # Ilk denemede kirisiklari yedi tane uzun sinus egrisi olarak
    # cizdim ve dokuya BAKINCA goruldu: yuzeyde kareyi bastan basa
    # gecen solucanlar var. Deri oyle kirilmaz; kirik bir COKGEN AGIDIR
    # — kisa, aci yapan, adacik cevreleyen.
    #
    # `proclib.worley`in kendi aciklamasi bunu zaten yaziyor: "kirik
    # cizgisi F2 - F1 ~ 0 olan yerdir". Kutuphaneyi tarif edildigi gibi
    # kullanmak, ona benzeyen bir sey uydurmaktan iyidir.
    kirik_hucre = max(8, int(round(SIZE_M / KIRIK_M)))
    g1, g2 = pl.worley(res, kirik_hucre * kirik_hucre, rng, jitter=0.9)
    sinir = g2 - g1
    genislik = max(1.0, res * KIRIK_M / SIZE_M * 0.16)   # px
    kir = np.exp(-(sinir / genislik) ** 2).astype(np.float32)

    # --- YUKSEKLIK ----------------------------------------------------
    # Tane yuzeyin tamami, kirisik ondan DAHA DERIN bir oyuk. Ikisini
    # toplamak deriyi kabartir; dogru olan kirisigin taneyi BASTIRMASI.
    h = 0.72 * tane + 0.28 * pl.fine_grain(res, rng)
    h = np.clip(h - 0.55 * kir, 0.0, 1.0)

    # --- ALBEDO: NOTR, KUMASLA AYNI AILEDE ----------------------------
    # Renk paletten gelir; doku yalnizca yuzeyi tasir.
    v = np.clip(0.72 + (h - float(h.mean())) * 0.22, 0.35, 0.95)
    bc = (pl.linear_to_srgb(np.stack([v, v, v], axis=-1)) * 255.0)
    bc = bc.round().astype(np.uint8)

    nrm = (pl.normal_from_height(h, KABARTMA * res / SIZE_M) * 255.0
           ).round().astype(np.uint8)

    # --- PURUZLULUK: KIRISIGIN TEPESI CILALIDIR -----------------------
    # Deri kullanildikca parlar ve en cok kirisigin SIRTI parlar; oluk
    # dibi mat kalir. Tek bir puruzluluk degeri bunu anlatamaz ve deriyi
    # plastik yapar.
    puru = np.clip(0.68 - 0.20 * h + 0.14 * kir, 0.34, 0.86)

    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    cukur = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    ao = np.clip(1.0 - 0.38 * cukur ** 0.7, 0.46, 1.0)

    metal = np.zeros_like(h)
    arm = (np.stack([ao, puru, metal], axis=-1) * 255.0)
    arm = arm.round().astype(np.uint8)
    return bc, nrm, arm, puru, ao


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    bc, nrm, arm, puru, ao = build(a.res)
    d = pl.write_texture_set(
        "kosele", a.res, SIZE_M, bc, nrm, arm,
        meta_extra=dict(
            generated_by="tools/textures/gen_kosele_texture.py",
            role="deri",
            use="mest, kayis ve kemer",
            why="M_Leather_Mest dokusuzdu (_BaseColorMap fileID 0) ve "
                "M_Leather dokuluydu ama dokusu KERESTEYDI "
                "(weathered_planks, boyanmis). Bir kayisa tahta damari "
                "giydirmek dokusuz birakmaktan daha az gorunur ama daha "
                "yanlis: yuzey yalan soyler.",
            base_color_note="BC bilerek notr. Renk paletten gelir "
                            "(leather koyu kahve, mest sari) ve malzeme "
                            "onu _BaseColor ile carpar.",
            grain_mm=GOZENEK_M * 1000.0,
        ),
        rough=puru, ao=ao, out_root=a.out)
    print(f"[HZ] kosele: {a.res}x{a.res}, {SIZE_M} m -> {d}")
    print(f"[HZ]   gozenek {GOZENEK_M * 1000:.1f} mm, "
          f"purzuluk {puru.min():.2f}-{puru.max():.2f}, "
          f"AO {ao.min():.2f}-{ao.max():.2f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
