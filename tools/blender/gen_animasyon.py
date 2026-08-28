"""
Hezarfen: 1632 — Animasyon seti (Faz 5).

Plan Bölüm 10'un istediği klipler: locomotion, tırmanma, kanat kuşanma,
kalkış, süzülüş, iniş/yuvarlanma, çakılma.

Klipler **rig'li karakter blend'inden** üretilir ve her klip kendi
FBX'ine yazılır (`SK_...@Klip.fbx` — Unity'nin sözleşmesi). Tek FBX'e
birden çok action yazmak mümkün ama Unity çoğu zaman yalnızca ilkini
okur ve bunu sessizce yapar.

Kullanım:
  blender --background --python tools/blender/gen_animasyon.py -- [--export]
"""

import argparse
import json
import math
import os
import sys

import bpy

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import anim_kit as ak               # noqa: E402
import hz_blender as hz             # noqa: E402
from export_fbx import export_fbx   # noqa: E402

ANIM_COL = "AnimExport"

#: Kaynak: rig'li uçuş karakteri.
KAYNAK = os.path.join("art", "blend", "karakter", "SK_Hezarfen_Ucus.blend")


def _vekil_mesh(arm, col):
    """Armature'a deri bağlı **en küçük** mesh.

    Tek bir üçgen, bütün ağırlığı `Hips`te. Görevi görünmek değil,
    Unity'ye "bu dosyada deri bağlı geometri var" demek.
    """
    import bmesh
    bm = bmesh.new()
    v = [bm.verts.new((0.0, 0.0, 0.9)), bm.verts.new((0.01, 0.0, 0.9)),
         bm.verts.new((0.0, 0.01, 0.9))]
    bm.faces.new(v)
    obj = hz.mesh_from_bmesh("AnimVekil", bm, col)
    g = obj.vertex_groups.new(name="Hips")
    g.add([0, 1, 2], 1.0, "REPLACE")
    m = obj.modifiers.new("arm", "ARMATURE")
    m.object = arm
    obj.parent = arm
    return obj


def _arm():
    for o in bpy.data.objects:
        if o.type == "ARMATURE":
            return o
    raise SystemExit("[HZ] HATA: blend icinde armature yok.")


def _dongu(arm, ad, kare, hiz, genlik):
    """Yürüyüş/koşu tipi kapalı döngü."""
    act = ak.klip_kur(arm, ad)
    for i in range(kare + 1):
        t = (i % kare) / float(kare)
        ak.sifirla(arm)
        ak.poz_ver(arm, ak.yurume_karesi(t, genlik))
        ak.anahtar(arm, i + 1)
    act.frame_range  # noqa
    return act


def _poz_dizisi(arm, ad, kareler):
    """`[(kare, {kemik: (x,y,z)})]` dizisinden klip."""
    act = ak.klip_kur(arm, ad)
    for kare, poz in kareler:
        ak.sifirla(arm)
        ak.poz_ver(arm, poz)
        ak.anahtar(arm, kare)
    return act


def _durus():
    return {"Spine": (1.5, 0.0, 0.0), "Chest": (-1.0, 0.0, 0.0),
            "LeftUpperArm": (0.0, 0.0, -4.0),
            "RightUpperArm": (0.0, 0.0, 4.0),
            "LeftLowerArm": (-9.0, 0.0, 0.0),
            "RightLowerArm": (-9.0, 0.0, 0.0)}


#: Basış evresinin döngüye oranı. İki ayak 0,5 faz kaymalı olduğu için
#: 0,55 demek %10 çift destek demektir — insan yürüyüşünde de böyledir.
BASIS_ORANI = 0.55


