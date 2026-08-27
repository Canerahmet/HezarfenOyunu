"""
Hezarfen: 1632 — Mahalle mescidi ve orta ölçek cami üreticisi (plan Faz 2b).

## Neden bu, neden şimdi

RESEARCH.md §4.1(g): **mahallenin çekirdeği mescittir**, doku ondan dallanır.
Sokak yerleştiricisi (ADR 0016) çalışıyor ama mescitsiz — yani mahallenin
merkezi boş. Caner (2026-08-20) camiyi sorduğunda ortaya çıkan boşluk buydu;
plan Faz 2b bunun için eklendi.

## Tipoloji

Osmanlı klasik dönem cami tipolojisi dört sınıfa ayrılır: merkezî kubbeli,
tek birim kubbeli, **ahşap çatılı**, ve melez. Mahalle mescidi mütevazı uçtadır:
tek mekân, tek şerefeli minare, son cemaat yeri.

**Varsayılan çatı AHŞAP'tır, kubbe değil** — ve bu bir kolaycılık değil,
tipolojik doğru: kurşun kubbe vakıflı büyük caminin işaretidir; mahalle mescidi
komşusu olan evlerle aynı alaturka kiremidi taşır. Kubbe `roof="dome"` ile
istenirse üretilir (orta ölçek cami için).

## Minare oranı

Klasik Osmanlı minaresi kurşun kalem gibidir: uzun silindirik gövde, konik
külah. Oranlar burada **türetilir**, elle verilmez — gövde çapı toplam
yükseklikten, şerefe yeri gövdenin belirli bir kesrinden. Elle verilen oran,
parametre değişince sessizce bozulan orandır.

Eksen sözleşmesi ev kitiyle aynı: giriş cephesi −Y (Unity'de +Z), yani
"yapının önü" sokağa bakar.
"""

import math

import bpy

import detay_kit as dk
import hz_blender as hz
import materials as mtl
import ottoman_kit as kit
import street_kit as sk

ROOF_TYPES = ("timber", "dome")

# Kursun: Poly Haven'da uygun CC0 dokusu YOK (25 metal dokusunun hepsi pasli
# sac; kursun paslanmaz, mat acik griye oksitlenir). Dokusuz PBR ile veriliyor
# ve bu bir BOSLUK olarak kayitli — bkz. ADR 0017.
LEAD = ((0.196, 0.203, 0.210), 0.42, "M_Lead_Sheet")


class MescitParams(object):
    """Mahalle mescidi parametreleri."""

    def __init__(self, **kw):
        self.hall = kw.get("hall", 9.0)                  # kare harim kenari (m)
        self.wall_h = kw.get("wall_h", 5.4)
        self.plinth = kw.get("plinth", 0.7)
        self.roof = kw.get("roof", "timber")
        self.roof_pitch_deg = kw.get("roof_pitch_deg", 28.0)
        self.eave = kw.get("eave", 0.9)

        self.portico = kw.get("portico", True)           # son cemaat yeri
        self.portico_depth = kw.get("portico_depth", 3.0)
        self.portico_bays = kw.get("portico_bays", 3)
        # "timber" mahalle mescidi, "stone" kagir cami. Bkz. `_portico`.
        self.portico_material = kw.get("portico_material", "timber")

        self.minaret = kw.get("minaret", True)
        self.minaret_h = kw.get("minaret_h", 19.0)       # zeminden alem ucuna
        self.minaret_side = kw.get("minaret_side", -1)   # -1 sol, +1 sag

        self.mihrab = kw.get("mihrab", True)
        self.wall_thickness = kw.get("wall_thickness", 0.55)   # kagir: evden kalin
        self.window_h = kw.get("window_h", 1.5)
        self.palette = kw.get("palette", "default")

    def validate(self):
        errs = []
        if self.roof not in ROOF_TYPES:
            errs.append(f"roof={self.roof} (secenekler: {ROOF_TYPES})")
        if not (5.0 <= self.hall <= 20.0):
            errs.append(f"hall={self.hall} m makul degil (5-20)")
        if self.wall_h < self.window_h + 2.0:
            errs.append(f"wall_h={self.wall_h} pencereye ({self.window_h}) yetmiyor")
        # Minare govdesi harimden yuksek olmali, yoksa siluet okunmaz.
        if self.minaret and self.minaret_h < self.wall_h + 6.0:
            errs.append(f"minaret_h={self.minaret_h} cok kisa (en az wall_h+6)")
        if self.portico_material not in ("timber", "stone"):
            errs.append(f"portico_material={self.portico_material} "
                        "(secenekler: timber, stone)")
        if self.wall_thickness > self.hall / 6.0:
            errs.append(f"wall_thickness={self.wall_thickness} harime gore kalin")
        if errs:
            raise ValueError("MescitParams gecersiz: " + "; ".join(errs))
        return self


