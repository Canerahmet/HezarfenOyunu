"""
**Padişah türbeleri** — Ayasofya ve Sultanahmet hazireleri. Faz 3, A-kademe.

## Bu kit neden `mahalle_kit`ten ayrı

`mahalle_kit.TurbeParams` mahalle türbesi içindir: 3 m yarıçapında,
4,6 m duvarlı, altı ya da sekizgen. Ölçekleri büyütmek yeterli olmazdı,
çünkü buradaki asıl mesele **plan çeşitliliği**: üç padişah türbesi
**üç ayrı plan** taşır ve kaynaklar bunu ayrı ayrı söyler.

| türbe | tarih | mimar | plan |
|---|---|---|---|
| **II. Selim** | 1577 | Sinan | **kare, köşeleri pahlı** (içi sekizgen galerili) |
| **III. Murad** | 1599 | Dâvud Ağa + Dalgıç Ahmed | **altıgen**, revaklı |
| **III. Mehmed** | 1604-1608 | Dalgıç Ahmed → Sedefkâr Mehmed | **sekizgen** |
| Şehzâdeler | 17. yy başı | — | küçük |
| **Sultan Ahmed** | 1619 | — | kare, revaklı |

Üçünü de düzgün sekizgen yapmak katalogda tutarlı görünürdü ve **üç
ayrı planı tek plana indirirdi** — Faz 3 boyunca kovaladığım hatanın
aynısı (yarım kubbe sayıları, ADR 0048).

`mahalle_kit`in `validate`ı kapalı türbe için `sides in (6, 8)` der; bu
**mahalle türbesinin** olgusudur. Kare-pahlı plan oraya sığmaz ve
zorlamak, kuralı taşıdığı olgudan koparmak olurdu (aynı hatayı bir kez
`karasur_kit` içinde yaptım, ADR 0049).

## Çift kabuk

II. Selim, III. Murad ve III. Mehmed türbelerinin üçü de **çift
kubbelidir** — Sinan'ın Kanûnî türbesinde kullandığı örtü. İç kabuk
dışarıdan görünmez ve **üretilmez** (Ayasofya'nın eksedralarında verilen
kararın aynısı, ADR 0045); katalog `double_shell` diye kaydeder.

## 1632'de

Dördü de ayakta. **I. Mustafa ve İbrahim türbesi 1639** — o tarihte
Ayasofya'nın vaftizhânesi hâlâ **yağhânedir** (ADR 0045). Yani 1632'de
Ayasofya haziresinde **dört** türbe vardır, beş değil.

**Kaynaklar**: TDV İslâm Ansiklopedisi "Selim II Türbesi", "Murad III
Türbesi", "Mehmed III Türbesi"; İBB Kültürel Miras. RESEARCH.md §5.19,
ADR 0054.
"""

import math

import bmesh

import hz_blender as hz
import ottoman_kit as kit
import detay_kit as dk


#: Türbe planları — üçü de ayrı ve kaynakta ayrı ayrı verilmiş.
PLANS = ("kare_pahli", "altigen", "sekizgen")


def _polygon(plan, half, chamfer=0.30):
    """
    Plan köşelerini (x, y) olarak verir.

    `kare_pahli`: kenar uzunluğu 2·half olan bir karenin köşeleri
    `chamfer`·half kadar kesilmiş hâli — **düzgün olmayan** bir sekizgen
    (dört uzun, dört kısa yüz). Düzgün sekizgenle karıştırılmamalı:
    II. Selim'in planını III. Mehmed'inkinden ayıran şey tam olarak budur.
    """
    if plan == "kare_pahli":
        c = half * chamfer
        return [(+half - c, +half), (+half, +half - c),
                (+half, -half + c), (+half - c, -half),
                (-half + c, -half), (-half, -half + c),
                (-half, +half - c), (-half + c, +half)]
    # ON YUZ -Y'YE BAKAR, kose degil.
    #
    # Ilk yazimda kose acilari `2pi*i/n + pi/n` idi ve altigende -Y'ye bir
    # KOSE dusuyordu; revak o koseye dayaniyor ve renderda yapiya
    # yapistirilmis gibi duruyordu. Bir revak bir YUZE dayanir.
    # Kaydirma: kose acilari -90 + 180/n + 360*i/n.
    n = 6 if plan == "altigen" else 8
    r = half / math.cos(math.pi / n)
    a0 = -math.pi * 0.5 + math.pi / n
    return [(r * math.cos(a0 + 2.0 * math.pi * i / n),
             r * math.sin(a0 + 2.0 * math.pi * i / n))
            for i in range(n)]


