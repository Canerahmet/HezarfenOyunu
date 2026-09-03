"""
Hezarfen: 1632 — Sakal ve saç **yüzeyi** dokusu (prosedürel, kendi telifimiz).

## Neden gerekti

Sakal bir kart dizisi değil, çeneye oturan bir **kabuk**
(`gen_hezarfen.py`, "SAKAL: KART DEGIL KABUK"). Kabuğun malzemesi
`M_Beard` ve ölçüldü: taban rengi haritası **yok**
(`_BaseColorMap: {fileID: 0}`). Bu deponun kumaş için zaten yazdığı
ders burada da geçerli — *dokusuz albedo HDRP'de plastik okur*. Yakın
plan karesinde sakal, çeneye geçirilmiş kahverengi bir **maske** gibi
duruyor: tek parça, tek renk, hiç kırılma yok.

Kart atlası (`gen_hair_texture.py`) bu işi göremez: o bir **alfa
atlasıdır**, tutamların arası saydamdır ve döşenmez. Kabuğun istediği
şey döşenebilir bir **yüzey**: kısa, sık, yön tutan teller.

## Ne üretiyor

İki set:

* `sakal` — kısa ve sık; çene sakalı ve bıyık. Teller aşağı-dışa
  yönelir, aralarında düzensiz kümelenme var (sakal düz taranmaz).
* `sac_yuzey` — biraz daha uzun ve daha düzenli; sarığın/takkenin
  altından görünen saç ve ense kabuğu.

## Albedo neden nötr

Kumaşta olduğu gibi: renk **paletten** gelir ve malzeme onu
`_BaseColor` ile çarpar (`beard` kestane, `beard_ak` ak). Renkli bir
albedo, yaşlının ak sakalını kestaneye çevirirdi.

Kullanım:
  python tools/textures/gen_sakal_texture.py [--res 1024]
"""

import argparse
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import proclib as pl   # noqa: E402


#: Setler. `size` dokunun kapladığı gerçek dünya ölçüsüdür (m) ve
#: doğrudan tel sıklığını belirler: 6 cm'lik bir karede 220 tel,
#: santimetreye ~37 tel eder — insan sakalının gerçek yoğunluğu
#: santimetrekarede 30-50 arasıdır.
SPECS = [
    dict(
        id="sakal",
        size=0.06,
        tel=1400,          # kare basina tel
        uzunluk=0.055,     # tel boyu (doku genisliginin orani)
        kalinlik=1.35,     # px
        yon=(0.0, 1.0),    # asagi
        yon_dagilim=0.55,  # yonun ne kadar sactigi (rad)
        kume=9.0,          # kumelenme frekansi
        kabartma=0.0011,   # m — telin yuzeyden yuksekligi
        use="sakal ve biyik kabugu",
    ),
    dict(
        id="sac_yuzey",
        size=0.09,
        tel=1700,
        uzunluk=0.085,
        kalinlik=1.15,
        yon=(0.12, 1.0),
        yon_dagilim=0.28,   # sac taranir: sakaldan daha az sacilma
        kume=6.0,
        kabartma=0.0016,
        use="sarik/takke altindan gorunen sac ve ense kabugu",
    ),
]

# SAC YUZEYI: BIR TUR YAZILDI, BIR TUR SILINDI, SONRA GEREKTI.
#
# Once uretildi, sonra "kullanacak malzeme yok" diye silindi — `M_Hair`
# bir KART malzemesiydi. Ardindan oglanin yakin plani cekildi:
# takkenin altindan cikan kartlar kulaklarin iki yaninda TEL gibi ve
# boynun cevresinde bir FIRFIR gibi okunuyordu. Ayni kusur bu depoda
# besinci kez ve sakalda cozumu zaten bulunmustu: kart degil KABUK.
# Kabuk dosenebilir bir yuzey ister; o yuzey bu.


def _teller(res, spec, rng):
    """Yönlü, kümelenmiş kısa teller — **döşenebilir**.

    Her tel bir doğru parçasıdır ve kenardan taşan kısmı karşı
    kenardan girer (modüler koordinat). Döşenebilirlik bir süs değil
    şart: sakal kabuğu dünya izdüşümüyle UV alıyor ve tekrarlayan bir
    dikiş çenenin ortasından geçerdi.
    """
    h = np.zeros((res, res), dtype=np.float32)

    # KUMELENME: sakal duz taranmaz, tutam tutam toplanir. Alcak
    # frekansli bir alan tellerin YOGUNLUGUNU ve yonunu birlikte
    # bukuyor; ikisini ayri gurultuyle surmek tutami dagitirdi.
    kume = pl.blob_field(res, int(spec["kume"] ** 2), res / spec["kume"] * 0.6,
                         1.0, rng)
    kume = (kume - kume.min()) / max(1e-6, kume.max() - kume.min())

    uz = spec["uzunluk"] * res
    yon0 = np.arctan2(spec["yon"][1], spec["yon"][0])
    kal = spec["kalinlik"]

    for _ in range(int(spec["tel"])):
        x0 = rng.random() * res
        y0 = rng.random() * res
        k = kume[int(y0) % res, int(x0) % res]

        # Yon: taban yon + kume bukmesi + tel basina gurultu.
        aci = (yon0
               + (k - 0.5) * spec["yon_dagilim"] * 2.0
               + rng.normal(0.0, spec["yon_dagilim"] * 0.5))
        boy = uz * (0.55 + 0.9 * rng.random())
        # Kok koyu, uc acik: telin kendi golgesi. Yukseklik profili
        # kokte 1, ucta 0,25 — uc incelir.
        adim = max(2, int(boy))
        for i in range(adim):
            t = i / float(adim - 1)
            x = (x0 + np.cos(aci) * boy * t) % res
            y = (y0 + np.sin(aci) * boy * t) % res
            deger = (1.0 - 0.75 * t) * (0.45 + 0.55 * k)
            xi, yi = int(x), int(y)
            r = int(np.ceil(kal))
            for dy in range(-r, r + 1):
                for dx in range(-r, r + 1):
                    d = np.hypot(dx, dy)
                    if d > kal:
                        continue
                    w = (1.0 - d / kal) ** 1.5
                    yy = (yi + dy) % res
                    xx = (xi + dx) % res
                    if h[yy, xx] < deger * w:
                        h[yy, xx] = deger * w
    return h