def minaret_base_side(p):
    """
    Minare kaidesinin kenar uzunluğu.

    Hem minareyi kuran hem de onu binaya **oturtan** kod bu sayıya ihtiyaç
    duyar. Tek yerde yaşamazsa ikisi ayrışır ve minare binadan kopar — ilk
    denemede tam olarak bu oldu.
    """
    r = max(0.42, p.minaret_h * 0.032)
    return max(1.7, r * 3.4)


def _minaret_center(p, half, front_y):
    """Minare ekseninin konumu — LOD'ların hepsi bunu okur, kopya yok."""
    side = minaret_base_side(p)
    off = side * 0.5 - side * 0.34        # kaide, duvara ucte biri kadar gomulur
    return p.minaret_side * (half + off), front_y + off


def build_materials(palette_name, textured=False):
    """Kurşun artık paletin kendisinde (ottoman_kit.PALETTES). Bkz. ADR 0020."""
    return kit.build_materials(palette_name, textured=textured)


# ------------------------------------------------------------------ minare

def _minaret(p, mats, col, name, center_xy, base_z):
    """
    Tek şerefeli klasik minare.

    Bölümler aşağıdan yukarı: **kaide** (kare taş), **pabuç** (kareden
    silindire geçiş), **gövde**, **şerefe** (çıkmalı balkon), **petek**
    (şerefe üstü ince gövde), **külah** (konik), **alem**.

    Oranlar toplam yükseklikten türetilir; elle verilseydi `minaret_h`
    değiştiğinde sessizce bozulurdu.
    """
    parts = []
    H = p.minaret_h
    kaide_h = H * 0.20
    pabuc_h = H * 0.09
    serefe_z = H * 0.66                     # serefe, toplam yuksekligin ~2/3'unde
    kulah_h = H * 0.16
    r = max(0.42, H * 0.032)                # govde yaricapi yukseklikten turer
    side = minaret_base_side(p)             # kaide kenari — TEK yerden

    z = base_z
    kaide = hz.make_box(f"{name}_Kaide", (side, side, kaide_h),
                        (center_xy[0], center_xy[1], z + kaide_h * 0.5), col)
    hz.assign(kaide, mats["stone"])
    parts.append(kaide)
    z += kaide_h

    # Pabuc: kareden silindire gecis. Kesik piramit yaklasimi — gercekte
    # pahli/ucgen yuzeylerden olusur, siluette fark etmez.
    pabuc = hz.make_tube(f"{name}_Pabuc", side * 0.62, r * 1.18, pabuc_h,
                         center_xy, z, segments=8, smooth=False, col=col)
    hz.assign(pabuc, mats["stone"])
    parts.append(pabuc)
    z += pabuc_h

    govde_h = base_z + serefe_z - z
    govde = hz.make_tube(f"{name}_Govde", r * 1.05, r, govde_h,
                         center_xy, z, segments=16, cap_top=False, col=col)
    hz.assign(govde, mats["stone"])
    parts.append(govde)
    z += govde_h

    # Serefe: `detay_kit.serefe` — mukarnas konsol + tabla + DELIKLI
    # korkuluk. Onceki hali bir koni + plaka + KAPALI bir bilezikti;
    # korkulugu korkuluk yapan sey bosluklaridir ve siluette okunan da o.
    # Mahalle mescidinin minaresi ince oldugu icin dikme sayisi az (10):
    # r=0,42'de on alti dikme sekiz santim araliga duser, yani doluya.
    for o in dk.serefe(f"{name}_Serefe", center_xy[0], center_xy[1], z, r,
                       col, korkuluk_n=10):
        parts.append(hz.assign(o, mats["stone"]))
    z += 0.14

    petek_h = base_z + H - kulah_h - z
    petek = hz.make_tube(f"{name}_Petek", r * 0.92, r * 0.86, petek_h,
                         center_xy, z, segments=16, cap_top=False, col=col)
    hz.assign(petek, mats["stone"])
    parts.append(petek)
    z += petek_h

    kulah = hz.make_tube(f"{name}_Kulah", r * 1.02, 0.0, kulah_h,
                         center_xy, z, segments=16, col=col)
    hz.assign(kulah, mats["lead"])
    parts.append(kulah)

    for o in dk.alem(f"{name}_Alem", center_xy[0], center_xy[1],
                     z + kulah_h, col, scale=0.6):
        parts.append(hz.assign(o, mats["lead"]))
    return parts


