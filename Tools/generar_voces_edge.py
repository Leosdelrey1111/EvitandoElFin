# -*- coding: utf-8 -*-
"""
Genera las voces del juego con voces NEURONALES de Microsoft Edge (es-MX: Dalia y Jorge), via el paquete edge-tts.
Son las voces mas naturales sin pagar ni crear cuentas, pero necesitan internet al generarlas.

Uso (lo llama el menu de Unity "9. Generar voces del Nivel 1"):
    python generar_voces_edge.py <manifiesto.json> <carpeta_salida> [--forzar]

AVISO: edge-tts usa el servicio en linea de lectura en voz alta de Edge sin una API oficial. Sirve muy bien para prototipos
y trabajos escolares; para publicar comercialmente conviene reemplazar los audios por grabaciones o por un servicio con licencia
(los .mp3 se reemplazan simplemente guardando otro archivo con el mismo nombre). Piper (generar_voces_piper.py) es la opcion con licencias libres.

Perfiles:
    Tonalli  -> es-MX-DaliaNeural, aguda y viva
    LaBufa   -> es-MX-DaliaNeural, grave y lenta (el juego le agrega eco de piedra)
    DonChuy  -> es-MX-JorgeNeural, pausado y grave
"""
import asyncio
import json
import os
import sys

import edge_tts

PERFILES = {
    "Tonalli": dict(voz="es-MX-DaliaNeural", rate="+8%", pitch="+24Hz"),
    "LaBufa":  dict(voz="es-MX-DaliaNeural", rate="-14%", pitch="-20Hz"),
    "DonChuy": dict(voz="es-MX-JorgeNeural", rate="-10%", pitch="-6Hz"),
}


async def sintetizar(texto, perfil, ruta):
    ultimo = None
    for intento in range(4):
        try:
            c = edge_tts.Communicate(texto, perfil["voz"], rate=perfil["rate"], pitch=perfil["pitch"])
            await c.save(ruta)
            if os.path.getsize(ruta) > 500:
                return
        except Exception as e:  # red, limite de peticiones...
            ultimo = e
        await asyncio.sleep(1.5 * (intento + 1))
    raise RuntimeError("No se pudo sintetizar (" + str(ultimo) + ")")


async def main():
    if len(sys.argv) < 3:
        print("ERROR uso: generar_voces_edge.py <manifiesto.json> <salida> [--forzar]")
        return 2
    manifiesto, salida = sys.argv[1], sys.argv[2]
    forzar = "--forzar" in sys.argv[3:]

    with open(manifiesto, "r", encoding="utf-8-sig") as f:
        frases = json.load(f)["frases"]
    os.makedirs(salida, exist_ok=True)
    print("VOZ Edge neuronal (es-MX: Dalia y Jorge)")
    sys.stdout.flush()

    for fr in frases:
        ruta = os.path.join(salida, fr["clave"] + ".mp3")
        otra = os.path.join(salida, fr["clave"] + ".wav")
        if os.path.exists(ruta) and not forzar:
            print("SALTADA " + fr["clave"])
            continue
        perfil = PERFILES.get(fr.get("voz", "Tonalli"), PERFILES["Tonalli"])
        try:
            await sintetizar(fr["texto"], perfil, ruta)
        except Exception as e:
            print("ERROR " + fr["clave"] + ": " + str(e))
            return 3
        # si habia una version .wav (Piper / Windows) se quita para que Resources.Load no se confunda
        if os.path.exists(otra):
            os.remove(otra)
            meta = otra + ".meta"
            if os.path.exists(meta):
                os.remove(meta)
        print("OK " + fr["clave"])
        sys.stdout.flush()

    print("FIN")
    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
