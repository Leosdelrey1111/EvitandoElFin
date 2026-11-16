# -*- coding: utf-8 -*-
"""
Genera las voces del juego con Piper (sintesis neuronal local, sin internet una vez instalado).

Uso (lo llama el menu de Unity "9. Generar voces del Nivel 1"):
    python generar_voces_piper.py <manifiesto.json> <carpeta_salida> [--forzar]

Voces (es_MX, licencias permisivas):
    - es_MX-claude-high  (mujer, Apache-2.0)  -> Tonalli (mas aguda y viva) y La Bufa (grave, lenta y con eco de cueva)
    - es_MX-ald-medium   (hombre, Unlicense)  -> Don Chuy (anciano, pausado)
El manifiesto es { "frases": [ { "clave": "intro_0", "texto": "...", "voz": "Tonalli|LaBufa|DonChuy" } ] }.
Cada frase produce <salida>/<clave>.wav (mono, 22.05 kHz, 16 bits). Para usar voces reales basta reemplazar el .wav.
"""
import io
import json
import os
import sys
import wave

import numpy as np
from piper import PiperVoice, SynthesisConfig

AQUI = os.path.dirname(os.path.abspath(__file__))
MODELOS = os.path.join(AQUI, "voces_piper", "modelos")

# modelo, velocidad (length_scale: >1 = mas lento), variacion, cambio de tono por remuestreo (>1 = mas agudo y rapido), eco
PERFILES = {
    "Tonalli": dict(modelo="es_MX-claude-high", length=0.95, ruido=0.70, ruido_w=0.80, tono=1.13, eco=False),
    "LaBufa":  dict(modelo="es_MX-claude-high", length=1.18, ruido=0.60, ruido_w=0.70, tono=0.86, eco=True),
    "DonChuy": dict(modelo="es_MX-ald-medium",  length=1.10, ruido=0.65, ruido_w=0.75, tono=0.95, eco=False),
}

_cache = {}


def cargar(nombre):
    if nombre not in _cache:
        ruta = os.path.join(MODELOS, nombre + ".onnx")
        _cache[nombre] = PiperVoice.load(ruta, config_path=ruta + ".json")
    return _cache[nombre]


def sintetizar(voz, texto, perfil):
    buf = io.BytesIO()
    with wave.open(buf, "wb") as wf:
        cfg = SynthesisConfig(length_scale=perfil["length"], noise_scale=perfil["ruido"], noise_w_scale=perfil["ruido_w"], normalize_audio=True)
        voz.synthesize_wav(texto, wf, syn_config=cfg)
    buf.seek(0)
    with wave.open(buf, "rb") as rf:
        tasa = rf.getframerate()
        datos = np.frombuffer(rf.readframes(rf.getnframes()), dtype=np.int16).astype(np.float32) / 32768.0
    return tasa, datos


def cambiar_tono(x, ratio):
    """Remuestreo lineal: ratio > 1 sube el tono (y acelera), ratio < 1 lo baja (y alenta)."""
    if abs(ratio - 1.0) < 0.001:
        return x
    n = int(len(x) / ratio)
    pos = np.arange(n) * ratio
    return np.interp(pos, np.arange(len(x)), x).astype(np.float32)


def eco_de_cueva(x, tasa):
    """Dos repeticiones suaves: voz de piedra dentro de una cueva."""
    extra = int(tasa * 0.5)
    y = np.concatenate([x, np.zeros(extra, dtype=np.float32)])
    for retraso, ganancia in ((0.070, 0.28), (0.150, 0.16), (0.260, 0.08)):
        d = int(tasa * retraso)
        y[d:d + len(x)] += x * ganancia
    return y


def guardar(ruta, tasa, x):
    pico = float(np.max(np.abs(x))) if len(x) else 1.0
    if pico > 0:
        x = x * (0.89 / pico)
    silencio_ini = np.zeros(int(tasa * 0.05), dtype=np.float32)
    silencio_fin = np.zeros(int(tasa * 0.18), dtype=np.float32)
    x = np.concatenate([silencio_ini, x, silencio_fin])
    pcm = np.clip(x * 32767.0, -32768, 32767).astype(np.int16)
    with wave.open(ruta, "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(tasa)
        wf.writeframes(pcm.tobytes())


def main():
    if len(sys.argv) < 3:
        print("ERROR uso: generar_voces_piper.py <manifiesto.json> <salida> [--forzar]")
        return 2
    manifiesto, salida = sys.argv[1], sys.argv[2]
    forzar = "--forzar" in sys.argv[3:]

    for p in PERFILES.values():
        if not os.path.exists(os.path.join(MODELOS, p["modelo"] + ".onnx")):
            print("ERROR Faltan los modelos de voz en " + MODELOS + " (ejecuta el menu 9c o Tools/InstalarVocesNeuronales.ps1).")
            return 2

    with open(manifiesto, "r", encoding="utf-8-sig") as f:
        frases = json.load(f)["frases"]
    os.makedirs(salida, exist_ok=True)
    print("VOZ Piper neuronal (es_MX: claude y ald)")

    for fr in frases:
        ruta = os.path.join(salida, fr["clave"] + ".wav")
        if os.path.exists(ruta) and not forzar:
            print("SALTADA " + fr["clave"])
            continue
        perfil = PERFILES.get(fr.get("voz", "Tonalli"), PERFILES["Tonalli"])
        voz = cargar(perfil["modelo"])
        tasa, x = sintetizar(voz, fr["texto"], perfil)
        x = cambiar_tono(x, perfil["tono"])
        if perfil["eco"]:
            x = eco_de_cueva(x, tasa)
        guardar(ruta, tasa, x)
        # si habia una version .mp3 (Edge) se quita para que Resources.Load no se confunda
        otra = os.path.join(salida, fr["clave"] + ".mp3")
        if os.path.exists(otra):
            os.remove(otra)
            if os.path.exists(otra + ".meta"):
                os.remove(otra + ".meta")
        print("OK " + fr["clave"])
        sys.stdout.flush()

    print("FIN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
