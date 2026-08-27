"""
Hezarfen: 1632 — Üretim, ticaret ve su yapıları (plan Faz 2b'nin kalanı).

Altı yapı, tek kit: **imaret, arasta, bozahane, değirmen, su terazisi,
muvakkithane**. Ortak yanları şu: hiçbiri anıt değil, hepsi şehrin
**çalışan** parçası. Bir şehri şehir yapan şey camileri kadar mutfağı,
çarşısı, değirmeni ve su künkleridir.

## İmaret — imza: SIRA SIRA BACA

İmaret bir aşevi değil bir **mutfak tesisidir**: mutfak, yemekhane,
ekmekhane (*fodla* pişirilir), kiler, görevli odaları ve avlu. Mutfak
mekânları dikdörtgen bir plan içinde **yan yana dizilir** ve kubbe (kimi
örnekte tonoz) ile örtülür. Değişmeyen unsur ise **farklı boydaki ocak
bacalarıdır** — imareti uzaktan imaret yapan şey odur.
*Kaynak:* TDV "İmaret"; METU JFA 2016/1, "Osmanlı Aşhanelerinin Kökeni".

## Arasta — imza: TEK ÇATI ALTINDA ORTAK RİTİM

Arasta *"bir eksen üzerinde dizilmiş dükkân sıraları"*dır; kimi üstü açık,
kimi **kâgir tonoz örtülü**, bir yolun iki yanında sıralanan dükkân gözleri.
Dükkânların **ayrı kapısı yoktur**: sabah ve akşam hep birlikte açılıp
kapanırlar. Yani arasta, dükkânların toplamı değil, **tek bir yapıdır**.

Ölçü çapası Selimiye Arastası'ndan: **256 m'de 73 kemer** → göz genişliği
**≈3,5 m**. Elimizdeki tek metrik değer budur ve `bay_w` odur.
*Kaynak:* Vikipedi "Arasta"; AA, "Selimiye'nin gelir kapısı Arasta Çarşısı".

## Bozahane — 1632'de AÇIK, sonra kapatıldı

Kahvehaneden sonra oyunun **ikinci zaman işareti**. IV. Murad'ın emriyle
yapılan 1638 esnaf sayımında İstanbul'da **300 bozahane** ve ~1100 bozacı
vardır; ayrıca **acı boza** üreten ~40 esnaf. Acı boza sarhoş edecek kadar
alkollüdür ve bozahaneler **IV. Murad döneminde kapatılmıştır** (1623–1640).
Yani 1632'de ayaktadırlar, kahvehaneler gibi.

Yapıyı bozahane yapan şey cephesi değil **arkasıdır**: mayalanma **küp**leri.
*Kaynak:* Evliya Çelebi (1638 esnaf alayı) aktarımları; İSTESOB "Bozacılar".

## Değirmen — imza: OLUK ve ÇARK

Su değirmeninde su, değirmenin yanına getirilip **5–6 m uzunluğunda taş bir
olukla** aşağı akıtılır ve çarkı döndürür. Yapı taş, örtüsü kiremit; içinde
değirmen taşı ve tahıl teknesi. `kind="at"` varyantında oluk ve çark yoktur;
gücü hayvan verir, ortada bir **dönme direği** durur.
*Kaynak:* Kültür Portalı "Değirmencilik"; Sanat Yorum, "Kemaliye Su Değirmeni".

## Su terazisi — imza: SU YUKARI ÇIKAR

*"Görece daha yüksek bir yerden künkle gelen suyun, yine künklerle daha
alçak yerlere ulaşmasını sağlayan **kule şeklinde kâgir yapı**."* Su
yapının tepesindeki **hazneye** çıkar, oradan bir sonraki teraziye gider;
amaç fazla basıncın künkleri patlatmasını önlemektir.

1632'de vardır: Kırkçeşme tesisleri (Kanûnî, Mimar Sinan, ~1563) 55 km'lik
hat boyunca su terazileri taşır.
*Kaynak:* Vikipedi "Su terazisi (yapı)"; TDV "Kırkçeşme Suları".

## Muvakkithane — VAR ama HER YERDE DEĞİL

İstanbul'un ilk muvakkithanesi **Fatih Camii'ninkidir (1470)** ve 17.
yüzyılda çalışır durumdadır (Süleymaniye'nin ilk muvakkitlerinden Ahmed
Nakşî Efendi; Fatih'te Müneccimek Mehmed). Yani 1632'de **vardır**.

Ama yaygınlaşması **18. yüzyıl sonu–19. yüzyıl başıdır**. 1632'de
muvakkithane bir **mahalle mescidine değil, selâtin camisine** aittir. Bu
bir yerleştirme kuralıdır ve varlığın kendisinden daha önemlidir — tekkenin
minaresizliği gibi, yokluğu da belgelidir.

Biçim: *"bir iki odadan büyük olmayan"*, cami avlusunda, büyük şebekeli
pencereli küçük yapı.
*Kaynak:* TDV/Belleten muvakkithane literatürü; Vikipedi "Muvakkithane".

Eksen sözleşmesi kitin geri kalanıyla aynı: giriş cephesi −Y (Unity'de +Z).
"""

import math

import hz_blender as hz
import ottoman_kit as kit
import street_kit as sk


def _put(parts, obj, mat):
    hz.assign(obj, mat)
    parts.append(obj)
    return obj


def _solid(name, size, center, col, mat):
    obj = hz.make_box(name, size, center, col)
    hz.assign(obj, mat)
    return obj


def _finish(parts, l1_parts, col, asset_name, mats, tex_sizes, kind, palette,
            extra=None):
    """LOD'ları birleştir, UV'le, `info` üret — altı yapıda da aynı."""
    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1_parts, f"SM_{asset_name}_LOD1", col)
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
                kind=kind, palette=palette)
    if extra:
        info.update(extra)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------------- imaret