def front_face_w(plan, half, chamfer=0.30):
    """Ön (−Y) yüzün genişliği — revak ondan türer, elle girilmez."""
    if plan == "kare_pahli":
        return 2.0 * half * (1.0 - chamfer)
    n = 6 if plan == "altigen" else 8
    return 2.0 * half * math.tan(math.pi / n)


def _prism(name, poly, base_z, height, col):
    """Kapalı prizma — verilen çokgenden."""
    bm = bmesh.new()
    hz.metric_layers(bm)
    bot = [bm.verts.new((x, y, base_z)) for x, y in poly]
    top = [bm.verts.new((x, y, base_z + height)) for x, y in poly]
    bm.verts.ensure_lookup_table()
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bot[i], bot[j], top[j], top[i]))
    bm.faces.new(list(reversed(bot)))
    bm.faces.new(top)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.mesh_from_bmesh(name, bm, col)


class SultanTurbeParams(object):
    """
    Padişah türbesi — kâgir gövde, kurşun kubbe, isteğe bağlı revak.

    `half` yüz ortasına uzaklıktır (apotem). Ölçüler **D3**: haritadaki
    ayak izleri (24-30 m) revağı ve hazire duvarını da içeriyor ve
    ayrıştırılmadı; gövde ölçüleri tipolojiktir.
    """

    def __init__(self, plan, half, wall_h, dome_rise, revak=False,
                 marble=False, double_shell=True, name_note="",
                 palette="default"):
        self.plan = plan
        self.half, self.wall_h = half, wall_h
        self.dome_rise = dome_rise
        self.revak, self.marble = revak, marble
        self.double_shell = double_shell
        self.name_note = name_note
        self.palette = palette

    @property
    def sides(self):
        return 6 if self.plan == "altigen" else 8

    @property
    def crown_z(self):
        return self.wall_h + self.dome_rise

    def validate(self):
        if self.plan not in PLANS:
            raise ValueError(f"plan={self.plan} — {PLANS}")
        # KUBBE GOVDEYI ORTMELI: kubbe yaricapi apoteme yakin olmali,
        # yoksa kutle "kutunun ustunde kucuk bir tumsek" gibi okunur.
        if self.dome_rise > self.half * 1.1:
            raise ValueError(
                f"kubbe kabarmasi {self.dome_rise:.1f} m, apotem "
                f"{self.half:.1f} m — kubbe yarim kureyi asamaz")
        if self.dome_rise < self.half * 0.45:
            raise ValueError(
                f"kubbe kabarmasi {self.dome_rise:.1f} m, apotem "
                f"{self.half:.1f} m — bu kadar basik bir kubbe turbede "
                "kapak gibi okunur")
        # Turbe ANITTIR: duvari apotemden alcak olamaz, yoksa yayvan
        # bir kutu olur.
        if self.wall_h < self.half * 0.9:
            raise ValueError(
                f"duvar {self.wall_h:.1f} m, apotem {self.half:.1f} m — "
                "padisah turbesi yayvan degil ANIT gibi durur")
        return self