# --------------------------------------------------------------- son cemaat

def _portico(p, mats, col, name, hall_front_y, base_z):
    """
    Son cemaat yeri — girişin önündeki üstü örtülü, önü açık bölüm.

    Mahalle mescidinde bu bölüm çoğu zaman **ahşap direklidir**, revaklı
    değil; taş kemerli revak daha varlıklı yapıların işaretidir. Direk
    aralığı `portico_bays`ten türetilir.

    `portico_material="stone"` direkleri **taş sütuna** çevirir. Bu bir
    süsleme seçeneği değil tipolojik ayrımdır: Doğancılar Camii bir
    çakırcıbaşının Sinan'a yaptırdığı **kâgir** yapıdır ve ayakta kalan
    özgün parçaları "mermer çerçeveli kapı" ile "ince kesme taş minare
    kaidesi"dir. İlk kurulumda direkler mahalle mescidinin aşı kırmızısı
    ahşabıyla çıktı ve render'da yapı bir kâgir cami gibi değil, boyalı
    ahşap saçaklı bir mescit gibi okundu.
    """
    parts = []
    w = p.hall
    d = p.portico_depth
    h = p.wall_h * 0.78
    y_out = hall_front_y - d

    stone = getattr(p, "portico_material", "timber") == "stone"
    post = 0.34 if stone else 0.24
    n = max(2, p.portico_bays + 1)
    for i in range(n):
        u = (i / (n - 1) - 0.5) * (w - post)
        if stone:
            col_obj = hz.make_tube(f"{name}_Sutun{i}", post * 0.5,
                                   post * 0.46, h,
                                   (u, y_out + post * 0.5), base_z,
                                   segments=12, col=col)
            hz.assign(col_obj, mats["marble"])
            parts.append(col_obj)
            cap = hz.make_box(f"{name}_Baslik{i}", (post * 1.5, post * 1.5,
                                                    post * 0.55),
                              (u, y_out + post * 0.5,
                               base_z + h + post * 0.275), col)
            hz.assign(cap, mats["marble"])
            parts.append(cap)
        else:
            col_obj = hz.make_box(f"{name}_Direk{i}", (post, post, h),
                                  (u, y_out + post * 0.5,
                                   base_z + h * 0.5), col)
            hz.assign(col_obj, mats["timber"])
            parts.append(col_obj)

    # Direkleri baglayan hatil: ahsapta kiris, tasta kemer kusagi.
    hb = 0.42 if stone else 0.30
    hatil = hz.make_box(f"{name}_Hatil", (w, 0.28 if not stone else 0.40, hb),
                        (0.0, y_out + post * 0.5,
                         base_z + h + (0.28 if stone else 0.0) + hb * 0.5), col)
    hz.assign(hatil, mats["cutstone"] if stone else mats["trim"])
    parts.append(hatil)

    # Ortu EGIMLI bir sundurmadir, duz levha degil.
    #
    # Ilk yazimda kutuydu ve render'da "tabela" gibi duruyordu: yagmurun nereye
    # aktigi okunmuyordu. Sundurma duvara yaslanir, disari dogru alcalir; bu
    # egim hem dogru yapim hem de gozun aradigi sey.
    high = base_z + h + 0.75          # duvar tarafi
    low = base_z + h + 0.18           # dis uc
    ortu = _lean_to(f"{name}_Sundurma", w + 0.6, hall_front_y + 0.35,
                    y_out - 0.35, high, low, 0.16, col)
    hz.assign(ortu, mats["roof"])
    parts.append(ortu)
    return parts