class ImaretParams(object):
    """
    İmaret — külliyenin mutfağı.

    `bays`: yan yana dizilmiş kubbeli mutfak gözü sayısı.
    `ekmekhane`: fodla fırını gözü (daha büyük kubbe, daha kalın baca).
    """

    def __init__(self, bays=4, bay=5.4, depth=7.2, wall_h=4.6, dome_h=1.9,
                 ekmekhane=True, court=True, court_d=8.0, wall_t=0.65,
                 palette="default"):
        self.bays, self.bay, self.depth = bays, bay, depth
        self.wall_h, self.dome_h = wall_h, dome_h
        self.ekmekhane, self.court, self.court_d = ekmekhane, court, court_d
        self.wall_t = wall_t
        self.palette = palette

    def validate(self):
        # Imareti imaret yapan sey SIRA'dir; iki gozlu bir yapi mutfak degil
        # bir odadir.
        if self.bays < 3:
            raise ValueError(f"bays={self.bays} — imaret mutfagi yan yana "
                             "dizilmis gozlerden olusur, en az uc")
        # Kubbe cok basik olursa dam duz okunur, ritim kaybolur.
        if self.dome_h < self.bay * 0.28:
            raise ValueError(f"dome_h={self.dome_h:.2f} gozune ({self.bay}) gore "
                             "basik — kubbe dam gibi okunur")
        if self.depth < self.bay:
            raise ValueError("mutfak gozu enine degil boyuna derindir")


