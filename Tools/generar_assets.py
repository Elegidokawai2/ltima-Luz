#!/usr/bin/env python3
"""
Generador de assets de "La Última Luz" (pixel art + audio chiptune).

Uso (desde la raíz del proyecto de Unity):
    python3 Tools/generar_assets.py

Crea los PNG en  Assets/Resources/UltimaLuz/Sprites/
y los WAV en      Assets/Resources/UltimaLuz/Audio/
Requiere: numpy y Pillow.
"""
import math, os, sys, wave, struct, random
import numpy as np
from PIL import Image

RAIZ = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
if len(sys.argv) > 1:
    RAIZ = os.path.abspath(sys.argv[1])
DIR_SPR = os.path.join(RAIZ, "Assets", "Resources", "UltimaLuz", "Sprites")
DIR_AUD = os.path.join(RAIZ, "Assets", "Resources", "UltimaLuz", "Audio")
FONDO_FUENTE = os.path.join(RAIZ, "Assets", "Gemini_Generated_Image_o8uhvno8uhvno8uh.jpg")

random.seed(31)
np.random.seed(31)

# ---------------------------------------------------------------- utilidades
def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def lienzo(w, h):
    return np.zeros((h, w, 4), dtype=np.uint8)

def desde_mapa(filas, pal):
    h = len(filas); w = max(len(f) for f in filas)
    img = lienzo(w, h)
    for y, f in enumerate(filas):
        for x, ch in enumerate(f):
            if ch in pal and pal[ch] is not None:
                img[y, x] = pal[ch]
    return img

def contorno(img, color):
    """Agrega un contorno de 1px alrededor de los pixeles opacos."""
    h, w, _ = img.shape
    a = img[:, :, 3] > 0
    out = img.copy()
    for y in range(h):
        for x in range(w):
            if a[y, x]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and a[ny, nx]:
                    out[y, x] = color
                    break
    return out

def pegar(dst, src, ox, oy):
    h, w, _ = src.shape
    for y in range(h):
        for x in range(w):
            if src[y, x, 3] > 0:
                X, Y = ox + x, oy + y
                if 0 <= X < dst.shape[1] and 0 <= Y < dst.shape[0]:
                    dst[Y, X] = src[y, x]

def hoja(frames):
    """Une frames del mismo tamaño en una tira horizontal."""
    h, w, _ = frames[0].shape
    out = lienzo(w * len(frames), h)
    for i, f in enumerate(frames):
        out[:, i * w:(i + 1) * w] = f
    return out

def guardar(img, nombre):
    os.makedirs(DIR_SPR, exist_ok=True)
    Image.fromarray(img, "RGBA").save(os.path.join(DIR_SPR, nombre + ".png"))

K = hexc("#140c1c")   # contorno

# ---------------------------------------------------------------- personaje
PAL_PJ = {
    ".": None, "k": K,
    "p": hexc("#5b2a86"), "P": hexc("#7d3fb0"), "q": hexc("#3b1a5c"),  # sombrero
    "o": hexc("#f28c28"),                                               # cinta
    "h": hexc("#4a2b1a"), "s": hexc("#f2c39b"), "S": hexc("#d99a74"),   # pelo / piel
    "e": hexc("#140c1c"), "r": hexc("#e0707a"),
    "O": hexc("#e8701a"), "Q": hexc("#b84f12"), "Y": hexc("#ffb347"),   # sudadera
    "g": hexc("#8c8c9c"), "G": hexc("#5a5a6a"), "w": hexc("#fff6b0"),   # linterna
    "b": hexc("#2b3a67"), "B": hexc("#1b2445"), "z": hexc("#3a2418"),   # pantalón/zapato
}
PJ_TORSO = [
    "......kkk.......",
    ".....kPpqk......",
    "....kPppqk......",
    "....kpppqqk.....",
    "...kooooooook...",
    "..kqpppppppqqk..",
    "...kkhhhhhhkk...",
    "....khssssssk...",
    "....khsseseSk...",
    "....khsssssSk...",
    "....kkOrrOkk....",
    "...kYOOOOOOkggk.",
    "..kYOOOOOOkGgww.",
    "..kQOOOOOOQkkk..",
]
PJ_PIERNAS = [
    ["...kQQQQQQk.....", "....kbk.kbk.....", "....kzk.kzk....."],
    ["...kQQQQQQk.....", "...kbk...kbk....", "...kzk...kzk...."],
    ["...kQQQQQQk.....", "....kbkkbk......", "....kzkkzk......"],
    ["...kQQQQQQk.....", ".....kbkbk......", ".....kzkzk......"],
]

