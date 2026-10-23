using System.Collections.Generic;
using UnityEngine;

namespace UltimaLuz
{
    /// <summary>Partículas pixeladas, textos flotantes y destellos.</summary>
    public class EfectosPixel
    {
        class Particula
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float vida, vidaMax, gravedad, friccion;
            public Color color;
            public int tam;
            public bool activa;
        }

        class TextoFlotante
        {
            public SpriteRenderer sr;
            public Vector2 pos;
            public float vida, vidaMax;
        }

        readonly Transform raiz;
        readonly List<Particula> particulas = new List<Particula>();
        readonly List<TextoFlotante> textos = new List<TextoFlotante>();
        const int Max = 400;
        int orden;
        int ordenTexto;

        public EfectosPixel(Transform padre, int ordenParticulas, int ordenTextos)
        {
            raiz = new GameObject("Efectos").transform;
            raiz.SetParent(padre, false);
            orden = ordenParticulas;
            ordenTexto = ordenTextos;
        }

        Particula Libre()
        {
            foreach (Particula p in particulas) if (!p.activa) return p;
            if (particulas.Count >= Max) return particulas[Random.Range(0, particulas.Count)];
            Particula n = new Particula();
            n.sr = ArtePixel.Crear("p", ArtePixel.PixelCentro, raiz, orden);
            particulas.Add(n);
            return n;
        }

        public void Emitir(Vector2 pos, Vector2 vel, Color color, float vida, int tam = 1, float gravedad = 0f, float friccion = 1.5f)
        {
            Particula p = Libre();
            p.pos = pos; p.vel = vel; p.color = color; p.vida = vida; p.vidaMax = vida;
            p.tam = tam; p.gravedad = gravedad; p.friccion = friccion; p.activa = true;
            p.sr.gameObject.SetActive(true);
            p.sr.color = color;
            p.sr.transform.localScale = new Vector3(tam, tam, 1);
        }

        public void Estallido(Vector2 pos, Color a, Color b, int cantidad, float velocidad, float vida, int tamMax = 2, float gravedad = 0f)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float v = Random.Range(velocidad * 0.3f, velocidad);
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * v;
                Emitir(pos, vel, Color.Lerp(a, b, Random.value), Random.Range(vida * 0.5f, vida), Random.Range(1, tamMax + 1), gravedad);
            }
        }

        /// <summary>Ectoplasma: sube como humo.</summary>
        public void Ectoplasma(Vector2 pos, Color color, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                Vector2 vel = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.4f, 2.6f));
                Emitir(pos + Random.insideUnitCircle * 0.4f, vel, color, Random.Range(0.5f, 1.1f), Random.Range(1, 3), -0.6f, 1.2f);
            }
        }

        /// <summary>Dulces que salen volando con gravedad.</summary>
        public void Dulces(Vector2 pos, int cantidad)
        {
            Color[] cols =
            {
                new Color32(255, 79, 123, 255), new Color32(255, 210, 63, 255), new Color32(79, 209, 255, 255),
                new Color32(155, 92, 255, 255), new Color32(125, 255, 106, 255)
            };
            for (int i = 0; i < cantidad; i++)
            {
                Vector2 vel = new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(3f, 6.5f));
                Emitir(pos, vel, cols[Random.Range(0, cols.Length)], Random.Range(0.7f, 1.2f), 2, 14f, 0.3f);
            }
        }

        public void Texto(string texto, Vector2 pos, Color32 color, float vida = 0.9f, int escala = 1)
        {
            TextoFlotante t = null;
            foreach (TextoFlotante x in textos) if (x.vida <= 0f) { t = x; break; }
            if (t == null)
            {
                t = new TextoFlotante();
                t.sr = ArtePixel.Crear("txt", null, raiz, ordenTexto);
                textos.Add(t);
            }
            t.sr.sprite = FuentePixel.Texto(texto, color, escala);
            t.sr.color = Color.white;
            t.sr.gameObject.SetActive(true);
            t.pos = pos;
            t.vida = vida;
            t.vidaMax = vida;
        }

        public void Actualizar(float dt)
        {
            foreach (Particula p in particulas)
            {
                if (!p.activa) continue;
                p.vida -= dt;
                if (p.vida <= 0f)
                {
                    p.activa = false;
                    p.sr.gameObject.SetActive(false);
                    continue;
                }
                p.vel.y -= p.gravedad * dt;
                p.vel *= Mathf.Max(0f, 1f - p.friccion * dt);
                p.pos += p.vel * dt;
                float k = p.vida / p.vidaMax;
                Color c = p.color;
                c.a = k > 0.4f ? 1f : (Mathf.Floor(k / 0.4f * 3f) + 1f) / 4f;
                p.sr.color = c;
                p.sr.transform.localPosition = ArtePixel.Snap(p.pos);
            }

            foreach (TextoFlotante t in textos)
            {
                if (t.vida <= 0f) continue;
                t.vida -= dt;
                if (t.vida <= 0f)
                {
                    t.sr.gameObject.SetActive(false);
                    continue;
                }
                float k = 1f - t.vida / t.vidaMax;
                Vector2 p = t.pos + new Vector2(0, Mathf.Min(k * 3f, 1f) * 0.9f);
                t.sr.transform.localPosition = ArtePixel.Snap(p);
                // parpadeo al final
                t.sr.enabled = !(k > 0.7f && Mathf.FloorToInt(k * 30f) % 2 == 0);
            }
        }

        public void Limpiar()
        {
            foreach (Particula p in particulas) { p.activa = false; p.sr.gameObject.SetActive(false); }
            foreach (TextoFlotante t in textos) { t.vida = 0f; t.sr.gameObject.SetActive(false); }
        }
    }
}
