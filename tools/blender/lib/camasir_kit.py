"""
**Çamaşır ipi** — mahalle avlusunun donatısı.

## Neden bu varlık dikkatle yapıldı

Plan donatı geçişinde çamaşır iplerini sayıyor ama **RESEARCH.md'de kayıt
yok**, ve kayıt olmayan yerde en büyük risk yanlış şehri çizmektir.

Sokağın bir yanından öbür yanına gerilmiş, cepheler arasında dalgalanan
çamaşır **Napoli/Cenova imgesidir**. Osmanlı mahallesinde çamaşır
avludadır: konut *hayatlı* ve *avlulu*dur, avlu duvarla çevrilidir ve
mahremiyet o duvarın işidir. Çamaşırı sokağa asmak, evin içini sokağa
asmak olurdu — mahalle dokusunun bütün mantığına aykırı.

Bu yüzden kit **sokak üstü ip üretmez**. Ürettiği şey avlu içinde, iki
direk ya da duvar arasında gerilmiş kısa bir iptir. Caner'in isteği
(*"hoş olabilir ya, çok abartmadığımız sürece"*) buradaki karşılığını
böyle buluyor: abartmamak bir **sıklık** ayarı değil, bir **yer** kararı.

## İp sarkar

Gerilmiş bir ip düz değildir; kendi ağırlığıyla **zincir eğrisi**
(catenary) çizer ve çamaşır asılınca daha da sarkar. Düz bir çizgi
"çubuk" gibi okunur. Sarkma `SAG_RATIO` ile açıklık boyunun oranı olarak
verilir.

## Doğruluk: D3

Ölçü kaynağı yok. Tek sayısal kısıt oranlar: ip 1,6-2,0 m yükseklikte
(asan kişinin erişebileceği), açıklık 2,5-4,5 m (avlu ölçeği), sarkma
açıklığın %8-14'ü (gerilmiş ama gergin olmayan ip).
"""

import math

import hz_blender as hz
import ottoman_kit as kit


#: İpin gerildiği yükseklik (m) — asan kişinin erişebileceği kot.
ROPE_Z = 1.78

#: Sarkma, açıklığın oranı olarak. Düz ip çubuk gibi okunur.
SAG_RATIO = 0.11

#: İp boyunca kaç parça (zincir eğrisini yaklaşıklamak için).
ROPE_SEGMENTS = 9


class CamasirParams(object):
    """
    Tek bir çamaşır ipi. `span` iki direk arası (m), `cloth` asılı parça
    sayısı (0 = boş ip).
    """

    def __init__(self, span=3.4, cloth=4, posts=True, palette="default"):
        self.span = span
        self.cloth = cloth
        self.posts = posts
        self.palette = palette

    def validate(self):
        # Avlu olcegi: 2,5 m'den kisa ip ise yaramaz, 4,5 m'den uzun olan
        # avluya sigmaz ve sokak boyu bir ipe donusur — kitin kacindigi sey.
        if not 2.5 <= self.span <= 4.5:
            raise ValueError(f"span={self.span} — avlu olceginde degil "
                             "(2,5-4,5 m). Daha uzunu SOKAK ipi olur ve bu "
                             "kit bilerek onu uretmez.")
        if self.cloth > 6:
            raise ValueError("bir avlu ipinde altidan cok parca abartidir")
        return self


def _rope(name, span, sag, z, col, segments=ROPE_SEGMENTS):
    """
    Sarkan ip — zincir eğrisi yaklaşıklaması.

    Parabol kullanılıyor: gerçek catenary `cosh`tur ama bu açıklıkta ikisi
    gözle ayırt edilemez ve parabol her segmentin eğimini doğrudan verir.
    """
    parts = []
    def yukseklik(t):                       # t: -1 .. +1
        return z - sag * (1.0 - t * t)
    for i in range(segments):
        t0 = -1.0 + 2.0 * i / segments
        t1 = -1.0 + 2.0 * (i + 1) / segments
        x0, x1 = t0 * span * 0.5, t1 * span * 0.5
        z0, z1 = yukseklik(t0), yukseklik(t1)
        ln = math.hypot(x1 - x0, z1 - z0)
        b = hz.make_box(f"{name}_{i}", (ln * 1.04, 0.022, 0.022),
                        (0.0, 0.0, 0.0), col)
        b.rotation_euler = (0.0, -math.atan2(z1 - z0, x1 - x0), 0.0)
        b.location = ((x0 + x1) * 0.5, 0.0, (z0 + z1) * 0.5)
        parts.append(b)
    return parts


def build_camasir(p, col, asset_name, textured=False):
    """Avlu çamaşır ipi. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    span = p.span
    sag = span * SAG_RATIO

    # --- IP ---------------------------------------------------------------
    for o in _rope(f"Ip_{asset_name}", span, sag, ROPE_Z, col):
        parts.append(hz.assign(o, mats["trim"]))

    # --- DIREKLER ---------------------------------------------------------
    # Ip iki direge baglanir. Duvara baglanan varyantta direk yoktur ama
    # o zaman ipin nereye tutundugu gorunmez; tekil varlik olarak
    # direkli uretilir ve sahnede duvara yaslanabilir.
    if p.posts:
        for sx in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"Direk_{sx}", (0.09, 0.09, ROPE_Z + 0.12),
                (sx * span * 0.5, 0.0, (ROPE_Z + 0.12) * 0.5), col),
                mats["timber_bare"]))

    # --- ASILI PARCALAR ---------------------------------------------------
    #
    # Bez ipin USTUNDEN sarkar: ip bezin icinden gecer, bez iki yana duser.
    # Duz bir dikdortgeni ipin altina asmak "levha" gibi okunur.
    for k in range(p.cloth):
        t = -1.0 + 2.0 * (k + 0.5) / max(1, p.cloth)
        x = t * span * 0.42
        z_ip = ROPE_Z - sag * (1.0 - t * t)
        # Parca boyu degisir: mendil, gomlek, carsaf ayni ipte durur.
        boy = 0.42 + 0.30 * ((k * 7) % 5) / 4.0
        en = 0.34 + 0.16 * ((k * 3) % 4) / 3.0
        for sy in (-1, 1):
            b = hz.make_box(f"Bez_{k}_{sy}", (en, 0.012, boy),
                            (0.0, 0.0, 0.0), col)
            # Iki yan hafifce disa acilir — bez ipin ustunden asilmis olur.
            b.rotation_euler = (math.radians(6.0) * sy, 0.0, 0.0)
            b.location = (x, sy * 0.018, z_ip - boy * 0.5 - 0.01)
            parts.append(hz.assign(b, mats["plaster"]))

    l1.append(hz.assign(hz.make_box(f"L1_{asset_name}",
                                    (span, 0.30, ROPE_Z * 0.5),
                                    (0.0, 0.0, ROPE_Z * 0.75), col),
                        mats["trim"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], 0.30, mx[2] - mn[2]),
                      (0.0, 0.0, (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["trim"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="camasir", palette=p.palette,
                status="draft", accuracy="D3",
                span=round(span, 2), sag=round(sag, 3), cloth=p.cloth,
                courtyard_only=True)
    return lod0, lod1, ucx, info