def _merdiven_yolu(t, ilerleme, yukselme, riht, uzanma):
    """Bir bacağın ayak yolu (kalçaya göre, metre): `(y, z)`.

    `ilerleme` / `yukselme`: **basış evresi boyunca gövdenin** yatay ve
    dikey yer değiştirmesi. Ayak o evrede dünyada sabittir, yani gövdeye
    göre tam bu kadar geriye ve aşağı gider.

    ## Neden yol hızdan türer, basamaktan değil

    İlk yazımda yolu doğrudan `basamak` ve `riht`ten kuruyordum ve genlik
    çözücüsü onları ölçekliyordu — ama hız da aynı `basamak`tan
    hesaplanıyordu. İkisi birbiriyle çekişti: çözücü basamağı küçültüyor,
    bekçi hâlâ büyük basamağın hızını bekliyordu. Kayma 6,7 cm'de takıldı.

    Şimdi yol **gövdenin gerçekte gideceği mesafeden** kuruluyor. Kayma
    tanım gereği sıfırdır; ölçüm artık düzeltmiyor, **doğruluyor** —
    bir bekçinin olması gereken iş bu.

    Merdivenin geometrisi (rıht/basamak) yine oradadır: hızı O belirledi.
    """
    if t < BASIS_ORANI:
        u = t / BASIS_ORANI
        y = -ilerleme * (0.5 - u)
        # Basis boyunca bacak ACILIR: basta bukuk (kisa), sonunda
        # neredeyse tam acik. Ilk yazimda yolu kalca kotunun ETRAFINA
        # simetrik koymustum ve basisin sonunda 0,924 m derinlik
        # istiyordu — bacak 0,827 m. Erisilemeyen hedef sessizce
        # kirpiliyor ve kirpilan her karede ayak kayiyordu.
        #
        # Tirmanan bir adamin bacagi zaten boyle calisir: ayak basamaga
        # bukuk konur, govde onun uzerinden gecerken bacak acilir.
        z = -(uzanma - yukselme * (1.0 - u))
    else:
        u = (t - BASIS_ORANI) / (1.0 - BASIS_ORANI)
        y = -ilerleme * (u - 0.5)
        # Salinim: ayak basamagi TEMIZLEYECEK kadar kalkar. Rihtin iki
        # katindan az kaldirmak ayagi basamaga surterdi.
        kalkis = math.sin(math.pi * u) * (riht * 2.0 + 0.05)
        z = -(uzanma - yukselme) - kalkis + yukselme * 0.0
    return y, z


def _merdiven_govde(t):
    """Tırmanışta gövde pozu — bacaklar hariç, IK sırasında sabit."""
    return {"Hips": (12.0, 0.0, 0.0), "Spine": (6.0, 0.0, 0.0),
            "Chest": (4.0, 0.0, 0.0), "Neck": (-8.0, 0.0, 0.0),
            "LeftUpperArm": (-54.0, 0.0, -14.0),      # el duvarda
            "LeftLowerArm": (-28.0, 0.0, 0.0),
            "RightUpperArm": (-14.0 * math.sin(2 * math.pi * t), 0.0, 6.0),
            "RightLowerArm": (-22.0, 0.0, 0.0)}


def _merdiven_kur(arm, kare, ilerleme, yukselme, riht, kalca_z, uzanma):
    """Merdiven klibini sayısal IK ile kurar.

    Ayak yolu **dünya** koordinatında verilir (kalça sabit sayılır);
    her karede iki bacak ayrı ayrı çözülür. Önceki karenin çözümü bir
    sonrakine tahmin olarak verilir — hem hızlandırır hem çözümü aynı
    dalda (diz aynı yöne bükülü) tutar.
    """
    tahmin = {"Left": (0.0, 0.0), "Right": (0.0, 0.0)}
    ayak_tahmin = {"Left": 0.0, "Right": 0.0}
    ofs = {y: ak.parmak_ofseti(arm, y) for y in ("Left", "Right")}
    hata = []
    for i in range(kare + 1):
        t = (i % kare) / float(kare)
        govde = _merdiven_govde(t)
        poz = dict(govde)
        for yan, faz in (("Left", 0.0), ("Right", 0.5)):
            dy, dz = _merdiven_yolu((t + faz) % 1.0, ilerleme, yukselme,
                                    riht, uzanma)
            hedef = (dy, kalca_z + dz)
            a, b = ak.bacak_ik(arm, yan, hedef, taban_poz=govde,
                               tahmin=tahmin[yan])
            tahmin[yan] = (a, b)
            # IK'nin GERCEKTEN yakinsayip yakinsamadigi olculur: hedefe
            # ulasilamiyorsa (bacak yetmiyor) klip sessizce yanlis olur.
            ak.sifirla(arm); ak.poz_ver(arm, govde)
            ak.poz_ver(arm, {f"{yan}UpperLeg": (a, 0.0, 0.0),
                             f"{yan}LowerLeg": (b, 0.0, 0.0)})
            import bpy as _b; _b.context.view_layer.update()
            ul = ak.ayak_dunya(arm, f"{yan}Foot")
            hata.append(((ul.y - hedef[0]) ** 2 + (ul.z - hedef[1]) ** 2) ** 0.5)
            # Parmak ucu da COZULUR: taban basamakta duz kalsin.
            c = ak.ayak_duzle(arm, yan,
                              (hedef[0] + ofs[yan][0], hedef[1] + ofs[yan][1]),
                              govde, a, b, tahmin=ayak_tahmin[yan])
            ayak_tahmin[yan] = c
            poz[f"{yan}UpperLeg"] = (a, 0.0, 0.0)
            poz[f"{yan}LowerLeg"] = (b, 0.0, 0.0)
            poz[f"{yan}Foot"] = (c, 0.0, 0.0)
        ak.sifirla(arm)
        ak.poz_ver(arm, poz)
        ak.anahtar(arm, i + 1)
    hz.log(f"IK yakinsama: ortalama {sum(hata)/len(hata)*100:.1f} cm, "
           f"en kotu {max(hata)*100:.1f} cm")