def frame_pj(i, bob):
    filas = (PJ_TORSO + PJ_PIERNAS[i])
    img = desde_mapa(filas, PAL_PJ)  # 16x17
    out = lienzo(16, 18)
    pegar(out, img, 0, 1 - bob)
    return out

def gen_personaje():
    frames = [frame_pj(0, 0), frame_pj(1, 1), frame_pj(0, 0), frame_pj(3, 1)]
    # frame de golpe (todo blanco/rojo) para efectos
    golpe = frames[0].copy()
    m = golpe[:, :, 3] > 0
    golpe[m] = hexc("#ff4d4d")
    golpe[(golpe[:, :, 0] == 255) & m] = hexc("#ff4d4d")
    frames.append(golpe)
    guardar(hoja(frames), "personaje")

# ---------------------------------------------------------------- fantasmas
def cuerpo_fantasma(w, h, fase, base, sombra, brillo, transl=255):
    img = lienzo(w, h)
    cx = (w - 1) / 2.0
    r = (w - 4) / 2.0
    top = 1
    cy = top + r
    fondo = h - 3
    for y in range(h):
        for x in range(w):
            dentro = False
            if y <= cy:
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r + 0.6:
                    dentro = True
            elif y <= fondo:
                if abs(x - cx) <= r + 0.3:
                    # ondas de la cola
                    ola = math.sin((x / (w - 4)) * math.pi * 3 + fase) * 1.4
                    if y <= fondo - 1 + ola:
                        dentro = True
            if dentro:
                c = base
                if x > cx + r * 0.35 or y > fondo - 3:
                    c = sombra
                if x < cx - r * 0.25 and y < cy - r * 0.2:
                    c = brillo
                img[y, x] = (c[0], c[1], c[2], transl)
    return img