def build_imaret(p, col, asset_name, textured=False):
    """Kubbeli mutfak sırası + bacalar (+ ekmekhane, + avlu). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W = p.bays * p.bay
    D = p.depth
    H = p.wall_h

    _put(parts, hz.make_box(f"{asset_name}_Kutle", (W, D, H),
                            (0.0, 0.0, H * 0.5), col), mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_Silme", (W + 0.34, D + 0.34, 0.24),
                            (0.0, 0.0, H + 0.12), col), mats["cutstone"])

    # Giris: mutfak avluya bakar, sokaga degil — kemerli tek acikliktan
    # servis yapilir.
    _put(parts, sk.arched_panel(f"{asset_name}_OnCephe", W, H, p.wall_t,
                                (0.0, -D * 0.5 + p.wall_t * 0.5, 0.0),
                                (1.0, 0.0), (0.0, -1.0),
                                spans=[(-1.1, 1.1)], sill_z=0.0, spring_z=2.5,
                                col=col), mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_KapiKaranlik", (2.2, 0.06, 3.1),
                            (0.0, -D * 0.5 + p.wall_t + 0.05, 1.55), col),
         mats["shadow"])

    # --- KUBBE + BACA sirasi. Bacalar FARKLI boyda: kaynak "farkli
    #     buyuklukteki ocaklarin bacalari" der ve imaretin uzaktan imzasi
    #     tam olarak o duzensiz dikey ritimdir.
    r = min(p.bay, D) * 0.46
    top = H + 0.24
    for i in range(p.bays):
        x = -W * 0.5 + (i + 0.5) * p.bay
        _put(parts, hz.make_box(f"{asset_name}_Kasnak{i}",
                                (r * 2.05, r * 2.05, 0.34), (x, 0.0, top + 0.17),
                                col), mats["cutstone"])
        _put(parts, hz.make_dome(f"{asset_name}_Kubbe{i}", r, p.dome_h,
                                 (x, 0.0), top + 0.34, segments=14, rings=5,
                                 col=col), mats["lead"])
        bh = 1.5 + 0.55 * ((i * 7) % 5) / 4.0       # duzensiz ama yinelenebilir
        bs = 0.46 + 0.10 * (i % 3)
        _put(parts, hz.make_box(f"{asset_name}_Baca{i}", (bs, bs, bh),
                                (x, D * 0.22, top + 0.34 + p.dome_h + bh * 0.5),
                                col), mats["stone"])
        _put(parts, hz.make_box(f"{asset_name}_BacaKulah{i}",
                                (bs + 0.22, bs + 0.22, 0.16),
                                (x, D * 0.22,
                                 top + 0.34 + p.dome_h + bh + 0.08), col),
             mats["cutstone"])
    height = top + 0.34 + p.dome_h + 2.3

    # --- EKMEKHANE: daha buyuk goz, daha kalin baca. Fodla burada pisirilir.
    if p.ekmekhane:
        ew, ed = p.bay * 1.35, D * 0.92
        ex = W * 0.5 + ew * 0.5 + 0.4
        _put(parts, hz.make_box(f"{asset_name}_Ekmekhane", (ew, ed, H * 0.92),
                                (ex, 0.0, H * 0.46), col), mats["stone"])
        er = min(ew, ed) * 0.46
        _put(parts, hz.make_dome(f"{asset_name}_EkmekKubbe", er, p.dome_h * 1.15,
                                 (ex, 0.0), H * 0.92 + 0.1, segments=12, rings=4,
                                 col=col), mats["lead"])
        _put(parts, hz.make_box(f"{asset_name}_EkmekBaca", (0.82, 0.82, 3.1),
                                (ex, ed * 0.22, H * 0.92 + 0.1 + p.dome_h * 1.15
                                 + 1.55), col), mats["stone"])
        W += ew + 0.4

    # --- AVLU: mutfak avludan servis yapar; duvar alcaktir, kapatmaz.
    #
    # Iki duzeltme, ikisi de inceleme paketinden:
    #   * Avlu MUTFAK BLOGUNA gore ortalanir, ekmekhaneyle genisleyen
    #     toplama gore degil — yoksa avlu yana kayik durur.
    #   * Duvarin KAPISI vardir. Kapisiz bir U, uc gevsek duvar gibi
    #     okunur; tekkede de tam bu hataya dusulmustu (ADR 0027 §6d).
    if p.court:
        kitchen_w = p.bays * p.bay
        cw = kitchen_w + 2.4
        cy = -D * 0.5 - p.court_d
        gate = 2.6
        for nm, size, ctr in (
            ("Sol", (0.42, p.court_d, 2.1), (-cw * 0.5, cy + p.court_d * 0.5, 1.05)),
            ("Sag", (0.42, p.court_d, 2.1), (cw * 0.5, cy + p.court_d * 0.5, 1.05)),
        ):
            _put(parts, hz.make_box(f"{asset_name}_Avlu{nm}", size, ctr, col),
                 mats["stone"])
        seg = (cw - gate) * 0.5
        for s in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_AvluOn{s}", (seg, 0.42, 2.1),
                                    (s * (gate + seg) * 0.5, cy, 1.05), col),
                 mats["stone"])
        for s in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_AvluPaye{s}",
                                    (0.58, 0.58, 2.5),
                                    (s * gate * 0.5, cy, 1.25), col),
                 mats["cutstone"])
        _put(parts, hz.make_box(f"{asset_name}_AvluLento", (gate + 0.6, 0.5, 0.4),
                                (0.0, cy, 2.7), col), mats["cutstone"])

    l1 = [_solid(f"{asset_name}_L1", (W, D, height * 0.82),
                 (0.0, 0.0, height * 0.41), col, mats["stone"])]
    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "imaret",
                   p.palette, dict(bays=p.bays, ekmekhane=p.ekmekhane))


# ------------------------------------------------------------------- arasta

class ArastaParams(object):
    """
    Arasta — bir eksen üzerinde dizilmiş dükkân sıraları.

    `bay_w` varsayılanı **3,5 m**: Selimiye Arastası 256 m'de 73 kemer taşır
    (256/73 = 3,51). Elimizdeki tek metrik değer budur.
    """

    def __init__(self, bays=8, bay_w=3.5, cell_d=4.2, aisle=3.6, wall_h=4.3,
                 vault=True, both_sides=True, palette="default"):
        self.bays, self.bay_w, self.cell_d = bays, bay_w, cell_d
        self.aisle, self.wall_h = aisle, wall_h
        self.vault, self.both_sides = vault, both_sides
        self.palette = palette

    @property
    def opening_half(self):
        return self.bay_w * 0.34

    @property
    def spring_z(self):
        """
        Kemer başlangıcı duvardan TÜRETİLİR, sabit bir sayı değil.

        Sabit yazılmıştı (2,2 m) ve `bay_w` 3,5 m'de kemer tepesi 3,75 m'ye
        çıkıp 3,60 m'lik duvarı aştı — `arched_panel` haklı olarak
        "üstünde duvar kalmıyor" diye reddetti. Göz genişliği değişince
        kemer de değişir; ikisini bağlamayan bir sayı bir gün taşar.
        """
        return self.wall_h * 0.44

    def validate(self):
        # Alti gozden az bir sira "arasta" degil, birkac dukkandir.
        if self.bays < 6:
            raise ValueError(f"bays={self.bays} — arasta bir SIRADIR, en az alti goz")
        # Koridor gecilebilir olmali; dar olursa yapi tonoz degil bacadir.
        if not (2.2 <= self.aisle <= 5.5):
            raise ValueError(f"aisle={self.aisle} m — 2,2-5,5 disi")
        if self.vault and not self.both_sides:
            raise ValueError("tonoz iki siraya oturur; tek sirada ortu tasiyani yok")
        _, rise = sk.arch_points(self.opening_half, self.spring_z)
        top = self.spring_z + rise
        if top > self.wall_h - 0.30:
            raise ValueError(
                f"kemer tepesi {top:.2f} m, duvar {self.wall_h:.2f} m — "
                f"ustunde tas kalmiyor; wall_h en az {top + 0.30:.2f} olmali")


def build_arasta(p, col, asset_name, textured=False):
    """Karşılıklı dükkân gözleri + (tonoz) örtü. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    L = p.bays * p.bay_w
    rows = (-1, 1) if p.both_sides else (-1,)
    y_in = p.aisle * 0.5

    for s in rows:
        yc = s * (y_in + p.cell_d * 0.5)
        _put(parts, hz.make_box(f"{asset_name}_Sira{s}",
                                (L, p.cell_d, p.wall_h), (0.0, yc, p.wall_h * 0.5),
                                col), mats["stone"])
        # Koridora bakan yuz: her gozde bir kemerli aciklik. Ayri kapi YOK —
        # kaynak: dukkanlar sabah ve aksam BIRLIKTE acilip kapanir. Bu yuzden
        # gozler ayni panelin uzerinde ayni ritimle acilir.
        spans = []
        for i in range(p.bays):
            c = -L * 0.5 + (i + 0.5) * p.bay_w
            spans.append((c - p.opening_half, c + p.opening_half))
        _put(parts, sk.arched_panel(f"{asset_name}_Cephe{s}", L, p.wall_h, 0.5,
                                    (0.0, yc - s * (p.cell_d * 0.5 - 0.25), 0.0),
                                    (1.0, 0.0), (0.0, -s * 1.0),
                                    spans=spans, sill_z=0.55,
                                    spring_z=p.spring_z, col=col), mats["stone"])
        for i in range(p.bays):
            c = -L * 0.5 + (i + 0.5) * p.bay_w
            # Tezgah: kepenk asagi acilinca tezgah olur (street_kit dukkani
            # ile ayni fikir). Arastayi carsi yapan sey bu surekli tezgahtir.
            _put(parts, hz.make_box(f"{asset_name}_Tezgah{s}_{i}",
                                    (p.bay_w * 0.72, 0.42, 0.16),
                                    (c, yc - s * (p.cell_d * 0.5 + 0.05), 0.62),
                                    col), mats["cutstone"])
            _put(parts, hz.make_box(f"{asset_name}_Karanlik{s}_{i}",
                                    (p.bay_w * 0.66, 0.06, 1.55),
                                    (c, yc - s * (p.cell_d * 0.5 - 0.5), 1.35),
                                    col), mats["shadow"])
        _put(parts, hz.make_box(f"{asset_name}_Saçak{s}",
                                (L + 0.3, p.cell_d + 0.5, 0.22),
                                (0.0, yc, p.wall_h + 0.11), col), mats["cutstone"])

    top = p.wall_h + 0.22
    if p.vault:
        # Besik tonoz: koridoru bastan basa orter. Yarim silindir yerine
        # kemerin kendi egrisini kullaniyoruz (street_kit ile ayni ARCH_C),
        # yoksa arasta tonozu kit'in oteki kemerlerinden baska bir yapi
        # ailesine ait gibi okunur.
        # Tonoz KORIDORU orter: gozler X boyunca dizilir, koridor da X'te
        # uzar — dolayisiyla kemer Y'de acilir ve tonoz X'te uzatilir.
        # Ilk yazimda kemer X'te aciliyordu ve arasta 28 m DERIN cikti
        # (beklenen 12 m); ayak izi olcumu bunu hemen gosterdi.
        half = p.aisle * 0.5
        pts, rise = sk.arch_points(half, 0.0)
        for i in range(len(pts) - 1):
            (u0, v0), (u1, v1) = pts[i], pts[i + 1]
            seg = math.hypot(u1 - u0, v1 - v0)
            ang = math.atan2(v1 - v0, u1 - u0)
            obj = hz.make_box(f"{asset_name}_Tonoz{i}", (L, 0.20, seg),
                              (0.0, 0.0, 0.0), col)
            obj.rotation_euler = (ang - math.pi * 0.5, 0.0, 0.0)
            obj.location = (0.0, (u0 + u1) * 0.5, top + (v0 + v1) * 0.5)
            _put(parts, obj, mats["cutstone"])
        top += rise

    l1 = [_solid(f"{asset_name}_L1",
                 (L, p.aisle + 2.0 * p.cell_d if p.both_sides
                  else p.aisle * 0.5 + p.cell_d, top),
                 (0.0, 0.0, top * 0.5), col, mats["stone"])]
    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "arasta",
                   p.palette, dict(bays=p.bays, bay_w=p.bay_w, vault=p.vault,
                                   length_m=round(L, 2)))


