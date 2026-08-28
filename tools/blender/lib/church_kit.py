"""
Hezarfen: 1632 — Kilise ve sinagog kiti (plan Faz 2b).

Galata'nın yarısı, Fener'in ve Balat'ın tamamı bu iki yapı olmadan
kurulamaz. Ama "kilise" tek bir tip değildir: 1632 İstanbul'unda **iki ayrı
hukukî durum** iki ayrı mimarî üretir ve fark siluetten okunur.

## Neden iki tip

**Galata Latin kilisesi** (`kind="latin"`) — Ceneviz kolonisi 1453'te
*antlaşmayla* teslim oldu; oradaki Katolik kiliseleri fethe önceleyen
yapılardır ve biçimlerini korudular. San Domenico (bugün Arap Camii) üç
nefli, sivri kemer pencereli, **ahşap çatılı** İtalyan Gotiği bir bazilikadır;
orta nef yan neflerden yüksektir ve **kare planlı bir çan kulesi** vardır
(sonradan minareye çevrilen kule budur). Yani Galata'da kule serbesttir.

**Suriçi/Fener/Balat Rum-Ermeni kilisesi** (`kind="orthodox"`) — burada zimmî
kısıtı işler: yeni yapı gösterişsiz olmak zorundadır. Tip, üç nefi **tek bir
ahşap beşik çatı** altında toplayan, sokaktan alçak okunan sade kâgir
dikdörtgendir; çan kulesi yoktur. Fener'deki Patrikhane kilisesi (1601'den
beri orada) "mütevazı dış görünüşü" ile tarif edilir.

Fark üç ölçüde toplanır: **kule var/yok**, **orta nef yükseltisi var/yok**,
ve gövde yüksekliği. Aynı kutuyu iki kez boyamak yerine bu üçü parametredir.

## Sinagog

Balat ve Hasköy sinagogları "kendine özgü bir mimarî tipoloji"ye sahip
değildir: dikdörtgen ya da kare planlı, taş ve ahşap, **yüksek duvarlı bir
avlunun ya da bahçenin içinde**. Yani sinagogu sinagog yapan şey cephesi
değil, **cephesinin olmaması**dır — sokaktan bir eve benzer, kapısı avluya
bakar. Bu yüzden burada kubbe, kule ya da kemerli pencere YOKTUR; pencereler
dikdörtgen ve şebekelidir, çatı mahallenin geri kalanı gibi alaturka kırma
çatıdır. Avlu duvarı ayrı bir varlıktır (`street_kit.build_avlu`) ve Unity
yerleştiricisi kurar.

## Bilinçli boşluk: gömük zemin

Kaynaklar zimmî kiliselerinin kısıtı aşmak için "ahşap ve yarı gömük"
kaldığını söyler — iç yükseklik zemini kazarak kazanılır. Bu **iç mekân**
özelliğidir; iç mekânlar henüz yok ve arazide çukur açmak taş kaide
sistemiyle çelişir. Bu yüzden burada yalnızca **dış sonucu** modellenir:
planına göre fazla alçak duran gövde. `sink` alanı bilgi olarak taşınır,
geometriye girmez. Bkz. ADR 0018.

Eksen sözleşmesi kitin geri kalanıyla aynı: giriş cephesi −Y (Unity'de +Z),
apsis +Y'de (yani mihrap yönü değil, **doğu**; yerleştirici döndürür).
"""

import bmesh

import hz_blender as hz
import ottoman_kit as kit
import street_kit as sk

KINDS = ("latin", "orthodox")


# ------------------------------------------------------------------ yardımcı