def gen_fantasmas():
    # Ladrón: fantasma verde menta con antifaz y costal
    lad = []
    for i in range(4):
        fase = i * math.pi / 2
        b = cuerpo_fantasma(16, 17, fase, hexc("#9cf0c8"), hexc("#5cc29a"), hexc("#e8fff4"))
        # antifaz
        for x in range(3, 13):
            b[6, x] = hexc("#1a1028")
            b[7, x] = hexc("#1a1028")
        b[6, 5] = hexc("#ffffff"); b[6, 10] = hexc("#ffffff")
        b[7, 2] = hexc("#1a1028"); b[7, 13] = hexc("#1a1028")
        # sonrisa traviesa
        b[10, 7] = hexc("#2a1a3a"); b[10, 8] = hexc("#2a1a3a"); b[9, 9] = hexc("#2a1a3a")
        b = contorno(b, K)
        # costal (bolsa de botín) a la izquierda
        costal = desde_mapa([
            "..kk..",
            ".kssk.",
            "kSsssk",
            "kSsssk",
            ".kSSk.",
        ], {".": None, "k": K, "s": hexc("#c8a26a"), "S": hexc("#9a7444")})
        out = lienzo(18, 18)
        pegar(out, b, 2, (0 if i in (0, 3) else 1))
        pegar(out, costal, 0, 10 + (0 if i in (1, 2) else 1))
        lad.append(out)
    # Perseguidor: fantasma pálido-lila, ojos rojos, boca con dientes
    per = []
    for i in range(4):
        fase = i * math.pi / 2
        b = cuerpo_fantasma(16, 17, fase, hexc("#e9e4ff"), hexc("#a99ad6"), hexc("#ffffff"))
        # cejas enojadas
        b[5, 4] = K; b[5, 5] = K; b[6, 6] = K
        b[5, 11] = K; b[5, 10] = K; b[6, 9] = K
        # ojos rojos
        for (x, y) in ((5, 7), (6, 7), (10, 7), (9, 7), (5, 8), (10, 8)):
            b[y, x] = hexc("#ff2a3a")
        b[7, 6] = hexc("#ffd0d0"); b[7, 9] = hexc("#ffd0d0")
        # boca abierta con dientes
        for x in range(5, 11):
            b[10, x] = hexc("#3a0a1a")
            b[11, x] = hexc("#3a0a1a")
        b[12, 6] = hexc("#3a0a1a"); b[12, 7] = hexc("#3a0a1a"); b[12, 8] = hexc("#3a0a1a"); b[12, 9] = hexc("#3a0a1a")
        b[10, 5] = hexc("#ffffff"); b[10, 7] = hexc("#ffffff"); b[10, 9] = hexc("#ffffff")
        b = contorno(b, K)
        # bracitos
        out = lienzo(18, 18)
        dy = 0 if i in (0, 3) else 1
        pegar(out, b, 1, dy)
        brazo = desde_mapa(["kk", "kw", ".k"], {".": None, "k": K, "w": hexc("#e9e4ff")})
        pegar(out, brazo, 0, 9 + dy)
        brazo2 = desde_mapa(["kk", "wk", "k."], {".": None, "k": K, "w": hexc("#a99ad6")})
        pegar(out, brazo2, 16, 9 + dy)
        per.append(out)
    guardar(hoja(lad), "fantasma_ladron")
    guardar(hoja(per), "fantasma_perseguidor")

# ---------------------------------------------------------------- cubeta de dulces
def gen_cubeta():
    W, H = 26, 26
    estados = []
    dulces_col = [hexc("#ff4f7b"), hexc("#ffd23f"), hexc("#4fd1ff"), hexc("#9b5cff"), hexc("#7dff6a"), hexc("#ffffff")]
    for est in range(4):  # 0 llena, 1 media, 2 casi vacía, 3 vacía/rota
        img = lienzo(W, H)
        cx, cy = 12.5, 16.5
        rx, ry = 10.5, 8.0
        for y in range(H):
            for x in range(W):
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d <= 1.0 and y >= 10:
                    # gajos de calabaza
                    gajo = abs(math.sin((x - cx) / rx * 2.2 * math.pi / 2 * 2))
                    c = hexc("#f07a1a")
                    if gajo < 0.25: c = hexc("#c4520e")
                    if x > cx + 4 or y > cy + 5: c = hexc("#c4520e") if c[0] == 0xf0 else hexc("#9a3c08")
                    if x < cx - 5 and y < cy - 1: c = hexc("#ffa94d")
                    img[y, x] = c
        # boca de la cubeta (borde superior)
        for x in range(4, 22):
            img[10, x] = hexc("#7a2e06")
            img[11, x] = hexc("#5a2004")
        # cara tallada (brilla amarilla)
        cara = hexc("#ffe066")
        for (x, y) in [(8, 14), (9, 14), (9, 13), (16, 14), (17, 14), (16, 13), (12, 16), (13, 16)]:
            img[y, x] = cara
        boca = [(7, 19), (8, 20), (9, 20), (10, 19), (11, 20), (12, 20), (13, 20), (14, 19), (15, 20), (16, 20), (17, 20), (18, 19)]
        if est == 3:
            boca = [(8, 20), (9, 19), (10, 19), (11, 19), (12, 19), (13, 19), (14, 19), (15, 19), (16, 19), (17, 20)]
        for (x, y) in boca:
            img[y, x] = cara
        # dulces asomando
        n = [16, 9, 4, 0][est]
        rnd = random.Random(7)
        pos = []
        for _ in range(n):
            x = rnd.randint(6, 18)
            y = rnd.randint(6, 9) if est == 0 else rnd.randint(8, 9)
            pos.append((x, y))
        for (x, y) in pos:
            c = dulces_col[rnd.randint(0, len(dulces_col) - 1)]
            img[y, x] = c
            img[y, x + 1] = c
            img[y + 1, x] = c
            img[y + 1, x + 1] = (int(c[0] * .7), int(c[1] * .7), int(c[2] * .7), 255)
        if est == 0:
            # paleta que sobresale
            img[3, 15] = hexc("#ff4f7b"); img[3, 16] = hexc("#ffffff"); img[4, 15] = hexc("#ffffff"); img[4, 16] = hexc("#ff4f7b")
            for y in range(5, 9): img[y, 15] = hexc("#e8e0d0")
        if est == 3:
            # grieta
            for (x, y) in [(13, 11), (13, 12), (14, 13), (14, 14), (13, 15), (12, 16)]:
                img[y, x] = hexc("#3a1404")
        # asa
        asa = hexc("#4a4a5a")
        for t in range(0, 181, 6):
            a = math.radians(t)
            x = int(round(cx + math.cos(a) * 9.5))
            y = int(round(10 - math.sin(a) * 8))
            if 0 <= y < H and 0 <= x < W and img[y, x, 3] == 0:
                img[y, x] = asa
        img = contorno(img, K)
        estados.append(img)
    guardar(hoja(estados), "cubeta")

