# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — `docs/feedback/` icin indeks uretir.

## Neden

Klasorde **elli** inceleme dosyasi var ve `README.md` yalnizca sureci
anlatiyor; hangi varligin incelendigini, hangisinin onaylandigini,
hangisinde Caner'e sorulmus acik bir soru kaldigini gormek icin dosyalari
tek tek acmak gerekiyor. Onay dongusunun tamami bu klasorden geciyor;
girisi olmayan bir klasor, dolu da olsa okunmaz.

Indeks TURETILMIS veridir, o yuzden elle yazilmaz: bu betik uretir ve
`README.md` icindeki iki isaret arasina yazar. Boylece yeni bir inceleme
dosyasi eklendiginde indeks bir komutla guncellenir ve eskimez.

## Durum nasil okunuyor

- `ONAYLANDI` ya da `OK v<n>` gecen dosya **onayli**,
- "Caner'e soru" / "hangisi?" / "Onay" basligi altinda bos birakilmis
  dosya **soru bekliyor**,
- ikisi de yoksa **kayit**.

Okuma metinden yapiliyor, yani dosya sablonu degisirse indeks de
degisir — bu bir kusur degil: sablon degistiginde indeksin de
degismesi DOGRU olan.

## Kullanim

    python tools/olcum/geri_bildirim_indeksi.py          # yazar
    python tools/olcum/geri_bildirim_indeksi.py --goster  # yalniz basar
"""

import argparse
import glob
import io
import os
import re
import sys

KOK = os.path.join("docs", "feedback")
BAS = "<!-- INDEKS BASLANGIC (uretilmis; tools/olcum/geri_bildirim_indeksi.py) -->"
SON = "<!-- INDEKS SON -->"


def _durum(metin):
    if re.search(r"ONAYLANDI|OK v\d", metin):
        return "onaylı"
    if re.search(r"Caner'e soru|hangisi\?|## Onay", metin):
        return "soru bekliyor"
    return "kayıt"


def _baslik(metin, dosya):
    for satir in metin.splitlines():
        if satir.startswith("# "):
            return satir[2:].strip()
    return dosya


def indeks():
    satirlar = []
    for y in sorted(glob.glob(os.path.join(KOK, "*.md"))):
        ad = os.path.basename(y)
        if ad == "README.md":
            continue
        m = io.open(y, encoding="utf-8", errors="replace").read()
        satirlar.append((ad, _baslik(m, ad), _durum(m)))
    out = [BAS, "", "## İçindekiler", "",
           f"_{len(satirlar)} inceleme dosyası. Bu bölüm üretilmiştir; "
           "elle düzenlenmez._", "",
           "| dosya | ne | durum |", "|---|---|---|"]
    for ad, bas, dur in satirlar:
        out.append(f"| [{ad}]({ad}) | {bas} | {dur} |")
    out += ["", SON]
    return "\n".join(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--goster", action="store_true")
    a = ap.parse_args()
    yeni = indeks()
    if a.goster:
        print(yeni)
        return 0
    p = os.path.join(KOK, "README.md")
    s = io.open(p, encoding="utf-8").read()
    if BAS in s and SON in s:
        s = s[:s.index(BAS)] + yeni + s[s.index(SON) + len(SON):]
    else:
        s = s.rstrip() + "\n\n" + yeni + "\n"
    io.open(p, "w", encoding="utf-8", newline="").write(s)
    print(f"[HZ] {p} guncellendi ({yeni.count('| [')} satir)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
