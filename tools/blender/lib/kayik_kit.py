"""
**Kayık ve pereme** — 1632 İstanbul'unun ulaşımı.

## Neden bunlar, neden bu kadarı

RESEARCH.md §"Ulaşım" tek cümlede hem tipi hem işlevi veriyor:

> *"kayık ve pereme (deniz taksisi) ana ulaşım; iskeleler tarifeli; at/katır
> karada; tekerlekli araba nadir. Boğaz ve Haliç geçişleri kayıkla."*

Bu, iki şeyi birden söylüyor ve ikincisi kolay atlanır: **Haliç'te köprü
yoktur.** Yani kayık bir süs değil, şehrin ulaşım sisteminin kendisidir. Boş
bir Haliç, boş bir cadde kadar yanlıştır.

Kaynak **iki tip** adlandırıyor — kayık ve pereme — ve kit tam o kadarını
üretir. Üçüncü bir tip eklemek (mavna, çektiri, at kayığı) kaynağın
söylemediğini söylemek olurdu; Osmanlı deniz taşıtı literatüründe o adlar
vardır ama 1632 İstanbul'unun gündelik su trafiği için bu belgede yoktur.

## Doğruluk: D3

Kaynak **işlevi** veriyor, **ölçüyü** vermiyor: peremenin boyu, kürek sayısı,
bordası kayıtlı değil. Bu yüzden kütle tipolojiktir (**D3, taslak**) ve
kitte tek sayısal iddia oranlardır — boy/en oranı bir kürek teknesininki
gibidir (~4:1 kayık, ~5:1 pereme), çünkü daha tombul bir tekne kürekle
gitmez ve daha ince olan devrilir. Bu, ölçü uydurmak değil; kürekli teknenin
işlemesi için sağlaması gereken kısıttır.

## Biçim nereden geliyor

Tekne kesitlerden **loft** edilir: boy boyunca N istasyon, her istasyon bir
U kesiti. Genişlik ve derinlik orta gövdede en yüksek, baş ve kıçta sıfıra
yakındır. Küpeşte (üst kenar) baş ve kıçta **yükselir** — bir teknenin
uzaktan tanınmasını sağlayan tek çizgi budur; düz küpeşteli bir gövde
sandık gibi okunur.
"""

import math

import bmesh
import hz_blender as hz
import ottoman_kit as kit


#: Kayık: küçük kürek teknesi. Boy/en ~4:1 (kürekli tekne kısıtı).
KAYIK_LEN, KAYIK_BEAM, KAYIK_DEPTH = 5.2, 1.30, 0.62

#: Pereme: deniz taksisi — daha uzun, daha çok kürek, yolcu taşır.
PEREME_LEN, PEREME_BEAM, PEREME_DEPTH = 8.6, 1.72, 0.78

#: Kesit sayısı. Tekne küçüktür ve sudan görülür; on istasyon
#: gövdeyi yumuşak gösterir, daha fazlası üçgeni boşa harcar.
STATIONS = 11

#: **Su hattı gövdenin neresinden geçer** — omurgadan yukarı, derinliğin oranı.
#:
#: İlk yazımda su hattı küpeşteyle aynı yerdeydi (z=0 hem omurganın üstü hem
#: küpeşte) ve ölçüm bunu ele verdi: su çekimi 0,62 m çıktı, yani gövde
#: derinliğinin TAMAMI. Orta bordası tam su hizasında olan bir tekne yüzmez,
#: batar. Yüklü bir kürek teknesi derinliğinin kabaca üçte birini çeker;
#: gerisi bordadır ve dalgayı o tutar.
DRAFT_RATIO = 0.36


