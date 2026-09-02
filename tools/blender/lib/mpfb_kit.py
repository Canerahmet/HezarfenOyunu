"""
**MPFB2 taban gövdesi** — parametrik insan, `karakter_kit.taban_getir`'in
yerine geçebilen bir kaynak.

## Neden

Caner (2026-08-30): *"daha gercekci modeller ve karakterler uretebilir
miyiz?"* — ve ADR 0068 bu soruyu 28 Ağustos'ta zaten incelemiş,
**MakeHuman: evet** demişti. O gün geldi.

Bugüne kadarki taban Blender Studio'nun CC0 paketiydi: iyi bir gövde ama
**tek** bir gövde (10 582 vertex, 1,69 m). MPFB2 parametriktir — yaş,
cinsiyet, boy, kilo, kas, oran, ırk kaydırıcıları. Faz 6'nın 40 000
sakininde asıl karşılığı budur: tek tabandan yüzlerce farklı gövde.

## Lisans — ticari yayın için doğrulandı

Eklenti kodu **GPL-3.0-or-later**; **çıktı CC0**. MakeHuman'ın kendi
SSS'i birebir: *"All core assets (the base mesh, targets, skins…) are
shared under CC0."* GPL aracı bağlar, ürettiğini değil — Blender'ın
kendisi gibi. Kayıt: `refs/LICENSES.md`.

**Sınır:** yalnız çekirdek varlıklar. Üçüncü taraf asset pack'i ayrı
lisans ister ve indirilmez.

## Sözleşme

`taban_getir_mpfb()` `karakter_kit.taban_getir()` ile **aynı şeyi**
döndürür: kimlik dönüşümlü, modifier'sız, ayakları z=0'da, boyu
`karakter_kit.HEDEF_BOY` olan tek bir mesh nesnesi. Hattın geri kalanı
(kıyafet gövdeden kopyalanır, rig gövdeden ölçülür) bu yüzden
değişmeden çalışır — ADR 0068'in *"taban değişimi bu hattın tasarlandığı
durumdur"* iddiası tam olarak buna dayanıyor.
"""

import math

import bpy
from mathutils import Matrix, Vector

import hz_blender as hz


#: Uzantı yolu — Blender 4.2+ eklentileri `bl_ext.<repo>.<id>` olarak
#: içe aktarılır. Kullanıcı kurulumu `user_default` deposuna düşer.
UZANTI = "bl_ext.user_default.mpfb"

#: MPFB gövdesinin GÖRÜNEN kısmını taşıyan vertex grubu. Geri kalanı
#: yardımcı geometridir (kıyafet uydurma ve eklem küpleri) ve oyun
#: mesh'ine girmez.
GOVDE_GRUBU = "body"


def _sinir(o):
    """
    Mesh'in KOSELERINDEN sınır — `hz.bounds` yerine.

    `hz.bounds` `obj.bound_box` okur ve o **önbelleklidir**: mesh verisi
    `o.data.transform(...)` ile doğrudan değiştirildiğinde depsgraph
    güncellenene kadar eski değeri döndürür. Ölçüldü — gövde 1,70'e
    ölçeklendikten sonra bile 1,6594 okundu, yani ölçek yanlış bir
    yükseklikten hesaplanmıştı.

    Bu, bu oturumda defalarca çıkan aynı kusur: bozuk olan ölçtüğün şey
    değil, ölçme biçimin. Köşeleri saymak önbellek tanımaz.
    """
    vs = o.data.vertices
    if not vs:
        return (0.0, 0.0, 0.0), (0.0, 0.0, 0.0)
    mn = [1e30, 1e30, 1e30]
    mx = [-1e30, -1e30, -1e30]
    for v in vs:
        for i in range(3):
            if v.co[i] < mn[i]:
                mn[i] = v.co[i]
            if v.co[i] > mx[i]:
                mx[i] = v.co[i]
    return tuple(mn), tuple(mx)


class MpfbYok(RuntimeError):
    """MPFB kurulu değil — ne yapılacağını AÇIKÇA söyler."""