def klipleri_kur(arm):
    """Bütün klipler. `[(ad, action, tur, hiz, dikey, temas)]` döner."""
    out = []

    # --- LOCOMOTION -------------------------------------------------------
    out.append(("Durus", _poz_dizisi(arm, "Durus", [
        (1, _durus()),
        (30, dict(_durus(), Spine=(2.6, 0.0, 0.0))),
        (60, _durus()),
    ]), "dongu", 0.0, 0.0, None))

    # Kare sayisi ve genlik ELLE YAZILMAZ: ikisi birden cozulur.
    # Hiz `WalkController`dan gelir, tempo insan yuruyusunun kendi
    # araligindan. Adim uzunlugu ikisinin sonucudur, tersi degil.
    #
    # TEMPO (adim/dk): yuruyus 110, kosu 165, merdiven 96 — merdivende
    # adim kisadir ve tempo duser, cunku her adim bir basamak
    # yukseltir.
    # Merdivenin hizi ELLE YAZILMAZ: basamak derinligi x tempo. Dikey
    # hiz da riht x tempo. Boylece tirmanma klibi merdivenin
    # GEOMETRISINDEN turer ve o geometri (T2, taslak) sorgulanabilir bir
    # sayidir; "0,55 m/s" ise sorgulanamaz bir tercihti.
    adim_s = 96.0 / 60.0
    merdiven_hiz = ak.MERDIVEN_BASAMAK * adim_s
    merdiven_dikey = ak.MERDIVEN_RIHT * adim_s
    hz.log(f"merdiven: basamak {ak.MERDIVEN_BASAMAK:.2f} m x {adim_s:.2f} "
           f"adim/s -> yatay {merdiven_hiz:.2f} m/s, dikey "
           f"{merdiven_dikey:.2f} m/s")

    # Bacak uzunluklari ve aci ISARETLERI iskeletten OLCULUR: kemik
    # roll'u otomatik hesaplandigi icin isaret iskeletten iskelete
    # degisebilir ve yanlis isaretli bir IK bacagi ters buker.
    L1, L2 = ak.bacak_boylari(arm, "Left")
    kalca_z = arm.data.bones["LeftUpperLeg"].head_local.z
    ayak_z = arm.data.bones["LeftFoot"].head_local.z
    hz.log(f"bacak: uyluk {L1:.3f} m, baldir {L2:.3f} m, "
           f"kalca-ayak {kalca_z - ayak_z:.3f} m (bacak {L1+L2:.3f} m — "
           "dinlenmede neredeyse tam acik)")

    for ad, hiz, tempo, dikey in (("Yurume", 1.4, 110.0, 0.0),
                                  ("Kosma", 3.6, 165.0, 0.0)):
        kare, genlik, kayma0 = ak.dongu_coz(arm, ak.yurume_karesi, hiz, tempo)
        hz.log(f"{ad}: cozulen dongu {kare} kare "
               f"({kare / float(ak.FPS):.2f} s), genlik {genlik:.2f}, "
               f"tempo {tempo:.0f} adim/dk, kayma {kayma0*100:.1f} cm")
        act = ak.klip_kur(arm, ad)
        for i in range(kare + 1):
            ak.sifirla(arm)
            ak.poz_ver(arm, ak.yurume_karesi((i % kare) / float(kare), genlik))
            ak.anahtar(arm, i + 1)
        out.append((ad, act, "dongu", hiz, dikey, None))

    # --- TIRMANMA: genlik COZULMEZ, geometri zaten belirlemistir --------
    #
    # Yuruyuste genlik serbestti (adim uzunlugu bir tercihtir) ve tempo
    # ile hiz arasinda cozuldu. Merdivende oyle degil: basamagin
    # derinligi adimin uzunlugudur. Cozulecek bir sey yok, yalnizca
    # dogru yolu yazmak var.
    T_m = 120.0 / 96.0
    kare_m = max(6, int(round(T_m * ak.FPS)))
    ilerleme = merdiven_hiz * BASIS_ORANI * T_m
    yukselme = merdiven_dikey * BASIS_ORANI * T_m
    hz.log(f"Merdiven: {kare_m} kare ({T_m:.2f} s), basis boyunca "
           f"ilerleme {ilerleme:.3f} m, yukselme {yukselme:.3f} m")
    act = ak.klip_kur(arm, "Merdiven")
    # Erisilebilir en buyuk uzanma: bacagin %97'si. Tam acik bacak
    # tekildir (Jacobian bozulur) ve gercekte de kimse dizini kilitleyip
    # merdiven cikmaz.
    uzanma = (L1 + L2) * 0.97
    _merdiven_kur(arm, kare_m, ilerleme, yukselme, ak.MERDIVEN_RIHT,
                  kalca_z, uzanma)
    # Pencere AYAK BASINA: sag ayak yarim faz gecikmelidir.
    out.append(("Merdiven", act, "dongu", merdiven_hiz, merdiven_dikey,
                {"LeftToes": (0.03, BASIS_ORANI - 0.03),
                 "RightToes": (0.5 + 0.03, 0.5 + BASIS_ORANI - 0.03)}))

    # --- KUSANMA ----------------------------------------------------------
    # Kanat sirttan alinir, once sol kol gecirilir, sonra sag, sonra
    # kayislar belde sikilir. Tek bir "kollar acilir" pozu bunu
    # anlatmazdi; kusanma bir SIRA isidir.
    out.append(("Kusanma", _poz_dizisi(arm, "Kusanma", [
        (1, _durus()),
        (18, {"Spine": (-8.0, 0.0, 0.0), "RightUpperArm": (-96.0, 0.0, 26.0),
              "RightLowerArm": (-64.0, 0.0, 0.0), "Neck": (-8.0, 0.0, 0.0)}),
        (38, {"Spine": (-4.0, 0.0, 0.0), "LeftUpperArm": (-104.0, 0.0, -30.0),
              "LeftLowerArm": (-70.0, 0.0, 0.0),
              "RightUpperArm": (-62.0, 0.0, 18.0),
              "RightLowerArm": (-38.0, 0.0, 0.0)}),
        (58, {"Spine": (6.0, 0.0, 0.0), "Neck": (10.0, 0.0, 0.0),
              "LeftUpperArm": (-24.0, 0.0, -10.0),
              "RightUpperArm": (-24.0, 0.0, 10.0),
              "LeftLowerArm": (-72.0, 0.0, 0.0),
              "RightLowerArm": (-72.0, 0.0, 0.0)}),
        (76, dict(ak.suzulme_pozu(), Hips=(8.0, 0.0, 0.0))),
    ]), "tek", 0.0, 0.0, None))

    # --- KALKIS -----------------------------------------------------------
    # Cok: cok, sonra it. Kuleden atlayan adam once ALCALIR.
    out.append(("Kalkis", _poz_dizisi(arm, "Kalkis", [
        (1, dict(ak.suzulme_pozu(), Hips=(8.0, 0.0, 0.0))),
        (10, {"Hips": (26.0, 0.0, 0.0), "LeftUpperLeg": (54.0, 0.0, 0.0),
              "RightUpperLeg": (54.0, 0.0, 0.0),
              "LeftLowerLeg": (-72.0, 0.0, 0.0),
              "RightLowerLeg": (-72.0, 0.0, 0.0),
              "Spine": (14.0, 0.0, 0.0),
              "LeftUpperArm": (-58.0, 0.0, 0.0),
              "RightUpperArm": (-58.0, 0.0, 0.0)}),
        (20, {"Hips": (-6.0, 0.0, 0.0), "LeftUpperLeg": (-26.0, 0.0, 0.0),
              "RightUpperLeg": (-26.0, 0.0, 0.0),
              "LeftLowerLeg": (-8.0, 0.0, 0.0),
              "RightLowerLeg": (-8.0, 0.0, 0.0),
              "LeftUpperArm": (-92.0, 0.0, 0.0),
              "RightUpperArm": (-92.0, 0.0, 0.0)}),
        (34, ak.suzulme_pozu()),
    ]), "tek", 0.0, 0.0, None))

    # --- SUZULME (blend agacinin uc pozlari) ------------------------------
    for ad, p, r in (("Suzulme", 0.0, 0.0),
                     ("Suzulme_Burun", -16.0, 0.0),
                     ("Suzulme_Kuyruk", 14.0, 0.0),
                     ("Suzulme_Sol", 0.0, -26.0),
                     ("Suzulme_Sag", 0.0, 26.0)):
        out.append((ad, _poz_dizisi(arm, ad, [
            (1, ak.suzulme_pozu(p, r)),
            (24, ak.suzulme_pozu(p, r)),
        ]), "poz", 0.0, 0.0, None))

    # --- INIS + YUVARLANMA -------------------------------------------------
    out.append(("Inis", _poz_dizisi(arm, "Inis", [
        (1, ak.suzulme_pozu(-10.0, 0.0)),
        (12, {"Hips": (34.0, 0.0, 0.0), "LeftUpperLeg": (48.0, 0.0, 0.0),
              "RightUpperLeg": (36.0, 0.0, 0.0),
              "LeftLowerLeg": (-20.0, 0.0, 0.0),
              "RightLowerLeg": (-14.0, 0.0, 0.0),
              "LeftUpperArm": (-70.0, 0.0, -18.0),
              "RightUpperArm": (-70.0, 0.0, 18.0)}),
        (24, {"Hips": (14.0, 0.0, 0.0), "Spine": (16.0, 0.0, 0.0),
              "LeftUpperLeg": (72.0, 0.0, 0.0),
              "RightUpperLeg": (58.0, 0.0, 0.0),
              "LeftLowerLeg": (-86.0, 0.0, 0.0),
              "RightLowerLeg": (-72.0, 0.0, 0.0),
              "LeftUpperArm": (-40.0, 0.0, -20.0),
              "RightUpperArm": (-40.0, 0.0, 20.0)}),
        (46, _durus()),
    ]), "tek", 0.0, 0.0, None))

    # --- CAKILMA -----------------------------------------------------------
    out.append(("Cakilma", _poz_dizisi(arm, "Cakilma", [
        (1, ak.suzulme_pozu(18.0, 12.0)),
        (8, {"Hips": (52.0, 0.0, 18.0), "Spine": (-22.0, 0.0, -14.0),
             "Neck": (-24.0, 0.0, 0.0),
             "LeftUpperArm": (-38.0, 0.0, -46.0),
             "RightUpperArm": (-96.0, 0.0, 30.0),
             "LeftUpperLeg": (36.0, 0.0, -14.0),
             "RightUpperLeg": (-18.0, 0.0, 10.0),
             "LeftLowerLeg": (-64.0, 0.0, 0.0),
             "RightLowerLeg": (-30.0, 0.0, 0.0)}),
        (30, {"Hips": (86.0, 0.0, 8.0), "Spine": (-14.0, 0.0, -6.0),
              "Neck": (-10.0, 0.0, 0.0),
              "LeftUpperArm": (-16.0, 0.0, -58.0),
              "RightUpperArm": (-22.0, 0.0, 52.0),
              "LeftUpperLeg": (10.0, 0.0, -22.0),
              "RightUpperLeg": (6.0, 0.0, 16.0),
              "LeftLowerLeg": (-42.0, 0.0, 0.0),
              "RightLowerLeg": (-36.0, 0.0, 0.0)}),
    ]), "tek", 0.0, 0.0, None))

    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--in", dest="kaynak", default=KAYNAK)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--catalog", default=os.path.join(
        "art", "blend", "karakter", "animasyon.json"))
    ap.add_argument("--export", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(args.kaynak))
    arm = _arm()
    hz.ensure_units()
    bpy.context.scene.render.fps = ak.FPS

    # --- DONUS EKSENI DENETIMI --------------------------------------------
    # "Bacak asagi bakiyorsa yerel X dunya X'idir" KABACA dogrudur ve
    # kabaca yeterli degil: yanlis eksende donen bir bacak yurumez, yana
    # acilir. Uygula ve OLC.
    ileri, yanal = ak.yon_denetimi(arm, "LeftUpperLeg", "LeftToes")
    hz.log(f"eksen denetimi: uyluk donusu ileri-geri {ileri*100:.1f} cm, "
           f"yanal {yanal*100:.1f} cm")
    if ileri < yanal * 2.0:
        raise SystemExit(
            f"[HZ] HATA uyluk yerel X'te donunce ayak {ileri*100:.1f} cm "
            f"ileri-geri, {yanal*100:.1f} cm YANAL gidiyor — kemik roll'u "
            "beklenen eksende degil, yurume dongusu yana acilir.")

    # --- ANIMASYON KOLEKSIYONU -------------------------------------------
    #
    # Unity, yalnizca ARMATURE tasiyan bir FBX'ten KLIP URETMEZ. Bunu
    # tahmin etmedim, olctum: ayni klip mesh'le birlikte disa
    # aktarildiginda Unity 1,07 s'lik klibi okudu; mesh'siz surumun
    # onucunde de "klip yok" dedi. Avatar ayarini (Human +
    # CopyFromOther) sabit tutup yalnizca mesh'i degistirdigim icin
    # degisken tekti.
    #
    # Cozum tam mesh'i her klibe koymak degil (13 x 6,7 MB = 87 MB, ve
    # hepsi LFS'e girerdi): armature'a bagli KUCUK BIR VEKIL yeter.
    # Unity "deri bagli bir sey var" gorur, dosya 200 KB kalir.
    col = hz.collection(ANIM_COL)
    for o in list(col.objects):
        col.objects.unlink(o)
    col.objects.link(arm)
    vekil = _vekil_mesh(arm, col)
    hz.log(f"vekil mesh: {vekil.name} ({len(vekil.data.polygons)} yuz) — "
           "Unity mesh'siz FBX'ten klip uretmiyor")

    os.makedirs(args.out_dir, exist_ok=True)
    taban = os.path.splitext(os.path.basename(args.kaynak))[0]
    kayit = []

    for ad, act, tur, hiz, dikey, temas in klipleri_kur(arm):
        arm.animation_data.action = act
        s0, s1 = act.frame_range
        bpy.context.scene.frame_start = int(round(s0))
        bpy.context.scene.frame_end = int(round(s1))
        sure = (s1 - s0) / float(ak.FPS)

        kayma = {}
        if tur == "dongu" and hiz > 0.01:
            olcum = ak.dongu_olc(arm, act, hiz, dikey_hiz=dikey,
                                 temas_araligi=temas)
            kayma = {k: v["kayma"] for k, v in olcum.items()}
            en_kotu = max(kayma.values()) if kayma else 0.0
            if en_kotu > ak.KAYMA_SINIRI:
                raise SystemExit(
                    f"[HZ] HATA {ad}: ayak kaymasi {en_kotu*100:.1f} cm "
                    f"(sinir {ak.KAYMA_SINIRI*100:.0f} cm). Adim uzunlugu "
                    f"{hiz:.1f} m/s hiziyla tutmuyor — ayaklar paten kayar.")

        tempo = (120.0 / sure) if (tur == "dongu" and sure > 0.01
                                   and hiz > 0.01) else None
        kayit.append(dict(
            ad=ad, tur=tur, kare=int(round(s1 - s0)) + 1,
            tempo=round(tempo, 1) if tempo else None,
            sure=round(sure, 3), hiz=hiz,
            dongu=tur in ("dongu", "poz"),
            kayma_cm=round(max(kayma.values()) * 100.0, 2) if kayma else None))

        if args.export:
            export_fbx(os.path.join(args.out_dir, f"{taban}@{ad}.fbx"),
                       collection_name=ANIM_COL, skinned=True)

    if not args.export:
        hz.log("FBX yazilmadi (--export ile yazilir).")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"fps": ak.FPS, "kaynak": taban, "klipler": kayit},
                  fh, ensure_ascii=False, indent=1)

    for k in kayit:
        ks = f", kayma {k['kayma_cm']:.1f} cm" if k["kayma_cm"] is not None else ""
        hz.log(f"{k['ad']:16s} {k['kare']:3d} kare / {k['sure']:.2f} s "
               f"({k['tur']}){ks}")
    hz.log(f"{len(kayit)} klip; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