# ----------------------------------------------------------------- bozahane

class BozahaneParams(object):
    """
    Bozahane — 1632'de **açık**, IV. Murad döneminde kapatıldı.

    `kup`: mayalanma küplerinin sayısı. Yapıyı bozahane yapan şey cephesi
    değil arkasıdır; küpsüz bir bozahane sıradan bir dükkândır.
    """

    def __init__(self, width=6.4, depth=5.6, wall_h=3.3, kup=5, sundurma=2.1,
                 open_w=2.1, seki=True, palette="default"):
        self.width, self.depth, self.wall_h = width, depth, wall_h
        self.kup, self.sundurma, self.seki = kup, sundurma, seki
        self.open_w = open_w
        self.palette = palette

    @property
    def spring_z(self):
        """Kemer başlangıcı duvardan türer — arastadaki aynı ders."""
        return self.wall_h * 0.42

    def validate(self):
        # Cephe acikligi duvarla ORANTILI olmali. Ilk yazimda aciklik
        # `width * 0,60` idi ve 6,4 m'lik cephede kemer tepesi 4,60 m'ye
        # cikip 3,30 m'lik duvari asti.
        _, rise = sk.arch_points(self.open_w * 0.5, self.spring_z)
        top = self.spring_z + rise
        if top > self.wall_h - 0.30:
            raise ValueError(
                f"kemer tepesi {top:.2f} m, duvar {self.wall_h:.2f} m — "
                f"aciklik ({self.open_w:.2f} m) cok genis")
        if self.kup < 3:
            raise ValueError(f"kup={self.kup} — mayalanma kupleri bozahanenin "
                             "IMZASIDIR, ucten az olursa yapi dukkan olur")
        if self.sundurma < 1.2:
            raise ValueError("sundurma dar — tezgah golgede kalmaz")
        if self.width < 4.0 or self.depth < 3.5:
            raise ValueError("bozahane bir odadan kucuk olamaz")


