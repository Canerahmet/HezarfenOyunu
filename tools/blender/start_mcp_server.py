"""Blender GUI oturumunda blender-mcp sunucusunu otomatik baslatir.

Kullanim (GUI acilir, sunucu kendiliginden baslar):

    & "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" ^
        --python tools/blender/start_mcp_server.py

Neden script? Eklenti arka plan modunda (-b) calismayi reddediyor: komutlar Blender'in
ana thread'inde yurutuldugu icin olay dongusu gerekiyor. Bu script, N-panelinden
"Connect to Claude" dugmesine basmanin elle yapilmayan karsiligidir.

Sunucu localhost:9876'ya baglanir (eklentinin varsayilani). Disari acilmaz.
"""

import bpy

PORT = 9876


def _start():
    """Blender UI hazir olduktan sonra bir kez calisir (bpy.app.timers geri cagrisi)."""
    scene = bpy.context.scene

    if getattr(scene, "blendermcp_port", None) is not None:
        scene.blendermcp_port = PORT

    try:
        bpy.ops.blendermcp.start_server()
    except Exception as exc:  # noqa: BLE001 - kullaniciya ham hatayi gostermek istiyoruz
        print(f"[hezarfen] MCP sunucusu baslatilamadi: {type(exc).__name__}: {exc}")
        return None

    running = getattr(scene, "blendermcp_server_running", False)
    if running:
        print(f"[hezarfen] BlenderMCP sunucusu calisiyor: localhost:{PORT}")
    else:
        print("[hezarfen] start_server cagrildi ama sunucu calismiyor gorunuyor.")
        print("[hezarfen] N-panel > BlenderMCP sekmesinden elle deneyin.")

    return None  # timer'i tekrarlama


if bpy.app.background:
    print("[hezarfen] Arka plan modunda MCP sunucusu calismaz. GUI ile baslatin.")
else:
    # UI tam kurulmadan operator cagirmak context hatasi verir; 1 sn gecikme yeterli.
    bpy.app.timers.register(_start, first_interval=1.0)
