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
from mathutils import Matrix

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


def _on_yonu(o):
    """
    Gövdenin baktığı Y yönü: -1 (-Y) ya da +1 (+Y). Ölçü **ayaktan**.

    ## Neden kafadan değil

    Önce kafa bandındaki en uzak noktaya baktım — ve o nokta burun
    değil **ense kubbesi** çıktı. Kafa y=0'da merkezli değildir, o
    yüzden `abs(en_uzak)` karşılaştırması yüzü değil kafanın hangi
    tarafının orijinden uzak olduğunu ölçer. Ölçüm "+Y" dedi; aynı
    gövdenin -Y'den alınan render'ı **yüzü** gösterdi. Sayı yanlış,
    resim doğruydu.

    ## Ayak neden şüpheye yer bırakmıyor

    Ayak bileğinden parmak ucuna olan mesafe, topuğa olanın yaklaşık
    iki katıdır — ve bu her insanda böyledir. Tek şart doğru referans:
    gövde merkezi değil, **baldırın kendisi**. `karakter_kit`'in eski
    hatası tam buydu; oradaki not "referans olarak bütün köşelerin
    ağırlık merkezini alıyordu" diyor.

    Referans: z = 0,06-0,11 bandındaki (alt baldır) köşelerin y
    ortalaması. Taban: z < 0,02 (yere basan taban).
    """
    vs = [v.co for v in o.data.vertices]
    zs = [v.z for v in vs]
    zmin, zmax = min(zs), max(zs)
    boy = zmax - zmin
    baldir = [v.y for v in vs
              if zmin + boy * 0.035 <= v.z <= zmin + boy * 0.065]
    taban = [v.y for v in vs if v.z <= zmin + boy * 0.012]
    if not baldir or not taban:
        return 0
    ref = sum(baldir) / len(baldir)
    ileri = max(taban) - ref          # +Y yonunde tasma
    geri = ref - min(taban)           # -Y yonunde tasma
    if abs(ileri - geri) < boy * 0.005:
        return 0                      # ayirt edilemiyor
    return 1 if ileri > geri else -1


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


def taban_getir_mpfb(col=None, makro=None, hedef_boy=None, alt_bolme=0):
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
    o.data.transform(o.matrix_world)
    o.matrix_world = Matrix.Identity(4)
    o.data.update()

    boy_hedef = float(hedef_boy or kar.HEDEF_BOY)
    mn, mx = _sinir(o)
    boy = mx[2] - mn[2]
    if boy < 1e-6:
        raise MpfbYok("Uretilen govdenin yuksekligi sifir.")
    olcek = boy_hedef / boy
    o.data.transform(Matrix.Diagonal((olcek, olcek, olcek, 1.0)))
    o.data.update()

    # Ayaklar tam z=0'a otursun: MPFB'nin "feet_on_ground" ayari
    # yaklasik calisiyor (olculdu: taban −0,0271 m). Boru hatti pivotun
    # tabanda oldugunu VARSAYIYOR ve bu varsayim yerlestiricilere kadar
    # gidiyor — 2,7 cm gomulu bir karakter kimsenin fark etmeyecegi ama
    # her karede duran bir hatadir.
    mn, mx = _sinir(o)
    o.data.transform(Matrix.Translation((0.0, 0.0, -mn[2])))
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
    if _on_yonu(o) > 0:
        o.data.transform(Matrix.Rotation(math.pi, 4, "Z"))
        o.data.update()
    if _on_yonu(o) > 0:
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
    return o


def olc(o):
    """Üretilen gövdenin ölçüsü — katalog ve denetim okur."""
    mn, mx = _sinir(o)
    return dict(vertex=len(o.data.vertices),
                ucgen=sum(len(p.vertices) - 2 for p in o.data.polygons),
                en=round(mx[0] - mn[0], 4),
                derinlik=round(mx[1] - mn[1], 4),
                boy=round(mx[2] - mn[2], 4),
                taban_z=round(mn[2], 5))

