"""
**Bedestenler** — Fatih vakfı, ~1461; 1632'de ayakta. Faz 3, A-kademe.

## Sayılar birbirini tutuyor ve bu tesadüf değil

Bedesten bir **ızgaradır**: kubbeler sıra sıra dizilir, kubbeleri taşıyan
ayaklar da ızgaranın iç düğümlerinde durur. Yani kubbe sayısı, ayak
sayısı ve dikdörtgenin oranı **birbirinden bağımsız değildir**:

```
kubbe  = sütun × satır
ayak   = (sütun − 1) × (satır − 1)
```

Kaynaklar üçünü de ayrı ayrı veriyor ve üçü de tutuyor:

| | ölçü | kubbe | ayak | ızgara |
|---|---|---|---|---|
| **Cevahir (İç) Bedesten** | 45,30 × 29,50 m | **15** | **8** | 5 × 3 |
| **Sandal Bedesteni** | 40 × 32 m | **20** | **12** | 5 × 4 |

5×3 = 15 ve 4×2 = 8. 5×4 = 20 ve 4×3 = 12. Dahası ızgara **ölçüyle de**
tutuyor: Cevahir'in gözü 9,06 × 9,83 m, Sandal'ınki 8,00 × 8,00 m —
ikisi de kareye yakın, ki kubbeli bir göz zaten kare ister.

Bu, projede ilk kez **üç bağımsız sayının bir geometriyi kapatması**.
`validate` ilişkiyi denetliyor: biri değişirse öteki ikisi de değişmek
zorunda.

## 1632'de Kapalıçarşı **bu değildir**

Bugün "Kapalıçarşı" denince akla gelen kâgir tonozlu sokaklar ağı
**sonradır**. 17. yüzyılda bedestenlerin arasındaki sokaklar **ahşap**
örtülüydü; bugünkü kâgir örtü büyük yangınlardan (1701) ve 1894
depreminden sonraki onarımların eseridir.

Üstelik **1618 yangını** 1632'den yalnızca on dört yıl öncedir: oyunun
geçtiği yılda çarşı **yakın zamanda yeniden kurulmuş** bir yerdir.

Bu yüzden burada yalnızca **iki bedesten** üretiliyor — onlar kâgirdir,
ölçülüdür ve 1632'de ayaktadır. Çevredeki çarşı dokusu Faz 4'ün işi.

**Kaynaklar**: Vikipedi "Sandal Bedesteni"; Discover Islamic Art
(Kapalıçarşı); Osmanlı Tarihi Ansiklopedisi "Kapalı Çarşı".
RESEARCH.md §5.18, ADR 0053.
"""

import math

import hz_blender as hz
import ottoman_kit as kit
import detay_kit as dk
import street_kit as sk


#: Cevahir (İç / Eski) Bedesten — **ölçülü**.
CEV_W, CEV_D = 45.30, 29.50
CEV_COLS, CEV_ROWS = 5, 3
#: Kubbe kilidi (m) — **ölçülü**.
CEV_CROWN = 14.89

#: Sandal Bedesteni — **ölçülü** plan, kilit **türetildi**.
SAN_W, SAN_D = 40.00, 32.00
SAN_COLS, SAN_ROWS = 5, 4
#: Cevahir'in kilit/göz oranından (14,89 / 9,44 = 1,58) türedi — **D3**.
SAN_CROWN = 12.60

#: Bedestenin **dört** kapısı vardır, her cephede birer.
BD_DOORS = 4


class BedestenParams(object):
    """
    Bedesten — ızgara planlı, kubbeli kâgir kapalı çarşı.

    Kubbe sayısı, ayak sayısı ve ölçü **birbirini kapatır**; üçünden
    ikisi ötekini belirler ve `validate` bunu denetler.
    """

    def __init__(self, width, depth, cols, rows, crown_z, doors=BD_DOORS,
                 palette="default"):
        self.width, self.depth = width, depth
        self.cols, self.rows = cols, rows
        self.crown_z = crown_z
        self.doors = doors
        self.palette = palette

    @property
    def domes(self):
        return self.cols * self.rows

    @property
    def piers(self):
        """İç düğümler — ızgaranın kendisinden çıkar, sayılmaz."""
        return (self.cols - 1) * (self.rows - 1)

    @property
    def bay_w(self):
        return self.width / self.cols

    @property
    def bay_d(self):
        return self.depth / self.rows

    @property
    def wall_h(self):
        """Duvar üstü = kubbe eteği. Kubbe yarım küre olduğuna göre
        kilit − göz yarıçapı."""
        return self.crown_z - min(self.bay_w, self.bay_d) * 0.5

    def validate(self):
        if self.doors != BD_DOORS:
            raise ValueError(f"{self.doors} kapi — bedestenin DORT kapisi var")
        # GOZ KAREYE YAKIN olmali: kubbeli bir goz kare ister ve bu,
        # izgaranin dogru secildiginin denetimidir.
        oran = self.bay_w / self.bay_d
        if not (0.80 <= oran <= 1.25):
            raise ValueError(
                f"goz {self.bay_w:.2f} x {self.bay_d:.2f} m (oran "
                f"{oran:.2f}) — kubbeli goz KAREYE YAKIN olmali; izgara "
                "yanlis secilmis demektir")
        if self.wall_h < 6.0:
            raise ValueError(
                f"duvar {self.wall_h:.2f} m — kilit ({self.crown_z}) gozun "
                "yaricapindan bu kadar az yukselirse kubbe yere oturur")
        return self