def _servisler():
    """MPFB servislerini getirir; yoksa ne yapılacağını söyleyerek patlar."""
    try:
        mod = __import__(f"{UZANTI}.services.humanservice",
                         fromlist=["HumanService"])
        hedef = __import__(f"{UZANTI}.services.targetservice",
                           fromlist=["TargetService"])
        return mod.HumanService, hedef.TargetService
    except Exception as e:                                  # noqa: BLE001
        raise MpfbYok(
            "MPFB2 kurulu degil ya da yuklenemedi: " + str(e) + "\n"
            "Kurulum:\n"
            "  blender --command extension install-file "
            "-r user_default -e <mpfb.zip>\n"
            "Paket: extensions.blender.org/add-ons/mpfb/ (GPL-3.0+, "
            "cikti CC0 — kayit refs/LICENSES.md)") from e


def makro_varsayilan():
    """Kaydırıcıların varsayılan sözlüğü (yaş, cinsiyet, boy, kilo…)."""
    _, TargetService = _servisler()
    return TargetService.get_default_macro_info_dict()


#: İnsan göz küresi çapı (m). Yetişkinde 24 mm ve kişiden kişiye
#: neredeyse hiç değişmez — bu yüzden bir ayar değil bir sabit.
GOZ_CAPI = 0.024

#: MakeHuman taban mesh'inde göz küresini taşıyan köşe grupları.
GOZ_GRUPLARI = ("helper-l-eye", "helper-r-eye")


def _grup_koseleri(o, adlar):
    """Adı verilen köşe gruplarındaki köşe indeksleri."""
    idx = set()
    for ad in adlar:
        vg = o.vertex_groups.get(ad)
        if vg is None:
            continue
        for v in o.data.vertices:
            if any(g.group == vg.index for g in v.groups):
                idx.add(v.index)
    return idx


def _goz_topla(o):
    """
    Göz küresinin ham geometrisi: `(koseler, yuzler)`.

    Nesne olarak değil **veri** olarak toplanıyor, çünkü gövdeye
    uygulanacak dönüşümlerin aynısı sonradan tek matrisle buna da
    uygulanacak. Bir Blender nesnesi taşımak, o nesnenin dönüşüm
    geçmişini ayrıca yönetmek demek olurdu.
    """
    import bmesh                                    # noqa: PLC0415
    idx = _grup_koseleri(o, GOZ_GRUPLARI)
    if not idx:
        return None
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.verts.ensure_lookup_table()
    sil = [v for v in bm.verts if v.index not in idx]
    bmesh.ops.delete(bm, geom=sil, context="VERTS")
    bm.verts.ensure_lookup_table()
    koseler = [v.co.copy() for v in bm.verts]
    yerel = {v: i for i, v in enumerate(bm.verts)}
    yuzler = [[yerel[v] for v in f.verts] for f in bm.faces]
    bm.free()
    return koseler, yuzler


def _sadece_govde(o, koru=()):
    """`body` (+ `koru`) grubunda olmayan her köşeyi siler."""
    import bmesh                                    # noqa: PLC0415
    tut = _grup_koseleri(o, (GOVDE_GRUBU,) + tuple(koru))
    if not tut:
        return
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.verts.ensure_lookup_table()
    sil = [v for v in bm.verts if v.index not in tut]
    if sil:
        bmesh.ops.delete(bm, geom=sil, context="VERTS")
    bm.to_mesh(o.data)
    bm.free()
    o.data.update()


def _goz_olcekle(obj, cap):
    """Her gözü **kendi merkezine göre** gerçek çapa indirir."""
    me = obj.data
    for isaret in (-1, 1):
        idx = [v.index for v in me.vertices if (v.co.x * isaret) > 0.0]
        if not idx:
            continue
        ps = [me.vertices[i].co for i in idx]
        mn = Vector((min(p.x for p in ps), min(p.y for p in ps),
                     min(p.z for p in ps)))
        mx = Vector((max(p.x for p in ps), max(p.y for p in ps),
                     max(p.z for p in ps)))
        c = (mn + mx) * 0.5
        # Kafesin capi: uc eksenin ORTALAMASI. Tek eksen almak yaniltir,
        # cunku kafes kure degil hafif basik bir elipsoit.
        d = ((mx[0] - mn[0]) + (mx[1] - mn[1]) + (mx[2] - mn[2])) / 3.0
        if d < 1e-9:
            continue
        k = cap / d
        for i in idx:
            me.vertices[i].co = c + (me.vertices[i].co - c) * k
    me.update()
    return obj