class KayikParams(object):
    """
    Tek bir tekne. `oars` kürek ÇİFTİ sayısıdır (0 = bağlı/boş tekne).
    """

    def __init__(self, kind="kayik", oars=1, thwarts=2, palette="default"):
        self.kind = kind
        self.oars = oars
        self.thwarts = thwarts
        self.palette = palette

    @property
    def length(self):
        return KAYIK_LEN if self.kind == "kayik" else PEREME_LEN

    @property
    def beam(self):
        return KAYIK_BEAM if self.kind == "kayik" else PEREME_BEAM

    @property
    def depth(self):
        return KAYIK_DEPTH if self.kind == "kayik" else PEREME_DEPTH

    def validate(self):
        if self.kind not in ("kayik", "pereme"):
            raise ValueError(f"kind={self.kind} — kaynak IKI tip adlandiriyor: "
                             "kayik ve pereme. Ucuncusu uydurma olurdu.")
        # Kurekli teknenin kisiti: cok tombul kurekle gitmez, cok ince devrilir.
        oran = self.length / self.beam
        if not 3.4 <= oran <= 5.6:
            raise ValueError(f"boy/en={oran:.2f} — kurekli tekne bandinda "
                             "(3,4-5,6) degil")
        if self.kind == "kayik" and self.oars > 2:
            raise ValueError("kayik kucuk teknedir; ikiden cok kurek cifti "
                             "peremenin isidir")
        return self


