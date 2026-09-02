"""
Hezarfen: 1632 — Kumaş dokuları üreticisi (prosedürel, KENDİ TELİFİMİZ).

## Neden bu doku eksikti

Ölçüldü: karakter hattındaki **on iki kumaş malzemesinin hepsi**
`kind=untextured` idi — düz albedo, normal yok, pürüzlülük yok. Sebep
zincirin başındaydı: `gen_hezarfen.py` giysi parçalarını bmesh'ten
kuruyor ve hiçbir yerde UV üretmiyordu, dolayısıyla takılacak bir doku
da olamazdı. `ottoman_kit`'in kendi yorumu bunu dürüstçe yazmış:
*"kumaş rolleri bilerek TEXTURE_ROLES'a girmiyor: bu rollerin dokusu
yok."*

Dokusuz bir albedo HDRP'de her zaman **plastik** okur. Karakterin
"gerçekçi değil" görünmesinin tek büyük sebebi model değil yüzeydi.

## Neden indirmek değil üretmek

Poly Haven'da CC0 kumaş var ve indirilebilirdi. Üretmek iki şey
kazandırıyor ve ikisi de ölçülebilir:

1. **Albedo nötr kalır.** Hazır bir fotoğraf dokusu kendi rengini
   getirir; `NPCYonetici` ise kişiden kişiye tonu `_BaseColor` ile
   ÇARPARAK değiştiriyor. Renkli bir albedo o çarpımı boğar ve şehirdeki
   herkes yine aynı renge düşer — yani çeşitlilik için üretilen yedi
   gövde bir doku yüzünden geri alınırdı.
2. **Dokuma dönemin dokumasıdır.** Elde eğrilmiş iplik kalınlığı düzensiz
   (*slub*); makine ipliği düzgündür. Fark siluette değil yakın çekimde
   görünür ve tam da "1632" duygusunu veren şeydir. Bir parametre olarak
   yazılabiliyorsa uydurma değildir.

Lisans sorusu böylece doğuşta yok: çıktı bizim eserimizdir.

## Dört kumaş, dört dokuma

| id | dokuma | nerede | gerekçe |
|---|---|---|---|
| `kumas_keten` | bez ayağı (1/1) | gömlek, sarık, yaşmak | keten iç çamaşırı ve baş örtüsünün kumaşı; ağartılmış, mat |
| `kumas_cuha` | dolgunlaştırılmış yün | entari, ferace, şalvar | çuha *dinklenir* (keçeleştirilir): dokuma bulanır, yüzey havlanır |
| `kumas_ipek` | atlas (4/1 atlama) | kuşak | uzun atlamalar ışığı tek yönde toplar — kuşağın parlaması bundan |
| `kumas_kece` | dokuma yok | takke, kavuk | keçe dokunmaz, dövülür: lif yönsüz ve mat |

## Yükseklikten normale, normalden AO'ya

Dokumanın kendisi bir yükseklik alanıdır: üstte kalan iplik kabarır,
altta kalan çukurda kalır. Normal o alandan türer; AO ise alanın
**laplasyanından** — çukurlar koyulaşır. Aynı yöntem kurşun örtüde
kullanıldı ve orada ölçülmüştü; burada tekrar edilmesinin sebebi
tutarlılık: iki yüzey aynı ışık altında aynı dili konuşsun.

## Döşenebilirlik

Bütün gürültü **periyodik**: kafes rastgele değerleri sarmalı
(`_periyodik_gurultu`) ve dokuma fonksiyonları tam sayı iplik sayısıyla
tanımlı. Döşeme dikişi olsaydı 40.000 sakinin üstünde ızgara gibi
görünürdü.

Kullanım:
  python tools/textures/gen_kumas_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import proclib as pl                                        # noqa: E402


# --------------------------------------------------------------- gürültü

def _periyodik_gurultu(res, hucre, rng, oktav=3):
    """
    Kenarları **sarmalı** değer gürültüsü — döşenebilir olmak zorunda.

    Basit `rng.random((res, res))` döşenir ama yapısızdır; Perlin/Worley
    ise sarmalı yazılmazsa dikiş bırakır. Burada kafes rastgele değerlerle
    doldurulur ve **kafesin kendisi** modülo ile sarılır: bilineer
    örnekleme sağdan çıkanı soldan alır, yani dikiş matematiksel olarak
    imkânsızdır.
    """
    toplam = np.zeros((res, res), dtype=np.float64)
    agirlik = 0.0
    for o in range(oktav):
        n = max(2, int(hucre * (2 ** o)))
        kafes = rng.random((n, n))
        # Ornekleme koordinatlari
        t = np.linspace(0.0, n, res, endpoint=False)
        i0 = np.floor(t).astype(int) % n
        i1 = (i0 + 1) % n
        f = (t - np.floor(t))[None, :]
        fu = f
        fv = f.T
        # Yumusak gecis (smoothstep) — dogrusal ara deger kafes izini birakir
        su = fu * fu * (3.0 - 2.0 * fu)
        sv = fv * fv * (3.0 - 2.0 * fv)
        a = kafes[np.ix_(i0, i0)]
        b = kafes[np.ix_(i0, i1)]
        c = kafes[np.ix_(i1, i0)]
        d = kafes[np.ix_(i1, i1)]
        ust = a + (b - a) * su
        alt = c + (d - c) * su
        katman = ust + (alt - ust) * sv
        w = 0.5 ** o
        toplam += katman * w
        agirlik += w
    return toplam / agirlik


# --------------------------------------------------------------- dokuma

def _dokuma(res, iplik, rng, atlama=1, slub=0.35, kabarik=1.0):
    """
    Dokuma yükseklik alanı. `(yukseklik, cozgu_ustte)` döner.

    `atlama`: bir çözgü ipliğinin kaç atkı üstünden geçtiği.
    1 = bez ayağı (1/1), 4 = atlas. Dimi ve atlas arasındaki fark
    **kaydırma**dır: her sıra bir öncekine göre bir iplik ötelenir, ve
    atlamalar bu yüzden köşegen bir çizgi kurar.

    `slub`: iplik kalınlığındaki düzensizlik. Elde eğrilmiş ipliğin
    imzası budur ve sıfır verilirse kumaş fabrika işi görünür.
    """
    u = (np.arange(res) + 0.5) / res * iplik          # cozgu ekseni
    v = (np.arange(res) + 0.5) / res * iplik          # atki ekseni
    U, V = np.meshgrid(u, v, indexing="xy")

    ic = np.floor(U).astype(int) % iplik              # cozgu indisi
    ia = np.floor(V).astype(int) % iplik              # atki indisi
    fu = U - np.floor(U)
    fv = V - np.floor(V)

    # IPLIK KALINLIGI IPLIGE OZGUDUR, TEXEL'E DEGIL.
    #
    # Kalinligi her texel'de rastgeleleseydik kumas degil kum olurdu.
    # Duzensizlik ipligin KENDISINDE tasinir: bir iplik boyunca ayni
    # kalir, komsu iplikte degisir. El egirmesinin gorunusu budur.
    kal_c = 1.0 + (rng.random(iplik) - 0.5) * 2.0 * slub
    kal_a = 1.0 + (rng.random(iplik) - 0.5) * 2.0 * slub

    # Iplik kesiti: yarim sinus — yuvarlak bir tel.
    kes_c = np.sin(np.pi * np.clip(fu, 0.0, 1.0)) * kal_c[ic]
    kes_a = np.sin(np.pi * np.clip(fv, 0.0, 1.0)) * kal_a[ia]

    # ATLAMA DESENI.
    #
    # `(ia * kaydirma + ic) % (atlama + 1) == 0` bir kosegen kurar; atlama
    # 1'de bu satranc tahtasi (bez ayagi), 4'te atlas olur.
    period = atlama + 1
    kaydirma = 1 if atlama <= 1 else 2      # atlasta 2, dimide 1
    cozgu_ustte = ((ia * kaydirma + ic) % period) != 0

    h = np.where(cozgu_ustte,
                 kes_c * 1.0 + kes_a * 0.30,
                 kes_a * 1.0 + kes_c * 0.30)
    return h * kabarik, cozgu_ustte


# --------------------------------------------------------------- kumaşlar

def _keten(res, rng):
    """Bez ayağı keten: sık, düzensiz iplikli, mat."""
    h, ust = _dokuma(res, iplik=96, rng=rng, atlama=1, slub=0.30)
    tuy = _periyodik_gurultu(res, 24, rng, oktav=3)
    h = h + (tuy - 0.5) * 0.10
    deger = 0.74 + (h - h.mean()) * 0.16 + (tuy - 0.5) * 0.05
    puru = 0.86 + (tuy - 0.5) * 0.06
    return h, deger, puru, ust


def _cuha(res, rng):
    """
    Dinklenmiş yün çuha: dokuma bulanık, yüzey havlı.

    Çuha dokunduktan sonra **dinklenir** — sıcak suda dövülerek
    keçeleştirilir. Bu yüzden dokuma görünür ama okunmaz: yüzeyi
    belirleyen şey ipliğin kendisi değil üstündeki hav. Dokuma alanını
    bulanıklaştırıp üstüne lif gürültüsü koymak bunun karşılığı.
    """
    h, ust = _dokuma(res, iplik=58, rng=rng, atlama=2, slub=0.40)
    # Bulanikligi ucuz ve DOSENEBILIR yoldan yap: kaydirip ortala.
    bul = np.zeros_like(h)
    k = max(1, res // 256)
    for dy in (-k, 0, k):
        for dx in (-k, 0, k):
            bul += np.roll(np.roll(h, dy, axis=0), dx, axis=1)
    h = bul / 9.0
    hav = _periyodik_gurultu(res, 64, rng, oktav=4)
    h = h * 0.55 + (hav - 0.5) * 0.55
    deger = 0.70 + (h - h.mean()) * 0.10 + (hav - 0.5) * 0.09
    puru = 0.93 + (hav - 0.5) * 0.05
    return h, deger, puru, ust


def _ipek(res, rng):
    """Atlas ipek: uzun atlamalar, düşük pürüzlülük, köşegen parlama."""
    h, ust = _dokuma(res, iplik=128, rng=rng, atlama=4, slub=0.12,
                     kabarik=0.6)
    ince = _periyodik_gurultu(res, 96, rng, oktav=2)
    deger = 0.78 + (h - h.mean()) * 0.10 + (ince - 0.5) * 0.03
    # PARLAKLIK ATLAMANIN USTUNDE.
    #
    # Asagidaki maske atlamayi tasiyan yuzeyi ayirir: cozgu
    # ustteyken ipek isigi toplar, atki gorundugu yerde
    # matlasir. Tek bir purzuluk degeri verseydik ipek saten degil
    # boyanmis keten olurdu.
    puru = np.where(ust, 0.26, 0.42) + (ince - 0.5) * 0.05
    return h, deger, puru, ust


def _kece(res, rng):
    """Keçe: dokuma yok, dövülmüş lif."""
    kaba = _periyodik_gurultu(res, 20, rng, oktav=3)
    ince = _periyodik_gurultu(res, 80, rng, oktav=4)
    h = (kaba - 0.5) * 0.7 + (ince - 0.5) * 0.5
    deger = 0.68 + (kaba - 0.5) * 0.10 + (ince - 0.5) * 0.07
    puru = 0.95 + (ince - 0.5) * 0.04
    return h, deger, puru, None


SPECS = [
    dict(id="kumas_keten", kabartma=0.0004, size=0.12, fn=_keten, tohum=16321,
         use="Keten gomlek, sarik ve yasmak (M_Cloth_Gomlek/Sarik/Yasmak)",
         dokuma="bez ayagi 1/1, 96 iplik/12 cm = 8 iplik/cm (el dokumasi)"),
    dict(id="kumas_cuha", kabartma=0.0009, size=0.15, fn=_cuha, tohum=16322,
         use="Entari, ferace ve salvar (M_Cloth_Entari/Ferace/Salvar)",
         dokuma="dimi 2/1, dinklenmis — dokuma bulanik, yuzey havli"),
    dict(id="kumas_ipek", kabartma=0.00018, size=0.10, fn=_ipek, tohum=16323,
         use="Kusak (M_Cloth_Kusak)",
         dokuma="atlas 4/1, 128 iplik/10 cm; parlaklik atlamalarda"),
    dict(id="kumas_kece", kabartma=0.0011, size=0.18, fn=_kece, tohum=16324,
         use="Takke ve kavuk (M_Cloth_Takke/Kavuk)",
         dokuma="dokuma yok — dovulmus lif"),
]


def build(spec, res):
    rng = np.random.default_rng(spec["tohum"])
    h, deger, puru, _ = spec["fn"](res, rng)

    h = h - h.min()
    if h.max() > 1e-9:
        h = h / h.max()

    # KABARTMA GENLIGI METRE CINSINDEN VERILIR, GOZLE DEGIL.
    #
    # `normal_from_height`'in kendi sozlesmesi bunu yaziyor:
    # `strength = genlik x cozunurluk / doku_boyu`. Gozle ayarlanan bir
    # deger ya duz ya kabarcikli bir yuzey verir ve ikisinin de sebebi
    # render'a bakinca anlasilmaz. Genlikler dokumanin kendisinden:
    # keten ipligi ~0,4 mm kabarir, dinklenmis yunun havi ~0,9 mm,
    # atlasin atlamasi ~0,18 mm (neredeyse duz — parlakligi bicimden
    # degil purzuluk farkindan alir), kece ~1,1 mm.
    nrm = (pl.normal_from_height(
        h, strength=spec["kabartma"] * res / spec["size"]) * 255.0
        ).round().astype(np.uint8)

    deger = np.clip(deger, 0.05, 0.98)
    # BC NOTR: renk paletten gelir, dokudan degil (dosya basligi).
    bc_lin = np.repeat(deger[..., None], 3, axis=-1)
    bc = (pl.linear_to_srgb(bc_lin) * 255.0).round().astype(np.uint8)

    puru = np.clip(puru, 0.05, 1.0)

    # AO YUKSEKLIGIN LAPLASYANINDAN: cukur koyu, tepe acik.
    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    cukur = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    ao = np.clip(1.0 - 0.42 * cukur ** 0.7, 0.42, 1.0)

    metal = np.zeros_like(h)
    arm = (np.stack([ao, puru, metal], axis=-1) * 255.0)
    arm = arm.round().astype(np.uint8)
    return bc, nrm, arm, puru, ao


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--res", type=int, default=1024)
    ap.add_argument("--out", default=pl.OUT_ROOT)
    a = ap.parse_args()

    for spec in SPECS:
        bc, nrm, arm, puru, ao = build(spec, a.res)
        d = pl.write_texture_set(
            spec["id"], a.res, spec["size"], bc, nrm, arm,
            meta_extra=dict(
                generated_by="tools/textures/gen_kumas_texture.py",
                role="kumas",
                use=spec["use"],
                weave=spec["dokuma"],
                why="Karakterin butun kumas malzemeleri dokusuzdu (olculdu: "
                    "12 malzeme kind=untextured) ve dokusuz albedo HDRP'de "
                    "plastik okur. Indirmek yerine uretildi cunku albedonun "
                    "NOTR kalmasi sart: kisiden kisiye ton NPCYonetici'de "
                    "_BaseColor ile CARPILIYOR ve renkli bir albedo o "
                    "carpimi bogardi.",
                base_color_note="BC bilerek notr (gri tonlamali dokuma "
                                "degeri). Renk paletten gelir; malzeme "
                                "_BaseColor'i paletin rengiyle carpar.",
            ),
            rough=puru, ao=ao, out_root=a.out)
        print(f"[HZ] {spec['id']}: {a.res}x{a.res}, {spec['size']} m -> {d}")
        print(f"[HZ]   purzuluk {puru.min():.2f}-{puru.max():.2f}, "
              f"AO {ao.min():.2f}-{ao.max():.2f}")

    print(f"[HZ] {len(SPECS)} prosedurel kumas dokusu uretildi")


if __name__ == "__main__":
    sys.exit(main())