def goz_yuvaya_otur(goz, govde, gomulme=0.0015):
    """
    Göz küresini **gövdeden ölçülen yuvasına** oturtur.

    ## Neden gerekti

    Helper küresi bir kafestir ve gerçek göz küresinden büyük. İlk
    denemede olduğu gibi bırakıldı: render'da küre kapakların önünde
    duruyor, yüz "patlak gözlü" okuyor. İkinci denemede anatomik çapa
    (24 mm) indirildi: bu kez küçük kaldı, yuvanın etrafında beyaz bir
    halka oluştu ve göz kaşın hizasına çıktı.

    İki deneme de aynı hatayı yaptı — kürenin ne kadar büyük olması
    gerektiğini **varsaydı**. Doğru soru yuvanın kendisinde: kapak
    açıklığının çevresindeki ten nerede duruyor? Küre oraya oturur.

    Ölçü: gözün merkezine yakın gövde köşelerinin **en öndeki** yüzeyi
    (burun -Y'de, yani en küçük y). Kürenin ön kutbu o yüzeyin
    `gomulme` kadar gerisine konur — 1,5 mm, gözyaşı filmi kalınlığı
    değil, kapağın kirpik kenarının kalınlığı.
    """
    if goz is None or govde is None:
        return goz
    gme, bme = goz.data, govde.data
    for isaret in (-1, 1):
        idx = [v.index for v in gme.vertices if (v.co.x * isaret) > 0.0]
        if not idx:
            continue
        ps = [gme.vertices[i].co for i in idx]
        c = Vector((sum(p.x for p in ps) / len(ps),
                    sum(p.y for p in ps) / len(ps),
                    sum(p.z for p in ps) / len(ps)))
        r = max((p - c).length for p in ps)

        # Yuvanin ONU: gozun merkezine yatayda yakin gövde koseleri.
        # Yaricapin 1,6 kati — kapagin kenarini alacak kadar genis,
        # burnu ve kasi almayacak kadar dar.
        menzil = r * 2.5
        on = None
        for v in bme.vertices:
            d = v.co - c
            if abs(d.x) > menzil or abs(d.z) > menzil:
                continue
            if d.length > menzil:
                continue
            if on is None or v.co.y < on:
                on = v.co.y
        if on is None:
            _en = min((v.co - c).length for v in bme.vertices)
            _yakin = min(bme.vertices, key=lambda v: (v.co - c).length)
            hz.log(f"goz {'sag' if isaret > 0 else 'sol'}: yuva bulunamadi "
                   f"(merkez {c.x:.3f},{c.y:.3f},{c.z:.3f} r {r*1000:.1f} mm, "
                   f"en yakin govde kosesi {_en*1000:.1f} mm @ "
                   f"{_yakin.co.x:.3f},{_yakin.co.y:.3f},{_yakin.co.z:.3f}, "
                   f"govde kose {len(bme.vertices)})")
            continue
        # Kurenin on kutbu: c.y - r. Hedef: on + gomulme.
        kaydir = (on + gomulme) - (c.y - r)
        for i in idx:
            gme.vertices[i].co.y += kaydir
        hz.log(f"goz {'sag' if isaret > 0 else 'sol'}: yaricap {r*1000:.1f} mm, "
               f"yuva onu {on:.4f}, kaydirma {kaydir*1000:+.1f} mm")
    gme.update()
    return goz