def _pencere(parts, mats, col, cx, cy, ux, uy, z, w, hh, tag):
    """Kemerli pencere: söve + boşluk + sivri kemer başı."""
    nx, ny = uy, ux
    parts.append(hz.assign(hz.make_box(
        f"PencSove_{tag}", (w + 0.7 if ux else 0.5, 0.5 if ux else w + 0.7,
                            hh + 0.7), (cx, cy, z + hh * 0.5), col),
        mats["cutstone"]))
    parts.append(hz.assign(hz.make_box(
        f"Penc_{tag}", (w if ux else 0.6, 0.6 if ux else w, hh),
        (cx, cy, z + hh * 0.5), col), mats["shadow"]))
    for o in dk.kemer(f"PencKemer_{tag}", cx, cy, ux, uy, w * 0.5,
                      z + hh, 0.26, 0.55, col, steps=6):
        parts.append(hz.assign(o, mats["cutstone"]))


def build_bedesten(p, col, asset_name, textured=False):
    """Bedesten. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D, h = p.width, p.depth, p.wall_h
    t = 1.4

    # --- Kagir kabuk ------------------------------------------------------
    parts.append(hz.assign(hz.make_box(f"Govde_{asset_name}", (W, D, h),
                                       (0.0, 0.0, h * 0.5), col),
                           mats["stone"]))

    # --- PAYANDALAR -------------------------------------------------------
    #
    # Bedestenin disini tanimlayan sey payandadir: ic ayaklarin tasidigi
    # kemerler duvara nerede iniyorsa disarida oraya bir payanda gelir.
    # Yerleri UYDURULMUS degil, `cols`/`rows` izgarasindan TURER — sayilan
    # deger ne kadar ic mekanı belirliyorsa dis cepheyi de o kadar belirler.
    # Payanda olmadan bedesten, ustune kubbe dizilmis bir ambar kutusuydu.
    pw, pj = 1.5, 0.9              # payanda genisligi / tasmasi
    npay = 0
    for i in range(p.cols + 1):
        px = -W * 0.5 + p.bay_w * i
        for sy in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"Payanda_X{i}_{sy}", (pw, pj, h * 0.92),
                (px, sy * (D * 0.5 + pj * 0.5), h * 0.46), col),
                mats["stone"]))
            npay += 1
    for j in range(p.rows + 1):
        py = -D * 0.5 + p.bay_d * j
        for sx in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"Payanda_Y{j}_{sx}", (pj, pw, h * 0.92),
                (sx * (W * 0.5 + pj * 0.5), py, h * 0.46), col),
                mats["stone"]))
            npay += 1
    if npay != 2 * (p.cols + p.rows + 2):
        raise ValueError(f"payanda {npay} — izgaradan turemiyor")

    # --- KEMERLI PENCERE SIRASI: payandalarin ARASINA ---------------------
    # Bedesten kapali bir yapidir; isik duvarin UST kusagindan girer.
    #
    # Denizlik kotu TURETILIR: kemerin tepesi silmenin altinda kalmali.
    # Elle "h * 0.70" yazdigimda kemerler saçagi deldi ve duvarin ustunde
    # havada kemer parcalari kaldi — kot, bagli oldugu seyden okunmali.
    pen_w = min(2.2, min(p.bay_w, p.bay_d) * 0.30)
    pen_h = 2.6
    kabarma = pen_w * 0.5 * math.sqrt(1.0 + 2.0 * sk.ARCH_C)
    pen_z = (h - 0.9) - kabarma - pen_h - 0.55
    for i in range(p.cols):
        px = -W * 0.5 + p.bay_w * (i + 0.5)
        for sy in (-1, 1):
            _pencere(parts, mats, col, px, sy * D * 0.5, 1.0, 0.0,
                     pen_z, pen_w, pen_h, f"X{i}_{sy}")
    for j in range(p.rows):
        py = -D * 0.5 + p.bay_d * (j + 0.5)
        for sx in (-1, 1):
            _pencere(parts, mats, col, sx * W * 0.5, py, 0.0, 1.0,
                     pen_z, pen_w, pen_h, f"Y{j}_{sx}")

    # DORT KAPI — her cephede birer. Bedestenin kapalilıgı onun tanimidir:
    # kiymetli mal saklanan yerdir ve gece kilitlenir; az ve belirgin kapi
    # o islevin kendisidir.
    door_w, door_h = 3.0, 5.2
    for i, (dx, dy, sw, sd) in enumerate((
            (0.0, -D * 0.5, door_w, 0.8), (0.0, +D * 0.5, door_w, 0.8),
            (-W * 0.5, 0.0, 0.8, door_w), (+W * 0.5, 0.0, 0.8, door_w))):
        parts.append(hz.assign(
            hz.make_box(f"Kapi_{i}", (sw, sd, door_h),
                        (dx, dy, door_h * 0.5), col), mats["shadow"]))
        parts.append(hz.assign(
            hz.make_box(f"KapiSove_{i}", (sw + 1.6, sd + 1.6, door_h + 1.2),
                        (dx, dy, (door_h + 1.2) * 0.5), col), mats["cutstone"]))
        parts.append(hz.assign(
            hz.make_box(f"KapiAcik_{i}", (sw, sd + 0.4, door_h),
                        (dx, dy, door_h * 0.5), col), mats["shadow"]))

    # Ust kusak: kubbelerin oturdugu kot. Tek bir kutu yerine KADEMELI
    # silme — duvarin bittigi yer bir cizgiyle degil bir golgeyle biter.
    for o in dk.silme(f"Kusak_{asset_name}", W, D, h - 0.9, col,
                      steps=3, h=0.90, out=0.62):
        parts.append(hz.assign(o, mats["cutstone"]))

    # --- IZGARA KUBBELER --------------------------------------------------
    r = min(p.bay_w, p.bay_d) * 0.46
    ndome = 0
    for i in range(p.cols):
        for j in range(p.rows):
            cx = -W * 0.5 + p.bay_w * (i + 0.5)
            cy = -D * 0.5 + p.bay_d * (j + 0.5)
            # Kasnak: kubbeyi duvardan ayirir; yoksa cati tek parca bir
            # tumsek yigini gibi okunur.
            parts.append(hz.assign(
                hz.make_tube(f"Kasnak_{i}{j}", r * 1.08, r * 1.05, 1.1,
                             (cx, cy), h, segments=12, cap_top=False,
                             col=col), mats["cutstone"]))
            parts.append(hz.assign(
                hz.make_dome(f"Kubbe_{i}{j}", r, p.crown_z - h - 1.1,
                             (cx, cy), h + 1.1, segments=14, rings=5,
                             col=col), mats["lead"]))
            for o in dk.kubbe_kaburga(f"KubbeDikis_{i}{j}", cx, cy, r,
                                      h + 1.1, p.crown_z - h - 1.1, col,
                                      n=12, w=0.09, steps=5):
                parts.append(hz.assign(o, mats["lead"]))
            ndome += 1
    if ndome != p.domes:
        raise ValueError(f"{ndome} kubbe uretildi, {p.domes} beklenmisti")

    l1.append(hz.assign(hz.make_box(f"L1_{asset_name}", (W, D, h),
                                    (0.0, 0.0, h * 0.5), col), mats["stone"]))
    for i in range(p.cols):
        for j in range(p.rows):
            l1.append(hz.assign(
                hz.make_dome(f"L1_Kubbe_{i}{j}", r, p.crown_z - h - 1.1,
                             (-W * 0.5 + p.bay_w * (i + 0.5),
                              -D * 0.5 + p.bay_d * (j + 0.5)), h + 1.1,
                             segments=8, rings=3, col=col), mats["lead"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="bedesten", palette=p.palette, status="draft",
                accuracy="D2", width=p.width, depth=p.depth,
                cols=p.cols, rows=p.rows, domes=p.domes, piers=p.piers,
                doors=p.doors, dome_crown_z=round(p.crown_z, 2),
                bay_w=round(p.bay_w, 2), bay_d=round(p.bay_d, 2),
                wall_h=round(p.wall_h, 2))
    return lod0, lod1, ucx, info