def build_bozahane(p, col, asset_name, textured=False):
    """Ahşap cepheli oda + sundurma + arkada mayalanma küpleri."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D, H = p.width, p.depth, p.wall_h

    _put(parts, hz.make_box(f"{asset_name}_Kutle", (W, D, H),
                            (0.0, 0.0, H * 0.5), col), mats["stone"])
    _put(parts, sk.arched_panel(f"{asset_name}_Cephe", W, H, 0.42,
                                (0.0, -D * 0.5 + 0.21, 0.0), (1.0, 0.0),
                                (0.0, -1.0),
                                spans=[(-p.open_w * 0.5, p.open_w * 0.5)],
                                sill_z=0.60, spring_z=p.spring_z, col=col),
         mats["timber"])
    _put(parts, hz.make_box(f"{asset_name}_Tezgah", (p.open_w + 0.3, 0.44, 0.16),
                            (0.0, -D * 0.5 - 0.10, 0.68), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Karanlik", (p.open_w - 0.1, 0.06, 1.42),
                            (0.0, -D * 0.5 + 0.46, 1.40), col), mats["shadow"])

    # Sundurma: bozanin satildigi yer kapinin ONUDUR.
    sy = -D * 0.5 - p.sundurma * 0.5
    _put(parts, hz.make_box(f"{asset_name}_Sundurma", (W + 0.5, p.sundurma, 0.14),
                            (0.0, sy, H - 0.35), col), mats["timber"])
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_Direk{sx}", (0.14, 0.14, H - 0.42),
                                (sx * (W * 0.5 - 0.2), sy - p.sundurma * 0.35,
                                 (H - 0.42) * 0.5), col), mats["timber"])
    if p.seki:
        _put(parts, hz.make_box(f"{asset_name}_Seki", (W * 0.9, 0.85, 0.34),
                                (0.0, sy - p.sundurma * 0.2, 0.17), col),
             mats["cutstone"])

    # --- MAYALANMA KUPLERI: arkada, sundurma altinda, sirali.
    ky = D * 0.5 + 0.75
    for i in range(p.kup):
        kx = (-(p.kup - 1) * 0.5 + i) * 0.92
        _put(parts, hz.make_tube(f"{asset_name}_Kup{i}", 0.34, 0.20, 0.95,
                                 (kx, ky), 0.0, col=col, segments=10),
             mats["roof"])
        _put(parts, hz.make_box(f"{asset_name}_KupKapak{i}", (0.30, 0.30, 0.05),
                                (kx, ky, 0.98), col), mats["timber"])
    _put(parts, hz.make_box(f"{asset_name}_KupSundurma",
                            ((p.kup + 1) * 0.92, 1.9, 0.12),
                            (0.0, ky, 2.05), col), mats["timber"])
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_KupDirek{sx}",
                                (0.12, 0.12, 2.0),
                                (sx * (p.kup + 1) * 0.42, ky + 0.7, 1.0), col),
             mats["timber"])

    _put(parts, hz.make_hip_roof(f"{asset_name}_Cati", W + 0.6, D + 0.6, 1.15,
                                 (0.0, 0.0), H, col=col), mats["roof"])
    _put(parts, hz.make_box(f"{asset_name}_Baca", (0.5, 0.5, 1.7),
                            (W * 0.28, D * 0.26, H + 1.4), col), mats["stone"])

    l1 = [_solid(f"{asset_name}_L1", (W, D, H + 1.15),
                 (0.0, 0.0, (H + 1.15) * 0.5), col, mats["stone"])]
    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "bozahane",
                   p.palette, dict(kup=p.kup))


# ----------------------------------------------------------------- değirmen

class DegirmenParams(object):
    """
    Değirmen. `kind`: `"su"` (oluk + çark) ya da `"at"` (dönme direği).

    `oluk_len` varsayılanı 5,5 m: kaynak suyun değirmenin yanına getirilip
    **5–6 m uzunluğunda taş bir olukla** çarka akıtıldığını söyler.
    """

    def __init__(self, kind="su", width=6.0, depth=7.2, wall_h=3.4,
                 wheel_r=1.35, oluk_len=5.5, palette="default"):
        self.kind = kind
        self.width, self.depth, self.wall_h = width, depth, wall_h
        self.wheel_r, self.oluk_len = wheel_r, oluk_len
        self.palette = palette

    def validate(self):
        if self.kind not in ("su", "at"):
            raise ValueError(f"bilinmeyen tur: {self.kind}")
        if self.kind == "su":
            if not (4.0 <= self.oluk_len <= 8.0):
                raise ValueError(f"oluk {self.oluk_len} m — kaynak 5-6 m der; "
                                 "4-8 disi kabul edilmiyor")
            # Carkin capi yapinin yarisini asarsa yapi carka asilmis olur.
            if self.wheel_r < 0.8 or self.wheel_r > self.wall_h * 0.6:
                raise ValueError(f"cark yaricapi {self.wheel_r} m makul degil")


def build_degirmen(p, col, asset_name, textured=False):
    """Taş değirmen binası + (su oluğu ve çark). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D, H = p.width, p.depth, p.wall_h

    _put(parts, hz.make_box(f"{asset_name}_Kutle", (W, D, H),
                            (0.0, 0.0, H * 0.5), col), mats["stone"])
    _put(parts, sk.arched_panel(f"{asset_name}_Cephe", W, H, 0.5,
                                (0.0, -D * 0.5 + 0.25, 0.0), (1.0, 0.0),
                                (0.0, -1.0), spans=[(-0.75, 0.75)],
                                sill_z=0.0, spring_z=2.05, col=col),
         mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_KapiKaranlik", (1.5, 0.06, 2.1),
                            (0.0, -D * 0.5 + 0.56, 1.05), col), mats["shadow"])
    _put(parts, hz.make_hip_roof(f"{asset_name}_Cati", W + 0.7, D + 0.7, 1.5,
                                 (0.0, 0.0), H, col=col), mats["roof"])
    top = H + 1.5

    # Tahil teknesi: kapinin yaninda, disarida. Un cuvali burada beklerdi.
    _put(parts, hz.make_box(f"{asset_name}_Tekne", (1.3, 0.8, 0.6),
                            (W * 0.5 - 0.9, -D * 0.5 - 0.55, 0.3), col),
         mats["trim"])

    if p.kind == "su":
        # --- OLUK: su YUKARIDAN gelir. Egimi olmayan bir oluk carki
        #     dondurmez; egim gorunur olmali, yoksa yapi "yaninda tekerlek
        #     olan bir ev" gibi okunur.
        # Oluk carkin USTUNDE biter: su yukaridan doker, carki cevirir.
        # Ilk yazimda oluk ucu carkin ORTASININ altinda kaliyordu (z_lo =
        # 1,55 r, cark tepesi 2 r) — yani su carkin icinden geciyordu ve
        # yapi "yaninda tekerlek olan bir ev" gibi okunuyordu. Egim de
        # duvar yuksekligine baglanmisti ve neredeyse duz cikiyordu.
        # Cark ekseni, en alt KANADIN ucu zemine degecek kadar yukarida.
        # Gercekte carkin alti savak icindedir (zeminin altinda) ama kitin
        # pivot sozlesmesi taban merkezdedir: -0,11 m'lik bir kanat ucu
        # denetimi dusuruyordu.
        wx = W * 0.5 + p.wheel_r + 0.25
        zc = p.wheel_r + 0.32
        z_lo = zc + p.wheel_r + 0.30
        z_hi = z_lo + p.oluk_len * 0.20        # ~%20 egim
        drop = z_hi - z_lo
        pitch = math.atan2(drop, p.oluk_len)
        oluk = hz.make_box(f"{asset_name}_Oluk",
                           (0.62, math.hypot(p.oluk_len, drop), 0.17),
                           (0.0, 0.0, 0.0), col)
        oluk.rotation_euler = (-pitch, 0.0, 0.0)
        oluk.location = (wx, -p.oluk_len * 0.5, (z_hi + z_lo) * 0.5)
        _put(parts, oluk, mats["cutstone"])
        # Oluk havada durmaz: iki ayak. Ayaklar olmadan yapi "yaninda
        # tekerlek olan bir ev" gibi okunuyordu.
        for i, t in enumerate((0.22, 0.68)):
            yy = -p.oluk_len * (1.0 - t)
            zz = z_lo + drop * t
            _put(parts, hz.make_box(f"{asset_name}_OlukAyak{i}",
                                    (0.34, 0.34, zz), (wx, yy, zz * 0.5), col),
                 mats["stone"])

        # --- CARK: gobek + parmaklar + iki cember + KANATLAR.
        #
        # Ilk yazimda cember `make_tube(r, r, ...)` ile kuruluyordu ve
        # `cap_top` varsayilan olarak ACIK oldugu icin daire DOLU cikti:
        # cark, kirmizi bir DISK gibi okunuyordu. Cember bir halkadir,
        # kapaksiz olmali. Ahsap da asi kirmizisi degil: asi boyasi EV
        # boyasidir, degirmen carki boyanmaz.
        wood = mats["trim"]
        # Gobek ORIJINDE kurulur (base_z = -yaris), sonra dondurulur, sonra
        # yerine tasinir. Onceki hali z=0'dan basliyordu ve `location`daki
        # `-0.39` donusun yol actigi kaymayi ELLE telafi ediyordu: sonuc
        # dogruydu ama gobek boyu degisirse sessizce bozulurdu. Ayni sebeple
        # `ottoman_kit._donus_denetimi` bu deyimi artik hata sayiyor.
        hub = hz.make_tube(f"{asset_name}_CarkGobek", 0.22, 0.22, 0.78,
                           (0.0, 0.0), -0.39, col=col, segments=8)
        hub.rotation_euler = (0.0, math.pi * 0.5, 0.0)
        hub.location = (wx, 0.0, zc)
        _put(parts, hub, wood)

        spokes = 8
        for i in range(spokes):
            a = 2.0 * math.pi * i / spokes
            arm = hz.make_box(f"{asset_name}_Parmak{i}",
                              (0.30, 0.07, p.wheel_r * 0.92), (0.0, 0.0, 0.0), col)
            arm.rotation_euler = (a, 0.0, 0.0)
            arm.location = (wx, math.sin(a) * p.wheel_r * 0.46,
                            zc + math.cos(a) * p.wheel_r * 0.46)
            _put(parts, arm, wood)
            # Kanat cemberin DISINA tasar; suyu tutan yuzey odur ve carki
            # cark yapan sey de disaridan sayilabilen o disler.
            pad = hz.make_box(f"{asset_name}_Kanat{i}",
                              (0.62, 0.06, 0.34), (0.0, 0.0, 0.0), col)
            pad.rotation_euler = (a, 0.0, 0.0)
            pad.location = (wx, math.sin(a) * (p.wheel_r + 0.09),
                            zc + math.cos(a) * (p.wheel_r + 0.09))
            _put(parts, pad, wood)

        for s in (-1, 1):
            rim = hz.make_tube(f"{asset_name}_CarkCember{s}", p.wheel_r,
                               p.wheel_r, 0.09, (0.0, 0.0), 0.0, col=col,
                               segments=16, cap_top=False, smooth=False)
            rim.rotation_euler = (0.0, math.pi * 0.5, 0.0)
            rim.location = (wx + s * 0.28, 0.0, zc)
            _put(parts, rim, wood)
        W += p.wheel_r * 2 + 0.5
    else:
        # AT DEGIRMENI: gucu hayvan verir. Ortada donme diregi ve dairesel
        # yurume izi; oluk ve cark YOKTUR.
        _put(parts, hz.make_tube(f"{asset_name}_Direk", 0.22, 0.18, H * 0.8,
                                 (0.0, 0.0), 0.0, col=col, segments=8),
             mats["timber"])
        _put(parts, hz.make_box(f"{asset_name}_Kol", (0.14, 3.2, 0.14),
                                (0.0, 1.5, H * 0.72), col), mats["timber"])

    l1 = [_solid(f"{asset_name}_L1", (p.width, D, top),
                 (0.0, 0.0, top * 0.5), col, mats["stone"])]
    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "degirmen",
                   p.palette, dict(mill_kind=p.kind))


