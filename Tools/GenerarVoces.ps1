# Genera los audios de voz del juego con las voces de Windows (SAPI, sin internet) a partir de un manifiesto JSON.
# Lo ejecuta el menu de Unity "EvitandoElFin / 9. Generar voces del Nivel 1", pero tambien se puede usar a mano:
#   powershell -File Tools\GenerarVoces.ps1 -Manifiesto voces.json -Salida Assets\Game\Resources\Voz [-Forzar]
# Cada frase del manifiesto es { "clave": "intro_0", "texto": "...", "voz": "Tonalli|LaBufa|DonChuy" }
# y produce <Salida>\<clave>.wav (mono, 22 kHz, 16 bits). Para usar voces reales basta con reemplazar el .wav con el mismo nombre.
param(
    [Parameter(Mandatory = $true)][string]$Manifiesto,
    [Parameter(Mandatory = $true)][string]$Salida,
    [switch]$Forzar
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech

$sintetizador = New-Object System.Speech.Synthesis.SpeechSynthesizer
$voz = $sintetizador.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -like 'es-*' } | Select-Object -First 1
if (-not $voz) {
    Write-Output "ERROR No hay ninguna voz en espanol instalada en Windows (Configuracion > Hora e idioma > Voz)."
    exit 2
}
$sintetizador.SelectVoice($voz.VoiceInfo.Name)
$cultura = $voz.VoiceInfo.Culture.Name
Write-Output ("VOZ " + $voz.VoiceInfo.Name + " (" + $cultura + ")")

# Perfiles: tono y velocidad relativos a la voz base
$perfiles = @{
    'Tonalli' = @{ tono = '+30%'; ritmo = '+8%'  }   # alebrije: aguda y ligera
    'LaBufa'  = @{ tono = '-30%'; ritmo = '-15%' }   # joven de piedra: grave y lenta
    'DonChuy' = @{ tono = '-40%'; ritmo = '-8%'  }   # anciano mercader: grave y pausado
}

$formato = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
$datos = Get-Content -Raw -Encoding UTF8 -Path $Manifiesto | ConvertFrom-Json

if (-not (Test-Path $Salida)) { New-Item -ItemType Directory -Force -Path $Salida | Out-Null }

foreach ($frase in $datos.frases) {
    $ruta = Join-Path $Salida ($frase.clave + '.wav')
    if ((Test-Path $ruta) -and (-not $Forzar)) { Write-Output ("SALTADA " + $frase.clave); continue }

    $perfil = $perfiles[[string]$frase.voz]
    if (-not $perfil) { $perfil = $perfiles['Tonalli'] }

    $texto = [System.Security.SecurityElement]::Escape([string]$frase.texto)
    $ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='$cultura'><prosody pitch='$($perfil.tono)' rate='$($perfil.ritmo)'>$texto</prosody></speak>"

    $sintetizador.SetOutputToWaveFile($ruta, $formato)
    $sintetizador.SpeakSsml($ssml)
    $sintetizador.SetOutputToNull()
    Write-Output ("OK " + $frase.clave)
}

$sintetizador.Dispose()
Write-Output "FIN"
