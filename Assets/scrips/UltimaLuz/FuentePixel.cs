using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UltimaLuz
{
    /// <summary>
    /// Fuente pixel art de 5x7 (proporcional) con soporte para acentos y Ñ.
    /// Genera sprites de texto con contorno y sombra, listos para la cámara de 384x216.
    /// </summary>
    public static class FuentePixel
    {
        const int AltoGlifo = 7;
        const int FilasAcento = 2;

        static Dictionary<char, string[]> glifos;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static readonly string[] Agudo = { "...#.", "..#.." };
        static readonly string[] Tilde = { ".##.#", "#..#." };
        static readonly string[] Dieresis = { ".#.#.", "....." };

        static void Iniciar()
        {
            if (glifos != null) return;
            glifos = new Dictionary<char, string[]>
            {
                ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
                ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
                ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
                ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
                ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
                ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
                ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" },
                ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
                ['I'] = new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" },
                ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
                ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
                ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
                ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
                ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
                ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
                ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
                ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
                ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
                ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
                ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
                ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
                ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
                ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
                ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
                ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
                ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
                ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
                ['1'] = new[] { ".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###" },
                ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
                ['3'] = new[] { "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###." },
                ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
                ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
                ['6'] = new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." },
                ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
                ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
                ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." },
                ['!'] = new[] { "#", "#", "#", "#", "#", ".", "#" },
                ['¡'] = new[] { "#", ".", "#", "#", "#", "#", "#" },
                ['?'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." },
                ['¿'] = new[] { "..#..", ".....", "..#..", ".#...", "#....", "#...#", ".###." },
                ['.'] = new[] { ".", ".", ".", ".", ".", ".", "#" },
                [','] = new[] { "..", "..", "..", "..", "..", ".#", "#." },
                [':'] = new[] { ".", ".", "#", ".", ".", "#", "." },
                ['-'] = new[] { "....", "....", "....", "####", "....", "....", "...." },
                ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
                ['/'] = new[] { "....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...." },
                ['\''] = new[] { "#", "#", ".", ".", ".", ".", "." },
                ['('] = new[] { ".#", "#.", "#.", "#.", "#.", "#.", ".#" },
                [')'] = new[] { "#.", ".#", ".#", ".#", ".#", ".#", "#." },
                ['<'] = new[] { "...#", "..#.", ".#..", "#...", ".#..", "..#.", "...#" },
                ['>'] = new[] { "#...", ".#..", "..#.", "...#", "..#.", ".#..", "#..." },
                ['*'] = new[] { ".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#" },
                ['·'] = new[] { ".", ".", ".", "#", ".", ".", "." },
            };
        }

        static char Base(char c, out string[] acento)
        {
            acento = null;
            switch (c)
            {
                case 'Á': acento = Agudo; return 'A';
                case 'É': acento = Agudo; return 'E';
                case 'Í': acento = Agudo; return 'I';
                case 'Ó': acento = Agudo; return 'O';
                case 'Ú': acento = Agudo; return 'U';
                case 'Ü': acento = Dieresis; return 'U';
                case 'Ñ': acento = Tilde; return 'N';
            }
            return c;
        }

        /// <summary>Mapa de bits (true = pixel encendido) del texto, sin escalar. [y][x], y=0 arriba.</summary>
        static bool[,] Rasterizar(string texto, out int ancho, out int alto)
        {
            Iniciar();
            texto = texto.ToUpperInvariant();
            string[] lineas = texto.Split('\n');
            int altoLinea = AltoGlifo + FilasAcento + 2;
            alto = lineas.Length * altoLinea - 2;
            int[] anchos = new int[lineas.Length];
            ancho = 1;
            for (int l = 0; l < lineas.Length; l++)
            {
                int w = 0;
                foreach (char ch in lineas[l]) w += AnchoGlifo(ch) + 1;
                anchos[l] = Mathf.Max(0, w - 1);
                ancho = Mathf.Max(ancho, anchos[l]);
            }
            bool[,] mapa = new bool[alto, ancho];
            for (int l = 0; l < lineas.Length; l++)
            {
                int x = (ancho - anchos[l]) / 2; // centrado
                int y0 = l * altoLinea;
                foreach (char ch in lineas[l])
                {
                    char b = Base(ch, out string[] acento);
                    if (glifos.TryGetValue(b, out string[] g))
                    {
                        int gw = g[0].Length;
                        for (int gy = 0; gy < AltoGlifo; gy++)
                            for (int gx = 0; gx < gw; gx++)
                                if (g[gy][gx] == '#') mapa[y0 + FilasAcento + gy, x + gx] = true;
                        if (acento != null)
                        {
                            int off = (gw - 5) / 2;
                            for (int ay = 0; ay < FilasAcento; ay++)
                                for (int ax = 0; ax < 5; ax++)
                                    if (acento[ay][ax] == '#')
                                    {
                                        int px = x + ax + off;
                                        if (px >= 0 && px < ancho) mapa[y0 + ay, px] = true;
                                    }
                        }
                    }
                    x += AnchoGlifo(ch) + 1;
                }
            }
            return mapa;
        }

        static int AnchoGlifo(char ch)
        {
            if (ch == ' ') return 3;
            char b = Base(ch, out _);
            if (glifos.TryGetValue(b, out string[] g)) return g[0].Length;
            return 3;
        }

        /// <summary>
        /// Crea (o recupera de caché) un sprite de texto.
        /// escala: tamaño de cada pixel de la fuente en pixeles virtuales.
        /// </summary>
        public static Sprite Texto(string texto, Color32 arriba, Color32 abajo, int escala = 1,
                                   bool contorno = true, bool sombra = true, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            Color32 cContorno = new Color32(20, 12, 28, 255);
            string clave = new StringBuilder().Append(texto).Append('|').Append(arriba.r).Append(',').Append(arriba.g).Append(',').Append(arriba.b)
                .Append('|').Append(abajo.r).Append(',').Append(abajo.g).Append(',').Append(abajo.b).Append('|').Append(escala)
                .Append(contorno).Append(sombra).Append(pivotX).Append(pivotY).ToString();
            if (cache.TryGetValue(clave, out Sprite s) && s != null) return s;

            bool[,] mapa = Rasterizar(texto, out int w0, out int h0);
            int borde = contorno ? 1 : 0;
            int sombraPx = sombra ? 1 : 0;
            int w = w0 * escala + borde * 2;
            int h = h0 * escala + borde * 2 + sombraPx;
            // ancho/alto par para que el centro caiga en pixel entero
            Color32[] px = new Color32[w * h];
            bool[] relleno = new bool[w * h];
            // y en textura: 0 = abajo
            for (int y = 0; y < h0 * escala; y++)
                for (int x = 0; x < w0 * escala; x++)
                    if (mapa[y / escala, x / escala])
                    {
                        int tx = x + borde;
                        int ty = h - 1 - (y + borde);
                        relleno[ty * w + tx] = true;
                    }
            Color32 cSombra = new Color32(10, 6, 18, 200);
            if (sombra)
            {
                for (int y = 1; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (relleno[y * w + x])
                        {
                            int sy = y - 1 - (contorno ? 1 : 0);
                            if (sy >= 0) px[sy * w + x] = cSombra;
                        }
            }
            if (contorno)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (relleno[y * w + x]) continue;
                        bool cerca = false;
                        for (int dy = -1; dy <= 1 && !cerca; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                                if (relleno[ny * w + nx]) { cerca = true; break; }
                            }
                        if (cerca) px[y * w + x] = cContorno;
                    }
            }
            int yMin = h, yMax = 0;
            for (int i = 0; i < relleno.Length; i++) if (relleno[i]) { int yy = i / w; yMin = Mathf.Min(yMin, yy); yMax = Mathf.Max(yMax, yy); }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (relleno[y * w + x])
                    {
                        float t = yMax > yMin ? (float)(y - yMin) / (yMax - yMin) : 1f;
                        px[y * w + x] = Color32.Lerp(abajo, arriba, t);
                    }

            Texture2D tex = ArtePixel.NuevaTextura(w, h);
            tex.SetPixels32(px);
            tex.Apply();
            s = ArtePixel.DesdeTextura(tex, pivotX, pivotY);
            if (cache.Count > 400) cache.Clear();
            cache[clave] = s;
            return s;
        }

        public static Sprite Texto(string texto, Color32 color, int escala = 1, bool contorno = true, bool sombra = true, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            return Texto(texto, color, color, escala, contorno, sombra, pivotX, pivotY);
        }
    }
}
