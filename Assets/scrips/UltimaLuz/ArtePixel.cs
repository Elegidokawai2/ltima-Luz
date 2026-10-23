using System.Collections.Generic;
using UnityEngine;

namespace UltimaLuz
{
    /// <summary>
    /// Utilidades de pixel art: carga y corte de sprites, texturas generadas y creación de SpriteRenderers.
    /// Todo trabaja a 16 pixeles por unidad sobre una resolución virtual de 384x216.
    /// </summary>
    public static class ArtePixel
    {
        public const int PPU = 16;
        public const int AnchoVirtual = 384;
        public const int AltoVirtual = 216;

        static Material material;
        static bool materialBuscado;
        static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        static Sprite pixelCentro, pixelEsquina;

        public static Material Material
        {
            get
            {
                if (!materialBuscado)
                {
                    materialBuscado = true;
                    // Material de sprites "unlit" del propio URP 2D (compatible con su batching).
                    // El shader legado "Sprites/Default" provocaba que los sprites mostraran texturas equivocadas.
                    Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (sh != null) material = new Material(sh);
                }
                return material;
            }
        }

        public static Texture2D Textura(string nombre)
        {
            if (texturas.TryGetValue(nombre, out Texture2D t) && t != null) return t;
            t = Resources.Load<Texture2D>("UltimaLuz/Sprites/" + nombre);
            if (t == null)
            {
                Debug.LogError("[Última Luz] Falta el sprite Resources/UltimaLuz/Sprites/" + nombre + ".png. Ejecuta Tools/generar_assets.py");
                t = NuevaTextura(16, 16);
                Color32[] px = new Color32[256];
                for (int i = 0; i < px.Length; i++) px[i] = ((i / 4 + i / 64) % 2 == 0) ? new Color32(255, 0, 255, 255) : new Color32(0, 0, 0, 255);
                t.SetPixels32(px);
                t.Apply();
            }
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            texturas[nombre] = t;
            return t;
        }

        /// <summary>Corta una tira horizontal de frames del mismo tamaño.</summary>
        public static Sprite[] Tira(string nombre, int frames, float pivotX = 0.5f, float pivotY = 0f)
        {
            Texture2D t = Textura(nombre);
            int w = Mathf.Max(1, t.width / frames);
            int h = t.height;
            Sprite[] arr = new Sprite[frames];
            for (int i = 0; i < frames; i++)
                arr[i] = Sprite.Create(t, new Rect(i * w, 0, w, h), PivotEntero(w, h, pivotX, pivotY), PPU, 0, SpriteMeshType.FullRect);
            return arr;
        }

        public static Sprite Unico(string nombre, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            Texture2D t = Textura(nombre);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), PivotEntero(t.width, t.height, pivotX, pivotY), PPU, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Pivote alineado a pixel entero para que el sprite no quede "entre pixeles".</summary>
        public static Vector2 PivotEntero(int w, int h, float px, float py)
        {
            return new Vector2(Mathf.Floor(w * px) / w, Mathf.Floor(h * py) / h);
        }

        public static Sprite DesdeTextura(Texture2D t, float px = 0.5f, float py = 0.5f, int ppu = PPU)
        {
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), PivotEntero(t.width, t.height, px, py), ppu, 0, SpriteMeshType.FullRect);
        }

        public static Texture2D NuevaTextura(int w, int h)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        /// <summary>Pixel blanco con pivote al centro (partículas).</summary>
        public static Sprite PixelCentro
        {
            get
            {
                if (pixelCentro == null) pixelCentro = CrearPixel(new Vector2(0.5f, 0.5f));
                return pixelCentro;
            }
        }

        /// <summary>Pixel blanco con pivote abajo-izquierda (barras y rectángulos).</summary>
        public static Sprite PixelEsquina
        {
            get
            {
                if (pixelEsquina == null) pixelEsquina = CrearPixel(Vector2.zero);
                return pixelEsquina;
            }
        }

        static Sprite CrearPixel(Vector2 pivot)
        {
            Texture2D t = NuevaTextura(1, 1);
            t.SetPixel(0, 0, Color.white);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 1, 1), pivot, PPU, 0, SpriteMeshType.FullRect);
        }

        public static SpriteRenderer Crear(string nombre, Sprite s, Transform padre, int orden)
        {
            GameObject go = new GameObject(nombre);
            if (padre != null) go.transform.SetParent(padre, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            // IMPORTANTE: primero el material y después el sprite. Si se cambia el material
            // después de asignar el sprite, Unity 6 conserva una textura vieja (se veía el fondo).
            if (Material != null) sr.sharedMaterial = Material;
            sr.sortingOrder = orden;
            sr.sprite = null;
            sr.sprite = s;
            return sr;
        }

        /// <summary>Redondea una posición a la rejilla de pixeles virtuales.</summary>
        public static Vector3 Snap(Vector2 p, float z = 0f)
        {
            return new Vector3(Mathf.Round(p.x * PPU) / PPU, Mathf.Round(p.y * PPU) / PPU, z);
        }

        /// <summary>Convierte pixeles virtuales (0,0 abajo-izquierda) a coordenadas locales de cámara.</summary>
        public static Vector3 VirtualALocal(float px, float py, float z = 10f)
        {
            return new Vector3((px - AnchoVirtual / 2f) / PPU, (py - AltoVirtual / 2f) / PPU, z);
        }

        /// <summary>Caja con borde estilo pixel art (para botones y paneles).</summary>
        public static Sprite Caja(int w, int h, Color32 relleno, Color32 borde, Color32 luz, Color32 sombra, bool esquinasRedondas = true)
        {
            Texture2D t = NuevaTextura(w, h);
            Color32[] px = new Color32[w * h];
            Color32 vacio = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool bordeX = x == 0 || x == w - 1;
                    bool bordeY = y == 0 || y == h - 1;
                    Color32 c = relleno;
                    if (bordeX || bordeY) c = borde;
                    else if (y == h - 2) c = luz;
                    else if (y == 1) c = sombra;
                    else if (x == 1) c = luz;
                    else if (x == w - 2) c = sombra;
                    if (esquinasRedondas && bordeX && bordeY) c = vacio;
                    px[y * w + x] = c;
                }
            }
            t.SetPixels32(px);
            t.Apply();
            return DesdeTextura(t);
        }

        public static Sprite Elipse(int w, int h, Color32 c)
        {
            Texture2D t = NuevaTextura(w, h);
            Color32[] px = new Color32[w * h];
            float cx = (w - 1) / 2f, cy = (h - 1) / 2f;
            float rx = w / 2f, ry = h / 2f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = ((x - cx) / rx) * ((x - cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry);
                    px[y * w + x] = d <= 1f ? c : new Color32(0, 0, 0, 0);
                }
            t.SetPixels32(px);
            t.Apply();
            return DesdeTextura(t, 0.5f, 0.5f);
        }

        /// <summary>Viñeta roja de daño (bordes rojos, centro transparente, con tramado).</summary>
        public static Sprite VinetaDano()
        {
            int w = 192, h = 108;
            Texture2D t = NuevaTextura(w, h);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w / 2f) / (w / 2f), dy = (y - h / 2f) / (h / 2f);
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                    float umbral = Oscuridad.Bayer(x, y);
                    a = Mathf.Floor(a * 4f + umbral) / 4f;
                    px[y * w + x] = new Color32(200, 20, 40, (byte)(Mathf.Clamp01(a) * 200));
                }
            t.SetPixels32(px);
            t.Apply();
            return DesdeTextura(t, 0.5f, 0.5f, PPU / 2);
        }
    }
}
