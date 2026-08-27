"""
Hezarfen: 1632 — **Topkapı Sarayı** siluet kiti (Faz 3, S-kademe).

Saray bir yapı değil bir kenttir; PLAN onu "**siluet** (uzaktan)" olarak
tarif eder ve doğrusu budur — uçuş hattından görülen şey kütlelerin
dizilişidir. Bu kit o siluetin **iki belirleyici parçasını** kurar:

* **Adalet Kulesi** — sarayın en yüksek öğesi,
* **Bâbüsselâm (Orta Kapı)** — çifte konik külahlı kapı.

## 1632'nin iki sessiz farkı

**Adalet Kulesi bugünkünden ALÇAKTIR.** Kanunî 1527-29'da taş bölümü
ekletti; kule o hâliyle **üç taş kat + ahşap bir üst kat + kurşun kaplı
piramidal külah**tır. **II. Mahmud (1819-20)** bir taş kat daha ekletti,
üstüne ahşap bir seyir bölümü koydurdu ve kurşun külahı yükseltti;
**Abdülaziz** bugünkü yüksek ve sivri külahı verdi. Yani bugün fotoğrafta
görülen kule 19. yüzyıldır.

Bu, Galata Kulesi'ndekiyle **aynı** hata ailesidir (ADR 0033): tanınan
siluet sonraki yüzyılların eseridir ve "herkesin bildiği hâli" modellemek
1632'yi silmektir.

**Alay Köşkü 1632'de AHŞAPTIR** — bugünkü mermer köşk II. Mahmud'undur
(1810/1819-20). Bu kit onu henüz kurmuyor; kayıt RESEARCH.md §5.7'de.

## Doğruluk

Ölçü **yok** — kütleler **D3**. Ama sayılabilir olan bağlanır: kulenin
**üç** taş katı ve Bâbüsselâm'ın **iki** kulesi geometriyi kısıtlar ve
üretici denetiminden geçer.
"""

import math

import bpy  # noqa: F401

import hz_blender as hz
import mahalle_kit as mak
import detay_kit as dk
import ottoman_kit as kit
import street_kit as sk

#: Kanunî düzenlemesinden sonraki taş kat sayısı — 1632'de bu kadardır.
#: II. Mahmud'un eklediği dördüncü kat 1819-20'dir.
ADALET_TAS_KAT = 3

#: Bâbüsselâm'ın kule sayısı (sayım).
BABUSSELAM_KULE = 2


class AdaletKulesiParams(object):
    """
    Adalet Kulesi, 1632 hâli.

    Kule Kubbealtı'nın yanında durur ve padişah divanı **kafesli
    pencereden** izler; bu yüzden gövdede kafesli bir açıklık vardır ve
    kule bir kule değil, bir **pencere taşıyıcısıdır**.
    """

    def __init__(self, side=6.40, tiers=ADALET_TAS_KAT, tier_h=4.30,
                 timber_h=3.40, roof_h=6.20, base_h=1.10, palette="default"):
        self.side = side
        self.tiers = tiers
        self.tier_h = tier_h
        self.timber_h = timber_h        # ahsap ust kat
        self.roof_h = roof_h            # kursun piramidal kulah
        self.base_h = base_h
        self.palette = palette

    @property
    def stone_h(self):
        return self.tiers * self.tier_h

    @property
    def total_h(self):
        return self.base_h + self.stone_h + self.timber_h + self.roof_h

    def validate(self):
        if self.tiers != ADALET_TAS_KAT:
            raise ValueError(
                f"tiers={self.tiers} — 1632'de kule UC tas katlidir; "
                "dorduncu kat II. Mahmud'un (1819-20) eklemesidir")
        # Kule sarayin EN YUKSEK ogesi olmali; degilse siluette kaybolur.
        if self.total_h < 18.0:
            raise ValueError(f"toplam {self.total_h:.1f} m — Adalet Kulesi "
                             "sarayin en yuksek ogesidir")
        # Ama bugunku kule DEGILDIR: II. Mahmud bir tas kat (~4,3 m) ve
        # yukseltilmis kulah ekledi, Abdulaziz sivriltti. 1632 kulesi o
        # eklemelerin toplamindan alcak kalmali.
        if self.total_h > 26.0:
            raise ValueError(f"toplam {self.total_h:.1f} m — bu 19. yuzyil "
                             "kulesidir; 1632 kulesi ondan ALCAKTIR")
        return self