def _goz_kur(ham, donusum, col):
    """Toplanan ham göz geometrisinden nesneyi kurar."""
    if not ham:
        return None
    koseler, yuzler = ham
    me = bpy.data.meshes.new("SM_Goz")
    me.from_pydata([donusum @ c for c in koseler], [], yuzler)
    me.update()
    obj = bpy.data.objects.new("Goz", me)
    if col is not None:
        col.objects.link(obj)
    else:
        bpy.context.scene.collection.objects.link(obj)
    # BIR KEZ BOLUNUR — IRISIN KENARI KOSELI OLMASIN.
    #
    # MakeHuman'in helper goz kuresi goz basina 70 yuz; iris yon
    # konisine dusen yuz sayisi 11 oluyor ve o kadar az yuzle daire
    # degil sekizgen ciziliyor. Bir bolme sonrasi 44 yuz: iris yuvarlak
    # okunuyor. Bedeli iki goz icin ~840 ucgen — 58 bin ucgenlik bir
    # karakterde %1,4, ve harcandigi yer YUZ.
    # HELPER KURESI GOZ DEGIL, GOZ KAFESIDIR — GERCEK CAPA INDIRILIR.
    #
    # MPFB'nin `helper-*` gruplari giysi/sac/goz varliklarinin OTURACAGI
    # kafeslerdir ve bilerek buyuktur. Renderda goruldu: kure kapaklarin
    # onunde duruyor, yuz "patlak gozlu" okunuyor.
    #
    # Insan goz kuresi capi 24 mm ve neredeyse hic degismez — bebekte
    # bile 16-17 mm, yetiskinde 24. Yani bu bir zevk ayari degil bir
    # OLCU: kafesin capi olculur, 24 mm'ye oranlanir, her goz KENDI
    # merkezine gore kucultulur. Merkez korunur cunku kafesin isaret
    # ettigi yer gozun donme merkezidir.
    _goz_olcekle(obj, GOZ_CAPI)

    bol = obj.modifiers.new("GozBolme", "SUBSURF")
    bol.levels = 1
    bol.render_levels = 1
    bol.subdivision_type = "CATMULL_CLARK"
    dg = bpy.context.evaluated_depsgraph_get()
    yeni_me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    obj.modifiers.clear()
    eski_me = obj.data
    obj.data = yeni_me
    obj.data.name = obj.name
    bpy.data.meshes.remove(eski_me)
    me = obj.data


    for poly in me.polygons:
        poly.use_smooth = True
    return obj