# ---------------------------------------------------------------- iconos HUD
def gen_iconos():
    corazon = [
        ".kk.kk.",
        "krrkrrk",
        "krwrrrk",
        "krrrrrk",
        ".krrrk.",
        "..krk..",
        "...k...",
    ]
    lleno = desde_mapa(corazon, {".": None, "k": K, "r": hexc("#ff3b4e"), "w": hexc("#ffc2c8")})
    vacio = desde_mapa(corazon, {".": None, "k": K, "r": hexc("#3a2a44"), "w": hexc("#4a3a54")})
    dulce = [
        "k.....k",
        "ykkkkky",
        ".kpwpk.",
        ".kpppk.",
        "ykkkkky",
        "k.....k",
        ".......",
    ]
    dl = desde_mapa(dulce, {".": None, "k": K, "y": hexc("#ffd23f"), "p": hexc("#ff4f7b"), "w": hexc("#ffd0de")})
    dv = desde_mapa(dulce, {".": None, "k": K, "y": hexc("#3a2a44"), "p": hexc("#3a2a44"), "w": hexc("#4a3a54")})
    guardar(hoja([lleno, vacio, dl, dv]), "iconos")

# ---------------------------------------------------------------- calabazas decorativas
def gen_calabaza():
    frames = []
    for f in range(2):
        img = lienzo(14, 13)
        cx, cy, rx, ry = 6.5, 7.5, 6.3, 4.8
        for y in range(13):
            for x in range(14):
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                    c = hexc("#e8701a")
                    if abs(x - cx) < 0.8 or abs(abs(x - cx) - 3.3) < 0.6: c = hexc("#b84f12")
                    if x > cx + 3 or y > cy + 3: c = hexc("#a9440e")
                    img[y, x] = c
        img[2, 6] = hexc("#3f7a2a"); img[2, 7] = hexc("#3f7a2a"); img[1, 7] = hexc("#5aa03a")
        luz = hexc("#ffe066") if f == 0 else hexc("#ffb02e")
        for (x, y) in [(4, 6), (5, 6), (4, 5), (9, 6), (8, 6), (9, 5), (4, 9), (5, 10), (6, 10), (7, 10), (8, 10), (9, 9), (6, 9), (7, 9)]:
            img[y, x] = luz
        frames.append(contorno(img, K))
    guardar(hoja(frames), "calabaza")