# Şebeke `street_kit`e taşındı — mescit, kilise ve sinagog aynı demir işini
# kullanır. İki yerde iki kopya, zamanla iki farklı şebeke demektir.
_iron_grille = sk.iron_grille


def _lean_to(name, width, y_high, y_low, z_high, z_low, thick, col):
    """Duvara yaslanıp dışarı alçalan eğimli örtü (sundurma)."""
    import bmesh
    hw = width * 0.5
    bm = bmesh.new()
    top = [(-hw, y_high, z_high), (hw, y_high, z_high),
           (hw, y_low, z_low), (-hw, y_low, z_low)]
    bot = [(x, y, z - thick) for (x, y, z) in top]
    vt = [bm.verts.new(v) for v in top]
    vb = [bm.verts.new(v) for v in bot]
    bm.verts.ensure_lookup_table()
    bm.faces.new(vt)
    bm.faces.new(list(reversed(vb)))
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((vt[i], vt[j], vb[j], vb[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.mesh_from_bmesh(name, bm, col)


# -------------------------------------------------------------------- mescit

def build_mescit(p, col, asset_name, textured=False):
    """Tam mescidi kurar. Dönüş: `(lod0, lod1, lod2, ucx, info)`."""
    p.validate()
    mats, tex_sizes = build_materials(p.palette, textured=textured)
    parts = []

    half = p.hall * 0.5
    front_y = -half                      # giris cephesi -Y (ev kitiyle ayni)
    z = 0.0

    # 1) Tas subasman
    plinth = hz.make_box(f"{asset_name}_Plinth", (p.hall + 0.3, p.hall + 0.3, p.plinth),
                         (0.0, 0.0, p.plinth * 0.5), col)
    hz.assign(plinth, mats["stone"])
    parts.append(plinth)
    z += p.plinth

    # 2) Harim duvarlari — DELIKLI panel (ev kitiyle ayni yontem, ADR 0013).
    #    Kagir yapi: duvar kalin, sove derinligi evden fazla; yakin planda
    #    mescidi evden ayiran ilk sey budur.
    t = p.wall_thickness
    win_z0 = p.wall_h * 0.34
    win = (win_z0, win_z0 + p.window_h)
    faces = (("front", (1.0, 0.0), (0.0, -1.0), (0.0, -half + t * 0.5)),
             ("back", (-1.0, 0.0), (0.0, 1.0), (0.0, half - t * 0.5)),
             ("left", (0.0, -1.0), (-1.0, 0.0), (-half + t * 0.5, 0.0)),
             ("right", (0.0, 1.0), (1.0, 0.0), (half - t * 0.5, 0.0)))

    for face, u_axis, n_axis, (ox, oy) in faces:
        span = p.hall if face in ("front", "back") else p.hall - 2.0 * t
        ops = []
        if face == "front":
            # Giris kapisi: yuksek, kemerli izlenimi veren tek buyuk aciklik.
            dw = min(1.5, span * 0.22)
            ops.append((-dw * 0.5, dw * 0.5, 0.16, 0.16 + 2.7))
            for s in (-1, 1):
                cu = s * span * 0.31
                ops.append((cu - 0.42, cu + 0.42, win[0], win[1]))
        else:
            # Yan ve kible duvarinda iki sira degil tek sira: mahalle
            # mescidi mutevazidir; ust sira pencere buyuk caminin isaretidir.
            for s in (-1, 0, 1):
                cu = s * span * 0.28
                ops.append((cu - 0.45, cu + 0.45, win[0], win[1]))
        wall = hz.make_wall_panel(f"{asset_name}_{face}", span, p.wall_h, t,
                                  (ox, oy, z), u_axis, n_axis, openings=ops, col=col)
        hz.assign(wall, mats["plaster"])
        parts.append(wall)

        # Aciklik arkasi karanlik + tas sove: ev kitindeki mantik, kagir olcekte.
        for (u0, u1, v0, v1) in ops:
            cu, cv = (u0 + u1) * 0.5, (v0 + v1) * 0.5
            ow, oh = u1 - u0, v1 - v0
            # Panel duvarin IC YUZUNE oturur. Ilk yazimda `-t + 0.03` yazmisim;
            # panel origini MID-KALINLIKTA oldugu icin bu, paneli ic yuzden
            # 0,25 m daha iceri itiyordu — aciklik derin bir kutu gibi
            # okunuyor, karanligi gorunmuyordu. Dogrusu yarim kalinlik.
            nz = -t * 0.5 - 0.02
            px = ox + u_axis[0] * cu + n_axis[0] * nz
            py = oy + u_axis[1] * cu + n_axis[1] * nz
            sz = (ow, 0.05, oh) if abs(u_axis[0]) > 0.5 else (0.05, ow, oh)
            dark = hz.make_box(f"{asset_name}_{face}_Dark", sz, (px, py, z + cv), col)
            hz.assign(dark, mats["shadow"])
            parts.append(dark)

            # Demir sebeke — kapiya degil, PENCERELERE.
            #
            # Olculdu: 0,55 m kalinligindaki kagir duvarda sove derin ve PARLAK
            # okuyor; aciklik uzaktan beyaz bir dikdortgen gibi duruyordu
            # (duvar bolgesi L~145, sapma 33). Eksik olan sey mimariydi:
            # cami pencerelerinin alt sirasi demir sebekelidir. Sebeke hem
            # dogru detay hem de acikligi okutan koyu ritim.
            if oh < 2.2:
                parts += _iron_grille(f"{asset_name}_{face}_Sebeke", ow, oh,
                                      (ox, oy, z), u_axis, n_axis, cu, cv,
                                      t, mats, col)
                # HAFIFLETME KEMERI: dikdortgen bir delik ne kagir ne
                # Osmanli. Pencerenin ustune oturan sivri kemer, hem
                # gercek yapisal ogedir hem de acikligi cepheye baglar.
                kx = ox + u_axis[0] * cu - n_axis[0] * 0.06
                ky = oy + u_axis[1] * cu - n_axis[1] * 0.06
                for o in dk.kemer(f"{asset_name}_{face}_Kemer",
                                  kx, ky, u_axis[0], u_axis[1],
                                  ow * 0.5, z + v1, 0.20, 0.34, col,
                                  steps=5):
                    parts.append(hz.assign(o, mats["cutstone"]))

    # SACAK SILMESI EKLENMEDI — ve bu bir karar.
    #
    # Bir kademe silme koymustum; renderda hic gorunmedi. Sebebi olculdu:
    # mahalle mescidi AHSAP CATILIDIR ve kiremit sacak duvardan silmeden
    # cok daha fazla tasar — silmeyi tumuyle yutar. Ustelik yanlisti da:
    # tas korniş kagir bir cati saciyla biten yapinin ogesidir; burada o
    # isi sacagin kendisi yapar. Gorunmeyen geometri eklemek, ucgeni
    # oduncu vermek demektir.

    # 3) Mihrap: kible duvarindan (+Y) disari tasan yarim kutle.
    if p.mihrab:
        mw = 1.9
        mih = hz.make_box(f"{asset_name}_Mihrab", (mw, 0.85, p.wall_h * 0.86),
                          (0.0, half + 0.42, z + p.wall_h * 0.43), col)
        hz.assign(mih, mats["plaster"])
        parts.append(mih)

    z += p.wall_h
    wall_top = z

    # 4) Orto: ahsap cati (varsayilan) ya da kursun kubbe.
    roof_w = p.hall + 2.0 * p.eave
    if p.roof == "timber":
        rh = 0.5 * roof_w * math.tan(math.radians(p.roof_pitch_deg))
        roof = hz.make_hip_roof(f"{asset_name}_Roof", roof_w, roof_w, rh,
                                center_xy=(0.0, 0.0), base_z=z, ridge_axis="X", col=col)
        hz.assign(roof, mats["roof"])
        parts.append(roof)
        fascia_h = max(0.16, rh * 0.07)
        fas = hz.make_box(f"{asset_name}_Fascia", (roof_w, roof_w, fascia_h),
                          (0.0, 0.0, z - fascia_h * 0.5), col)
        hz.assign(fas, mats["trim"])
        parts.append(fas)
        total_h = z + rh
    else:
        # Kubbe sekizgen bir kasnak uzerine oturur; kasnak olmadan kubbe
        # duvara dogrudan yapisir ve gecis (pandantif) okunmaz.
        drum_h = p.hall * 0.14
        # Kasnak BILEREK sekizgendir; yumusak golgelendirilirse koseleri erir
        # ve kubbeyle tek bir sisman kutle gibi okunur.
        drum = hz.make_tube(f"{asset_name}_Kasnak", p.hall * 0.56, p.hall * 0.54,
                            drum_h, (0.0, 0.0), z, segments=8, cap_top=False,
                            smooth=False, col=col)
        hz.assign(drum, mats["stone"])
        parts.append(drum)
        rr = p.hall * 0.54
        dh = rr * 0.78                      # Osmanli kubbesi hafif basiktir
        dome = hz.make_dome(f"{asset_name}_Kubbe", rr, dh, (0.0, 0.0), z + drum_h,
                            segments=24, rings=8, col=col)
        hz.assign(dome, mats["lead"])
        parts.append(dome)
        alem = hz.make_tube(f"{asset_name}_KubbeAlem", 0.10, 0.02, 0.9,
                            (0.0, 0.0), z + drum_h + dh, segments=8, col=col)
        hz.assign(alem, mats["lead"])
        parts.append(alem)
        total_h = z + drum_h + dh + 0.9

    # 5) Son cemaat yeri
    if p.portico:
        parts += _portico(p, mats, col, asset_name, front_y, p.plinth)

    # 6) Minare — harimin GIRIS KOSESINE gomulur.
    #
    # Minare serbest duran bir kule degildir; kaidesi duvara girer ve govde
    # oradan yukselir. Ilk denemede yandan 0,9 m aciga konmustu ve render'da
    # binadan kopuk, "yanina dikilmis" gibi duruyordu. Kaide kenarinin ucte
    # biri kadar bindirmek onu yapinin parcasi yapar.
    if p.minaret:
        mx, my = _minaret_center(p, half, front_y)
        parts += _minaret(p, mats, col, f"{asset_name}_Minare", (mx, my), 0.0)
        total_h = max(total_h, p.minaret_h)

    # --- LOD0 ---
    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)

    # --- LOD1: acikliksiz kutle + cati/kubbe + minare (siluet) ---
    l1 = [_solid(f"{asset_name}_L1_Body", (p.hall, p.hall, wall_top),
                 (0.0, 0.0, wall_top * 0.5), col, mats["plaster"])]
    if p.roof == "timber":
        rh = 0.5 * roof_w * math.tan(math.radians(p.roof_pitch_deg))
        r1 = hz.make_hip_roof(f"{asset_name}_L1_Roof", roof_w, roof_w, rh,
                              center_xy=(0.0, 0.0), base_z=wall_top,
                              ridge_axis="X", col=col)
        hz.assign(r1, mats["roof"])
        l1.append(r1)
    else:
        d1 = hz.make_dome(f"{asset_name}_L1_Kubbe", p.hall * 0.54, p.hall * 0.54 * 0.78,
                          (0.0, 0.0), wall_top + p.hall * 0.14, segments=12, rings=5, col=col)
        hz.assign(d1, mats["lead"])
        l1.append(d1)
    if p.minaret:
        # Konum LOD0 ile BIREBIR ayni olmali; ayrisirsa LOD gecisinde minare
        # yana ziplar ve bu, uzaktan bakan oyuncunun gozune carpan tek sey olur.
        mx, my = _minaret_center(p, half, front_y)
        r = max(0.42, p.minaret_h * 0.032)
        m1 = hz.make_tube(f"{asset_name}_L1_Minare", r * 1.4, r * 0.9,
                          p.minaret_h * 0.84, (mx, my), 0.0, segments=8, col=col)
        hz.assign(m1, mats["stone"])
        l1.append(m1)
        k1 = hz.make_tube(f"{asset_name}_L1_Kulah", r * 1.0, 0.0, p.minaret_h * 0.16,
                          (mx, my), p.minaret_h * 0.84, segments=8, col=col)
        hz.assign(k1, mats["lead"])
        l1.append(k1)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    # --- LOD2: yalnizca siluet ---
    l2 = [_solid(f"{asset_name}_L2_Mass", (p.hall, p.hall, wall_top),
                 (0.0, 0.0, wall_top * 0.5), col, mats["plaster"])]
    if p.roof == "timber":
        rh = 0.5 * roof_w * math.tan(math.radians(p.roof_pitch_deg))
        r2 = hz.make_hip_roof(f"{asset_name}_L2_Roof", roof_w, roof_w, rh,
                              center_xy=(0.0, 0.0), base_z=wall_top, ridge_axis="X", col=col)
        hz.assign(r2, mats["roof"])
        l2.append(r2)
    else:
        d2 = hz.make_dome(f"{asset_name}_L2_Kubbe", p.hall * 0.54, p.hall * 0.42,
                          (0.0, 0.0), wall_top, segments=8, rings=3, col=col)
        hz.assign(d2, mats["lead"])
        l2.append(d2)
    if p.minaret:
        mx, my = _minaret_center(p, half, front_y)
        r = max(0.42, p.minaret_h * 0.032)
        m2 = hz.make_tube(f"{asset_name}_L2_Minare", r * 1.3, r * 0.8, p.minaret_h,
                          (mx, my), 0.0, segments=6, col=col)
        hz.assign(m2, mats["stone"])
        l2.append(m2)
    lod2 = kit.join_parts(l2, f"SM_{asset_name}_LOD2", col)

    # --- Carpisma: yalnizca harim kutlesi. Minare ayri ve INCE olmali ki
    #     ucusta "degmedim ama carpistim" olmasin; su an tek kutle yeterli.
    ucx = hz.make_box(f"UCX_{asset_name}", (p.hall, p.hall, wall_top),
                      (0.0, 0.0, wall_top * 0.5), col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1, lod2):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx_ = hz.bounds(lod0)
    info = {
        "footprint_x": round(mx_[0] - mn[0], 3),
        "footprint_y": round(mx_[1] - mn[1], 3),
        "height": round(total_h, 3),
        "wall_top": round(wall_top, 3),
        "pivot_min_z": round(mn[2], 4),
        "tris_lod0": kit.tri_count(lod0),
        "tris_lod1": kit.tri_count(lod1),
        "tris_lod2": kit.tri_count(lod2),
        "hall": p.hall,
        "roof": p.roof,
        "minaret_h": p.minaret_h if p.minaret else 0.0,
        "palette": p.palette,
    }
    return lod0, lod1, lod2, ucx, info


def _solid(name, size, center, col, mat):
    obj = hz.make_box(name, size, center, col)
    hz.assign(obj, mat)
    return obj