# -------------------------------------------------------------- su terazisi

class SuTeraziParams(object):
    """
    Su terazisi — kule şeklinde kâgir yapı; su tepedeki **hazneye** çıkar.

    Yüksekliği keyfî değildir: terazi, beslediği noktadan yüksek olmak
    zorundadır, yoksa su çıkmaz. Ama 18 m'yi aşan bir taş kule minare olur.
    """

    def __init__(self, height=9.0, base_side=2.3, top_side=1.45, hazne=1.05,
                 sides=4, kunk=True, palette="default"):
        self.height, self.base_side, self.top_side = height, base_side, top_side
        self.hazne, self.sides, self.kunk = hazne, sides, kunk
        self.palette = palette

    def validate(self):
        if not (4.0 <= self.height <= 18.0):
            raise ValueError(f"height={self.height} m — 4-18 disi; "
                             "daha yuksegi minare olur")
        # Ince uzun bir tas kule bacadir. Terazi tasiyicidir: govde
        # yuksekligin sekizde birinden ince olamaz.
        if self.base_side < self.height / 8.0:
            raise ValueError(f"base_side={self.base_side:.2f} yuksekligine gore "
                             "ince — bu bir baca, terazi degil")
        if self.top_side > self.base_side:
            raise ValueError("govde yukari dogru INCELIR")


