using System.Collections.Generic;
using UnityEngine;

namespace UltimaLuz
{
    /// <summary>
    /// Capa de oscuridad pixelada con tramado (dithering). Cada frame se recalcula con las
    /// fuentes de luz (halo del jugador, cono de la linterna, cubeta, calabazas, fantasmas).
    /// </summary>
    public class Oscuridad
    {
        public struct Luz
        {
            public Vector2 pos;
            public float radio;
            public float intensidad;
            public bool calida;
        }

        const int W = 192, H = 108;        // media resolución: cada celda = 2x2 pixeles virtuales
        const float Celda = 2f / ArtePixel.PPU;

        static readonly float[] bayer4 =
        {
            0f, 8f, 2f, 10f,
            12f, 4f, 14f, 6f,
            3f, 11f, 1f, 9f,
            15f, 7f, 13f, 5f
        };

        public static float Bayer(int x, int y) { return bayer4[(y & 3) * 4 + (x & 3)] / 16f; }

        readonly Texture2D textura;
        readonly Color32[] pixeles = new Color32[W * H];
        readonly float[] vineta = new float[W * H];
        public readonly SpriteRenderer render;
        public readonly List<Luz> luces = new List<Luz>();

        // Cono de la linterna
        public bool conoActivo;
        public Vector2 conoOrigen;
        public Vector2 conoDireccion = Vector2.right;
        public float conoAlcance = 7.5f;
        public float conoMedioAngulo = 24f;

        public float ambiente = 0.62f;
        public float parpadeo = 1f;

        static readonly Color32 colorOscuro = new Color32(8, 5, 22, 255);
        static readonly Color32 colorCalido = new Color32(255, 214, 120, 255);
        static readonly Color32 colorFrio = new Color32(150, 255, 210, 255);

        public Oscuridad(Transform padre, int orden)
        {
            textura = ArtePixel.NuevaTextura(W, H);
            Sprite s = ArtePixel.DesdeTextura(textura, 0.5f, 0.5f, ArtePixel.PPU / 2);
            render = ArtePixel.Crear("Oscuridad", s, padre, orden);
            render.transform.localPosition = new Vector3(0, 0, 10);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - W / 2f) / (W / 2f);
                    float dy = (y - H / 2f) / (H / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy * 0.9f);
                    vineta[y * W + x] = Mathf.Clamp01((d - 0.55f) / 0.7f) * 0.3f;
                }
        }

        /// <param name="centro">Posición del mundo que corresponde al centro de la pantalla (sin sacudida).</param>
        public void Actualizar(Vector2 centro)
        {
            float cosMax = Mathf.Cos(conoMedioAngulo * Mathf.Deg2Rad);
            float cosSuave = Mathf.Cos((conoMedioAngulo - 7f) * Mathf.Deg2Rad);
            Vector2 dir = conoDireccion.sqrMagnitude > 0.0001f ? conoDireccion.normalized : Vector2.right;
            int nLuces = luces.Count;
            float alcance2 = conoAlcance * conoAlcance;

            for (int y = 0; y < H; y++)
            {
                float wy = centro.y + (y - H / 2f + 0.5f) * Celda;
                for (int x = 0; x < W; x++)
                {
                    float wx = centro.x + (x - W / 2f + 0.5f) * Celda;
                    float luz = 0f;
                    float calida = 0f;
                    float fria = 0f;

                    for (int i = 0; i < nLuces; i++)
                    {
                        Luz l = luces[i];
                        float dx = wx - l.pos.x, dy = wy - l.pos.y;
                        float d2 = dx * dx + dy * dy;
                        float r2 = l.radio * l.radio;
                        if (d2 >= r2) continue;
                        float v = (1f - Mathf.Sqrt(d2) / l.radio) * l.intensidad;
                        if (v > luz) luz = v;
                        if (l.calida) { if (v * 0.5f > calida) calida = v * 0.5f; }
                        else if (v * 0.45f > fria) fria = v * 0.45f;
                    }

                    if (conoActivo)
                    {
                        float dx = wx - conoOrigen.x, dy = wy - conoOrigen.y;
                        float d2 = dx * dx + dy * dy;
                        if (d2 < alcance2 && d2 > 0.0001f)
                        {
                            float d = Mathf.Sqrt(d2);
                            float c = (dx * dir.x + dy * dir.y) / d;
                            if (c > cosMax)
                            {
                                float borde = c >= cosSuave ? 1f : (c - cosMax) / (cosSuave - cosMax);
                                float lejos = 1f - Mathf.Pow(d / conoAlcance, 2.2f);
                                float v = Mathf.Clamp01(borde * lejos * 1.25f) * parpadeo;
                                if (v > luz) luz = v;
                                float cv = v * 0.9f;
                                if (cv > calida) calida = cv;
                            }
                        }
                    }

                    int idx = y * W + x;
                    float aOsc = Mathf.Clamp01((ambiente + vineta[idx]) * (1f - Mathf.Clamp01(luz)));
                    float aCal = calida * 0.22f;
                    float aFri = fria * 0.2f;
                    float umbral = bayer4[(y & 3) * 4 + (x & 3)] / 16f;
                    aOsc = Mathf.Floor(aOsc * 6f + umbral) / 6f;
                    aCal = Mathf.Floor(aCal * 10f + umbral) / 10f;
                    aFri = Mathf.Floor(aFri * 10f + umbral) / 10f;

                    float aTotal = aOsc + aCal + aFri;
                    if (aTotal <= 0.001f)
                    {
                        pixeles[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    float r = (colorOscuro.r * aOsc + colorCalido.r * aCal + colorFrio.r * aFri) / aTotal;
                    float g = (colorOscuro.g * aOsc + colorCalido.g * aCal + colorFrio.g * aFri) / aTotal;
                    float b = (colorOscuro.b * aOsc + colorCalido.b * aCal + colorFrio.b * aFri) / aTotal;
                    pixeles[idx] = new Color32((byte)r, (byte)g, (byte)b, (byte)(Mathf.Clamp01(aTotal) * 255f));
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply(false);
        }
    }
}