def build(spec, res, tohum=1632):
    rng = np.random.default_rng(tohum + hash(spec["id"]) % 9973)

    h = _teller(res, spec, rng)
    # Ince tane: derinin kendisi de tamamen duz degil ve teller
    # arasindan gorunur.
    h = np.clip(h + pl.fine_grain(res, rng) * 0.06, 0.0, 1.0)

    # ALBEDO NOTR VE **ORTALAMASI KUMASLA AYNI AILEDE**.
    #
    # Ilk denemede 0,30 + 0,62h yazdim: ortalamasi 0,45 ve karsitligi
    # cok yuksek. Sonuc renderda goruldu — palet rengiyle (0,105 /
    # 0,072 / 0,052) carpilinca sakal SIMSIYAH cikti ve teller siyah
    # yarik gibi okundu. Doku bir renk degil bir YUZEY tasir; koyulugu
    # albedodan degil AO ve normalden gelmeli.
    #
    # Kumas dokularinin sozlesmesi 0,68-0,78 taban + dar salinim
    # (`gen_kumas_texture.py`). Sac biraz daha karsit cunku teller
    # birbirini golgeler, ama taban ayni ailede kalir.
    v = np.clip(0.70 + (h - float(h.mean())) * 0.34, 0.30, 0.95)
    bc_lin = np.stack([v, v, v], axis=-1)
    bc = (pl.linear_to_srgb(np.clip(bc_lin, 0.0, 1.0)) * 255.0)
    bc = bc.round().astype(np.uint8)

    nrm = (pl.normal_from_height(
        h, spec["kabartma"] * res / spec["size"]) * 255.0
        ).round().astype(np.uint8)

    # PURUZLULUK: sac teli parlaktir ama sakal degil. Tel ustu daha
    # duzgun (0,42), aralari mat (0,86) — isik tellerin sirtindan
    # kirilir ve kutle "sac" okunur.
    puru = np.clip(0.86 - 0.44 * h, 0.38, 0.92)

    lap = (np.roll(h, 1, 0) + np.roll(h, -1, 0)
           + np.roll(h, 1, 1) + np.roll(h, -1, 1) - 4.0 * h)
    m = float(np.abs(lap).max())
    cukur = np.clip(lap / m, 0.0, 1.0) if m > 1e-9 else np.zeros_like(h)
    # Sakal kendi icinde KOYU: teller arasi bosluk isik almaz. AO
    # kumasinkinden derin (0,30 taban) ve bu bilincli — sig bir AO
    # sakali yine tek parca bir kutle yapardi.
    ao = np.clip(1.0 - 0.58 * cukur ** 0.6, 0.30, 1.0)

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
                generated_by="tools/textures/gen_sakal_texture.py",
                role="sakal",
                use=spec["use"],
                why="M_Beard'in taban rengi haritasi YOKTU (olculdu: "
                    "_BaseColorMap fileID 0) ve dokusuz albedo HDRP'de "
                    "plastik okur — yakin planda sakal ceneye gecirilmis "
                    "kahverengi bir maske gibi duruyordu. Kart atlasi bu "
                    "isi goremez: sakal kart degil KABUK, ve kabuk "
                    "dosenebilir bir yuzey ister.",
                base_color_note="BC bilerek notr. Renk paletten gelir "
                                "(beard kestane, beard_ak ak) ve malzeme "
                                "onu _BaseColor ile carpar; renkli bir "
                                "albedo yaslinin ak sakalini kestaneye "
                                "cevirirdi.",
            ),
            rough=puru, ao=ao, out_root=a.out)
        print(f"[HZ] {spec['id']}: {a.res}x{a.res}, {spec['size']} m -> {d}")
        print(f"[HZ]   purzuluk {puru.min():.2f}-{puru.max():.2f}, "
              f"AO {ao.min():.2f}-{ao.max():.2f}")

    print(f"[HZ] {len(SPECS)} prosedurel sac/sakal yuzeyi uretildi")


if __name__ == "__main__":
    sys.exit(main())
