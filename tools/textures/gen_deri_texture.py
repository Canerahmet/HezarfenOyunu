"""
Hezarfen: 1632 — Deri dokusu üreticisi (bizim bestemiz, CC0 maskelerden).

## Neden

Ölçüldü: `M_Skin` `kind=untextured` idi — tek bir düz renk. Bir yüzü yüz
yapan şeylerin hiçbiri yoktu: dudağın koyuluğu, göz kapağının gölgesi,
kulağın kanlanması, gözenek. HDRP'de dokusuz bir ten her zaman **mum**
gibi okur ve bu, karakterin "gerçekçi değil" görünmesinin ikinci büyük
sebebiydi (birincisi kumaştı — `gen_kumas_texture.py`).

## Girdi: MPFB2'nin kendi bölge maskeleri

MPFB2 eklentisi `data/textures/` altında **MakeHuman UV uzayında** bölge
maskeleri taşıyor: `mpfb_face`, `mpfb_lips`, `mpfb_eyelids`, `mpfb_ears`
ve bir SSS haritası. Bunlar eklentinin "enhanced skin" gölgelendiricisinin
girdisi ve **çekirdek varlık** — yani `refs/LICENSES.md`'deki MPFB2
satırının kapsadığı CC0 kümesi.

Gövdemiz zaten MakeHuman taban mesh'inden geliyor ve onun UV yerleşimini
taşıyor; maskeler bu yüzden hizalı. Kendi maskemizi çizmeye çalışmak
(dudak nerede, kulak nerede) UV yerleşimini tahmin etmek olurdu — bu
depoda "kaynak niteliksel olduğunda metrik geometri UYDURMA" kuralının
doku hâli.

## Çıktı bizim: maske değil BESTE

İndirilen şey bir deri dokusu değil, nerede ne olduğunu söyleyen siyah-
beyaz maskeler. Tenin rengi, dudağın ne kadar koyulaşacağı, gözeneğin
sıklığı, kulağın ne kadar kanlanacağı burada yazılıyor — ve hepsi
ölçülebilir sayılar.

## Albedo yine NÖTR

Kumaşta olduğu gibi: ten rengi **paletten** gelir ve kişiden kişiye
`_BaseColor` ile çarpılır. Doku yalnızca **oranları** taşır — dudak
tenden %28 koyu, göz kapağı %18, kulak %8 daha kırmızı. Sabit bir ten
rengi pişirilseydi şehirdeki herkes aynı tende olurdu; oysa asıl istenen
bunun tersi.

Kullanım:
  python tools/textures/gen_deri_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl                                        # noqa: E402


#: MPFB2 eklentisinin veri dizini. Sürüm/kullanıcı yolu makineye bağlı,
#: o yüzden aranır — sabit yol yazmak bu dosyayı ilk makine değişiminde
#: yalancı yapardı.
MPFB_ARAMA = [
    os.path.expandvars(
        r"%APPDATA%\Blender Foundation\Blender\5.2\extensions"
        r"\user_default\mpfb\data\textures"),
    os.path.expanduser(
        "~/.config/blender/5.2/extensions/user_default/mpfb/data/textures"),
]


def _maske_dizini():
    for y in MPFB_ARAMA:
        if os.path.isdir(y):
            return y
    raise SystemExit(
        "[HZ] HATA: MPFB2 doku dizini bulunamadi. Arananlar:\n  "
        + "\n  ".join(MPFB_ARAMA)
        + "\nMPFB2 kurulu mu? (refs/LICENSES.md, MPFB 2.0.17)")


def _maske(dizin, ad, res):
    """Tek kanallı 0-1 maske, `res` boyutuna indirilmiş."""
    im = Image.open(os.path.join(dizin, ad)).convert("L")
    im = im.resize((res, res), Image.LANCZOS)
    return np.asarray(im, dtype=np.float64) / 255.0


def _gozenek(res, rng):
    """
    Gözenek alanı — ince, yönsüz, **döşenmeyen** (UV atlası döşenmez).

    Ten gözeneği bir desen değil bir dağılımdır: sıklığı bölgeye göre
    değişir (burun ve alın sık, yanak seyrek) ama biz bölgeyi maskelerle
    ayırdığımız için burada tek bir taban alan yeterli.
    """
    kaba = rng.random((res // 8, res // 8))
    kaba = np.asarray(Image.fromarray((kaba * 255).astype(np.uint8))
                      .resize((res, res), Image.BICUBIC),
                      dtype=np.float64) / 255.0
    ince = rng.random((res // 2, res // 2))
    ince = np.asarray(Image.fromarray((ince * 255).astype(np.uint8))
                      .resize((res, res), Image.BILINEAR),
                      dtype=np.float64) / 255.0
    return kaba * 0.45 + ince * 0.55


def _adalar(maske, esik=0.16):
    """Maskedeki bağlı bölgeler: `[(cy, cx, yariçap), ...]`."""
    ikili = maske > esik
    etiket = np.zeros(ikili.shape, dtype=np.int32)
    su = 0
    res = ikili.shape[0]
    for y in range(res):
        for x in range(res):
            if not ikili[y, x] or etiket[y, x]:
                continue
            su += 1
            yigin = [(y, x)]
            etiket[y, x] = su
            while yigin:
                cy, cx = yigin.pop()
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = cy + dy, cx + dx
                    if 0 <= ny < res and 0 <= nx < res                             and ikili[ny, nx] and not etiket[ny, nx]:
                        etiket[ny, nx] = su
                        yigin.append((ny, nx))
    cikti = []
    for i in range(1, su + 1):
        ys, xs = np.nonzero(etiket == i)
        if len(ys) < 12:
            continue
        # Yaricap KISA eksenden: iris ovalin icine sigmali, tasmamali.
        ry = (ys.max() - ys.min()) * 0.5
        rx = (xs.max() - xs.min()) * 0.5
        cikti.append((ys.mean(), xs.mean(), min(ry, rx)))
    return cikti


def _goz_ciz(deger, kan, kapak, res):
    """
    Gözü **deriye çizer** — geometri değil.

    Göz küresi geometrisi ölçülüp reddedildi (gerekçe
    `gen_hezarfen.py`'de): MakeHuman'ın `helper-*-eye` kafesi yüzün
    10 cm önünde duruyor ve gözün gerçek yerini gövde söylemiyor.

    UV uzayı söylüyor. `mpfb_eyelids` maskesi göz kapağı adalarını tam
    olarak işaretliyor; iki ada, iki göz. İrisi adanın merkezine
    **daire** olarak çizmek, adanın atlastaki dönüklüğünden bağımsızdır
    — bir daire döndürülse de dairedir. Oval çizseydim, ada 30 derece
    dönük olduğu gün göz yamuk bakardı ve sebebi görünmezdi.

    Oranlar gözden: göz açıklığının kısa ekseni ~11 mm, iris çapı
    ~11,7 mm — yani iris açıklığı neredeyse doldurur (`IRIS_ORAN`).
    Bebek gündüz ~4 mm, irisin ~%34'ü.
    """
    # ADANIN KENDI BICIMI KULLANILIR, DAIRE DEGIL.
    #
    # Ilk yazimda irisi adanin merkezine DAIRE olarak ciziyordum ve
    # renderda hicbir sey gorunmedi. Tani turu sebebini gosterdi
    # (maskeler cig renkle boyandi): kapak adasi UV'de 38x90 pikselik
    # dik bir oval, ama modelde bunun yalnizca INCE BIR SERIDI goruluyor
    # — gerisi kapak kivriminin icinde, kapali. Yani dairenin %90'i
    # gorunmeyen yuzeye dusuyordu.
    #
    # Ada zaten gozun kendi bicimi. Ak olarak adanin TAMAMI boyanir;
    # iris, adanin uzun ekseni boyunca ortadaki %38'lik bant olur.
    # Boylece gorunen serit ne olursa olsun uzerinde ak ve iris bulunur.
    adalar = _adalar(kapak)
    yy, xx = np.mgrid[0:res, 0:res]
    ada_maske = kapak > 0.16
    for (cy, cx, r) in adalar:
        if r < 2.0:
            continue
        yakin = ada_maske & (np.abs(yy - cy) < r * 6.0)             & (np.abs(xx - cx) < r * 6.0)
        if not yakin.any():
            continue
        # Adanin uzun ekseni: hangi yonde daha genis.
        ys, xs = np.nonzero(yakin)
        dikey = (ys.max() - ys.min()) >= (xs.max() - xs.min())
        uzun = (yy - cy) if dikey else (xx - cx)
        yari = ((ys.max() - ys.min()) if dikey
                else (xs.max() - xs.min())) * 0.5
        if yari < 1.0:
            continue

        # AK: adanin tamami, tenden acik ama beyaz degil (damarli).
        deger[yakin] = 1.18
        kan[yakin] = np.maximum(kan[yakin], 0.10)

        # IRIS: uzun eksende ortadaki bant. Oran gozden — iris capi
        # 11,7 mm, goz aciklginin uzun ekseni ~30 mm: %39.
        iris = yakin & (np.abs(uzun) < yari * 0.39)
        deger[iris] = 0.30
        kan[iris] = 0.55
        bebek = yakin & (np.abs(uzun) < yari * 0.14)
        deger[bebek] = 0.06
        kan[bebek] = 0.0

        # UST KAPAK GOLGESI: adanin ust ucte biri her zaman golgededir
        # (kapak ustten sarkar) ve bu, gozun oyulmus gorunmesini saglar.
        ust = yakin & (uzun < -yari * 0.45)
        deger[ust] *= 0.55
    return len(adalar)


def build(res, dizin, tohum=16330):
    rng = np.random.default_rng(tohum)

    yuz = _maske(dizin, "mpfb_face.jpg", res)
    dudak = _maske(dizin, "mpfb_lips.jpg", res)
    kapak = _maske(dizin, "mpfb_eyelids.jpg", res)
    kulak = _maske(dizin, "mpfb_ears.jpg", res)

    gz = _gozenek(res, rng)

    # --- DEGER (parlaklik) -------------------------------------------
    #
    # Taban 1,0: doku bir CARPANDIR, ten rengi paletten gelir. Bolgeler
    # bu tabandan asagi iner.
    deger = np.ones((res, res), dtype=np.float64)
    deger -= dudak * 0.28          # dudak tenden koyu
    deger -= kapak * 0.18          # goz kapagi golgede
    deger -= yuz * 0.03            # yuz govdeden bir tik koyu (gunes)
    # Gozenek: yalnizca YUZDE belirgin, govdede cok hafif.
    deger -= (gz - 0.5) * (0.055 + 0.075 * yuz)

    # --- RENK KAYMASI ------------------------------------------------
    #
    # Dudak ve kulak KANLANIR: kirmizi kanal korunur, yesil ve mavi
    # cekilir. Tek bir "koyulastirma" ile yapilamaz — koyu dudak gri
    # dudaktir, kirmizi degil.
    kan = np.clip(dudak * 1.0 + kulak * 0.55 + kapak * 0.25, 0.0, 1.0)

    # --- GOZ: kan maskesi kuruldu, simdi cizilebilir --------------
    goz_sayisi = _goz_ciz(deger, kan, kapak, res)
    r = deger * (1.0 + kan * 0.10)
    g = deger * (1.0 - kan * 0.16)
    b = deger * (1.0 - kan * 0.20)
    lin = np.clip(np.stack([r, g, b], axis=-1), 0.02, 1.0)
    bc = (pl.linear_to_srgb(lin) * 255.0).round().astype(np.uint8)

    # --- NORMAL ------------------------------------------------------
    #
    # Yukseklik: gozenek cukurlari + dudak cizgileri. Genlik metre
    # cinsinden ve gercek: bir gozenek ~0,08 mm derinliginde.
    h = (gz - 0.5) * (0.35 + 0.65 * yuz) + dudak * 0.25
    h = h - h.min()
    if h.max() > 1e-9:
        h /= h.max()
    # Govde UV atlasi ~2 m'lik bir insani kapsar; 0,00008 m gozenek
    # derinligi bu olcekte cok zayif kalirdi, cunku atlas alani deri
    # alanindan buyuk. Olcut: yuz atlasin ~%12'sini kaplar ve yuz
    # ~0,20 m'dir, yani atlas ~1,7 m'ye karsilik gelir.
    nrm = (pl.normal_from_height(h, strength=0.00010 * res / 1.7)
           * 255.0).round().astype(np.uint8)

    # --- PURZULUK ----------------------------------------------------
    #
    # Ten her yerde ayni purzulukte degil: alin ve burun yaglidir
    # (parlar), dudak nemlidir, govde matt. Tek bir deger verilseydi yuz
    # ya mum ya toz gibi cikardi.
    puru = 0.62 * np.ones((res, res))
    puru -= yuz * 0.10
    puru -= dudak * 0.22
    puru += (gz - 0.5) * 0.06
    puru = np.clip(puru, 0.20, 0.85)

    # AO: cukurlar (gozenek, dudak cizgisi) ve goz kapagi.
    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    cukur = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    ao = np.clip(1.0 - 0.30 * cukur ** 0.7 - kapak * 0.25, 0.40, 1.0)

    metal = np.zeros_like(h)
    arm = (np.stack([ao, puru, metal], axis=-1) * 255.0)
    arm = arm.round().astype(np.uint8)
    return bc, nrm, arm, puru, ao, (yuz, dudak, kapak, kulak), goz_sayisi


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    # TANI KIPI: maskeleri cig renklerle boyar. Hizanin dogru olup
    # olmadigini gozle degil KESIN olarak gostermek icin — ince bir
    # doku "gorunmuyor" oldugunda sebebi hiza mi yoksa incelik mi,
    # bakarak ayirt edilemiyor.
    ap.add_argument("--tani", action="store_true",
                    help="Maskeleri cig renge boyar (hiza sinamasi)")
    a = ap.parse_args()

    dizin = _maske_dizini()
    bc, nrm, arm, puru, ao, maskeler, goz_sayisi = build(a.res, dizin)
    yuz, dudak, kapak, kulak = maskeler
    if a.tani:
        t = np.asarray(bc, dtype=np.float64)
        t[yuz > 0.3] = (60, 200, 60)        # yuz  = yesil
        t[kulak > 0.3] = (60, 120, 255)     # kulak= mavi
        t[dudak > 0.3] = (255, 40, 40)      # dudak= kirmizi
        t[kapak > 0.3] = (255, 240, 40)     # kapak= sari
        bc = t.round().astype(np.uint8)

    d = pl.write_texture_set(
        "deri_insan", a.res, 1.7, bc, nrm, arm,
        meta_extra=dict(
            generated_by="tools/textures/gen_deri_texture.py",
            role="skin",
            use="Insan teni (M_Skin) — MakeHuman UV yerlesiminde",
            why="M_Skin dokusuzdu (kind=untextured) ve dokusuz ten HDRP'de "
                "mum gibi okur. Dudak, goz kapagi, kulak ve gozenek "
                "olmadan bir yuz yuz olmuyor.",
            inputs="MPFB2 cekirdek bolge maskeleri (mpfb_face/lips/"
                   "eyelids/ears) — CC0, refs/LICENSES.md MPFB 2.0.17 "
                   "satirinin kapsaminda. Maskeler NEREDE NE oldugunu "
                   "soyler; ten rengi, oranlar ve gozenek bizim.",
            uv_space="MakeHuman taban mesh varsayilan UV — govde ayni "
                     "mesh'ten turedigi icin hizali.",
            base_color_note="BC bilerek NOTR carpan (taban 1,0). Ten rengi "
                            "paletten gelir ve kisiden kisiye _BaseColor "
                            "ile carpilir.",
        ),
        rough=puru, ao=ao, out_root=a.out)

    print(f"[HZ] deri_insan: {a.res}x{a.res} -> {d}")
    print(f"[HZ]   cizilen goz adasi: {goz_sayisi}")
    print(f"[HZ]   maske kapsami: yuz %{yuz.mean()*100:.1f}, "
          f"dudak %{dudak.mean()*100:.2f}, kapak %{kapak.mean()*100:.2f}, "
          f"kulak %{kulak.mean()*100:.2f}")
    print(f"[HZ]   purzuluk {puru.min():.2f}-{puru.max():.2f}, "
          f"AO {ao.min():.2f}-{ao.max():.2f}")


if __name__ == "__main__":
    sys.exit(main())