def build_sultan_turbe(p, col, asset_name, textured=False):
    """Padişah türbesi. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    body_mat = mats["marble"] if p.marble else mats["cutstone"]
    poly = _polygon(p.plan, p.half)

    # --- Subasman + govde ------------------------------------------------
    parts.append(hz.assign(_prism(f"Subasman_{asset_name}",
                                  _polygon(p.plan, p.half + 0.5), 0.0, 0.6,
                                  col), mats["cutstone"]))
    parts.append(hz.assign(_prism(f"Govde_{asset_name}", poly, 0.6,
                                  p.wall_h - 0.6, col), body_mat))
    # SACAK: tek bir kusak degil, KADEMELI silme. `detay_kit.silme` kutu
    # tabanlidir ve turbe cokgen planlidir; ayni kademe mantigini cokgene
    # cevirmek icin ustuste uc prizma. Tek kusak, yapiyi tepesinden duz
    # kesilmis gibi birakiyordu.
    for k, (dz, dh, dout) in enumerate(((0.00, 0.26, 0.18),
                                        (0.26, 0.26, 0.34),
                                        (0.52, 0.30, 0.50))):
        parts.append(hz.assign(
            _prism(f"Sacak{k}_{asset_name}", _polygon(p.plan, p.half + dout),
                   p.wall_h - 0.82 + dz, dh, col), mats["cutstone"]))

    # Pencereler: her yuzde iki sira (turbenin ici aydinliktir).
    n = len(poly)
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        mx_, my_ = (x0 + x1) * 0.5, (y0 + y1) * 0.5
        L = math.hypot(x1 - x0, y1 - y0)
        if L < 2.0:                       # pahli kose: pencere yok
            continue
        ang = math.atan2(y1 - y0, x1 - x0)
        # Pencere ONCE cerceve, SONRA bosluk. Cercevesiz bir dikdortgen
        # delik, duvarda kesilmis kagit gibi okunuyordu.
        nx0, ny0 = math.sin(ang), -math.cos(ang)
        for row, (zf, hh) in enumerate(((0.32, 0.20), (0.66, 0.15))):
            sove = hz.make_box(f"PencSove_{i}_{row}",
                               (L * 0.26 + 0.55, 0.30,
                                p.wall_h * hh + 0.55), (0.0, 0.0, 0.0), col)
            sove.rotation_euler = (0.0, 0.0, ang)
            sove.location = (mx_ + nx0 * 0.10, my_ + ny0 * 0.10,
                             p.wall_h * zf)
            hz.assign(sove, mats["marble"] if p.marble else mats["cutstone"])
            parts.append(sove)
            win = hz.make_box(f"Pencere_{i}_{row}", (L * 0.26, 0.6,
                                                     p.wall_h * hh),
                              (0.0, 0.0, 0.0), col)
            win.rotation_euler = (0.0, 0.0, ang)
            win.location = (mx_, my_, p.wall_h * zf)
            hz.assign(win, mats["shadow"])
            parts.append(win)
            # Sivri kemer basi: duz lentolu delik ne Osmanli ne bir sey.
            nx, ny = math.sin(ang), -math.cos(ang)
            for o in dk.kemer(f"PencKemer_{i}_{row}",
                              mx_ + nx * 0.08, my_ + ny * 0.08,
                              math.cos(ang), math.sin(ang),
                              L * 0.13, p.wall_h * zf + p.wall_h * hh * 0.5,
                              0.26, 0.40, col, steps=6):
                parts.append(hz.assign(o, body_mat))

    # KOSE SUTUNCELERI: Osmanli turbesinin kosesi keskin degildir; gomme
    # bir sutunce iki yuzu birbirinden ayirir ve dusey golge cizgisi verir.
    for i in range(n):
        cx_, cy_ = poly[i]
        d = math.hypot(cx_, cy_)
        # Kose YARICAPI `half` degildir: cokgende kose merkeze
        # `half / cos(pi/n)` uzaklıktadır. `half`e olceklemek sutunceyi
        # duvarin ICINE gomdu ve renderda yuzeyde beyaz benekler cikti —
        # gorunen sey sutunce degil, duvari delen kutunun kosesiydi.
        f = (d - 0.12) / max(1e-6, d)
        parts.append(hz.assign(
            hz.make_tube(f"KoseSutunce_{i}", 0.28, 0.28, p.wall_h - 1.0,
                         (cx_ * f, cy_ * f), 0.6, segments=8,
                         cap_top=False, cap_bottom=False, col=col),
            body_mat))

    # --- Kubbe (dis kabuk) ------------------------------------------------
    r = p.half / math.cos(math.pi / len(poly)) * 0.98
    parts.append(hz.assign(
        hz.make_tube(f"Kasnak_{asset_name}", r * 0.92, r * 0.88, 1.4,
                     (0.0, 0.0), p.wall_h, segments=len(poly),
                     cap_top=False, col=col), mats["cutstone"]))
    parts.append(hz.assign(
        hz.make_dome(f"Kubbe_{asset_name}", r * 0.90, p.dome_rise,
                     (0.0, 0.0), p.wall_h + 1.4, segments=20, rings=6,
                     col=col), mats["lead"]))
    for o in dk.kubbe_kaburga(f"KubbeDikis_{asset_name}", 0.0, 0.0, r * 0.90,
                              p.wall_h + 1.4, p.dome_rise, col, n=16,
                              w=0.10, steps=5):
        parts.append(hz.assign(o, mats["lead"]))
    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0,
                     p.wall_h + 1.4 + p.dome_rise, col, scale=0.9):
        parts.append(hz.assign(o, mats["lead"]))

    # --- Revak (III. Murad ve Sultan Ahmed) -------------------------------
    revak_bays = 0
    if p.revak:
        # Revak genisligi ON YUZDEN turer: bir revak dayandigi yuzden
        # genis olamaz (biraz tasar, o kadar).
        rw = front_face_w(p.plan, p.half) * 1.12
        rd, rh = 4.6, p.wall_h * 0.58
        ry = -(p.half + rd * 0.5)
        revak_bays = 3
        parts += dk.revak_sirasi(
            mats, col, f"Revak_{asset_name}",
            -rw * 0.5, ry - rd * 0.5, rw * 0.5, ry - rd * 0.5,
            revak_bays, 0.6, rh, 0.40,
            bay=rd, bay_dir=(0.0, 1.0), spandrel_h=1.1)

    # --- Kapi -------------------------------------------------------------
    # KAPI: mukarnas kavsarali kucuk tackapi. Padisah turbesinin kapisi
    # bir delik degildir; camininkiyle ayni dili konusur, kucuguyle.
    parts += dk.tackapi(mats, col, f"Kapi_{asset_name}",
                        0.0, -p.half, 0.0,
                        front_face_w(p.plan, p.half) * 0.62,
                        p.wall_h * 0.66, 1.1,
                        kapi_w=2.2, kapi_h=3.8, sutunce=False,
                        mihrabiye=True)

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(_prism(f"L1_{asset_name}", poly, 0.0, p.wall_h, col),
                        body_mat))
    l1.append(hz.assign(
        hz.make_dome(f"L1_Kubbe_{asset_name}", r * 0.90, p.dome_rise,
                     (0.0, 0.0), p.wall_h + 1.4, segments=10, rings=3,
                     col=col), mats["lead"]))

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

    # PLANIN GERCEKTEN FARKLI OLDUGUNU MESH'TEN OLC: kare-pahli sekizgen
    # DUZGUN DEGILDIR ve bunu yuz uzunluklarinin yayilimindan gorursun.
    lens = []
    for i in range(len(poly)):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % len(poly)]
        lens.append(math.hypot(x1 - x0, y1 - y0))
    spread = (max(lens) - min(lens)) / max(lens)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="turbe_selatin", plan=p.plan, palette=p.palette,
                status="draft", accuracy="D3",
                sides=len(poly), face_spread=round(spread, 3),
                walls=True, acik=False, revak=p.revak,
                revak_bays=revak_bays, marble=p.marble,
                double_shell=p.double_shell,
                wall_h=round(p.wall_h, 2),
                dome_crown_z=round(p.crown_z + 1.4, 2))
    return lod0, lod1, ucx, info