# ---------------------------------------------------------------- murciélago
def gen_murcielago():
    a = [
        "k.........k",
        "kk...k.k..kk",
        "kkk..kkk.kkk",
        ".kkkkrkrkkk.",
        "..kkkkkkkk..",
        "....k..k....",
    ]
    b = [
        "............",
        ".....k.k....",
        "..kkkkkkkk..",
        ".kkkkrkrkkkk",
        "kk..kkkkk.kk",
        "k...........",
    ]
    pal = {".": None, "k": hexc("#1a1024"), "r": hexc("#ff3b4e")}
    guardar(hoja([desde_mapa(a, pal), desde_mapa(b, pal)]), "murcielago")

# ---------------------------------------------------------------- mira / cursor
def gen_mira():
    m = [
        "...kkk...",
        "...kyk...",
        "...kyk...",
        "kkk...kkk",
        "kyy.y.yyk",
        "kkk...kkk",
        "...kyk...",
        "...kyk...",
        "...kkk...",
    ]
    m[4] = "kyy...yyk"
    img = desde_mapa(m, {".": None, "k": K, "y": hexc("#ffe066")})
    img[4, 4] = hexc("#ff8c1a")
    guardar(img, "mira")

# ---------------------------------------------------------------- niebla (tileable)
def gen_niebla():
    W, H = 128, 64
    rng = np.random.default_rng(5)
    ruido = np.zeros((H, W))
    for esc, amp in ((16, 1.0), (8, 0.5), (4, 0.25)):
        g = rng.random((H // esc + 1, W // esc + 1))
        g[:, -1] = g[:, 0]; g[-1, :] = g[0, :]
        ys = np.linspace(0, H // esc, H, endpoint=False)
        xs = np.linspace(0, W // esc, W, endpoint=False)
        y0 = ys.astype(int); x0 = xs.astype(int)
        fy = (ys - y0)[:, None]; fx = (xs - x0)[None, :]
        fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
        a = g[y0][:, x0]; b = g[y0][:, x0 + 1]; c = g[y0 + 1][:, x0]; d = g[y0 + 1][:, x0 + 1]
        ruido += amp * (a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + d * fx * fy)
    ruido /= ruido.max()
    bayer = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0
    img = lienzo(W, H)
    for y in range(H):
        for x in range(W):
            v = ruido[y, x]
            lvl = 0
            if v > 0.55 + bayer[y % 4, x % 4] * 0.15: lvl = 1
            if v > 0.72 + bayer[y % 4, x % 4] * 0.1: lvl = 2
            if lvl:
                img[y, x] = (170, 180, 215, 34 if lvl == 1 else 60)
    guardar(img, "niebla")

# ---------------------------------------------------------------- fondo (cementerio)
def gen_fondo():
    W, H = 384, 216
    if os.path.exists(FONDO_FUENTE):
        im = Image.open(FONDO_FUENTE).convert("RGB")
        w0, h0 = im.size
        tw = int(h0 * 16 / 9)
        if tw <= w0:
            x0 = (w0 - tw) // 2
            im = im.crop((x0, 0, x0 + tw, h0))
        else:
            th = int(w0 * 9 / 16); y0 = (h0 - th) // 2
            im = im.crop((0, y0, w0, y0 + th))
        im = im.resize((W, H), Image.LANCZOS)
        im = im.quantize(64, method=Image.Quantize.MEDIANCUT).convert("RGBA")
        guardar(np.array(im), "fondo")
        return
    # respaldo procedural si no existe la imagen
    img = lienzo(W, H)
    rng = random.Random(3)
    for y in range(H):
        for x in range(W):
            v = rng.random()
            c = hexc("#2a2230") if v > 0.1 else hexc("#352a3a")
            img[y, x] = c
    guardar(img, "fondo")

# =================================================================== AUDIO
SR = 22050

def guardar_wav(nombre, datos):
    os.makedirs(DIR_AUD, exist_ok=True)
    datos = np.clip(datos, -1, 1)
    pcm = (datos * 32000).astype(np.int16)
    with wave.open(os.path.join(DIR_AUD, nombre + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(pcm.tobytes())

def t_(dur):
    return np.arange(int(SR * dur)) / SR

def cuadrada(freq, t, duty=0.5):
    fase = np.cumsum(np.broadcast_to(freq, t.shape) / SR) % 1.0
    return np.where(fase < duty, 1.0, -1.0)

def triangular(freq, t):
    fase = np.cumsum(np.broadcast_to(freq, t.shape) / SR) % 1.0
    return 4 * np.abs(fase - 0.5) - 1

def ruido(n):
    return np.random.uniform(-1, 1, n)

def env(n, a=0.005, d=0.1, s=0.6, r=0.1, total=None):
    total = n
    e = np.ones(total)
    na, nd, nr = int(a * SR), int(d * SR), int(r * SR)
    na = min(na, total);
    e[:na] = np.linspace(0, 1, na) if na else 1
    if nd: e[na:na + nd] = np.linspace(1, s, len(e[na:na + nd]))
    e[na + nd:] = s
    if nr: e[-nr:] *= np.linspace(1, 0, len(e[-nr:]))
    return e

def decae(n, k):
    return np.exp(-np.arange(n) / SR * k)

def sfx():
    # click de linterna
    t = t_(0.06)
    s = cuadrada(1800, t, 0.3) * decae(len(t), 90) * 0.35 + ruido(len(t)) * decae(len(t), 160) * 0.3
    guardar_wav("linterna", s)

    # carga (loop): zumbido eléctrico que se acelera con pitch
    t = t_(0.5)
    f = 330 + 8 * np.sin(2 * np.pi * 12 * t)
    s = (cuadrada(f, t, 0.25) * 0.18 + triangular(f * 2, t) * 0.12) * (0.8 + 0.2 * np.sin(2 * np.pi * 16 * t))
    guardar_wav("carga", s)

    # fantasma desvanecido: aullido descendente + destellos
    t = t_(0.75)
    f = 900 * np.exp(-t * 2.4) + 180 + 25 * np.sin(2 * np.pi * 7 * t)
    s = triangular(f, t) * 0.45 * decae(len(t), 3.5)
    chispa = np.zeros_like(t)
    for i, fq in enumerate([1568, 2093, 2637, 3136]):
        st = int(i * 0.06 * SR); tt = t_(0.12)
        seg = cuadrada(fq, tt, 0.5) * decae(len(tt), 30) * 0.18
        chispa[st:st + len(seg)] += seg[:len(chispa) - st]
    guardar_wav("fantasma_muere", s + chispa)

    # puntos (moneda)
    t = t_(0.18)
    f = np.where(t < 0.06, 988, 1319)
    guardar_wav("puntos", cuadrada(f, t, 0.5) * decae(len(t), 14) * 0.3)

    # jugador golpeado
    t = t_(0.45)
    f = 420 * np.exp(-t * 4) + 60
    s = cuadrada(f, t, 0.5) * 0.4 * decae(len(t), 5) + ruido(len(t)) * 0.35 * decae(len(t), 12)
    guardar_wav("golpe", s)

    # dulces robados: crujido + risita de fantasma
    t = t_(0.7)
    s = ruido(len(t)) * 0.4 * decae(len(t), 18)
    risa = np.zeros_like(t)
    for i in range(4):
        st = int((0.15 + i * 0.11) * SR); tt = t_(0.09)
        fq = 760 - i * 60 + 40 * np.sin(2 * np.pi * 30 * tt)
        seg = triangular(fq, tt) * 0.35 * env(len(tt), 0.005, 0.02, 0.8, 0.04)
        risa[st:st + len(seg)] += seg[:len(risa) - st]
    guardar_wav("robo", s + risa)

    # aparición de fantasma: susurro
    t = t_(0.6)
    n = ruido(len(t))
    n = np.convolve(n, np.ones(8) / 8, mode="same")
    s = n * np.sin(np.pi * t / 0.6) * 0.35 + triangular(220 + 60 * np.sin(2 * np.pi * 3 * t), t) * 0.08 * np.sin(np.pi * t / 0.6)
    guardar_wav("aparicion", s)

    # campana de oleada
    t = t_(1.6)
    s = np.zeros_like(t)
    for fq, a in ((196, 0.5), (392, 0.3), (588, 0.18), (784, 0.12), (1046, 0.06)):
        s += np.sin(2 * np.pi * fq * t) * a
    s *= decae(len(t), 2.2) * 0.6
    guardar_wav("campana", s)

    # game over: arpegio menor descendente
    notas = [440, 349.2, 293.7, 220, 174.6, 146.8]
    s = np.zeros(int(SR * 2.0))
    for i, fq in enumerate(notas):
        st = int(i * 0.22 * SR); tt = t_(0.5 if i < 5 else 1.0)
        seg = (cuadrada(fq, tt, 0.5) * 0.25 + triangular(fq / 2, tt) * 0.25) * decae(len(tt), 3 if i == 5 else 6)
        s[st:st + len(seg)] += seg[:len(s) - st]
    guardar_wav("game_over", s)

    # botón
    t = t_(0.08)
    guardar_wav("boton", cuadrada(np.where(t < 0.03, 660, 990), t, 0.5) * decae(len(t), 25) * 0.3)
    t = t_(0.04)
    guardar_wav("hover", cuadrada(1320, t, 0.25) * decae(len(t), 60) * 0.15)

    # oleada superada (fanfarria corta)
    s = np.zeros(int(SR * 0.9))
    for i, fq in enumerate([523.3, 659.3, 784, 1046.5]):
        st = int(i * 0.09 * SR); tt = t_(0.5)
        seg = cuadrada(fq, tt, 0.25) * decae(len(tt), 6) * 0.22
        s[st:st + len(seg)] += seg[:len(s) - st]
    guardar_wav("oleada_ok", s)

# ---------------------------------------------------------------- música
NOTA = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}

def fq(n):
    if n is None: return None
    nombre, oct_ = n[:-1], int(n[-1])
    return 440.0 * 2 ** ((NOTA[nombre] + (oct_ - 4) * 12) / 12)

def pista(bpm, compases, lead, bajo, arp_acordes, perc=True, vol=0.8):
    paso = 60 / bpm / 4  # semicorchea
    total = int(compases * 16 * paso * SR)
    out = np.zeros(total)
    def poner(st, seg):
        out[st:st + len(seg)] += seg[:max(0, total - st)]
    # lead: lista de (nota, duración en semicorcheas)
    i = 0
    for nota, dur in lead:
        if nota:
            tt = t_(dur * paso)
            f = fq(nota) * (1 + 0.006 * np.sin(2 * np.pi * 5.5 * tt))
            seg = cuadrada(f, tt, 0.25) * 0.16 * env(len(tt), 0.005, 0.05, 0.7, 0.04)
            poner(int(i * paso * SR), seg)
        i += dur
    # bajo
    i = 0
    for nota, dur in bajo:
        if nota:
            tt = t_(dur * paso)
            seg = triangular(fq(nota), tt) * 0.32 * env(len(tt), 0.003, 0.08, 0.8, 0.03)
            poner(int(i * paso * SR), seg)
        i += dur
    # arpegios (un acorde por compás, semicorcheas)
    for c, acorde in enumerate(arp_acordes):
        for k in range(16):
            n = acorde[k % len(acorde)]
            tt = t_(paso * 0.9)
            seg = cuadrada(fq(n), tt, 0.125) * 0.055 * decae(len(tt), 18)
            poner(int((c * 16 + k) * paso * SR), seg)
    # percusión
    if perc:
        for k in range(compases * 16):
            st = int(k * paso * SR)
            if k % 8 == 0:
                tt = t_(0.15); f = 120 * np.exp(-tt * 30) + 45
                poner(st, np.sin(2 * np.pi * np.cumsum(f) / SR) * 0.45 * decae(len(tt), 20))
            if k % 8 == 4:
                tt = t_(0.12); poner(st, ruido(len(tt)) * 0.2 * decae(len(tt), 28))
            if k % 2 == 1:
                tt = t_(0.03); poner(st, ruido(len(tt)) * 0.05 * decae(len(tt), 90))
    out *= vol
    # pequeño eco
    d = int(paso * 3 * SR)
    eco = np.zeros_like(out); eco[d:] = out[:-d] * 0.25
    return np.tanh((out + eco) * 1.2) * 0.85

def musica():
    # Menú: lento y misterioso (Re menor)
    lead_m = [
        ("D5", 4), ("F5", 4), ("A5", 6), ("G5", 2),
        ("F5", 4), ("E5", 4), ("D5", 8),
        ("C#5", 4), ("E5", 4), ("A5", 6), ("G5", 2),
        ("F5", 4), ("E5", 2), ("F5", 2), ("E5", 8),
        ("D5", 4), ("F5", 4), ("A5", 6), ("A#5", 2),
        ("A5", 4), ("G5", 4), ("F5", 8),
        ("E5", 4), ("G5", 4), ("F5", 4), ("E5", 4),
        ("D5", 12), (None, 4),
    ]
    bajo_m = []
    for n in ["D2", "D2", "A1", "A1", "D2", "A#1", "A1", "D2"]:
        bajo_m += [(n, 6), (n, 2), (None, 4), (n, 4)]
    acordes_m = [["D4", "F4", "A4", "D5"], ["D4", "F4", "A4", "F4"], ["C#4", "E4", "A4", "E4"], ["A3", "C#4", "E4", "A4"],
                 ["D4", "F4", "A4", "D5"], ["A#3", "D4", "F4", "A#4"], ["A3", "C#4", "E4", "G4"], ["D4", "F4", "A4", "F4"]]
    m = pista(84, 8, lead_m, bajo_m, acordes_m, perc=False, vol=0.9)
    guardar_wav("musica_menu", m)

    # Juego: más rápido y tenso (Mi menor / frigio)
    frase_a = [("E5", 2), ("G5", 2), ("B5", 2), ("G5", 2), ("C6", 4), ("B5", 2), ("A5", 2),
               ("G5", 2), ("F#5", 2), ("E5", 2), ("F5", 2), ("E5", 8)]
    frase_b = [("E5", 2), ("G5", 2), ("B5", 2), ("D6", 2), ("C6", 4), ("B5", 4),
               ("A5", 2), ("G5", 2), ("F#5", 2), ("G5", 2), ("F#5", 4), ("D#5", 4)]
    frase_c = [("B5", 3), ("A5", 1), ("G5", 2), ("F5", 2), ("E5", 4), (None, 4),
               ("F5", 2), ("G5", 2), ("A5", 2), ("G5", 2), ("F5", 4), ("E5", 4)]
    lead_j = frase_a + frase_b + frase_a + frase_c
    bajo_j = []
    for n in ["E2", "E2", "C2", "B1", "E2", "E2", "F2", "E2"]:
        bajo_j += [(n, 2), (n, 2), (n, 2), (None, 2), (n, 2), (n, 2), (n + "", 2), (n, 2)]
    acordes_j = [["E4", "G4", "B4", "E5"], ["E4", "G4", "B4", "G4"], ["C4", "E4", "G4", "C5"], ["B3", "D#4", "F#4", "B4"],
                 ["E4", "G4", "B4", "E5"], ["E4", "G4", "B4", "D5"], ["F4", "A4", "C5", "F5"], ["E4", "G4", "B4", "E5"]]
    j = pista(132, 8, lead_j, bajo_j, acordes_j, perc=True, vol=0.8)
    guardar_wav("musica_juego", j)

# ===================================================================
def main():
    print("Generando sprites en", DIR_SPR)
    gen_fondo(); gen_personaje(); gen_fantasmas(); gen_cubeta(); gen_iconos()
    gen_calabaza(); gen_murcielago(); gen_mira(); gen_niebla()
    print("Generando audio en", DIR_AUD)
    sfx(); musica()
    print("Listo.")

if __name__ == "__main__":
    main()
