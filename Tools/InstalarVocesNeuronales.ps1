# Instala Piper (sintesis de voz neuronal local) y las voces es_MX usadas por el juego.
#   - Crea Tools\voces_piper\venv (entorno de Python) e instala piper-tts + numpy.
#   - Descarga es_MX-claude-high (Apache-2.0) y es_MX-ald-medium (Unlicense) desde rhasspy/piper-voices (Hugging Face).
# Es idempotente: si ya esta todo instalado, no vuelve a descargar. Requiere Python 3 e internet.
# Despues: menu de Unity "9b. Regenerar TODAS las voces", o  python Tools\generar_voces_piper.py <manifiesto> <salida> --forzar
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$tools = Split-Path -Parent $MyInvocation.MyCommand.Path
$base = Join-Path $tools 'voces_piper'
$venv = Join-Path $base 'venv'
$modelos = Join-Path $base 'modelos'
$py = Join-Path $venv 'Scripts\python.exe'

Write-Output 'PASO Buscando Python...'
if (-not (Test-Path $py)) {
    $sistema = Get-Command python -ErrorAction SilentlyContinue
    if (-not $sistema) { Write-Output 'ERROR Python 3 no esta instalado (https://www.python.org/downloads/).'; exit 2 }
    New-Item -ItemType Directory -Force -Path $base | Out-Null
    Write-Output 'PASO Creando entorno de Python...'
    & $sistema.Source -m venv $venv
}

Write-Output 'PASO Instalando Piper (puede tardar)...'
& $py -m pip install --quiet --disable-pip-version-check piper-tts numpy edge-tts
if ($LASTEXITCODE -ne 0) { Write-Output 'ERROR pip no pudo instalar piper-tts.'; exit 3 }

New-Item -ItemType Directory -Force -Path $modelos | Out-Null
$url = 'https://huggingface.co/rhasspy/piper-voices/resolve/main/es/es_MX'
$archivos = @(
    @("$url/claude/high/es_MX-claude-high.onnx.json", 'es_MX-claude-high.onnx.json'),
    @("$url/ald/medium/es_MX-ald-medium.onnx.json", 'es_MX-ald-medium.onnx.json'),
    @("$url/ald/medium/es_MX-ald-medium.onnx", 'es_MX-ald-medium.onnx'),
    @("$url/claude/high/es_MX-claude-high.onnx", 'es_MX-claude-high.onnx')
)
foreach ($a in $archivos) {
    $destino = Join-Path $modelos $a[1]
    if ((Test-Path $destino) -and ((Get-Item $destino).Length -gt 1000)) { continue }
    Write-Output ("PASO Descargando " + $a[1] + '...')
    curl.exe -L --silent --show-error --fail -o $destino $a[0]
    if ($LASTEXITCODE -ne 0) { Write-Output ("ERROR No se pudo descargar " + $a[1]); exit 4 }
}

Write-Output 'FIN'