def _hull(name, L, B, D, col):
    """
    Gövdeyi kesitlerden loft eder.

    Her istasyonun genişliği `sin` eğrisiyle verilir: baş ve kıçta sıfıra
    yaklaşır, ortada en geniştir. Küpeşte baş ve kıçta yükselir — teknenin
    silueti odur.
    """
    bm = bmesh.new()
    halkalar = []
    for i in range(STATIONS):
        t = i / (STATIONS - 1.0)                   # 0 = kic, 1 = bas
        x = (t - 0.5) * L
        # Govde dolgunlugu: uclarda sivri, ortada genis.
        f = math.sin(math.pi * t) ** 0.62
        b = max(0.06, B * 0.5 * f)
        d = max(0.10, D * (0.35 + 0.65 * f))
        # Kupeste yukselmesi: uclarda +, ortada 0.
        seer = D * 0.42 * (1.0 - math.sin(math.pi * t)) ** 1.6
        halka = []
        # U kesiti: 7 nokta (iskele kupeste -> omurga -> sancak kupeste)
        #
        # `kaldir` govdeyi su hattina gore YUKSELTIR: omurga suyun altinda,
        # kupeste ustunde kalsin. Bkz. DRAFT_RATIO.
        kaldir = d * (1.0 - DRAFT_RATIO)
        for u in (-1.0, -0.72, -0.34, 0.0, 0.34, 0.72, 1.0):
            y = b * u
            # Karina: ortada derin, bordaya dogru yukselir.
            z = -d * (1.0 - abs(u) ** 1.7) + seer * abs(u) + kaldir
            halka.append(bm.verts.new((x, y, z)))
        halkalar.append(halka)

    bm.verts.ensure_lookup_table()
    for i in range(STATIONS - 1):
        a, b2 = halkalar[i], halkalar[i + 1]
        for j in range(len(a) - 1):
            bm.faces.new((a[j], a[j + 1], b2[j + 1], b2[j]))
    # Bas ve kic kapaklari
    bm.faces.new(list(reversed(halkalar[0])))
    bm.faces.new(halkalar[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.mesh_from_bmesh(name, bm, col)


def build_kayik(p, col, asset_name, textured=False):
    """Kayık ya da pereme. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    L, B, D = p.length, p.beam, p.depth

    # --- GOVDE ------------------------------------------------------------
    # Yapisal ahsap BOYANMAZ (ADR 0035): asi kirmizisi ev boyasidir, tekne
    # boyasi degil. Uskudar iskelesinde verilen ayni karar.
    parts.append(hz.assign(_hull(f"Govde_{asset_name}", L, B, D, col),
                           mats["timber_bare"]))

    # --- KUPESTE KUSAGI ---------------------------------------------------
    # Ust kenari ceviren ince serit. Govdeyi "kabuk" degil "tekne" yapan sey
    # bu cizgidir; onsuz kesitin ustu acik bir olukmus gibi okunuyor.
    for sy in (-1, 1):
        for i in range(STATIONS - 1):
            t0, t1 = i / (STATIONS - 1.0), (i + 1) / (STATIONS - 1.0)
            x0, x1 = (t0 - 0.5) * L, (t1 - 0.5) * L
            b0 = max(0.06, B * 0.5 * math.sin(math.pi * t0) ** 0.62)
            b1 = max(0.06, B * 0.5 * math.sin(math.pi * t1) ** 0.62)
            s0 = D * 0.42 * (1.0 - math.sin(math.pi * t0)) ** 1.6
            s1 = D * 0.42 * (1.0 - math.sin(math.pi * t1)) ** 1.6
            mx, my = (x0 + x1) * 0.5, sy * (b0 + b1) * 0.5
            b = hz.make_box(f"Kupeste_{sy}_{i}",
                            (math.hypot(x1 - x0, (b1 - b0)) * 1.05, 0.07, 0.09),
                            (0.0, 0.0, 0.0), col)
            b.rotation_euler = (0.0, -math.atan2(s1 - s0, x1 - x0),
                                math.atan2(sy * (b1 - b0), x1 - x0))
            b.location = (mx, my, (s0 + s1) * 0.5 + 0.02
                          + D * (1.0 - DRAFT_RATIO))
            parts.append(hz.assign(b, mats["trim"]))

    # --- OTURAKLAR (thwart) -----------------------------------------------
    # Enine sira. Yolcunun oturdugu yer ve ayni zamanda govdeyi ayakta tutan
    # baglanti — kurekli teknede oturak yapisaldir, mobilya degil.
    for k in range(p.thwarts):
        t = 0.30 + 0.40 * (k + 0.5) / max(1, p.thwarts)
        x = (t - 0.5) * L
        b = B * math.sin(math.pi * t) ** 0.62
        parts.append(hz.assign(hz.make_box(
            f"Oturak_{k}", (0.24, b * 0.92, 0.06),
            (x, 0.0, D * (1.0 - DRAFT_RATIO) - D * 0.16), col),
            mats["timber_bare"]))

    # --- KUREKLER ---------------------------------------------------------
    # Kurek KUPESTEDE doner: sapi ICERIDE (kurekcinin elinde), palasi
    # DISARIDA ve suda. Ilk yazimda lumun merkezi kupesteden 0,95 m disarida
    # duruyordu ve ic ucu tekneye ancak 0,30 m giriyordu — renderda kurekler
    # tekneye degmeyen, havada duran cubuklar gibi okundu. Merkez iceri
    # alindi: sap ~0,80 m iceride, pala ~1,70 m disarida.
    for k in range(p.oars):
        t = 0.38 + 0.22 * k
        x = (t - 0.5) * L
        b = B * 0.5 * math.sin(math.pi * t) ** 0.62
        for sy in (-1, 1):
            sap = hz.make_box(f"Kurek_{k}{sy}", (0.055, 2.5, 0.055),
                              (0.0, 0.0, 0.0), col)
            sap.rotation_euler = (math.radians(-16.0) * sy, 0.0, 0.0)
            sap.location = (x, sy * (b + 0.45),
                            D * (1.0 - DRAFT_RATIO) + 0.10)
            parts.append(hz.assign(sap, mats["timber_bare"]))
            pala = hz.make_box(f"KurekPala_{k}{sy}", (0.085, 0.62, 0.02),
                               (0.0, 0.0, 0.0), col)
            pala.rotation_euler = (math.radians(-16.0) * sy, 0.0, 0.0)
            pala.location = (x, sy * (b + 1.78),
                             D * (1.0 - DRAFT_RATIO) + 0.10 - 0.48)
            parts.append(hz.assign(pala, mats["timber_bare"]))

    # --- LOD1: kutle. Uzaktan bir tekne bir lekedir. --------------------
    l1.append(hz.assign(hz.make_box(f"L1_{asset_name}", (L * 0.94, B, D * 0.9),
                                    (0.0, 0.0, D * (1.0 - DRAFT_RATIO)
                                     - D * 0.45), col),
                        mats["timber_bare"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}", (L, B, D),
                      (0.0, 0.0, (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["timber_bare"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="kayik", boat_kind=p.kind, palette=p.palette,
                status="draft", accuracy="D3",
                length=round(L, 2), beam=round(B, 2),
                draft_depth=round(D, 3),
                length_beam=round(L / B, 2),
                oar_pairs=p.oars, thwarts=p.thwarts)
    return lod0, lod1, ucx, info