def build_adalet_kulesi(p, col, asset_name, textured=False):
    """Adalet Kulesi (1632). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    S = p.side

    # --- Kaide -----------------------------------------------------------
    parts.append(hz.assign(hz.make_box(f"Kaide_{asset_name}",
                                       (S + 1.1, S + 1.1, p.base_h),
                                       (0.0, 0.0, p.base_h * 0.5), col),
                           mats["cutstone"]))
    z = p.base_h

    # --- UC TAS KAT: her katta silme, govdede kafesli pencere ------------
    for k in range(p.tiers):
        parts.append(hz.assign(hz.make_box(f"TasKat{k}_{asset_name}",
                                           (S, S, p.tier_h),
                                           (0.0, 0.0, z + p.tier_h * 0.5),
                                           col), mats["cutstone"]))
        # Kat silmesi KADEMELI: tek bir 22 cm'lik kutu uzaktan cizgi,
        # yakindan bant okunuyordu (bkz. `detay_kit.silme` gerekcesi).
        for o in dk.silme(f"Silme{k}_{asset_name}", S, S,
                          z + p.tier_h - 0.44, col, steps=3, h=0.44,
                          out=0.30):
            parts.append(hz.assign(o, mats["cutstone"]))
        # HUNKAR PENCERESI: en ust tas katta, Kubbealti'na (-Y) bakar.
        # Kuleyi kule yapan sey bu penceredir; padisah divani buradan izler.
        if k == p.tiers - 1:
            w, h = S * 0.42, p.tier_h * 0.52
            ctr = (0.0, -S * 0.5, z + p.tier_h * 0.50)
            parts.append(hz.assign(hz.make_box(f"HunkarKaranlik_{asset_name}",
                                               (w, 0.28, h), ctr, col),
                                   mats["shadow"]))
            # Kafes: mahalle kitindeki demir sebeke. Ayni demir isciligi
            # sarayda da kullanilir; ikinci bir nusha yazmak, zamanla iki
            # farkli sebeke demektir.
            mak._face_grille(parts, mats, col, f"HunkarKafes_{asset_name}",
                             w, h, ctr, (1.0, 0.0), (0.0, -1.0), 0.30)
            parts.append(hz.assign(hz.make_box(
                f"HunkarSove_{asset_name}", (w + 0.70, 0.34, h + 0.70),
                (0.0, -S * 0.5 - 0.10, ctr[2]), col), mats["marble"]))
            for o in dk.kemer(f"HunkarKemer_{asset_name}", 0.0,
                              -S * 0.5 - 0.10, 1.0, 0.0, w * 0.5,
                              ctr[2] + h * 0.5, 0.28, 0.40, col, steps=6):
                parts.append(hz.assign(o, mats["marble"]))
        else:
            for sgn in (-1, 1):
                pz = z + p.tier_h * 0.52
                parts.append(hz.assign(hz.make_box(
                    f"PencSove{k}_{sgn}", (0.85 + 0.55, 0.30, 1.45 + 0.55),
                    (sgn * S * 0.22, -S * 0.5 - 0.08, pz), col),
                    mats["cutstone"]))
                parts.append(hz.assign(
                    hz.make_box(f"Pencere{k}_{sgn}", (0.85, 0.26, 1.45),
                                (sgn * S * 0.22, -S * 0.5, pz), col),
                    mats["shadow"]))
                for o in dk.kemer(f"PencKemer{k}_{sgn}", sgn * S * 0.22,
                                  -S * 0.5 - 0.08, 1.0, 0.0, 0.425,
                                  pz + 0.725, 0.22, 0.36, col, steps=5):
                    parts.append(hz.assign(o, mats["cutstone"]))
        z += p.tier_h

    # --- AHSAP UST KAT ---------------------------------------------------
    #
    # BOYASIZ ahsap: bu bir saray yapisidir, ev degil; asi kirmizisi ev
    # boyasidir (ADR 0035, `timber_bare`).
    tw = S + 0.90
    # ELIBOGRUNDE: ahsap kat tas govdeden her yandan 45 cm tasar ve bu
    # tasma bosluga oturmaz — ucgen payandalar tasir. Ev kitindeki cumba
    # payandasinin ta kendisi (`ottoman_kit._corbel_bracket`); ikinci bir
    # nusha yazmak, zamanla iki farkli payanda demek olurdu.
    for i in range(4):
        a = math.pi * 0.5 * i
        for t in (-0.28, 0.28):
            b = kit._corbel_bracket(f"Payanda_{i}_{t:+.2f}", 0.30, 0.62,
                                    0.85, (0.0, 0.0, 0.0), col)
            b.rotation_euler = (0.0, 0.0, a)
            b.location = (math.sin(a) * S * 0.5 + math.cos(a) * tw * t,
                          math.cos(a) * S * 0.5 - math.sin(a) * tw * t,
                          z - 0.42)
            parts.append(hz.assign(b, mats["timber_bare"]))

    parts.append(hz.assign(hz.make_box(f"AhsapKat_{asset_name}",
                                       (tw, tw, p.timber_h),
                                       (0.0, 0.0, z + p.timber_h * 0.5), col),
                           mats["timber_bare"]))
    for i in range(4):
        a = math.pi * 0.5 * i
        parts.append(hz.assign(
            hz.make_box(f"AhsapCam{i}_{asset_name}",
                        (tw * 0.62 if i % 2 == 0 else 0.12,
                         0.12 if i % 2 == 0 else tw * 0.62,
                         p.timber_h * 0.48),
                        (math.sin(a) * tw * 0.5, math.cos(a) * tw * 0.5,
                         z + p.timber_h * 0.56), col), mats["glass"]))
    parts.append(hz.assign(hz.make_box(f"AhsapSacak_{asset_name}",
                                       (tw + 0.75, tw + 0.75, 0.26),
                                       (0.0, 0.0, z + p.timber_h + 0.13), col),
                           mats["timber_bare"]))
    z += p.timber_h + 0.26

    # --- KURSUN PIRAMIDAL KULAH -----------------------------------------
    #
    # PIRAMIT, koni degil: kaynak "kursun kapli piramidal cati" der ve
    # bugunku SIVRI koni Abdulaziz'indir.
    parts.append(hz.assign(hz.make_tube(f"Kulah_{asset_name}",
                                        (tw + 0.75) * 0.62, 0.0, p.roof_h,
                                        (0.0, 0.0), z, segments=4,
                                        phase=math.pi * 0.25, col=col),
                           mats["lead"]))
    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0, z + p.roof_h, col,
                     scale=0.8):
        parts.append(hz.assign(o, mats["lead"]))
    parts.append(hz.assign(hz.make_tube(f"AlemMil_{asset_name}", 0.09, 0.02,
                                        1.0,
                                        (0.0, 0.0), z + p.roof_h, segments=6,
                                        col=col), mats["lead"]))

    l1.append(hz.assign(hz.make_box("L1_Govde", (S, S, p.base_h + p.stone_h),
                                    (0.0, 0.0, (p.base_h + p.stone_h) * 0.5),
                                    col), mats["cutstone"]))
    l1.append(hz.assign(hz.make_box("L1_Ahsap", (tw, tw, p.timber_h),
                                    (0.0, 0.0, p.base_h + p.stone_h
                                     + p.timber_h * 0.5), col),
                        mats["timber_bare"]))
    l1.append(hz.assign(hz.make_tube("L1_Kulah", (tw + 0.75) * 0.62, 0.0,
                                     p.roof_h, (0.0, 0.0), z, segments=4,
                                     phase=math.pi * 0.25, col=col),
                        mats["lead"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["cutstone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="saray_kule", stone_tiers=p.tiers,
                palette=p.palette, status="draft", accuracy="D3")
    return lod0, lod1, ucx, info


class BabusselamParams(object):
    """
    Bâbüsselâm (Orta Kapı) — **çifte konik külahlı** kapı.

    Kuleler 1632'de vardır; tartışma yalnızca **kimin** eklediğidir
    (Necipoğlu Fatih der, yaygın görüş Kanunî) ve iki ihtimal de 1632'den
    öncedir — yani soru modeli etkilemez, kayda geçer.
    """

    def __init__(self, width=22.0, depth=7.5, wall_h=8.60, tower_r=2.35,
                 tower_h=13.50, cone_h=7.20, gate_w=3.60, palette="default"):
        self.width, self.depth = width, depth
        self.wall_h = wall_h
        self.tower_r, self.tower_h, self.cone_h = tower_r, tower_h, cone_h
        self.gate_w = gate_w
        self.palette = palette

    def validate(self):
        # Kuleler cepheden YUKSELMELI, yoksa "cifte kuleli kapi" okunmaz.
        if self.tower_h < self.wall_h + 3.0:
            raise ValueError(f"tower_h={self.tower_h} — kuleler cepheden "
                             "belirgin yukselmeli")
        if self.gate_w > self.width * 0.3:
            raise ValueError(f"gate_w={self.gate_w} cepheye gore genis")
        return self


def build_babusselam(p, col, asset_name, textured=False):
    """Bâbüsselâm. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D, H = p.width, p.depth, p.wall_h

    # --- Cephe: ortada KEMERLI kapi -------------------------------------
    parts.append(hz.assign(sk.arched_panel(
        f"Cephe_{asset_name}", W, H, D, (0.0, 0.0, 0.0), (1.0, 0.0),
        (0.0, -1.0), spans=[(-p.gate_w * 0.5, p.gate_w * 0.5)], sill_z=0.0,
        spring_z=H * 0.42, col=col), mats["cutstone"]))
    parts.append(hz.assign(hz.make_box(f"KapiKaranlik_{asset_name}",
                                       (p.gate_w, D * 0.9, H * 0.62),
                                       (0.0, 0.0, H * 0.31), col),
                           mats["shadow"]))
    for o in dk.silme(f"Sacak_{asset_name}", W, D, H - 0.30, col,
                      steps=3, h=0.62, out=0.55):
        parts.append(hz.assign(o, mats["cutstone"]))

    # --- TACKAPI: bu yapinin KENDISI bir kapidir -------------------------
    #
    # Cephede kemerli bir delik vardi ve o kadar. Oysa Babusselam'in adi da
    # islevi de kapidir: saray halkinin disinda herkesin ATTAN INDIGI esik.
    # Onu bir delik olarak birakmak, yapiyi tanimlayan seyi modellememekti.
    # Nis agzi (0,52 x genislik) kapi acikliginden GENIS tutulur ki
    # arkadaki kemerli panel nisin icinde okunsun.
    tk_w = p.gate_w * 2.4
    parts += dk.tackapi(mats, col, f"Tackapi_{asset_name}",
                        0.0, -D * 0.5, 0.0, tk_w, H * 1.10, 1.35,
                        kapi_w=p.gate_w, kapi_h=H * 0.62)

    # --- IKI KULE, KONIK kulahli (sayilan deger) ------------------------
    for sgn in (-1, 1):
        cx = sgn * (W * 0.5 - p.tower_r - 0.30)
        parts.append(hz.assign(hz.make_tube(f"Kule_{sgn}", p.tower_r,
                                            p.tower_r * 0.96, p.tower_h,
                                            (cx, 0.0), 0.0, segments=14,
                                            col=col), mats["cutstone"]))
        # Kulahin oturdugu bilezigi KONSOL SIRASI tasir; onsuz bilezik
        # govdeden buyumus gibi okunuyordu (Galata'da olculen ayni sey).
        # Konsol INCE olmali: ilk denemede tasma 0,34 / boy 0,72 idi ve
        # renderda mazgal siperi gibi okundu — Babusselam'in kuleleri
        # mazgalli degildir, kulahin oturdugu bir bilezikleri vardir.
        for o in dk.konsol_dizisi(f"KuleKonsol_{sgn}", cx, 0.0, p.tower_r,
                                  p.tower_h - 0.86, col, n=20, out=0.16,
                                  h=0.42):
            parts.append(hz.assign(o, mats["cutstone"]))
        parts.append(hz.assign(hz.make_tube(f"KuleSilme_{sgn}",
                                            p.tower_r * 1.15,
                                            p.tower_r * 1.12, 0.30,
                                            (cx, 0.0), p.tower_h - 0.30,
                                            segments=14, col=col),
                               mats["cutstone"]))
        # KONIK kulah — kursun. Bu iki koni sarayin en taninan isaretidir.
        parts.append(hz.assign(hz.make_tube(f"Kulah_{sgn}",
                                            p.tower_r * 1.16, 0.0, p.cone_h,
                                            (cx, 0.0), p.tower_h,
                                            segments=16, col=col),
                               mats["lead"]))
        for o in dk.alem(f"Alem_{sgn}", cx, 0.0, p.tower_h + p.cone_h, col,
                         scale=0.7):
            parts.append(hz.assign(o, mats["lead"]))
        for i in range(3):
            pz = 3.0 + i * 3.4
            parts.append(hz.assign(
                hz.make_box(f"KulePencere_{sgn}{i}", (0.42, 0.22, 0.95),
                            (cx, -p.tower_r, pz), col), mats["shadow"]))
            for o in dk.kemer(f"KulePencKemer_{sgn}{i}", cx,
                              -p.tower_r + 0.04, 1.0, 0.0, 0.21,
                              pz + 0.475, 0.16, 0.26, col, steps=4):
                parts.append(hz.assign(o, mats["cutstone"]))

    l1.append(hz.assign(hz.make_box("L1_Cephe", (W, D, H),
                                    (0.0, 0.0, H * 0.5), col),
                        mats["cutstone"]))
    for sgn in (-1, 1):
        cx = sgn * (W * 0.5 - p.tower_r - 0.30)
        l1.append(hz.assign(hz.make_tube(f"L1_Kule{sgn}", p.tower_r,
                                         p.tower_r, p.tower_h, (cx, 0.0), 0.0,
                                         segments=8, col=col),
                            mats["cutstone"]))
        l1.append(hz.assign(hz.make_tube(f"L1_Kulah{sgn}", p.tower_r * 1.16,
                                         0.0, p.cone_h, (cx, 0.0), p.tower_h,
                                         segments=8, col=col), mats["lead"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["cutstone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="saray_kapi", towers=BABUSSELAM_KULE,
                palette=p.palette, status="draft", accuracy="D3")
    return lod0, lod1, ucx, info