def build_su_terazisi(p, col, asset_name, textured=False):
    """Daralan kâgir kule + tepede hazne + künk. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    H = p.height

    _put(parts, hz.make_box(f"{asset_name}_Kaide",
                            (p.base_side + 0.45, p.base_side + 0.45, 0.5),
                            (0.0, 0.0, 0.25), col), mats["cutstone"])
    # Govde: daralan bir prizma. `make_tube` cok kenarli kesit verir; dort
    # kenarli hali kare kuledir ve kaynagin tarif ettigi budur.
    _put(parts, hz.make_tube(f"{asset_name}_Govde",
                             p.base_side * 0.5 * math.sqrt(2.0),
                             p.top_side * 0.5 * math.sqrt(2.0), H - 0.5,
                             (0.0, 0.0), 0.5, col=col, segments=p.sides,
                             smooth=False, phase=math.pi / p.sides),
         mats["cutstone"])

    # Bakim kapisi: kulenin dibindedir; kunk tikanirsa oradan girilir.
    _put(parts, hz.make_box(f"{asset_name}_BakimKapi", (0.62, 0.06, 1.15),
                            (0.0, -p.base_side * 0.5 - 0.02, 1.05), col),
         mats["shadow"])

    # --- HAZNE: yapinin ISI budur. Su buraya cikar ve buradan devam eder.
    top = H - 0.5 + 0.5
    _put(parts, hz.make_box(f"{asset_name}_HazneBilezik",
                            (p.top_side + 0.5, p.top_side + 0.5, 0.22),
                            (0.0, 0.0, top + 0.11), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Hazne",
                            (p.top_side + 0.24, p.top_side + 0.24, p.hazne),
                            (0.0, 0.0, top + 0.22 + p.hazne * 0.5), col),
         mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_HazneKapak",
                            (p.top_side + 0.46, p.top_side + 0.46, 0.14),
                            (0.0, 0.0, top + 0.22 + p.hazne + 0.07), col),
         mats["stone"])

    if p.kunk:
        # Kunk: gelen ve giden. Tek kunk, terazinin ne ise yaradigini
        # anlatmaz — su bir yerden GELIR, bir yere GIDER.
        # Kunkun cikacagi yuz, KULENIN O YUKSEKLIKTEKI genisligidir.
        # Ikisini de taban olcusune gore koymustum ve ustteki kunk havada
        # duruyordu — gövde yukari dogru inceliyor.
        for s, zz, face in ((-1, top + 0.42, (p.top_side + 0.24) * 0.5),
                            (1, 1.35, p.base_side * 0.5)):
            # Kunk ORIJINDE kurulur, sonra dondurulur, sonra tasinir.
            # Onceki hali z=0'dan basliyordu; X ekseninde dondurulunce mesh
            # merkezi -Y'ye 0,475 m kayiyordu ve bu kayma HER IKI kunke de
            # AYNI yonde uygulaniyordu — yani gelen ile giden kunk simetrik
            # degildi, biri 0,95 m daha disari tasiyordu. Iki kunkun anlami
            # simetrilerinde: su bir yerden GELIR, bir yere GIDER.
            kunk = hz.make_tube(f"{asset_name}_Kunk{s}", 0.13, 0.13, 0.95,
                                (0.0, 0.0), -0.475, col=col, segments=8)
            kunk.rotation_euler = (math.pi * 0.5, 0.0, 0.0)
            kunk.location = (0.0, s * (face + 0.28), zz)
            _put(parts, kunk, mats["roof"])

    height = top + 0.22 + p.hazne + 0.14
    l1 = [_solid(f"{asset_name}_L1", (p.base_side, p.base_side, height),
                 (0.0, 0.0, height * 0.5), col, mats["cutstone"])]
    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "su_terazisi",
                   p.palette, dict(tower_h=round(height, 2)))


# ------------------------------------------------------------ muvakkithane

class MuvakkithaneParams(object):
    """
    Muvakkithane — *"bir iki odadan büyük olmayan"* yapı.

    1632'de **vardır** ama yalnızca **selâtin camisi** avlusunda. Bu bir
    yerleştirme kuralıdır ve `source` notunda taşınır.
    """

    def __init__(self, width=4.6, depth=4.0, wall_h=3.2, rooms=1,
                 window_w=1.55, window_h=1.55, dome=False, eave=0.75,
                 palette="default"):
        self.width, self.depth, self.wall_h = width, depth, wall_h
        self.rooms = rooms
        self.window_w, self.window_h = window_w, window_h
        self.dome, self.eave = dome, eave
        self.palette = palette

    def validate(self):
        if self.rooms not in (1, 2):
            raise ValueError("muvakkithane bir ya da IKI odadir")
        # Buyurse mektebe donusur; kaynak acikca "bir iki odadan buyuk
        # olmayan" der.
        if self.width * self.rooms > 9.5 or self.depth > 6.0:
            raise ValueError("muvakkithane bu kadar buyuk olamaz")
        # Muvakkit isik ister ve GORULUR olmalidir: pencere genisligi
        # cephenin ucte birinden dar olamaz.
        if self.window_w < self.width / 3.0:
            raise ValueError(f"pencere {self.window_w:.2f} m — cepheye gore dar; "
                             "muvakkit hem isik ister hem gorunur olmali")


def build_muvakkithane(p, col, asset_name, textured=False):
    """Küçük kâgir oda + büyük şebekeli pencere + geniş saçak."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W = p.width * p.rooms
    D, H = p.depth, p.wall_h

    _put(parts, hz.make_box(f"{asset_name}_Kutle", (W, D, H),
                            (0.0, 0.0, H * 0.5), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Kursu", (W + 0.3, D + 0.3, 0.34),
                            (0.0, 0.0, 0.17), col), mats["cutstone"])

    # --- PENCERE(LER): buyuk, sebekeli, tezgahli. Muvakkit saati buradan
    #     verir; pencere bir aciklik degil bir GISEDIR.
    for i in range(p.rooms):
        cx = -W * 0.5 + (i + 0.5) * p.width
        _put(parts, hz.make_box(f"{asset_name}_PencereBosluk{i}",
                                (p.window_w, 0.08, p.window_h),
                                (cx, -D * 0.5 + 0.06, 1.05 + p.window_h * 0.5),
                                col), mats["shadow"])
        # Sebeke mescit/kilise/sinagogla AYNI: demir isciligi mahalleye
        # aittir, cemaate degil (street_kit.iron_grille).
        parts.extend(sk.iron_grille(
            f"{asset_name}_Sebeke{i}", p.window_w, p.window_h,
            (cx, -D * 0.5, 1.05), (1.0, 0.0), (0.0, -1.0),
            0.0, p.window_h * 0.5, 0.24, mats, col))
        _put(parts, hz.make_box(f"{asset_name}_Tezgah{i}",
                                (p.window_w + 0.3, 0.34, 0.12),
                                (cx, -D * 0.5 - 0.12, 1.02), col), mats["marble"])

    # Kapi yan cephede: gise on cepheyi tutar. Esik KURSU USTUNDEDIR —
    # zeminden baslayan bir kapi, yapinin oturdugu kursuyu yok sayar (ve
    # pivot denetimini de dusurur: 2,05 m'lik kapi 1,02'de merkezlenince
    # tabani -5 mm'ye iniyordu).
    _put(parts, hz.make_box(f"{asset_name}_Kapi", (0.06, 0.95, 2.05),
                            (W * 0.5 - 0.02, D * 0.12, 0.34 + 1.025), col),
         mats["shadow"])

    if p.dome:
        _put(parts, hz.make_box(f"{asset_name}_Kasnak", (W * 0.86, D * 0.86, 0.3),
                                (0.0, 0.0, H + 0.15), col), mats["cutstone"])
        r = min(W, D) * 0.43
        _put(parts, hz.make_dome(f"{asset_name}_Kubbe", r, r * 0.62, (0.0, 0.0),
                                 H + 0.3, segments=12, rings=4, col=col),
             mats["lead"])
        top = H + 0.3 + r * 0.62
    else:
        _put(parts, hz.make_hip_roof(f"{asset_name}_Cati", W + p.eave * 2,
                                     D + p.eave * 2, 1.05, (0.0, 0.0), H,
                                     col=col), mats["roof"])
        top = H + 1.05

    l1 = [_solid(f"{asset_name}_L1", (W, D, top), (0.0, 0.0, top * 0.5), col,
                 mats["cutstone"])]
    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "muvakkithane",
                   p.palette, dict(rooms=p.rooms, dome=p.dome))


# ================================================== İskele (ahşap, 1632)

#: Üsküdar iskelesi — ölçüsü **yok**, kütle **D3**.
#:
#: Belgeli olan şey iskelenin **varlığı** ve **camiye adını vermiş
#: olması**: Üsküdar Mihrimah Sultan Camii'nin yaygın adı **"İskele
#: Camii"**dir ve sebebi yanı başındaki iskeledir. Yani iskele, camiden
#: bağımsız bir ayrıntı değil — caminin adının kaynağı.
#:
#: **1632'de AHŞAP**: kâgir rıhtımlar 19. yüzyıldır. Yapısal ahşap
#: **boyanmaz** (`timber_bare`, ADR 0035): tuzlu havada duran bir iskele
#: aşı boyalı bir cumba değildir.
ISK_LEN, ISK_W = 34.0, 6.0
ISK_DECK_Z = 1.6
ISK_PILE_N = 9

