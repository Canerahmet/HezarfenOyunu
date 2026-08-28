"""
Hezarfen: 1632 - Uskudar iskelesi ve Alay Kosku (ahsap).

Faz 3'un iki 🟡 satirini kapatir: Uskudar Mihrimah'in "Eksik: iskele" ve
Topkapi siluetinin "Alay Kosku 1632'de AHSAP — kayitli, uretilmedi".

Ayrinti: `lib/works_kit.py`, `lib/kosk_kit.py`, RESEARCH.md 5.20, ADR 0055.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_iskele_ve_alay.py -- \
      --textured --out-dir unity/HezarfenGame/Assets/_Import
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import works_kit as wk             # noqa: E402
import kosk_kit as kk              # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE_ISKELE = (
    "USKUDAR ISKELESI, 1632 — **AHSAP**. "
    "DOGRULUK BASAMAGI **D3**: olcusu yok, kutle tipolojik. "
    "BELGELI OLAN SEY ISKELENIN VARLIGI VE CAMIYE ADINI VERMIS OLMASI: "
    "Uskudar Mihrimah Sultan Camii'nin yaygin adi **'ISKELE CAMII'**dir "
    "ve sebebi yani basindaki iskeledir. Yani iskele, camiden bagimsiz "
    "bir ayrinti degil — CAMININ ADININ KAYNAGI. Faz 3'te cami uretilmis "
    "ama iskele 'eksik' diye kayitliydi (ADR 0036); bu tur onu kapatir. "
    "**1632'DE AHSAP**: kagir rihtimlar 19. yuzyildir. Yapisal ahsap "
    "BOYANMAZ (`timber_bare`, ADR 0035) — tuzlu havada duran bir iskele "
    "asi boyali bir cumba degildir. "
    "Yerlestirici `iskele` turunu SUYA dondurur (ShoreKinds, ADR 0039); "
    "pivot KIYI UCUNDADIR ve yapi +Y'de denize uzanir. "
    "RESEARCH.md 5.20, ADR 0055"
)

SOURCE_ALAY = (
    "ALAY KOSKU, 1632 — **AHSAP**. Sur-i Sultani uzerinde, sokaga tasan "
    "seyir kosku; padisah devlet ricalinin ALAYLARINI buradan izlerdi. "
    "DOGRULUK BASAMAGI **D3**: kutle tipolojik. "
    "**BUGUNKU KAGIR KOSK 1810 ya da 1819-20**, II. MAHMUD'undur. "
    "Kaynak iki seyi birden soyluyor ve ikincisi BEKLENMEDIKTIR: "
    "16. yuzyilda ayni yerde AHSAP bir kosk vardi, ve II. Mahmud'un "
    "yapisi **DAHA YUKSEK** bir koskun ya da kulenin yerine gecti. "
    "Yani burada 1632 yapisi bugunkunden ALCAK DEGIL, YUKSEKTIR — Galata "
    "Kulesi (ADR 0033) ve Adalet Kulesi'nin (ADR 0040) TERSI. 'Eski olan "
    "alcaktir' diye bir kural yok; HER YAPI AYRI SORULUR. "
    "INCILI KOSK'LE AYNI AILE: ikisi de bir DUVARIN ustunde durur, ikisi "
    "de TASAR, ve ikisinde de padisah bir seyi SEYREDER — Incili "
    "Kosk'ten Hezarfen'in ucusu (ADR 0039), buradan alaylar. Ayni yapi "
    "tipi, ayni islev ailesi. "
    "1632'DE YOK: 1855 Telgrafhane-i Amire (koskun yanina yapildi ve kosk "
    "telgraf mudurlerine tahsis edildi). "
    "RESEARCH.md 5.20, ADR 0055"
)


def add_args(p):
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None)
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())
    infos = []

    # --- Iskele -----------------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    ip = wk.IskeleParams(palette=a.palette)
    lod0, lod1, ucx, info = wk.build_iskele(ip, col, "UskudarIskelesi",
                                            textured=a.textured)
    if info["kind"] != "iskele":
        raise SystemExit("[HZ] HATA: tur 'iskele' olmali (suya doner).")
    if info["material"] != "ahsap":
        raise SystemExit("[HZ] HATA: 1632'de iskele AHSAPTIR.")
    hz.log(f"UskudarIskelesi: {ip.length:.1f} x {ip.width:.1f} m, "
           f"{ip.piles} kazik cifti, guverte {ip.deck_z:.1f} m")
    hz.log(f"  ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"LOD0={info['tris_lod0']}")
    info.update(name="UskudarIskelesi", prefab="PF_UskudarIskelesi",
                tier="T1", source=SOURCE_ISKELE)
    infos.append(info)
    if a.blend_dir:
        os.makedirs(a.blend_dir, exist_ok=True)
        hz.save_blend(os.path.join(a.blend_dir, "UskudarIskelesi.blend"))
    if a.out_dir:
        os.makedirs(a.out_dir, exist_ok=True)
        export_fbx(os.path.join(a.out_dir, "SM_UskudarIskelesi.fbx"),
                   collection_name=COLLECTION)

    # --- Genel iskele (mahalle iskelesi) ----------------------------------
    #
    # Uskudar'inki ANA iskeledir ve camiye adini vermistir; Halic ile
    # Bogaz'in obur agizlarindaki iskeleler daha kucuktur. Ayni prefab'i
    # her yere koymak, Uskudar'in belgeli ozelligini (buyukluk, kayikhane)
    # belgesi olmayan yerlere tasimak olurdu.
    #
    # Bu iskelenin KONUMU degil VARLIGI belgelidir: Evliya Unkapani ve
    # Balikpazari iskelelerini faal sayar, Kasimpasa Tersanesi calisir,
    # Karakoy Kapisi kiyiya acilir. Konum ayrica turetilir (kiyi cizgisine
    # en yakin nokta + denize acilma) — Uskudar icin kullanilan yontemin
    # ayni.
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    gp = wk.IskeleParams(length=wk.ISK_LEN * 0.68, width=wk.ISK_W * 0.80,
                         piles=max(3, int(wk.IskeleParams().piles * 0.7)),
                         kayikhane=False, palette=a.palette)
    lod0, lod1, ucx, info = wk.build_iskele(gp, col, "Iskele",
                                            textured=a.textured)
    if info["material"] != "ahsap":
        raise SystemExit("[HZ] HATA: 1632'de iskele AHSAPTIR.")
    hz.log(f"Iskele (genel): {gp.length:.1f} x {gp.width:.1f} m, "
           f"{gp.piles} kazik cifti")
    info.update(name="Iskele", prefab="PF_Iskele", tier="T2",
                status="draft",
                source=("MAHALLE ISKELESI, 1632 — AHSAP. VARLIGI belgeli, "
                        "KONUMU turetilmis. Evliya Unkapani ve Balikpazari "
                        "iskelelerini faal sayar; Kasimpasa Tersanesi "
                        "calisir; Karakoy Kapisi kiyiya acilir. Ama hicbiri "
                        "metrik konum vermez. Konum, Uskudar iskelesi icin "
                        "kullanilan YONTEMLE turetildi: capadan (cami, kule, "
                        "mahalle merkezi) kendi 1632 kiyi cizgimize en yakin "
                        "nokta, oradan denize acilma; yon kiyinin yerel "
                        "normalinden — iskele kiyiya DIKTIR. Olcusu "
                        "Uskudar'inkinden kucuk: o ana iskeledir ve camiye "
                        "adini vermistir, otekilerin oyle bir kaydi yok."))
    infos.append(info)
    if a.blend_dir:
        hz.save_blend(os.path.join(a.blend_dir, "Iskele.blend"))
    if a.out_dir:
        export_fbx(os.path.join(a.out_dir, "SM_Iskele.fbx"),
                   collection_name=COLLECTION)

    # --- Alay Kosku -------------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    ap = kk.AlayKoskuParams(palette=a.palette)
    lod0, lod1, ucx, info = kk.build_alay_kosku(ap, col, "AlayKosku",
                                                textured=a.textured)
    if info["material"] != "ahsap":
        raise SystemExit("[HZ] HATA: 1632'de Alay Kosku AHSAPTIR; kagir "
                         "kosk 1810/1819-20, II. Mahmud'undur.")
    if info["cumba"] < 1.2:
        raise SystemExit("[HZ] HATA: kosk sokaga TASMALI.")
    hz.log(f"AlayKosku: sur {ap.wall_h:.1f} m + govde {ap.body_h:.1f} m, "
           f"tasma {ap.jut:.1f} m, toplam {info['height']:.2f} m")
    hz.log(f"  ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"LOD0={info['tris_lod0']}")
    info.update(name="AlayKosku", prefab="PF_AlayKosku", tier="T1",
                source=SOURCE_ALAY)
    infos.append(info)
    if a.blend_dir:
        hz.save_blend(os.path.join(a.blend_dir, "AlayKosku.blend"))
    if a.out_dir:
        export_fbx(os.path.join(a.out_dir, "SM_AlayKosku.fbx"),
                   collection_name=COLLECTION)

    for i in infos:
        # Muafiyet TURE gore, ADA gore degil.
        #
        # Once `!= "UskudarIskelesi"` yaziliydi ve tek iskele varken
        # dogru calisiyordu. Ikinci iskele gelince muafiyet yalan
        # soyledi: ayni tur, ayni gerekce, farkli ad. Bir kuralin
        # gerekcesi "bu varlik" degil "bu tur varlik" ise, kural da
        # ture bakmali.
        #
        # Iskelenin pivotu KIYI UCUNDADIR ve kaziklari suyun icinde
        # asagi iner; sifir pivot beklemek onu havada birakirdi.
        if abs(i["pivot_min_z"]) > 0.01 and i.get("kind") != "iskele":
            raise SystemExit(
                f"[HZ] HATA {i['name']} pivot {i['pivot_min_z']:.3f} — "
                "yapinin tabani z=0'da olmali.")

    def katalog_yaz(yol, kayitlar):
        os.makedirs(os.path.dirname(os.path.abspath(yol)), exist_ok=True)
        cat = {"variants": []}
        if os.path.exists(yol):
            with open(yol, encoding="utf-8") as fh:
                cat = json.load(fh)
        names = {i["name"] for i in kayitlar}
        rest = [v for v in cat.get("variants", []) if v.get("name") not in names]
        rest += kayitlar
        rest.sort(key=lambda v: v["name"])
        with open(yol, "w", encoding="utf-8") as fh:
            json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
        hz.log(f"katalog: {yol} ({len(rest)} kayit)")

    if a.catalog:
        # LANDMARK KATALOGU BELGELI YAPILAR ICINDIR.
        #
        # Genel mahalle iskelesi landmark degil: adi yok, kaynagi yok,
        # KONUMU turetilmis. Onu landmark katalogena yazinca var olan bir
        # test hakli olarak itiraz etti — "her landmark T1 olmali" kurali
        # o katalogun ne oldugunu soyluyordu ve ben oraya baska bir sey
        # koymustum.
        #
        # Sehir donatisi `works` katalogunda yasar (arasta, bozahane,
        # degirmen ile ayni yerde).
        lm = [i for i in infos if i["name"] != "Iskele"]
        donati = [i for i in infos if i["name"] == "Iskele"]
        if lm:
            katalog_yaz(a.catalog, lm)
        if donati:
            katalog_yaz(os.path.join("art", "blend", "works", "catalog.json"),
                        donati)

    hz.log("gen_iskele_ve_alay OK")


if __name__ == "__main__":
    main()