def taban_getir_mpfb(col=None, makro=None, hedef_boy=None, alt_bolme=0,
                     goz_ver=False):
    """
    Parametrik taban gövde üretir ve boru hattının sözleşmesine sokar.

    `makro` verilmezse varsayılan (ortalama yetişkin) kullanılır.
    `hedef_boy` verilmezse `karakter_kit.HEDEF_BOY` (1,70 m).
    """
    import karakter_kit as kar                     # dairesel ithali kir

    HumanService, TargetService = _servisler()

    info = HumanService._create_default_human_info_dict()
    info["phenotype"] = dict(TargetService.get_default_macro_info_dict())
    if makro:
        info["phenotype"].update(makro)

    ayar = HumanService.get_default_deserialization_settings()
    # OYUN MESH'I: alt bolme yok. Varsayilan 1, yani dort kat ucgen —
    # bu govdeden kiyafet de KOPYALANIYOR (kiyafet_kit.kopya_kabuk),
    # yani alt bolme bedeli iki kere odenir.
    ayar["subdiv_levels"] = int(alt_bolme)
    ayar["load_clothes"] = False
    ayar["override_rig"] = ""          # rig BIZDE: rig_kit govdeden olcer
    ayar["feet_on_ground"] = True
    # YARDIMCI GEOMETRI SILINMESIN — GOZ ONUN ICINDE.
    #
    # `mask_helpers` varsayilan True ve MPFB helper koselerini bir MASK
    # modifier'iyla ayikliyor; biz de onu uyguluyorduk. Olculdu: sonucta
    # `helper-l-eye` grubu BOS kaliyor, yani goz kuresi mesh'ten tamamen
    # cikiyor. Kapatiliyor; ayiklamayi asagida KENDIMIZ yapiyoruz ve
    # gozu ayirdiktan sonra yapiyoruz.
    ayar["mask_helpers"] = False

    onceki = set(bpy.data.objects)
    HumanService.deserialize_from_dict(info, ayar)
    yeni = [o for o in set(bpy.data.objects) - onceki if o.type == "MESH"]
    if not yeni:
        raise MpfbYok("MPFB govde uretmedi (mesh donmedi).")
    # Birden fazla mesh donerse en cok koseli olan govdedir.
    o = max(yeni, key=lambda x: len(x.data.vertices))

    # --- YARDIMCI GEOMETRI SILINIR ---------------------------------
    #
    # MPFB govdesi 19 158 koseyle gelir ama bunun bir kismi GORUNMEZ
    # yardimci geometridir: kiyafet uydurma kabuklari (`helper-*`) ve
    # eklem kupleri (`joint-*`). MPFB onlari bir MASK modifier'iyla
    # gizler ("Hide helpers", grup `body`). Gizlemek oyun icin yetmez —
    # export edilen ag onlari yine tasir. Modifier UYGULANIR.
    # SHAPE KEY'LER ONCE PISIRILIR.
    #
    # MPFB govdesi morph hedeflerini shape key olarak tasir ve Blender
    # shape key'li bir mesh'e modifier UYGULAMAZ ("Modifier cannot be
    # applied to a mesh with shape keys" — ilk kosuda tam bunu dedi).
    # Elle pisirmeye kalkmak yerine aracin kendi islevi kullaniliyor:
    # `bake_targets` gecici bir karisim anahtari kurup otekileri siler,
    # sonra onu da siler; geriye pismis mesh kalir.
    TargetService.bake_targets(o)

    # --- GOZ AYRILIR (maskeden ONCE) -------------------------------
    #
    # Sira onemli: maske uygulanirsa goz kuresi mesh'ten silinir ve
    # geriye adi duran bos bir kose grubu kalir.
    # GOZ SIMDILIK MESH'IN ICINDE KALIR.
    #
    # Ilk yazimda goz burada ayri bir nesne olarak toplaniyor ve gövdeye
    # uygulanan dönüşümler bir matriste biriktirilip ona da
    # uygulaniyordu. Ölçüm reddetti: gozun merkezine en yakin gövde
    # kösesi **100,7 mm** çıktı — yani küre yüzün on santim önünde
    # duruyordu. Matris birikimi bir yerde yanlıştı ve hangi adımda
    # olduğunu aramak, aramaya değmeyecek bir şeydi.
    #
    # Çünkü daha basit ve yanılmaz bir yol var: gözü gövdenin İÇİNDE
    # bırakmak. Aynı mesh'in köşesi olarak bütün dönüşümleri kendiliğinden
    # alır ve hiçbir matris yazılmaz. Ayırma en sona, dönüşümler
    # bittikten sonraya kalır.
    #
    # Öteki yardımcı geometri (saç, etek, diş kafesleri) burada silinir:
    # kalsalardı gövdenin ölçülen boyu saç kafesinin tepesi olurdu ve
    # ölçek yanlış çıkardı. Göz kafesi kafanın İÇİNDE, sınırı büyütmez.
    _sadece_govde(o, koru=GOZ_GRUPLARI if goz_ver else ())

    # --- YARDIMCI GEOMETRIYI KENDIMIZ AYIKLA -----------------------
    #
    # `mask_helpers` kapatildi, yani MPFB'nin kurdugu maske artik yok.
    # Ayni isi burada yapiyoruz: `body` grubunda OLMAYAN her kose
    # silinir. Fark su ki bu satir GOZ AYRILDIKTAN sonra kosuyor.


    maske = [m for m in o.modifiers if m.type == "MASK"]
    if maske:
        bpy.context.view_layer.objects.active = o
        for m in maske:
            if m.vertex_group != GOVDE_GRUBU:
                continue
            bpy.ops.object.modifier_apply(modifier=m.name)
    o.modifiers.clear()
    o.animation_data_clear()

    # --- SOZLESME: kimlik donusumu, ayaklar z=0, boy HEDEF_BOY -------
    #
    # `taban_getir` ile ayni son durum; hattin geri kalani bunu bekliyor
    # ve butun olcum kodu yerel koordinat yaziyor.
    # DONUSUMLER BIRIKTIRILIR — goz ayni yolu gecmek zorunda.
    _don = Matrix.Identity(4)

    _m = o.matrix_world.copy()
    o.data.transform(_m)
    _don = _m @ _don
    o.matrix_world = Matrix.Identity(4)
    o.data.update()

    boy_hedef = float(hedef_boy or kar.HEDEF_BOY)
    mn, mx = _sinir(o)
    boy = mx[2] - mn[2]
    if boy < 1e-6:
        raise MpfbYok("Uretilen govdenin yuksekligi sifir.")
    olcek = boy_hedef / boy
    _m = Matrix.Diagonal((olcek, olcek, olcek, 1.0))
    o.data.transform(_m)
    _don = _m @ _don
    o.data.update()

    # Ayaklar tam z=0'a otursun: MPFB'nin "feet_on_ground" ayari
    # yaklasik calisiyor (olculdu: taban −0,0271 m). Boru hatti pivotun
    # tabanda oldugunu VARSAYIYOR ve bu varsayim yerlestiricilere kadar
    # gidiyor — 2,7 cm gomulu bir karakter kimsenin fark etmeyecegi ama
    # her karede duran bir hatadir.
    mn, mx = _sinir(o)
    _m = Matrix.Translation((0.0, 0.0, -mn[2]))
    o.data.transform(_m)
    _don = _m @ _don
    o.data.update()

    # --- BURUN -Y'YE CEVRILIR -----------------------------------
    #
    # Hattin sozlesmesi: burun Blender -Y'de (Unity +Z) —
    # `karakter_kit.one_cevir`. MPFB govdesi +Y'ye bakiyor.
    #
    # Bunu hattin istatistiksel yon olcumune BIRAKMIYORUM ve sebebi
    # olculdu: `one_cevir` guven 0,50'nin altinda donmuyor, MPFB
    # govdesinde guven 0,40 cikti — yani hem donus hem de "burun +Y'de
    # kalmasin" degismezi ATLANDI. Karakter sirti donuk uretildi ve
    # uretim hicbir hata vermedi. Bu proje ayni kusuru bir kez yasadi;
    # o zaman da "hata ancak oyunda, kamera arkaya gectiginde goruldu".
    #
    # Kaynagin kendi sozlesmesi BILINIYOR, o yuzden donus kesin yapilir
    # ve sonra OLCULEREK dogrulanir.
    if kar.on_yonu(o) > 0:
        _m = Matrix.Rotation(math.pi, 4, "Z")
        o.data.transform(_m)
        _don = _m @ _don
        o.data.update()
    if kar.on_yonu(o) > 0:
        raise MpfbYok(
            "Govde donusten sonra hala +Y'ye bakiyor — olcum ters calisiyor.")

    # ONBELLEGI TAZELE — BU NESNE BASKALARINA VERILIYOR.
    #
    # `o.data.transform(...)` mesh'i degistirir ama `obj.bound_box`
    # onbelleklidir ve depsgraph guncellenene kadar ESKI degeri doner.
    # Kendi olcumumu koselerden yaparak kurtulmustum; ama bu nesne
    # `karakter_kit.normalize`'a gidiyor ve O `hz.bounds` okuyor —
    # govde 1,70'e olceklenmis oldugu halde "boy 1,6594, hedef 1,7"
    # diye patladi.
    #
    # Kendi olcumumu duzeltip komsununkini bozuk birakmak, kusuru
    # cozmek degil tasimaktir. Tazeleme burada yapilir.
    o.data.update()
    if bpy.context.view_layer:
        bpy.context.view_layer.update()

    hz.link(o, col)
    if not goz_ver:
        return o

    # AYIRMA EN SONDA: butun donusumler bitti, goz artik yerinde.
    _ham = _goz_topla(o)
    _sadece_govde(o)                     # gozu govdeden cikar
    goz = _goz_kur(_ham, Matrix.Identity(4),
                   hz.collection(col) if isinstance(col, str) else col)
    if goz is None:
        hz.log("UYARI: goz kuresi bulunamadi — MPFB helper gruplari bos.")
    return o, goz


def olc(o):
    """Üretilen gövdenin ölçüsü — katalog ve denetim okur."""
    mn, mx = _sinir(o)
    return dict(vertex=len(o.data.vertices),
                ucgen=sum(len(p.vertices) - 2 for p in o.data.polygons),
                en=round(mx[0] - mn[0], 4),
                derinlik=round(mx[1] - mn[1], 4),
                boy=round(mx[2] - mn[2], 4),
                taban_z=round(mn[2], 5))