def _shed(name, y0, y1, x_high, x_low, z_high, z_low, thick, col):
    """Tek eğimli örtü (yan nef sundurma çatısı) — X yönünde alçalır."""
    bm = bmesh.new()
    top = [(x_high, y0, z_high), (x_high, y1, z_high),
           (x_low, y1, z_low), (x_low, y0, z_low)]
    vt = [bm.verts.new(v) for v in top]
    vb = [bm.verts.new((x, y, z - thick)) for (x, y, z) in top]
    bm.verts.ensure_lookup_table()
    bm.faces.new(vt)
    bm.faces.new(list(reversed(vb)))
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((vt[i], vt[j], vb[j], vb[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def gable_roof(name, width, depth, height, center_xy, base_z, thick, col):
    """
    Beşik çatının **örtüsü** — mahya boyunca (Y) iki eğimli levha.

    Neden dolu bir prizma değil: prizmanın iki ucu düşey üçgendir ve kiremit
    malzemesi alır. İlk denemede tam bunu yaptım ve cephe **kiremitten bir
    alınlıkla** çıktı — beşik çatının alınlığı duvardır (`_gable_wall`), çatı
    yalnızca onun üstünden aşar. Hata render'da anında okunuyordu ama ölçüsü
    şuydu: alınlık üçgeni duvarla aynı malzemede olmak zorunda.
    """
    hw = width * 0.5
    cx, cy = center_xy
    y0, y1 = cy - depth * 0.5, cy + depth * 0.5
    out = []
    for s in (-1, 1):
        out.append(_shed(f"{name}{'S' if s < 0 else 'D'}", y0, y1,
                         cx, cx + s * hw, base_z + height, base_z, thick, col))
    return out


def _gable_wall(name, width, height, y_center, thickness, base_z, col):
    """Alınlık duvarı — çatı eğimini izleyen üçgen kâgir uç."""
    hw, ht = width * 0.5, thickness * 0.5
    bm = bmesh.new()
    faces = []
    for n in (-ht, ht):
        tri = [bm.verts.new((-hw, y_center + n, base_z)),
               bm.verts.new((hw, y_center + n, base_z)),
               bm.verts.new((0.0, y_center + n, base_z + height))]
        faces.append(tri)
    bm.verts.ensure_lookup_table()
    a, b = faces
    bm.faces.new(a)
    bm.faces.new(list(reversed(b)))
    for i in range(3):
        j = (i + 1) % 3
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def _put(parts, obj, mat):
    hz.assign(obj, mat)
    parts.append(obj)
    return obj


def _bay_spans(length, bays, opening_w):
    """Uzunluk boyunca `bays` adet eşit açıklığın (u0, u1) listesi."""
    pitch = length / bays
    half = opening_w * 0.5
    return [(-length * 0.5 + (i + 0.5) * pitch - half,
             -length * 0.5 + (i + 0.5) * pitch + half) for i in range(bays)]


# ------------------------------------------------------------------- kilise

class KiliseParams(object):
    def __init__(self, **kw):
        self.kind = kw.get("kind", "latin")
        self.nave_w = kw.get("nave_w", 7.2)          # orta nef ic genisligi
        self.aisle_w = kw.get("aisle_w", 3.6)        # yan nef ic genisligi
        self.length = kw.get("length", 24.0)         # ic uzunluk (apsis haric)
        self.bays = kw.get("bays", 5)
        self.wall_t = kw.get("wall_t", 0.75)

        self.aisle_h = kw.get("aisle_h", 6.0)        # yan nef duvar yuksekligi
        self.nave_h = kw.get("nave_h", 10.2)         # latin: clerestory tepesi
        self.sink = kw.get("sink", 0.0)              # bilgi; geometriye girmez

        self.window_w = kw.get("window_w", 0.90)
        self.window_sill = kw.get("window_sill", 2.20)
        self.window_spring = kw.get("window_spring", 4.40)
        self.portal_w = kw.get("portal_w", 1.90)
        self.portal_sill = kw.get("portal_sill", 0.22)
        self.portal_spring = kw.get("portal_spring", 2.50)

        self.tower = kw.get("tower", None)           # None -> tipe gore
        self.tower_h = kw.get("tower_h", 17.0)
        self.tower_side = kw.get("tower_side", -1)
        self.tower_w = kw.get("tower_w", 3.60)
        # Haç: VARSAYILAN KAPALI — Caner, 2026-08-20, Karar 5 = B.
        #
        # "zimmî gösterişsizliği kuralını her cemaate uygula". Kaynağım zaten
        # aksi yönde güçlü değildi: Galata'da dışarıdan haç gösteriminin 1632
        # uygulaması doğrulanamadı. Parametre duruyor (`cross=True`), çünkü
        # belge çıkarsa karar geri alınabilir olmalı — ama artık ONU İSTEMEK
        # gerekir, sessizce gelmez.
        self.cross = kw.get("cross", False)
        # ŞEREFE — kule minareye çevrildiğinde eklenen ezan balkonu.
        #
        # Arap Camii'nin minaresi yeni bir yapı değildir: San Domenico'nun
        # KARE PLANLI ÇAN KULESİDİR (RESEARCH §4.2(a): "sonradan minareye
        # çevrilen kule budur"). Yani dönüşüm kuleyi yıkıp yerine silindir
        # dikmek değil; haçı indirmek, şerefe eklemek, külahı kurşunlamak.
        # İstanbul'un en tanınır siluetlerinden biri tam bu yüzden: bir
        # İtalyan çan kulesi, minare olarak.
        self.serefe = kw.get("serefe", False)
        self.apse = kw.get("apse", True)
        self.apse_r = kw.get("apse_r", 3.40)
        self.palette = kw.get("palette", None)       # None -> tipe gore

    def resolve(self):
        """Tipin dayattığı varsayılanları uygular."""
        if self.kind not in KINDS:
            raise ValueError(f"kind={self.kind} (secenekler: {KINDS})")
        latin = self.kind == "latin"
        if self.tower is None:
            self.tower = latin
        if self.palette is None:
            self.palette = "default" if latin else "nonmuslim"
        return self

    def validate(self):
        self.resolve()
        errs = []
        if self.kind == "orthodox":
            if self.tower:
                errs.append("orthodox tipte can kulesi olamaz (zimmi kisiti) — "
                            "Galata icin kind='latin' kullan")
            if self.nave_h > self.aisle_h + 0.01:
                errs.append(f"orthodox tipte orta nef yukseltisi yok: "
                            f"nave_h ({self.nave_h}) = aisle_h ({self.aisle_h}) olmali")
        else:
            if self.nave_h < self.aisle_h + 2.0:
                errs.append(f"latin bazilikada orta nef yan neften en az 2 m "
                            f"yuksek olmali ({self.nave_h} vs {self.aisle_h})")
        if self.serefe and not self.tower:
            errs.append("serefe kulesiz olamaz — serefe kulenin balkonudur")
        if self.serefe and self.cross:
            errs.append("serefe ve hac ayni kulede olamaz: kule ya can "
                        "kulesidir ya minare")
        pitch = self.length / max(1, self.bays)
        if self.window_w > pitch - 1.0:
            errs.append(f"window_w={self.window_w} bu ritimde ({pitch:.2f} m) "
                        f"ayak birakmiyor")
        if self.apse and self.apse_r > (self.nave_w * 0.5 + self.aisle_w):
            errs.append(f"apse_r={self.apse_r} govdeden genis")
        if errs:
            raise ValueError("KiliseParams gecersiz: " + "; ".join(errs))
        return self

    # --- turetilmis dis olculer -------------------------------------------
    @property
    def outer_w(self):
        return self.nave_w + 2.0 * self.aisle_w + 2.0 * self.wall_t

    @property
    def outer_l(self):
        return self.length + 2.0 * self.wall_t


def _window_row(mats, col, name, parts, panel_w, panel_h, thickness, origin,
                u_axis, n_axis, n_win, win_w, sill, spring, spread, body,
                grille=True):
    """Kemerli pencere sırası + arkasındaki karanlık + şebeke."""
    spans = _bay_spans(spread, n_win, win_w)
    _put(parts, sk.arched_panel(name, panel_w, panel_h, thickness, origin,
                                u_axis, n_axis, spans=spans, sill_z=sill,
                                spring_z=spring, col=col), body)
    oh = (spring - sill) + win_w * 0.45
    for (u0, u1) in spans:
        cu = (u0 + u1) * 0.5
        _put_shadow(parts, mats, col, name, origin, u_axis, n_axis,
                    cu, sill, oh, win_w, thickness)
        if grille:
            for bar in sk.iron_grille(f"{name}_Sebeke", win_w, oh, origin,
                                      u_axis, n_axis, cu, sill + oh * 0.5,
                                      thickness, mats, col):
                parts.append(bar)


def _shell(p, mats, col, name, parts, height, win_sill, win_spring, win_w,
           portal=True, front_split_z=None):
    """
    Dış kabuk: iki yan duvar + arka duvar + delikli ön cephe.

    `front_split_z` verilirse ön cephe İKİ panele bölünür: altta taç kapı,
    üstte pencere sırası. Tek panelde ikisi olamaz — `arched_panel` bir panelde
    tek basma kotu kabul eder (gerekçe orada). Bölmenin mimarî karşılığı da
    doğrudur: cephe iki kat sırasıdır, tek yüzey değil.
    """
    W, L, t = p.outer_w, p.outer_l, p.wall_t
    body = mats["stone"]

    for s in (-1, 1):
        _window_row(mats, col, f"{name}_YanDuvar{'S' if s < 0 else 'D'}", parts,
                    L, height, t, (s * (W * 0.5 - t * 0.5), 0.0, 0.0),
                    (0.0, 1.0), (float(s), 0.0), p.bays, win_w,
                    win_sill, win_spring, L, body)

    inner_w = W - 2.0 * t
    back = hz.make_box(f"{name}_ArkaDuvar", (inner_w, t, height),
                       (0.0, L * 0.5 - t * 0.5, height * 0.5), col)
    _put(parts, back, body)

    if portal:
        fh = float(front_split_z) if front_split_z else height
        front = sk.arched_panel(
            f"{name}_OnCephe", inner_w, fh, t,
            (0.0, -L * 0.5 + t * 0.5, 0.0), (1.0, 0.0), (0.0, -1.0),
            spans=[(-p.portal_w * 0.5, p.portal_w * 0.5)],
            sill_z=p.portal_sill, spring_z=p.portal_spring, col=col)
        _put(parts, front, body)
        _portal_dress(p, mats, col, name, parts, -L * 0.5 + t * 0.5, t)
        if front_split_z:
            ph = height - fh
            _window_row(mats, col, f"{name}_OnUstSira", parts, inner_w, ph, t,
                        (0.0, -L * 0.5 + t * 0.5, fh), (1.0, 0.0), (0.0, -1.0),
                        3, win_w * 0.80, ph * 0.24, ph * 0.60,
                        inner_w * 0.62, body)
    else:
        front = hz.make_box(f"{name}_OnCephe", (inner_w, t, height),
                            (0.0, -L * 0.5 + t * 0.5, height * 0.5), col)
        _put(parts, front, body)


def _put_shadow(parts, mats, col, name, origin, u_axis, n_axis, cu, cv, oh,
                ow, thick):
    """Açıklığın arkasındaki karanlık levha — boşluk boşluk gibi okunsun diye."""
    ox, oy, oz = origin
    n = -thick * 0.5 + 0.05
    px = ox + u_axis[0] * cu + n_axis[0] * n
    py = oy + u_axis[1] * cu + n_axis[1] * n
    sz = (ow, 0.05, oh) if abs(u_axis[0]) > 0.5 else (0.05, ow, oh)
    obj = hz.make_box(f"{name}_Karanlik", sz, (px, py, oz + cv + oh * 0.5), col)
    _put(parts, obj, mats["shadow"])


def _portal_dress(p, mats, col, name, parts, y_wall, t):
    """Taç kapı: kesme taş söve + basamak + ahşap kanat."""
    pw, ps = p.portal_w, p.portal_spring
    _, rise = sk.arch_points(pw * 0.5, ps)
    top = ps + rise
    y_out = y_wall - t * 0.5

    # Sove: acikligin iki yaninda ve ustunde kesme tas kusak.
    for s in (-1, 1):
        _put(parts, hz.make_box(
            f"{name}_PortalSove", (0.30, 0.14, top - p.portal_sill + 0.30),
            (s * (pw * 0.5 + 0.15), y_out - 0.07,
             p.portal_sill + (top - p.portal_sill + 0.30) * 0.5), col),
            mats["cutstone"])
    _put(parts, hz.make_box(
        f"{name}_PortalAlinlik", (pw + 0.90, 0.16, 0.34),
        (0.0, y_out - 0.08, top + 0.32), col), mats["cutstone"])

    # Karanlik + ahsap kanat.
    _put(parts, hz.make_box(
        f"{name}_KapiKaranlik", (pw, 0.05, ps - p.portal_sill),
        (0.0, y_wall + t * 0.5 - 0.05, p.portal_sill + (ps - p.portal_sill) * 0.5),
        col), mats["shadow"])
    _put(parts, hz.make_box(
        f"{name}_KapiKanat", (pw - 0.10, 0.09, ps - p.portal_sill - 0.06),
        (0.0, y_out - 0.05,
         p.portal_sill + (ps - p.portal_sill - 0.06) * 0.5), col), mats["timber"])

    # Basamak: yukseklikten TURETILIR, elle sayilmaz.
    n = max(1, int(round(p.portal_sill / 0.18)))
    for i in range(n):
        z = p.portal_sill * (i + 1) / n
        d = 0.34 * (n - i)
        _put(parts, hz.make_box(
            f"{name}_Basamak{i}", (pw + 1.10, d, z),
            (0.0, y_out - d * 0.5, z * 0.5), col), mats["cutstone"])


def _apse(p, mats, col, name, parts, height):
    """Yarım çokgen apsis + yarım konik örtü. Yarısı gövdenin içinde kalır."""
    y = p.outer_l * 0.5
    body = hz.make_tube(f"{name}_Apsis", p.apse_r, p.apse_r, height,
                        (0.0, y), 0.0, segments=10, cap_top=True,
                        smooth=False, col=col)
    _put(parts, body, mats["stone"])
    # Apsis örtüsü YARIM KUBBEdir, konik külah değil.
    #
    # İlk üretimde koni kullanıldı — kolaydı ama yanlış: apsisin üstünü örten
    # şey yarım kubbedir (konka), ve konik bir külah apsisi kule kaidesine
    # benzetiyordu. Kubbe gövdenin yarısı duvarın içinde kaldığı için de
    # bedeli yok.
    dome = hz.make_dome(f"{name}_ApsisKonka", p.apse_r + 0.22,
                        p.apse_r * 0.72, (0.0, y), height,
                        segments=10, rings=6, col=col)
    _put(parts, dome, mats["roof"])


def _tower(p, mats, col, name, parts):
    """
    Çan kulesi — kare planlı, çan katı dört yönden kemerli.

    Galata'ya özgüdür: antlaşmayla korunan Ceneviz kiliselerinin kulesi
    yerinde kaldı (San Domenico'nunki sonradan minare oldu). Suriçi kilisesine
    kule koymak dönem hatasıdır; `validate` bunu reddeder.
    """
    s = p.tower_w
    x = p.tower_side * (p.outer_w * 0.5 + s * 0.5 - 0.45)
    y = -p.outer_l * 0.5 + s * 0.5
    belfry_h = s
    shaft_h = p.tower_h - belfry_h

    _put(parts, hz.make_box(f"{name}_KuleGovde", (s, s, shaft_h),
                            (x, y, shaft_h * 0.5), col), mats["stone"])
    _put(parts, hz.make_box(f"{name}_KuleSilme", (s + 0.28, s + 0.28, 0.22),
                            (x, y, shaft_h + 0.11), col), mats["cutstone"])

    # Can kati: dort kemerli panel bir kutu olusturur.
    t = 0.50
    z0 = shaft_h + 0.22

    # SEREFE — kule minare olduysa (bkz. KiliseParams.serefe).
    if getattr(p, "serefe", False):
        # Kademeli bilezik: govdeden tasan iki silme, ustunde doseme,
        # cevresinde korkuluk. Mukarnas soyutlanmistir; okunmasi gereken
        # sey balkonun KENDISI, isciligi degil.
        for i, (buyume, kalinlik) in enumerate(((0.30, 0.16), (0.62, 0.18))):
            _put(parts, hz.make_box(f"{name}_SerefeBilezik{i}",
                                    (s + buyume, s + buyume, kalinlik),
                                    (x, y, shaft_h - 0.34 + i * 0.17), col),
                 mats["cutstone"])
        gen = s + 1.10
        _put(parts, hz.make_box(f"{name}_SerefeDoseme", (gen, gen, 0.16),
                                (x, y, shaft_h + 0.08), col), mats["cutstone"])
        kh, kt = 0.92, 0.14
        for sx in (-1, 1):
            _put(parts, hz.make_box(f"{name}_SerefeKorkulukX",
                                    (kt, gen, kh),
                                    (x + sx * (gen * 0.5 - kt * 0.5), y,
                                     shaft_h + 0.16 + kh * 0.5), col),
                 mats["cutstone"])
        for sy in (-1, 1):
            _put(parts, hz.make_box(f"{name}_SerefeKorkulukY",
                                    (gen - 2.0 * kt, kt, kh),
                                    (x, y + sy * (gen * 0.5 - kt * 0.5),
                                     shaft_h + 0.16 + kh * 0.5), col),
                 mats["cutstone"])
    ow = s * 0.42
    spring = belfry_h * 0.55
    sill = belfry_h * 0.16
    for sx in (-1, 1):
        _put(parts, sk.arched_panel(
            f"{name}_CanKatX", s, belfry_h, t,
            (x + sx * (s * 0.5 - t * 0.5), y, z0), (0.0, 1.0), (float(sx), 0.0),
            spans=[(-ow * 0.5, ow * 0.5)], sill_z=sill, spring_z=spring,
            col=col), mats["stone"])
    for sy in (-1, 1):
        _put(parts, sk.arched_panel(
            f"{name}_CanKatY", s - 2.0 * t, belfry_h, t,
            (x, y + sy * (s * 0.5 - t * 0.5), z0), (1.0, 0.0), (0.0, float(sy)),
            spans=[(-ow * 0.5, ow * 0.5)], sill_z=sill, spring_z=spring,
            col=col), mats["stone"])
    _put(parts, hz.make_box(f"{name}_CanKaranlik",
                            (s - 2.0 * t - 0.1, s - 2.0 * t - 0.1, belfry_h * 0.6),
                            (x, y, z0 + belfry_h * 0.42), col), mats["shadow"])

    z1 = z0 + belfry_h
    _put(parts, hz.make_box(f"{name}_KuleKorniş", (s + 0.34, s + 0.34, 0.26),
                            (x, y, z1 + 0.13), col), mats["cutstone"])
    _put(parts, hz.make_hip_roof(f"{name}_KuleKulah", s + 0.20, s + 0.20,
                                 s * 0.55, (x, y), z1 + 0.26, "X", col),
         mats["roof"])
    return x, y, z1 + 0.26 + s * 0.55


def _cross(mats, col, name, parts, x, y, z):
    """Haç — dövme demir. Latin kilisesinde kule tepesinde."""
    _put(parts, hz.make_box(f"{name}_Hac", (0.07, 0.07, 1.30),
                            (x, y, z + 0.65), col), mats["trim"])
    _put(parts, hz.make_box(f"{name}_HacKol", (0.62, 0.07, 0.07),
                            (x, y, z + 0.92), col), mats["trim"])


def build_kilise(p, col, asset_name, textured=False):
    """Kilise — Galata Latin bazilikası ya da suriçi mütevazı tipi."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, L, t = p.outer_w, p.outer_l, p.wall_t

    _put(parts, hz.make_box(f"{asset_name}_Subasman", (W + 0.34, L + 0.34, 0.55),
                            (0.0, 0.0, 0.275), col), mats["stone"])

    # Kule ve orta nef yükseltisi olmayan tipte cephe TAMAMEN boş kalıyordu:
    # 15 m genişliğinde tek kapılı düz duvar. Bazilikanın cephesi iki
    # sıradır — altta taç kapı, üstte pencere. Yükseklik yetmiyorsa üst sıra
    # düşer (küçük mahalle kilisesi gerçekten sade cepheye sahiptir).
    split = None
    if p.kind == "orthodox":
        _, prise = sk.arch_points(p.portal_w * 0.5, p.portal_spring)
        cand = p.portal_spring + prise + 0.49 + 0.40   # alinlik + temiz aralik
        if p.aisle_h - cand >= 1.35:
            split = cand
    _shell(p, mats, col, asset_name, parts, p.aisle_h,
           p.window_sill, p.window_spring, p.window_w, front_split_z=split)

    if p.apse:
        _apse(p, mats, col, asset_name, parts,
              p.aisle_h - (0.0 if p.kind == "latin" else 0.80))

    if p.kind == "latin":
        # Orta nef yukseltisi (clerestory) — yan nef catisinin USTUNDE kalir.
        cw = p.nave_w + 2.0 * t
        cz0 = p.aisle_h - 0.30
        ch = p.nave_h - cz0
        cl_sill = 2.00
        cl_spring = 2.85
        cl_w = 0.70
        for s in (-1, 1):
            _window_row(mats, col,
                        f"{asset_name}_Clerestory{'S' if s < 0 else 'D'}", parts,
                        L, ch, t, (s * (cw * 0.5 - t * 0.5), 0.0, cz0),
                        (0.0, 1.0), (float(s), 0.0), p.bays, cl_w,
                        cl_sill, cl_spring, L, mats["stone"], grille=False)
        # Bati penceresi: cephenin ust sirasi. Arka uc sagir kalir (apsis).
        _window_row(mats, col, f"{asset_name}_BatiPenceresi", parts,
                    cw - 2.0 * t, ch, t,
                    (0.0, -L * 0.5 + t * 0.5, cz0), (1.0, 0.0), (0.0, -1.0),
                    1, cl_w * 1.55, ch * 0.26, ch * 0.60,
                    (cw - 2.0 * t) * 0.5, mats["stone"], grille=False)
        _put(parts, hz.make_box(
            f"{asset_name}_ClerestoryArka", (cw - 2.0 * t, t, ch),
            (0.0, L * 0.5 - t * 0.5, cz0 + ch * 0.5), col), mats["stone"])

        # Yan nef catilari: clerestory duvarina yaslanip disari alcalir.
        roof_top = p.aisle_h + 1.35
        for s in (-1, 1):
            _put(parts, _shed(f"{asset_name}_YanCati", -L * 0.5 - 0.30,
                              L * 0.5 + 0.30, s * (cw * 0.5), s * (W * 0.5 + 0.55),
                              roof_top, p.aisle_h, 0.22, col), mats["roof"])
        # Orta nef besik catisi: once ALINLIK duvari, sonra ustunden asan ortu.
        rh = (cw + 0.70) * 0.34
        for sy in (-1, 1):
            _put(parts, _gable_wall(f"{asset_name}_Alinlik", cw,
                                    rh * (cw / (cw + 0.70)),
                                    sy * (L * 0.5 - t * 0.5), t, p.nave_h, col),
                 mats["stone"])
        for obj in gable_roof(f"{asset_name}_OrtaCati", cw + 0.70, L + 0.70, rh,
                              (0.0, 0.0), p.nave_h, 0.22, col):
            _put(parts, obj, mats["roof"])
        total_h = p.nave_h + rh
        if p.tower:
            tx, ty, tz = _tower(p, mats, col, asset_name, parts)
            total_h = max(total_h, tz)
            if p.cross:
                _cross(mats, col, asset_name, parts, tx, ty, tz)
                total_h += 1.30
    else:
        # Uc nef TEK besik cati altinda — mutevazi tipin imzasi.
        rh = (W + 1.10) * 0.26
        for sy in (-1, 1):
            _put(parts, _gable_wall(f"{asset_name}_Alinlik", W,
                                    rh * (W / (W + 1.10)),
                                    sy * (L * 0.5 - t * 0.5), t, p.aisle_h, col),
                 mats["stone"])
        for obj in gable_roof(f"{asset_name}_Cati", W + 1.10, L + 1.10, rh,
                              (0.0, 0.0), p.aisle_h, 0.22, col):
            _put(parts, obj, mats["roof"])
        total_h = p.aisle_h + rh
        # Sacak altinda ahsap hatil sirasi — kagir kutuyu bitirir.
        for sx in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_Hatil", (0.22, L + 0.6, 0.20),
                                    (sx * (W * 0.5 + 0.11), 0.0, p.aisle_h - 0.10),
                                    col), mats["trim"])

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)

    l1 = [_solid(f"{asset_name}_L1", (W, L, p.aisle_h), (0.0, 0.0, p.aisle_h * 0.5),
                 col, mats["stone"]),
          _solid(f"{asset_name}_L1r", (W + 1.0, L + 1.0, total_h - p.aisle_h),
                 (0.0, 0.0, (total_h + p.aisle_h) * 0.5), col, mats["roof"])]
    if p.kind == "latin" and p.tower:
        l1.append(_solid(f"{asset_name}_L1t", (p.tower_w, p.tower_w, p.tower_h),
                         (p.tower_side * (W * 0.5 + p.tower_w * 0.5 - 0.45),
                          -L * 0.5 + p.tower_w * 0.5, p.tower_h * 0.5),
                         col, mats["stone"]))
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    ucx = hz.make_box(f"UCX_{asset_name}", (W, L, p.aisle_h),
                      (0.0, 0.0, p.aisle_h * 0.5), col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(L, 3),
                kind=f"kilise_{p.kind}", palette=p.palette,
                tower=bool(p.tower), sink=round(float(p.sink), 3),
                bays=p.bays)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------------ sinagog

class SinagogParams(object):
    def __init__(self, **kw):
        self.width = kw.get("width", 11.5)
        self.length = kw.get("length", 15.5)
        self.wall_t = kw.get("wall_t", 0.55)
        self.stone_h = kw.get("stone_h", 3.20)     # tas kat yuksekligi
        self.height = kw.get("height", 6.60)       # saçak alti
        self.bays = kw.get("bays", 3)
        self.window_w = kw.get("window_w", 0.85)
        self.window_h = kw.get("window_h", 1.35)
        self.gallery = kw.get("gallery", True)     # kadinlar mahfili pencereleri
        self.porch = kw.get("porch", True)
        self.palette = kw.get("palette", "nonmuslim")

    def validate(self):
        errs = []
        pitch = self.length / max(1, self.bays)
        if self.window_w > pitch - 1.2:
            errs.append(f"window_w={self.window_w} bu ritimde ayak birakmiyor")
        if self.gallery and self.height < self.stone_h + self.window_h + 1.4:
            errs.append(f"height={self.height} kadinlar mahfili sirasina yetmiyor")
        if errs:
            raise ValueError("SinagogParams gecersiz: " + "; ".join(errs))
        return self


def build_sinagog(p, col, asset_name, textured=False):
    """
    Sinagog — sokaktan **ev gibi** okunan dikdörtgen ibadet salonu.

    Kemer, kubbe, kule YOK: pencereler dikdörtgen ve şebekeli, çatı mahallenin
    alaturka kırma çatısı. Bu bir eksiklik değil, tipin kendisidir — sinagogu
    sinagog yapan şey cephesi değil, avlusudur. Avlu duvarını yerleştirici
    `street_kit.build_avlu` ile kurar.
    """
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, L, t = p.width, p.length, p.wall_t
    H = p.height

    _put(parts, hz.make_box(f"{asset_name}_Subasman", (W + 0.30, L + 0.30, 0.45),
                            (0.0, 0.0, 0.225), col), mats["stone"])

    # Iki pencere sirasi: alt salon, ust KADINLAR MAHFILI. Mahfil sinagogun
    # zorunlu ic bolumudur; disariya iki sira pencere olarak yansir.
    if p.gallery:
        sills = [p.stone_h - p.window_h - 0.55, H - p.window_h - 0.85]
    else:
        sills = [H * 0.45]      # mahfilsiz cemaatte tek sira, salonun ustunde

    for s in (-1, 1):
        spans = _bay_spans(L, p.bays, p.window_w)
        ops = [(a, b, sill, sill + p.window_h)
               for sill in sills for (a, b) in spans]
        origin = (s * (W * 0.5 - t * 0.5), 0.0, 0.0)
        _put(parts, hz.make_wall_panel(
            f"{asset_name}_YanDuvar{'S' if s < 0 else 'D'}", L, H, t,
            origin, (0.0, 1.0), (float(s), 0.0), ops, col), mats["plaster"])
        for (a, b, v0, v1) in ops:
            cu = (a + b) * 0.5
            _put_shadow(parts, mats, col, asset_name, origin, (0.0, 1.0),
                        (float(s), 0.0), cu, v0, v1 - v0, p.window_w, t)
            for bar in sk.iron_grille(f"{asset_name}_Sebeke", p.window_w,
                                      v1 - v0, origin, (0.0, 1.0),
                                      (float(s), 0.0), cu, (v0 + v1) * 0.5,
                                      t, mats, col):
                parts.append(bar)

    inner_w = W - 2.0 * t
    _put(parts, hz.make_box(f"{asset_name}_ArkaDuvar", (inner_w, t, H),
                            (0.0, L * 0.5 - t * 0.5, H * 0.5), col),
         mats["plaster"])

    # On cephe: mutevazi ahsap kapi + ust sira pencere. Kapi AVLUYA bakar.
    door_w, door_h = 1.35, 2.25
    ops = [(-door_w * 0.5, door_w * 0.5, 0.16, 0.16 + door_h)]
    if p.gallery:
        for (a, b) in _bay_spans(inner_w, 2, p.window_w):
            ops.append((a, b, sills[-1], sills[-1] + p.window_h))
    front_origin = (0.0, -L * 0.5 + t * 0.5, 0.0)
    _put(parts, hz.make_wall_panel(f"{asset_name}_OnCephe", inner_w, H, t,
                                   front_origin, (1.0, 0.0), (0.0, -1.0),
                                   ops, col), mats["plaster"])
    for (a, b, v0, v1) in ops:
        cu = (a + b) * 0.5
        _put_shadow(parts, mats, col, asset_name, front_origin, (1.0, 0.0),
                    (0.0, -1.0), cu, v0, v1 - v0, b - a, t)
    _put(parts, hz.make_box(f"{asset_name}_KapiKanat",
                            (door_w - 0.08, 0.09, door_h - 0.05),
                            (0.0, -L * 0.5 - 0.02, 0.16 + (door_h - 0.05) * 0.5),
                            col), mats["timber"])
    _put(parts, hz.make_box(f"{asset_name}_Esik", (door_w + 0.9, 0.42, 0.16),
                            (0.0, -L * 0.5 - 0.21, 0.08), col), mats["cutstone"])

    # Tas kat / siva sinirini belli eden silme.
    _put(parts, hz.make_box(f"{asset_name}_Silme", (W + 0.14, L + 0.14, 0.14),
                            (0.0, 0.0, p.stone_h), col), mats["cutstone"])

    if p.porch:
        _put(parts, hz.make_box(f"{asset_name}_Sundurma", (door_w + 1.6, 1.15, 0.14),
                                (0.0, -L * 0.5 - 0.58, door_h + 0.55), col),
             mats["trim"])
        for s in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_SundurmaDirek",
                                    (0.12, 0.12, door_h + 0.48),
                                    (s * (door_w * 0.5 + 0.6), -L * 0.5 - 1.05,
                                     (door_h + 0.48) * 0.5), col), mats["timber"])

    roof_h = (W + 1.4) * 0.30
    _put(parts, hz.make_hip_roof(f"{asset_name}_Cati", W + 1.4, L + 1.4,
                                 roof_h, (0.0, 0.0), H, "Y", col), mats["roof"])
    total_h = H + roof_h

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (W, L, H), (0.0, 0.0, H * 0.5), col,
                 mats["plaster"]),
          _solid(f"{asset_name}_L1r", (W + 1.4, L + 1.4, roof_h),
                 (0.0, 0.0, H + roof_h * 0.5), col, mats["roof"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (W, L, H), (0.0, 0.0, H * 0.5), col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(L, 3),
                kind="sinagog", palette=p.palette, tower=False,
                sink=0.0, bays=p.bays)
    return lod0, lod1, ucx, info


def _solid(name, size, center, col, mat):
    obj = hz.make_box(name, size, center, col)
    hz.assign(obj, mat)
    return obj
