# La Última Luz

Juego 2D pixel art hecho en Unity 6. Protege la cubeta de dulces con tu linterna: ilumina a un fantasma 2.5 s seguidos para desvanecerlo.

- **Fantasma ladrón** (verde): va directo a la cubeta.
- **Fantasma perseguidor** (lila): te persigue a ti.
- Tú y la cubeta tienen 3 vidas cada uno; si alguno llega a 0, pierdes.

## Jugar en el navegador

https://elegidokawai2.github.io/ltima-Luz/

## Controles

| Acción | Tecla |
| --- | --- |
| Mover | WASD / flechas |
| Apuntar linterna | Mouse |
| Encender linterna | Espacio / clic izquierdo (mantener) |
| Pausa | Esc / P |
| Reiniciar (en Game Over) | R |
| Volver al menú (en Game Over) | Esc / M |

## Descargar

Descarga la versión para tu sistema desde [Releases](https://github.com/Elegidokawai2/ltima-Luz/releases/latest).

### macOS (Apple Silicon: M1/M2/M3/M4)

1. Descarga `LaUltimaLuz-macOS.zip` y descomprímelo (doble clic).
2. Mueve `LaUltimaLuz.app` a la carpeta **Aplicaciones** (opcional).
3. La app no está firmada por Apple, así que la primera vez macOS la bloquea:
   - Haz **clic derecho → Abrir** sobre la app y confirma **Abrir**.
   - Si dice *"está dañada y no se puede abrir"* o no aparece la opción, abre Terminal y ejecuta:
     ```bash
     xattr -dr com.apple.quarantine /Applications/LaUltimaLuz.app
     ```
     (ajusta la ruta si no la moviste a Aplicaciones). Luego ábrela normal.
   - En macOS 15+ también puedes ir a **Ajustes del Sistema → Privacidad y seguridad** y pulsar **Abrir de todos modos**.

> Los Mac con Intel no están soportados por este build (solo arm64).

### Windows (10/11, 64 bits)

1. Descarga `LaUltimaLuz-Windows.zip`.
2. Clic derecho → **Extraer todo…** (no lo ejecutes desde dentro del zip).
3. Abre la carpeta `LaUltimaLuz-Windows` y ejecuta `LaUltimaLuz.exe`.
4. Si aparece **"Windows protegió tu PC"** (SmartScreen), pulsa **Más información → Ejecutar de todas formas**.

Mantén todos los archivos juntos: el `.exe` necesita la carpeta `LaUltimaLuz_Data`, `UnityPlayer.dll` y el resto.

### Web (offline)

`LaUltimaLuz-WebGL.zip` contiene el build web. No funciona abriendo `index.html` con doble clic; hay que servirlo:

```bash
cd WebGL
python3 -m http.server 8000
# abre http://localhost:8000
```

## Desarrollo

### Requisitos

- Unity **6000.0.83f1** (Unity 6) con módulos *Mac Build Support*, *Windows Build Support (Mono)* y *WebGL Build Support*.
- **Git LFS**: los sprites (`.png`) y sonidos (`.wav`) están en LFS. Sin él, Unity importa archivos de texto en lugar de assets y el juego se ve con cuadros magenta y sin sonido.

```bash
git lfs install
git clone https://github.com/Elegidokawai2/ltima-Luz.git
# si ya lo clonaste sin LFS:
git lfs pull
```

### Compilar

Desde el editor: menú **Build → macOS / Windows / WebGL**. Los builds quedan en `Builds/` (ignorado por git).

Desde la terminal (los tres a la vez):

```bash
/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath . \
  -executeMethod BuildScript.BuildAll -logFile build.log
```

El build WebGL se genera sin compresión para que funcione en GitHub Pages (que no envía cabeceras `Content-Encoding` para `.br`/`.gz`).

### Estructura

- `Assets/Scenes/UltimaLuz.unity` — escena principal; el juego se crea por código al cargarla.
- `Assets/scrips/UltimaLuz/` — código del juego (`JuegoUltimaLuz.cs` es el núcleo).
- `Assets/Resources/UltimaLuz/` — sprites y audio cargados en tiempo de ejecución.
- `Assets/Editor/BuildScript.cs` — builds para macOS, Windows y WebGL.
- `Tools/generar_assets.py` — genera los sprites y sonidos.