#: İskelenin baktığı yön (derece) — **ölçüldü**: kendi 1632 kıyı
#: çizgimizin Üsküdar Mihrimah önündeki yerel **normali**. `Waterward`
#: (en alçak arazi yönü) burada yetmez: iskele zaten suyun içindedir ve
#: "en derin yön" boğazın boyunca çıkabilir. İskele kıyıya **dik** uzanır.
ISK_FACE_DEG = 306.8


class IskeleParams(object):
    """
    Ahşap iskele — kazıklar üzerinde güverte, ucunda bağlama babaları.

    Pivot **kıyı ucundadır** ve yapı **−Y** yönünde denize uzanır.

    ## İşaret bir kez ters çıktı

    İlk kurulumda iskele **+Y**'de uzanıyordu. Eksen sözleşmesi
    (CLAUDE.md) şöyle: prefabın **+Z**'si ön cephedir ve o Blender'da
    **−Y**'ye karşılık gelir. Yerleştirici `LookRotation(face)` ile
    +Z'yi suya çevirir; +Y'de kurulan iskele bu yüzden **karaya** doğru
    uzanıyordu. Ölçüldü ve çevrildi.

    Yön için `Waterward` (en alçak arazi yönü) **yetmez**: iskele zaten
    suyun içindedir ve orada "en derin yön" boğazın **boyunca** olabilir.
    İskele kıyıya **dik** uzanır; bu yüzden yön kıyı çizgisinin yerel
    **normalinden** ölçüldü ve `face_deg` olarak bildiriliyor
    (Yedikule'de kullanılan yöntemin aynısı, ADR 0050).
    """

    def __init__(self, length=ISK_LEN, width=ISK_W, deck_z=ISK_DECK_Z,
                 piles=ISK_PILE_N, kayikhane=True, palette="default"):
        self.length, self.width = length, width
        self.deck_z = deck_z
        self.piles = piles
        self.kayikhane = kayikhane
        self.palette = palette

    def validate(self):
        # Iskele DENIZE UZANIR: uzunlugu genisliginden belirgin fazla
        # olmali, yoksa iskele degil rihtim olur.
        if self.length < self.width * 3.0:
            raise ValueError(
                f"iskele {self.length:.1f} x {self.width:.1f} m — denize "
                "uzanan bir yapi; boyu eninin en az uc kati olmali, yoksa "
                "rihtim olur")
        # Guverte SU USTUNDE olmali ama merdiven istemeyecek kadar alcak.
        if not (0.9 <= self.deck_z <= 2.5):
            raise ValueError(f"guverte {self.deck_z:.1f} m — kayiktan "
                             "cikilabilecek kotta olmali")
        if self.piles < 4:
            raise ValueError("iskele en az dort kazik cifti ister")
        return self


def build_iskele(p, col, asset_name, textured=False):
    """Ahşap iskele. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    L, W = p.length, p.width

    # KAZIKLAR: cift cift, boyu suyun altina iner.
    pile_r, pile_h = 0.24, p.deck_z + 2.6
    for i in range(p.piles):
        y = -L * (i + 0.5) / p.piles
        for sx in (-1, 1):
            parts.append(hz.assign(
                hz.make_tube(f"Kazik_{i}{sx}", pile_r, pile_r * 0.92, pile_h,
                             (sx * (W * 0.5 - 0.5), y), -2.6, segments=8,
                             col=col), mats["timber_bare"]))

    # GUVERTE + kirisler
    parts.append(hz.assign(hz.make_box(f"Guverte_{asset_name}",
                                       (W, L, 0.22),
                                       (0.0, -L * 0.5, p.deck_z), col),
                           mats["timber_bare"]))
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"Kiris_{sx}", (0.26, L, 0.42),
                        (sx * (W * 0.5 - 0.5), -L * 0.5, p.deck_z - 0.32), col),
            mats["timber_bare"]))

    # KORKULUK: yanlarda, ucta yok (kayik oradan yanasir).
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"Korkuluk_{sx}", (0.12, L, 0.10),
                        (sx * (W * 0.5 - 0.12), -L * 0.5, p.deck_z + 1.0), col),
            mats["timber_bare"]))
        for i in range(p.piles):
            y = -L * (i + 0.5) / p.piles
            parts.append(hz.assign(
                hz.make_tube(f"KorkulukDikme_{i}{sx}", 0.09, 0.08, 1.0,
                             (sx * (W * 0.5 - 0.12), y), p.deck_z + 0.11,
                             segments=6, col=col), mats["timber_bare"]))

    # BAGLAMA BABALARI: ucta, kalin ve yuksek.
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_tube(f"Baba_{sx}", 0.34, 0.30, 1.5,
                         (sx * (W * 0.5 - 0.6), -(L - 1.2)), p.deck_z + 0.11,
                         segments=8, col=col), mats["timber_bare"]))

    # KAYIKHANE: kiyi ucunda kucuk ahsap sundurma.
    if p.kayikhane:
        kh_w, kh_d, kh_h = W + 1.2, 5.0, 3.2
        parts.append(hz.assign(
            hz.make_box(f"Kayikhane_{asset_name}", (kh_w, kh_d, kh_h),
                        (0.0, -2.8, p.deck_z + kh_h * 0.5), col),
            mats["timber_bare"]))
        parts.append(hz.make_hip_roof(f"KayikhaneCati_{asset_name}",
                                      kh_w + 1.0, kh_d + 1.0, 1.4,
                                      (0.0, -2.8), p.deck_z + kh_h, col=col))
        hz.assign(parts[-1], mats["roof"])

    l1.append(hz.assign(hz.make_box(f"L1_{asset_name}", (W, L, 0.9),
                                    (0.0, -L * 0.5, p.deck_z), col),
                        mats["timber_bare"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["timber_bare"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="iskele", palette=p.palette, status="draft",
                accuracy="D3", material="ahsap",
                length=p.length, width=p.width, piles=p.piles,
                deck_z=p.deck_z, kayikhane=p.kayikhane,
                # Kiyi cizgisinin yerel NORMALINDEN olculdu (denize dogru).
                face_deg=ISK_FACE_DEG)
    return lod0, lod1, ucx, info
