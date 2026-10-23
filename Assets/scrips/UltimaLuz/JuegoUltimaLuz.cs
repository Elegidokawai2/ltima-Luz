using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UltimaLuz
{
    /// <summary>
    /// LA ÚLTIMA LUZ — juego completo en un solo componente.
    /// Protege la cubeta de dulces con tu linterna: ilumina a un fantasma 2.5 s seguidos para desvanecerlo.
    /// Fantasma ladrón → va a la cubeta. Fantasma perseguidor → va hacia ti.
    /// Tú y la cubeta tienen 3 vidas; si alguno llega a 0, pierdes.
    /// </summary>
    public class JuegoUltimaLuz : MonoBehaviour
    {
        // ------------------------------------------------------------------ ajustes
        [Header("Jugador")]
        public float velocidadJugador = 5.5f;
        public float tiempoInvulnerable = 2f;
        public int vidasJugador = 3;

        [Header("Cubeta")]
        public int vidasCubeta = 3;

        [Header("Linterna")]
        public float tiempoParaDesvanecer = 2.5f;
        public float alcanceLinterna = 7.5f;
        public float medioAnguloLinterna = 24f;
        [Tooltip("Velocidad (por segundo) a la que baja la carga cuando el fantasma sale de la luz")]
        public float caidaCarga = 0.8f;
        [Tooltip("Multiplicador de velocidad de un fantasma mientras se ilumina")]
        public float frenadoPorLuz = 0.5f;

        [Header("Puntos")]
        public int puntosLadron = 100;
        public int puntosPerseguidor = 150;
        public int bonoOleada = 250;

        // ------------------------------------------------------------------ constantes
        const int O_FONDO = -1000, O_SOMBRA = -800, O_ENTIDAD = 500, O_OSCURIDAD = 1000,
                  O_FANTASMA = 1300, O_BARRA = 1500, O_MURCIELAGO = 1550, O_PARTICULAS = 1600, O_TEXTOS = 1700,
                  O_VINETA = 1900, O_HUD = 2000, O_PANEL = 3000, O_CURSOR = 5000;

        static readonly Vector2 LimiteMin = new Vector2(-10.6f, -5.6f);
        static readonly Vector2 LimiteMax = new Vector2(10.6f, 4.6f);
        static readonly Vector2 PosCubeta = new Vector2(0f, -0.8f);
        static Vector2 CentroCubeta => PosCubeta + new Vector2(0f, 0.8f);
        const string ClaveRecord = "UltimaLuz.Record";

        static readonly Color32 Amarillo = new Color32(255, 224, 102, 255);
        static readonly Color32 Naranja = new Color32(242, 140, 40, 255);
        static readonly Color32 NaranjaOscuro = new Color32(200, 80, 16, 255);
        static readonly Color32 Blanco = new Color32(255, 246, 228, 255);
        static readonly Color32 Lila = new Color32(190, 170, 240, 255);
        static readonly Color32 Rojo = new Color32(255, 70, 80, 255);
        static readonly Color32 RojoOscuro = new Color32(150, 20, 40, 255);
        static readonly Color32 Gris = new Color32(150, 140, 170, 255);
        static readonly Color32 VerdeFantasma = new Color32(156, 240, 200, 255);
        static readonly Color32 LilaFantasma = new Color32(233, 228, 255, 255);

        // ------------------------------------------------------------------ tipos internos
        enum Estado { Menu, Jugando, Pausa, GameOver }
        enum FaseOleada { Anuncio, Activa, Descanso }

        class Espectro
        {
            public bool ladron;
            public bool decorativo;
            public Vector2 pos, vel;
            public float velocidad;
            public float carga;
            public float tAnim, fase;
            public float aparicion;
            public float aturdido;
            public SpriteRenderer sr, sombra, barraFondo, barraRelleno;
            public Vector2 Centro => pos + new Vector2(0f, 0.6f);
        }

        class Murcielago
        {
            public SpriteRenderer sr;
            public Vector2 pos;
            public float vel, fase, t;
        }

        class Calabaza
        {
            public SpriteRenderer sr;
            public Vector2 pos;
            public float t, ruido;
        }

        class Boton
        {
            public Rect rect;
            public SpriteRenderer fondo, texto;
            public Sprite normal, encima;
            public Vector3 posTexto;
            public Action accion;
            public bool sobre;
        }

        // ------------------------------------------------------------------ estado
        Estado estado;
        Camera camara;
        Camera camaraPantalla;
        RenderTexture rt;
        Transform mundo, uiRaiz, hudRaiz;
        Oscuridad oscuridad;
        EfectosPixel efectos;
        AudioUltimaLuz audioJuego;

        // sprites
        Sprite[] sprJugador, sprLadron, sprPerseguidor, sprCubeta, sprIconos, sprCalabaza, sprMurcielago;
        Sprite sprSombra, sprSombraChica;

        // escena
        SpriteRenderer jugadorSr, jugadorSombra, cubetaSr, cubetaSombra, cursorSr, vinetaSr;
        readonly SpriteRenderer[] pipsCubeta = new SpriteRenderer[3];
        readonly List<Calabaza> calabazas = new List<Calabaza>();
        readonly List<Murcielago> murcielagos = new List<Murcielago>();
        readonly List<Espectro> espectros = new List<Espectro>();
        readonly List<Boton> botones = new List<Boton>();
        readonly List<GameObject> objetosUI = new List<GameObject>();

        // jugador
        Vector2 jugadorPos;
        bool mirandoDerecha = true;
        float tAnimJugador;
        float invulnerable;
        float tGolpe;
        int vidas;
        bool linternaEncendida;
        Vector2 direccionLinterna = Vector2.right;
        Espectro objetivo;

        // cubeta
        int vidasBolsa;
        float flashCubeta;

        // partida
        int puntuacion;
        int puntuacionMostrada = -1;
        int combo = 1;
        float ultimaMuerte = -99f;
        float tiempoJuego;
        int oleada;
        FaseOleada faseOleada;
        float tFase;
        readonly List<bool> colaAparicion = new List<bool>();
        float tAparicion;
        float intervaloAparicion;
        int fantasmasEliminados;
        bool perdiendo;
        float tPerdiendo;
        string razonDerrota;
        float escalaTiempo = 1f;

        // HUD
        SpriteRenderer hudPuntos, hudOleada, hudCombo, banner, banner2;
        readonly SpriteRenderer[] hudCorazones = new SpriteRenderer[3];
        readonly SpriteRenderer[] hudDulces = new SpriteRenderer[3];
        float tBanner;
        float tCombo;
        int comboMostrado = -1;
        int oleadaMostrada = -1;

        // menú
        SpriteRenderer tituloSr, titulo2Sr, nuevoRecordSr;
        float tiempoReal;

        // efectos de pantalla
        float sacudida;
        float vineta;
        float tMurcielago = 3f;

        // ratón
        Vector2 ratonVirtual;
        Vector2 ratonMundo;

        // ================================================================== arranque
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoIniciar()
        {
            if (SceneManager.GetActiveScene().name != "UltimaLuz") return;
            if (FindAnyObjectByType<JuegoUltimaLuz>() != null) return;
            new GameObject("UltimaLuz").AddComponent<JuegoUltimaLuz>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;

            // Desactiva cámaras ajenas de la escena (si las hay)
            foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled = false;
            foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) l.enabled = false;

            CrearCamaras();

            mundo = new GameObject("Mundo").transform;
            uiRaiz = new GameObject("UI").transform;
            uiRaiz.SetParent(camara.transform, false);
            uiRaiz.localPosition = new Vector3(0, 0, 10);
            hudRaiz = new GameObject("HUD").transform;
            hudRaiz.SetParent(camara.transform, false);
            hudRaiz.localPosition = new Vector3(0, 0, 10);

            CargarSprites();
            CrearEscenario();

            oscuridad = new Oscuridad(camara.transform, O_OSCURIDAD);
            efectos = new EfectosPixel(mundo, O_PARTICULAS, O_TEXTOS);
            audioJuego = new AudioUltimaLuz(gameObject);

            vinetaSr = ArtePixel.Crear("VinetaDano", ArtePixel.VinetaDano(), camara.transform, O_VINETA);
            vinetaSr.transform.localPosition = new Vector3(0, 0, 10);
            vinetaSr.color = new Color(1, 1, 1, 0);

            cursorSr = ArtePixel.Crear("Mira", ArtePixel.Unico("mira"), camara.transform, O_CURSOR);

            CrearHUD();
            IrMenu();
        }

        void CrearCamaras()
        {
            rt = new RenderTexture(ArtePixel.AnchoVirtual, ArtePixel.AltoVirtual, 24, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            rt.name = "PantallaPixel";
            rt.Create();

            GameObject gc = new GameObject("CamaraJuego");
            camara = gc.AddComponent<Camera>();
            camara.orthographic = true;
            camara.orthographicSize = ArtePixel.AltoVirtual / 2f / ArtePixel.PPU;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color32(10, 6, 18, 255);
            camara.transform.position = new Vector3(0, 0, -10);
            camara.targetTexture = rt;
            camara.allowHDR = false;
            camara.allowMSAA = false;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = 50f;
            gc.tag = "MainCamera";
            gc.AddComponent<AudioListener>();

            GameObject gp = new GameObject("CamaraPantalla");
            camaraPantalla = gp.AddComponent<Camera>();
            camaraPantalla.cullingMask = 0;
            camaraPantalla.clearFlags = CameraClearFlags.SolidColor;
            camaraPantalla.backgroundColor = Color.black;
            camaraPantalla.depth = -100;
            camaraPantalla.orthographic = true;
            camaraPantalla.transform.position = new Vector3(0, 0, -200);
        }

        void CargarSprites()
        {
            sprJugador = ArtePixel.Tira("personaje", 5, 0.5f, 0f);
            sprLadron = ArtePixel.Tira("fantasma_ladron", 4, 0.5f, 0f);
            sprPerseguidor = ArtePixel.Tira("fantasma_perseguidor", 4, 0.5f, 0f);
            sprCubeta = ArtePixel.Tira("cubeta", 4, 0.5f, 0f);
            sprIconos = ArtePixel.Tira("iconos", 4, 0.5f, 0.5f);
            sprCalabaza = ArtePixel.Tira("calabaza", 2, 0.5f, 0f);
            sprMurcielago = ArtePixel.Tira("murcielago", 2, 0.5f, 0.5f);
            sprSombra = ArtePixel.Elipse(12, 4, new Color32(5, 3, 12, 110));
            sprSombraChica = ArtePixel.Elipse(10, 3, new Color32(5, 3, 12, 80));
        }

        void CrearEscenario()
        {
            ArtePixel.Crear("Fondo", ArtePixel.Unico("fondo"), mundo, O_FONDO).transform.localPosition = Vector3.zero;


            Vector2[] posCalabazas =
            {
                new Vector2(-2.6f, 0.9f), new Vector2(2.6f, 0.9f), new Vector2(-2.9f, -1.9f), new Vector2(2.9f, -1.9f),
                new Vector2(-8.6f, 3.9f), new Vector2(8.7f, -4.6f)
            };
            foreach (Vector2 p in posCalabazas)
            {
                Calabaza c = new Calabaza { pos = p, ruido = UnityEngine.Random.value * 10f };
                c.sr = ArtePixel.Crear("Calabaza", sprCalabaza[0], mundo, OrdenY(O_ENTIDAD, p.y));
                c.sr.transform.localPosition = ArtePixel.Snap(p);
                calabazas.Add(c);
            }

            cubetaSombra = ArtePixel.Crear("SombraCubeta", ArtePixel.Elipse(22, 6, new Color32(5, 3, 12, 120)), mundo, O_SOMBRA);
            cubetaSombra.transform.localPosition = ArtePixel.Snap(PosCubeta + new Vector2(0, 0.1f));
            cubetaSr = ArtePixel.Crear("Cubeta", sprCubeta[0], mundo, OrdenY(O_ENTIDAD, PosCubeta.y));
            cubetaSr.transform.localPosition = ArtePixel.Snap(PosCubeta);
            for (int i = 0; i < 3; i++)
            {
                pipsCubeta[i] = ArtePixel.Crear("PipDulce", sprIconos[2], mundo, O_BARRA);
                pipsCubeta[i].transform.localPosition = ArtePixel.Snap(PosCubeta + new Vector2((i - 1) * 0.5625f, 2.0f));
            }

            jugadorSombra = ArtePixel.Crear("SombraJugador", sprSombra, mundo, O_SOMBRA);
            jugadorSr = ArtePixel.Crear("Jugador", sprJugador[0], mundo, O_ENTIDAD);
        }

        static int OrdenY(int baseOrden, float y)
        {
            return baseOrden - Mathf.RoundToInt(y * ArtePixel.PPU);
        }

        // ================================================================== bucle principal
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            tiempoReal += dt;

            LeerRaton();
            Keyboard kb = Keyboard.current;

            switch (estado)
            {
                case Estado.Menu:
                    if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) IniciarPartida();
                    ActualizarMenu(dt);
                    break;
                case Estado.Jugando:
                    if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) && !perdiendo) { Pausar(); break; }
                    ActualizarJuego(dt);
                    break;
                case Estado.Pausa:
                    if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) Continuar();
                    audioJuego.Carga(false, 0f);
                    break;
                case Estado.GameOver:
                    if (kb != null && kb.rKey.wasPressedThisFrame) IniciarPartida();
                    else if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame)) IrMenu();
                    ActualizarEspectrosQuietos(dt);
                    break;
            }

            if (estado != Estado.Pausa)
            {
                efectos.Actualizar(dt * escalaTiempo);
                ActualizarMurcielagos(dt);
            }
            ActualizarAmbiente(dt);
            ActualizarBotones();
            ActualizarHUD(dt);
            ActualizarCamaraYLuces(dt);
            ActualizarCursor();
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || rt == null) return;
            CalcularPantalla(out float s, out float ox, out float oy);
            GUI.DrawTexture(new Rect(Mathf.Round(ox), Mathf.Round(oy), ArtePixel.AnchoVirtual * s, ArtePixel.AltoVirtual * s), rt, ScaleMode.StretchToFill, false);
        }

        void OnDestroy()
        {
            if (camara != null) camara.targetTexture = null;
            if (rt != null) rt.Release();
            Cursor.visible = true;
        }

        void OnApplicationFocus(bool foco)
        {
            Cursor.visible = !foco;
            if (!foco && estado == Estado.Jugando && !perdiendo) Pausar();
        }

        static void CalcularPantalla(out float escala, out float ox, out float oy)
        {
            float s = Mathf.Min(Screen.width / (float)ArtePixel.AnchoVirtual, Screen.height / (float)ArtePixel.AltoVirtual);
            float si = Mathf.Floor(s);
            if (si >= 1f && si / s > 0.85f) s = si;
            escala = Mathf.Max(0.01f, s);
            ox = (Screen.width - ArtePixel.AnchoVirtual * escala) / 2f;
            oy = (Screen.height - ArtePixel.AltoVirtual * escala) / 2f;
        }

        void LeerRaton()
        {
            Mouse m = Mouse.current;
            if (m == null) return;
            Vector2 p = m.position.ReadValue();
            CalcularPantalla(out float s, out float ox, out float oy);
            ratonVirtual = new Vector2(Mathf.Clamp((p.x - ox) / s, 0, ArtePixel.AnchoVirtual - 1), Mathf.Clamp((p.y - oy) / s, 0, ArtePixel.AltoVirtual - 1));
            ratonMundo = new Vector2((ratonVirtual.x - ArtePixel.AnchoVirtual / 2f) / ArtePixel.PPU, (ratonVirtual.y - ArtePixel.AltoVirtual / 2f) / ArtePixel.PPU);
        }

        // ================================================================== estados
        void IrMenu()
        {
            estado = Estado.Menu;
            perdiendo = false;
            escalaTiempo = 1f;
            LimpiarEspectros();
            efectos.Limpiar();
            LimpiarUI();
            MostrarHUD(false);
            jugadorSr.gameObject.SetActive(false);
            jugadorSombra.gameObject.SetActive(false);
            MostrarCubeta(false);
            audioJuego.Carga(false, 0f);
            audioJuego.Musica("musica_menu");

            // fantasmas decorativos
            for (int i = 0; i < 4; i++)
            {
                Espectro e = CrearEspectro(i % 2 == 0, new Vector2(UnityEngine.Random.Range(-9f, 9f), UnityEngine.Random.Range(-5f, 3f)));
                e.decorativo = true;
                e.aparicion = 1f;
                e.fase = i * 1.7f;
                e.velocidad = UnityEngine.Random.Range(0.6f, 1.1f);
            }

            // UI del menú
            titulo2Sr = CrearTextoUI("LA", Amarillo, Naranja, 2, 192, 178, O_PANEL + 2);
            tituloSr = CrearTextoUI("ÚLTIMA LUZ", new Color32(255, 236, 140, 255), NaranjaOscuro, 4, 192, 150, O_PANEL + 2);
            CrearTextoUI("PROTEGE LOS DULCES DE LOS FANTASMAS", Lila, 1, 192, 122, O_PANEL + 2);
            CrearBoton("INICIAR", 192, 84, 108, 28, 2, IniciarPartida);
            CrearTextoUI("WASD: MOVER   MOUSE: APUNTAR   ESPACIO / CLIC: LINTERNA", Gris, 1, 192, 30, O_PANEL + 2);
            int record = PlayerPrefs.GetInt(ClaveRecord, 0);
            if (record > 0) CrearTextoUI("RÉCORD: " + record, Amarillo, 1, 192, 16, O_PANEL + 2);
        }

        void IniciarPartida()
        {
            LimpiarUI();
            LimpiarEspectros();
            efectos.Limpiar();
            estado = Estado.Jugando;
            perdiendo = false;
            escalaTiempo = 1f;

            vidas = vidasJugador;
            vidasBolsa = vidasCubeta;
            puntuacion = 0;
            puntuacionMostrada = -1;
            comboMostrado = -1;
            oleadaMostrada = -1;
            combo = 1;
            ultimaMuerte = -99f;
            tiempoJuego = 0f;
            fantasmasEliminados = 0;
            invulnerable = 0f;
            tGolpe = 0f;
            objetivo = null;
            jugadorPos = new Vector2(0f, -3.2f);
            mirandoDerecha = true;

            jugadorSr.gameObject.SetActive(true);
            jugadorSombra.gameObject.SetActive(true);
            MostrarCubeta(true);
            ActualizarCubeta();
            MostrarHUD(true);

            audioJuego.Musica("musica_juego");
            audioJuego.TonoMusica(1f);
            IniciarOleada(1);
        }

        void Pausar()
        {
            if (estado != Estado.Jugando) return;
            estado = Estado.Pausa;
            audioJuego.Efecto("boton", 0.6f, 0f);
            audioJuego.PausarMusica(true);
            CrearVeloUI(0.55f);
            CrearPanelUI(170, 124, 192, 108);
            CrearTextoUI("PAUSA", Amarillo, Naranja, 3, 192, 148, O_PANEL + 2);
            CrearBoton("CONTINUAR", 192, 114, 110, 20, 1, Continuar);
            CrearBoton("REINICIAR", 192, 88, 110, 20, 1, IniciarPartida);
            CrearBoton("MENÚ", 192, 62, 110, 20, 1, IrMenu);
        }

        void Continuar()
        {
            if (estado != Estado.Pausa) return;
            LimpiarUI();
            estado = Estado.Jugando;
            audioJuego.PausarMusica(false);
        }

        void Perder(string razon)
        {
            if (perdiendo) return;
            perdiendo = true;
            razonDerrota = razon;
            tPerdiendo = 1.4f;
            escalaTiempo = 0.3f;
            linternaEncendida = false;
            objetivo = null;
            audioJuego.Carga(false, 0f);
            audioJuego.DetenerMusica();
            audioJuego.Efecto("game_over", 1f, 0f);
            sacudida = Mathf.Max(sacudida, 0.5f);
        }

        void MostrarGameOver()
        {
            estado = Estado.GameOver;
            perdiendo = false;
            escalaTiempo = 1f;
            int record = PlayerPrefs.GetInt(ClaveRecord, 0);
            bool nuevo = puntuacion > record;
            if (nuevo)
            {
                record = puntuacion;
                PlayerPrefs.SetInt(ClaveRecord, record);
                PlayerPrefs.Save();
            }

            CrearVeloUI(0.6f);
            CrearPanelUI(220, 160, 192, 104);
            CrearTextoUI("PERDISTE", new Color32(255, 120, 120, 255), RojoOscuro, 3, 192, 162, O_PANEL + 2);
            CrearTextoUI(razonDerrota, Naranja, 1, 192, 138, O_PANEL + 2);
            CrearTextoUI("PUNTUACIÓN: " + puntuacion, Blanco, 2, 192, 118, O_PANEL + 2);
            CrearTextoUI("OLEADA: " + oleada + "    FANTASMAS: " + fantasmasEliminados, Lila, 1, 192, 99, O_PANEL + 2);
            if (nuevo && puntuacion > 0)
                nuevoRecordSr = CrearTextoUI("¡NUEVO RÉCORD!", Amarillo, Naranja, 1, 192, 85, O_PANEL + 2);
            else
            {
                nuevoRecordSr = null;
                CrearTextoUI("RÉCORD: " + record, Amarillo, 1, 192, 85, O_PANEL + 2);
            }
            CrearBoton("REINICIAR", 140, 54, 92, 22, 1, IniciarPartida);
            CrearBoton("MENÚ", 244, 54, 92, 22, 1, IrMenu);
            audioJuego.Musica("musica_menu");
            audioJuego.TonoMusica(0.85f);
        }

        // ================================================================== menú
        void ActualizarMenu(float dt)
        {
            if (tituloSr != null)
            {
                int bob = Mathf.RoundToInt(Mathf.Sin(tiempoReal * 2f) * 2f);
                tituloSr.transform.localPosition = ArtePixel.VirtualALocal(192, 150 + bob, 0);
                titulo2Sr.transform.localPosition = ArtePixel.VirtualALocal(192, 178 + bob, 0);
            }
            // fantasmas que pasean en círculos
            foreach (Espectro e in espectros)
            {
                e.tAnim += dt;
                Vector2 objetivoPaseo = new Vector2(Mathf.Sin(tiempoReal * 0.25f * e.velocidad + e.fase) * 9f,
                                                    Mathf.Cos(tiempoReal * 0.33f * e.velocidad + e.fase * 1.3f) * 4.2f - 0.8f);
                e.vel = Vector2.Lerp(e.vel, (objetivoPaseo - e.pos) * 0.8f, dt * 2f);
                e.pos += e.vel * dt;
                DibujarEspectro(e, false);
            }
        }

        // ================================================================== juego
        void ActualizarJuego(float dtReal)
        {
            if (perdiendo)
            {
                tPerdiendo -= dtReal;
                if (tPerdiendo <= 0f) { MostrarGameOver(); return; }
            }
            float dt = dtReal * escalaTiempo;
            tiempoJuego += dt;

            // --- movimiento directo, sin inercia
            Keyboard kb = Keyboard.current;
            Mouse m = Mouse.current;
            Vector2 mov = Vector2.zero;
            if (kb != null && !perdiendo)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mov.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mov.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) mov.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) mov.y += 1f;
            }
            if (mov.sqrMagnitude > 1f) mov.Normalize();
            jugadorPos += mov * velocidadJugador * dt;
            jugadorPos.x = Mathf.Clamp(jugadorPos.x, LimiteMin.x, LimiteMax.x);
            jugadorPos.y = Mathf.Clamp(jugadorPos.y, LimiteMin.y, LimiteMax.y);
            // no atravesar la cubeta
            Vector2 dc = jugadorPos - (PosCubeta + new Vector2(0, 0.25f));
            Vector2 escalaColision = new Vector2(1f, 2f);
            Vector2 dcs = Vector2.Scale(dc, escalaColision);
            if (dcs.magnitude < 1.05f && dcs.sqrMagnitude > 0.0001f)
                jugadorPos = PosCubeta + new Vector2(0, 0.25f) + Vector2.Scale(dcs.normalized * 1.05f, new Vector2(1f, 0.5f));

            // --- linterna
            bool pedirLinterna = !perdiendo && ((kb != null && kb.spaceKey.isPressed) || (m != null && m.leftButton.isPressed));
            if (pedirLinterna && !linternaEncendida) audioJuego.Efecto("linterna", 0.8f);
            if (!pedirLinterna && linternaEncendida) audioJuego.Efecto("linterna", 0.4f, 0.1f);
            linternaEncendida = pedirLinterna;

            Vector2 origenProv = jugadorPos + new Vector2(0f, 0.4f);
            Vector2 haciaRaton = ratonMundo - origenProv;
            if (haciaRaton.sqrMagnitude > 0.04f) direccionLinterna = haciaRaton.normalized;

            if (linternaEncendida) mirandoDerecha = direccionLinterna.x >= 0f;
            else if (mov.x > 0.01f) mirandoDerecha = true;
            else if (mov.x < -0.01f) mirandoDerecha = false;

            Vector2 origenLinterna = OrigenLinterna();

            // --- oleadas
            ActualizarOleadas(dt);

            // --- objetivo de la linterna (solo uno a la vez)
            Espectro nuevo = null;
            if (linternaEncendida)
            {
                if (objetivo != null && espectros.Contains(objetivo) && EnCono(objetivo, origenLinterna)) nuevo = objetivo;
                else
                {
                    float mejor = float.MaxValue;
                    foreach (Espectro e in espectros)
                    {
                        if (e.aparicion < 0.5f || !EnCono(e, origenLinterna)) continue;
                        Vector2 d = e.Centro - origenLinterna;
                        float puntaje = Vector2.Angle(direccionLinterna, d) + d.magnitude * 1.5f - e.carga * 20f;
                        if (puntaje < mejor) { mejor = puntaje; nuevo = e; }
                    }
                }
            }
            objetivo = nuevo;

            // --- fantasmas
            Vector2 centroJugador = jugadorPos + new Vector2(0, 0.5f);
            for (int i = espectros.Count - 1; i >= 0; i--)
            {
                Espectro e = espectros[i];
                bool iluminado = e == objetivo;
                if (iluminado) e.carga += dt;
                else e.carga = Mathf.Max(0f, e.carga - caidaCarga * dt);

                if (e.carga >= tiempoParaDesvanecer)
                {
                    Desvanecer(e);
                    continue;
                }

                MoverEspectro(e, dt, iluminado, centroJugador);

                if (!perdiendo && e.aparicion >= 0.6f)
                {
                    if (e.ladron)
                    {
                        Vector2 d = e.Centro - CentroCubeta;
                        if (d.magnitude < 0.95f) { RoboDulces(e); continue; }
                    }
                    else if (invulnerable <= 0f && (e.Centro - centroJugador).magnitude < 0.72f)
                    {
                        GolpearJugador(e);
                        continue;
                    }
                }
                DibujarEspectro(e, iluminado);
            }

            audioJuego.Carga(objetivo != null, objetivo != null ? objetivo.carga / tiempoParaDesvanecer : 0f);

            // --- jugador
            invulnerable = Mathf.Max(0f, invulnerable - dt);
            tGolpe = Mathf.Max(0f, tGolpe - dt);
            DibujarJugador(mov, dt);

            // combo
            if (tiempoJuego - ultimaMuerte > 3.5f) combo = 1;
        }

        Vector2 OrigenLinterna()
        {
            return jugadorPos + new Vector2(mirandoDerecha ? 0.4375f : -0.4375f, 0.375f);
        }

        bool EnCono(Espectro e, Vector2 origen)
        {
            Vector2 d = e.Centro - origen;
            float dist = d.magnitude;
            if (dist > alcanceLinterna + 0.3f) return false;
            if (dist < 0.6f) return true;
            float holgura = Mathf.Atan2(0.45f, dist) * Mathf.Rad2Deg;
            return Vector2.Angle(direccionLinterna, d) <= medioAnguloLinterna + holgura;
        }

        void MoverEspectro(Espectro e, float dt, bool iluminado, Vector2 centroJugador)
        {
            e.tAnim += dt;
            if (e.aparicion < 1f) e.aparicion = Mathf.Min(1f, e.aparicion + dt / 0.7f);
            float factor = (iluminado ? frenadoPorLuz : 1f) * (e.aparicion < 1f ? 0.5f : 1f);

            if (e.aturdido > 0f)
            {
                e.aturdido -= dt;
                e.vel *= Mathf.Max(0f, 1f - 3f * dt);
            }
            else if (e.ladron)
            {
                Vector2 dir = (CentroCubeta - e.Centro).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                Vector2 deseo = (dir + perp * Mathf.Sin(e.tAnim * 2.2f + e.fase) * 0.4f).normalized * e.velocidad * factor;
                e.vel = Vector2.MoveTowards(e.vel, deseo, 8f * dt);
            }
            else
            {
                Vector2 dir = (centroJugador - e.Centro).normalized;
                Vector2 deseo = dir * e.velocidad * factor;
                // separación entre perseguidores
                foreach (Espectro o in espectros)
                {
                    if (o == e || o.ladron) continue;
                    Vector2 s = e.pos - o.pos;
                    float d = s.magnitude;
                    if (d < 1f && d > 0.001f) deseo += s / d * (1f - d) * 2f;
                }
                e.vel = Vector2.MoveTowards(e.vel, deseo, 6f * dt);
            }
            e.pos += e.vel * dt;
        }

        void DibujarEspectro(Espectro e, bool iluminado)
        {
            Sprite[] frames = e.ladron ? sprLadron : sprPerseguidor;
            int f = Mathf.FloorToInt(e.tAnim * 6f + e.fase) & 3;
            e.sr.sprite = frames[f];
            if (Mathf.Abs(e.vel.x) > 0.05f) e.sr.flipX = e.vel.x < 0f;

            int bob = Mathf.RoundToInt(Mathf.Sin(e.tAnim * 3f + e.fase) * 1.5f);
            float progreso = e.carga / tiempoParaDesvanecer;
            int temblor = iluminado ? UnityEngine.Random.Range(-1, 2) : 0;
            Vector2 p = e.pos + new Vector2(temblor, bob + 2) / ArtePixel.PPU;
            e.sr.transform.localPosition = ArtePixel.Snap(p);
            e.sr.sortingOrder = OrdenY(O_FANTASMA, e.pos.y);

            float alfa;
            if (e.aparicion < 1f) alfa = Mathf.Floor(e.aparicion * 4f) / 4f;
            else alfa = 0.9f;
            if (progreso > 0f) alfa *= 1f - progreso * 0.5f;
            Color c = Color.white;
            if (iluminado && Mathf.FloorToInt(tiempoReal * 16f) % 2 == 0) c = new Color(1f, 1f, 0.55f);
            c.a = alfa;
            e.sr.color = c;

            e.sombra.transform.localPosition = ArtePixel.Snap(e.pos);
            e.sombra.color = new Color(1, 1, 1, e.aparicion);

            bool barra = progreso > 0.001f && !e.decorativo;
            e.barraFondo.gameObject.SetActive(barra);
            e.barraRelleno.gameObject.SetActive(barra);
            if (barra)
            {
                Vector2 bp = e.pos + new Vector2(-8f, 21f) / ArtePixel.PPU;
                Vector3 bpos = ArtePixel.Snap(bp);
                e.barraFondo.transform.localPosition = bpos;
                e.barraFondo.transform.localScale = new Vector3(16, 4, 1);
                int ancho = Mathf.Clamp(Mathf.RoundToInt(progreso * 14f), 1, 14);
                e.barraRelleno.transform.localPosition = bpos + new Vector3(1f, 1f, 0) / ArtePixel.PPU;
                e.barraRelleno.transform.localScale = new Vector3(ancho, 2, 1);
                e.barraRelleno.color = Color.Lerp(new Color32(255, 200, 60, 255), new Color32(255, 255, 230, 255), progreso);
            }
        }

        void ActualizarEspectrosQuietos(float dt)
        {
            foreach (Espectro e in espectros)
            {
                e.tAnim += dt;
                e.vel *= Mathf.Max(0f, 1f - 2f * dt);
                e.pos += e.vel * dt;
                DibujarEspectro(e, false);
            }
        }

        void DibujarJugador(Vector2 mov, float dt)
        {
            bool caminando = mov.sqrMagnitude > 0.01f;
            if (caminando) tAnimJugador += dt;
            else tAnimJugador = 0f;
            int frame = caminando ? Mathf.FloorToInt(tAnimJugador * 10f) % 4 : 0;
            if (tGolpe > 0f) frame = 4;
            jugadorSr.sprite = sprJugador[frame];
            jugadorSr.flipX = !mirandoDerecha;
            jugadorSr.transform.localPosition = ArtePixel.Snap(jugadorPos);
            jugadorSr.sortingOrder = OrdenY(O_ENTIDAD, jugadorPos.y);
            jugadorSr.enabled = invulnerable <= 0f || Mathf.FloorToInt(invulnerable * 12f) % 2 == 0;
            jugadorSombra.transform.localPosition = ArtePixel.Snap(jugadorPos + new Vector2(0, 0.05f));
        }

        // ------------------------------------------------------------------ eventos de juego
        void Desvanecer(Espectro e)
        {
            if (tiempoJuego - ultimaMuerte < 3.5f) combo = Mathf.Min(combo + 1, 5);
            else combo = 1;
            ultimaMuerte = tiempoJuego;
            tCombo = combo > 1 ? 2.5f : 0f;

            int puntos = (e.ladron ? puntosLadron : puntosPerseguidor) * combo;
            puntuacion += puntos;
            fantasmasEliminados++;

            Color col = e.ladron ? (Color)VerdeFantasma : (Color)LilaFantasma;
            efectos.Ectoplasma(e.Centro, col, 24);
            efectos.Estallido(e.Centro, new Color32(255, 240, 150, 255), Color.white, 16, 6f, 0.5f, 2);
            efectos.Texto("+" + puntos, e.Centro + new Vector2(0, 0.7f), Amarillo, 1f);
            audioJuego.Efecto("fantasma_muere", 0.9f);
            audioJuego.Efecto("puntos", 0.5f, 0f);
            sacudida = Mathf.Max(sacudida, 0.08f);
            if (objetivo == e) objetivo = null;
            DestruirEspectro(e);
        }

        void RoboDulces(Espectro e)
        {
            vidasBolsa = Mathf.Max(0, vidasBolsa - 1);
            ActualizarCubeta();
            flashCubeta = 0.35f;
            efectos.Dulces(CentroCubeta + new Vector2(0, 0.6f), 18);
            efectos.Ectoplasma(e.Centro, VerdeFantasma, 14);
            efectos.Texto("¡-1 DULCES!", CentroCubeta + new Vector2(0, 1.6f), Rojo, 1.1f);
            audioJuego.Efecto("robo", 1f);
            sacudida = Mathf.Max(sacudida, 0.3f);
            if (objetivo == e) objetivo = null;
            DestruirEspectro(e);
            if (vidasBolsa <= 0) Perder("¡SE ROBARON TODOS LOS DULCES!");
        }

        void GolpearJugador(Espectro e)
        {
            vidas = Mathf.Max(0, vidas - 1);
            invulnerable = tiempoInvulnerable;
            tGolpe = 0.25f;
            vineta = 0.8f;
            sacudida = Mathf.Max(sacudida, 0.35f);
            Vector2 c = jugadorPos + new Vector2(0, 0.5f);
            efectos.Estallido(c, Rojo, Color.white, 18, 6f, 0.5f, 2);
            efectos.Ectoplasma(e.Centro, LilaFantasma, 12);
            efectos.Texto("¡AY!", c + new Vector2(0, 1f), Rojo, 0.8f);
            audioJuego.Efecto("golpe", 1f);
            if (objetivo == e) objetivo = null;
            DestruirEspectro(e);

            // onda expansiva: aleja a los perseguidores cercanos
            foreach (Espectro o in espectros)
            {
                if (o.ladron) continue;
                Vector2 d = o.Centro - c;
                if (d.magnitude < 3.5f)
                {
                    o.vel = d.normalized * 7f;
                    o.aturdido = 0.8f;
                }
            }
            if (vidas <= 0) Perder("¡LOS FANTASMAS TE ATRAPARON!");
        }

        // ------------------------------------------------------------------ oleadas
        void IniciarOleada(int n)
        {
            oleada = n;
            colaAparicion.Clear();
            int ladrones = 2 + n + n / 3;
            int perseguidores = 1 + (n - 1) / 2;
            for (int i = 0; i < ladrones; i++) colaAparicion.Add(true);
            for (int i = 0; i < perseguidores; i++) colaAparicion.Add(false);
            // mezcla (el primero siempre es ladrón)
            for (int i = colaAparicion.Count - 1; i > 1; i--)
            {
                int j = UnityEngine.Random.Range(1, i + 1);
                bool t = colaAparicion[i]; colaAparicion[i] = colaAparicion[j]; colaAparicion[j] = t;
            }
            intervaloAparicion = Mathf.Max(0.6f, 2.4f - 0.2f * (n - 1));
            faseOleada = FaseOleada.Anuncio;
            tFase = 2.2f;
            MostrarBanner("OLEADA " + n, n == 1 ? "¡PROTEGE LA CUBETA!" : "¡VIENEN MÁS!");
            audioJuego.Efecto("campana", 0.9f, 0f);
            audioJuego.TonoMusica(1f + Mathf.Min(0.15f, 0.02f * (n - 1)));
        }

        void ActualizarOleadas(float dt)
        {
            if (perdiendo) return;
            switch (faseOleada)
            {
                case FaseOleada.Anuncio:
                    tFase -= dt;
                    if (tFase <= 0f) { faseOleada = FaseOleada.Activa; tAparicion = 0.2f; }
                    break;
                case FaseOleada.Activa:
                    tAparicion -= dt;
                    int maxVivos = 4 + oleada;
                    if (tAparicion <= 0f && colaAparicion.Count > 0 && espectros.Count < maxVivos)
                    {
                        bool ladron = colaAparicion[0];
                        colaAparicion.RemoveAt(0);
                        AparecerEspectro(ladron);
                        tAparicion = intervaloAparicion * UnityEngine.Random.Range(0.75f, 1.25f);
                    }
                    if (colaAparicion.Count == 0 && espectros.Count == 0)
                    {
                        int bono = bonoOleada * oleada;
                        puntuacion += bono;
                        MostrarBanner("¡OLEADA SUPERADA!", "BONO +" + bono);
                        audioJuego.Efecto("oleada_ok", 1f, 0f);
                        faseOleada = FaseOleada.Descanso;
                        tFase = 3f;
                    }
                    break;
                case FaseOleada.Descanso:
                    tFase -= dt;
                    if (tFase <= 0f) IniciarOleada(oleada + 1);
                    break;
            }
        }

        void AparecerEspectro(bool ladron)
        {
            Vector2 p;
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0: p = new Vector2(-12.8f, UnityEngine.Random.Range(-5.5f, 4.5f)); break;
                case 1: p = new Vector2(12.8f, UnityEngine.Random.Range(-5.5f, 4.5f)); break;
                case 2: p = new Vector2(UnityEngine.Random.Range(-11f, 11f), 6.6f); break;
                default: p = new Vector2(UnityEngine.Random.Range(-11f, 11f), -8.3f); break;
            }
            Espectro e = CrearEspectro(ladron, p);
            float extra = 0.1f * (oleada - 1);
            e.velocidad = ladron ? Mathf.Min(1.35f + extra, 2.6f) : Mathf.Min(1.7f + extra, 3.1f);
            e.velocidad *= UnityEngine.Random.Range(0.9f, 1.1f);
            audioJuego.Efecto("aparicion", 0.35f, 0.15f);
        }

        Espectro CrearEspectro(bool ladron, Vector2 p)
        {
            Espectro e = new Espectro
            {
                ladron = ladron,
                pos = p,
                fase = UnityEngine.Random.Range(0f, 6.28f),
                aparicion = 0f
            };
            e.sombra = ArtePixel.Crear("SombraFantasma", sprSombraChica, mundo, O_SOMBRA);
            e.sr = ArtePixel.Crear(ladron ? "FantasmaLadron" : "FantasmaPerseguidor", (ladron ? sprLadron : sprPerseguidor)[0], mundo, O_FANTASMA);
            e.barraFondo = ArtePixel.Crear("BarraFondo", ArtePixel.PixelEsquina, mundo, O_BARRA);
            e.barraFondo.color = new Color32(20, 12, 28, 255);
            e.barraRelleno = ArtePixel.Crear("BarraRelleno", ArtePixel.PixelEsquina, mundo, O_BARRA + 1);
            e.barraFondo.gameObject.SetActive(false);
            e.barraRelleno.gameObject.SetActive(false);
            espectros.Add(e);
            DibujarEspectro(e, false);
            return e;
        }

        void DestruirEspectro(Espectro e)
        {
            espectros.Remove(e);
            if (e.sr != null) Destroy(e.sr.gameObject);
            if (e.sombra != null) Destroy(e.sombra.gameObject);
            if (e.barraFondo != null) Destroy(e.barraFondo.gameObject);
            if (e.barraRelleno != null) Destroy(e.barraRelleno.gameObject);
        }

        void LimpiarEspectros()
        {
            for (int i = espectros.Count - 1; i >= 0; i--) DestruirEspectro(espectros[i]);
            objetivo = null;
        }

        // ------------------------------------------------------------------ cubeta
        void MostrarCubeta(bool visible)
        {
            cubetaSr.gameObject.SetActive(visible);
            cubetaSombra.gameObject.SetActive(visible);
            foreach (SpriteRenderer p in pipsCubeta) p.gameObject.SetActive(visible);
        }

        void ActualizarCubeta()
        {
            int idx = Mathf.Clamp(vidasCubeta - vidasBolsa, 0, 3);
            if (vidasCubeta != 3) idx = Mathf.Clamp(Mathf.RoundToInt(3f * (1f - vidasBolsa / (float)vidasCubeta)), 0, 3);
            cubetaSr.sprite = sprCubeta[idx];
            for (int i = 0; i < 3; i++) pipsCubeta[i].sprite = sprIconos[i < vidasBolsa ? 2 : 3];
        }

        // ================================================================== ambiente
        void ActualizarAmbiente(float dt)
        {
            // calabazas parpadeantes
            foreach (Calabaza c in calabazas)
            {
                c.t -= dt;
                if (c.t <= 0f)
                {
                    c.sr.sprite = sprCalabaza[UnityEngine.Random.value < 0.75f ? 0 : 1];
                    c.t = UnityEngine.Random.Range(0.05f, 0.25f);
                }
            }

            // cubeta: destello al ser robada
            if (flashCubeta > 0f)
            {
                flashCubeta -= dt;
                bool on = Mathf.FloorToInt(flashCubeta * 20f) % 2 == 0;
                cubetaSr.color = on ? new Color(1f, 0.5f, 0.5f) : Color.white;
                int temblor = on ? 1 : -1;
                cubetaSr.transform.localPosition = ArtePixel.Snap(PosCubeta + new Vector2(temblor / 16f, 0));
                if (flashCubeta <= 0f)
                {
                    cubetaSr.color = Color.white;
                    cubetaSr.transform.localPosition = ArtePixel.Snap(PosCubeta);
                }
            }
            // pips sobre la cubeta flotan ligeramente
            for (int i = 0; i < 3; i++)
            {
                int bob = Mathf.RoundToInt(Mathf.Sin(tiempoReal * 3f + i) * 1f);
                pipsCubeta[i].transform.localPosition = ArtePixel.Snap(PosCubeta + new Vector2((i - 1) * 0.5625f, 2.0f + bob / 16f));
            }

            // viñeta de daño
            vineta = Mathf.Max(0f, vineta - dt * 1.6f);
            vinetaSr.color = new Color(1, 1, 1, Mathf.Floor(vineta * 4f) / 4f);
        }

        void ActualizarMurcielagos(float dt)
        {
            tMurcielago -= dt;
            if (tMurcielago <= 0f)
            {
                tMurcielago = UnityEngine.Random.Range(5f, 11f);
                int n = UnityEngine.Random.Range(1, 4);
                bool izq = UnityEngine.Random.value < 0.5f;
                float y = UnityEngine.Random.Range(0f, 5.5f);
                for (int i = 0; i < n; i++)
                {
                    Murcielago b = new Murcielago
                    {
                        pos = new Vector2(izq ? -13f - i * 1.2f : 13f + i * 1.2f, y + UnityEngine.Random.Range(-0.8f, 0.8f)),
                        vel = (izq ? 1f : -1f) * UnityEngine.Random.Range(3f, 4.5f),
                        fase = UnityEngine.Random.value * 6f
                    };
                    b.sr = ArtePixel.Crear("Murcielago", sprMurcielago[0], mundo, O_MURCIELAGO);
                    b.sr.flipX = !izq;
                    murcielagos.Add(b);
                }
            }
            for (int i = murcielagos.Count - 1; i >= 0; i--)
            {
                Murcielago b = murcielagos[i];
                b.t += dt;
                b.pos.x += b.vel * dt;
                float y = b.pos.y + Mathf.Sin(b.t * 5f + b.fase) * 0.35f;
                b.sr.sprite = sprMurcielago[Mathf.FloorToInt(b.t * 9f + b.fase) & 1];
                b.sr.transform.localPosition = ArtePixel.Snap(new Vector2(b.pos.x, y));
                if (Mathf.Abs(b.pos.x) > 15f)
                {
                    Destroy(b.sr.gameObject);
                    murcielagos.RemoveAt(i);
                }
            }
        }

        void ActualizarCamaraYLuces(float dt)
        {
            sacudida = Mathf.Max(0f, sacudida - dt * 1.2f);
            Vector2 off = sacudida > 0.01f ? UnityEngine.Random.insideUnitCircle * sacudida : Vector2.zero;
            Vector3 cpos = ArtePixel.Snap(off, -10f);
            camara.transform.position = cpos;

            float parpadeo = 0.94f + 0.06f * Mathf.PerlinNoise(tiempoReal * 6f, 0.3f);
            oscuridad.luces.Clear();
            bool enPartida = estado != Estado.Menu;

            if (enPartida)
            {
                oscuridad.luces.Add(new Oscuridad.Luz { pos = jugadorPos + new Vector2(0, 0.5f), radio = 2.1f, intensidad = 0.95f, calida = true });
                float fc = 0.9f + 0.1f * Mathf.PerlinNoise(tiempoReal * 4f, 1.7f);
                oscuridad.luces.Add(new Oscuridad.Luz { pos = CentroCubeta, radio = 2.7f * fc, intensidad = 0.9f, calida = true });
            }
            else
            {
                oscuridad.luces.Add(new Oscuridad.Luz { pos = ratonMundo, radio = 3.2f, intensidad = 0.95f, calida = true });
            }
            foreach (Calabaza c in calabazas)
            {
                float f = 0.85f + 0.15f * Mathf.PerlinNoise(tiempoReal * 7f, c.ruido);
                oscuridad.luces.Add(new Oscuridad.Luz { pos = c.pos + new Vector2(0, 0.35f), radio = 1.6f * f, intensidad = 0.85f, calida = true });
            }
            foreach (Espectro e in espectros)
                oscuridad.luces.Add(new Oscuridad.Luz { pos = e.Centro, radio = 1.3f, intensidad = 0.5f * e.aparicion, calida = false });

            oscuridad.conoActivo = (estado == Estado.Jugando || estado == Estado.Pausa) && linternaEncendida;
            oscuridad.conoOrigen = OrigenLinterna();
            oscuridad.conoDireccion = direccionLinterna;
            oscuridad.conoAlcance = alcanceLinterna;
            oscuridad.conoMedioAngulo = medioAnguloLinterna;
            oscuridad.parpadeo = parpadeo;
            oscuridad.ambiente = estado == Estado.Menu ? 0.5f : 0.6f;
            if (estado != Estado.Pausa) oscuridad.Actualizar(new Vector2(cpos.x, cpos.y));
        }

        void ActualizarCursor()
        {
            cursorSr.transform.localPosition = ArtePixel.VirtualALocal(Mathf.Round(ratonVirtual.x), Mathf.Round(ratonVirtual.y), 10f);
            if (Application.isFocused) Cursor.visible = false;
        }

        // ================================================================== HUD
        void CrearHUD()
        {
            SpriteRenderer franja = ArtePixel.Crear("FranjaHUD", ArtePixel.PixelEsquina, hudRaiz, O_HUD);
            franja.transform.localPosition = ArtePixel.VirtualALocal(0, 200, 0);
            franja.transform.localScale = new Vector3(ArtePixel.AnchoVirtual, 16, 1);
            franja.color = new Color32(10, 6, 18, 170);
            SpriteRenderer linea = ArtePixel.Crear("LineaHUD", ArtePixel.PixelEsquina, hudRaiz, O_HUD);
            linea.transform.localPosition = ArtePixel.VirtualALocal(0, 199, 0);
            linea.transform.localScale = new Vector3(ArtePixel.AnchoVirtual, 1, 1);
            linea.color = new Color32(242, 140, 40, 200);

            SpriteRenderer etVidas = ArtePixel.Crear("EtiquetaVidas", FuentePixel.Texto("VIDAS", Blanco, 1, true, true, 0f, 0.5f), hudRaiz, O_HUD + 1);
            etVidas.transform.localPosition = ArtePixel.VirtualALocal(4, 208, 0);
            for (int i = 0; i < 3; i++)
            {
                hudCorazones[i] = ArtePixel.Crear("Corazon", sprIconos[0], hudRaiz, O_HUD + 1);
                hudCorazones[i].transform.localPosition = ArtePixel.VirtualALocal(40 + i * 9, 208, 0);
            }
            SpriteRenderer etBolsa = ArtePixel.Crear("EtiquetaBolsa", FuentePixel.Texto("DULCES", Blanco, 1, true, true, 0f, 0.5f), hudRaiz, O_HUD + 1);
            etBolsa.transform.localPosition = ArtePixel.VirtualALocal(72, 208, 0);
            for (int i = 0; i < 3; i++)
            {
                hudDulces[i] = ArtePixel.Crear("Dulce", sprIconos[2], hudRaiz, O_HUD + 1);
                hudDulces[i].transform.localPosition = ArtePixel.VirtualALocal(114 + i * 9, 208, 0);
            }
            hudOleada = ArtePixel.Crear("Oleada", null, hudRaiz, O_HUD + 1);
            hudOleada.transform.localPosition = ArtePixel.VirtualALocal(198, 208, 0);
            hudPuntos = ArtePixel.Crear("Puntos", null, hudRaiz, O_HUD + 1);
            hudPuntos.transform.localPosition = ArtePixel.VirtualALocal(380, 208, 0);
            hudCombo = ArtePixel.Crear("Combo", null, hudRaiz, O_HUD + 1);
            hudCombo.transform.localPosition = ArtePixel.VirtualALocal(380, 190, 0);

            banner = ArtePixel.Crear("Banner", null, hudRaiz, O_HUD + 5);
            banner.transform.localPosition = ArtePixel.VirtualALocal(192, 150, 0);
            banner2 = ArtePixel.Crear("Banner2", null, hudRaiz, O_HUD + 5);
            banner2.transform.localPosition = ArtePixel.VirtualALocal(192, 128, 0);
            banner.gameObject.SetActive(false);
            banner2.gameObject.SetActive(false);
        }

        void MostrarHUD(bool visible)
        {
            hudRaiz.gameObject.SetActive(visible);
            if (!visible) tBanner = 0f;
        }

        void MostrarBanner(string texto, string sub)
        {
            banner.sprite = FuentePixel.Texto(texto, new Color32(255, 236, 140, 255), NaranjaOscuro, 2);
            banner2.sprite = string.IsNullOrEmpty(sub) ? null : FuentePixel.Texto(sub, Lila, 1);
            tBanner = 2.4f;
        }

        void ActualizarHUD(float dt)
        {
            if (!hudRaiz.gameObject.activeSelf) return;
            for (int i = 0; i < 3; i++)
            {
                hudCorazones[i].sprite = sprIconos[i < vidas ? 0 : 1];
                hudDulces[i].sprite = sprIconos[i < vidasBolsa ? 2 : 3];
                // el último corazón late cuando queda poca vida
                bool latido = (i == 0 && vidas == 1) && Mathf.FloorToInt(tiempoReal * 4f) % 2 == 0;
                hudCorazones[i].transform.localPosition = ArtePixel.VirtualALocal(40 + i * 9, 208 + (latido ? 1 : 0), 0);
                bool latidoD = (i == 0 && vidasBolsa == 1) && Mathf.FloorToInt(tiempoReal * 4f) % 2 == 0;
                hudDulces[i].transform.localPosition = ArtePixel.VirtualALocal(114 + i * 9, 208 + (latidoD ? 1 : 0), 0);
            }
            if (puntuacionMostrada != puntuacion)
            {
                puntuacionMostrada = puntuacion;
                hudPuntos.sprite = FuentePixel.Texto("PUNTOS " + puntuacion.ToString("000000"), Amarillo, Amarillo, 1, true, true, 1f, 0.5f);
            }
            if (oleadaMostrada != oleada)
            {
                oleadaMostrada = oleada;
                hudOleada.sprite = FuentePixel.Texto("OLEADA " + oleada, Lila, 1);
            }
            if (estado != Estado.Pausa) tCombo = Mathf.Max(0f, tCombo - dt);
            if (tCombo > 0f && combo > 1)
            {
                if (comboMostrado != combo)
                {
                    comboMostrado = combo;
                    hudCombo.sprite = FuentePixel.Texto("COMBO X" + combo, Naranja, Amarillo, 1, true, true, 1f, 0.5f);
                }
                hudCombo.enabled = tCombo > 0.6f || Mathf.FloorToInt(tCombo * 10f) % 2 == 0;
            }
            else
            {
                hudCombo.enabled = false;
                comboMostrado = -1;
            }

            if (tBanner > 0f)
            {
                if (estado != Estado.Pausa) tBanner -= dt;
                bool visible = tBanner > 0f && (tBanner < 2.1f || Mathf.FloorToInt(tBanner * 12f) % 2 == 0);
                banner.gameObject.SetActive(visible);
                banner2.gameObject.SetActive(visible && banner2.sprite != null);
            }
            else
            {
                banner.gameObject.SetActive(false);
                banner2.gameObject.SetActive(false);
            }
        }

        // ================================================================== UI (botones y paneles)
        SpriteRenderer CrearTextoUI(string texto, Color32 arriba, Color32 abajo, int escala, float px, float py, int orden)
        {
            SpriteRenderer sr = ArtePixel.Crear("Texto", FuentePixel.Texto(texto, arriba, abajo, escala), uiRaiz, orden);
            sr.transform.localPosition = ArtePixel.VirtualALocal(px, py, 0);
            objetosUI.Add(sr.gameObject);
            return sr;
        }

        SpriteRenderer CrearTextoUI(string texto, Color32 color, int escala, float px, float py, int orden)
        {
            return CrearTextoUI(texto, color, color, escala, px, py, orden);
        }

        void CrearVeloUI(float alfa)
        {
            SpriteRenderer v = ArtePixel.Crear("Velo", ArtePixel.PixelEsquina, uiRaiz, O_PANEL);
            v.transform.localPosition = ArtePixel.VirtualALocal(0, 0, 0);
            v.transform.localScale = new Vector3(ArtePixel.AnchoVirtual, ArtePixel.AltoVirtual, 1);
            v.color = new Color(0.02f, 0.01f, 0.06f, alfa);
            objetosUI.Add(v.gameObject);
        }

        void CrearPanelUI(int w, int h, float cx, float cy)
        {
            Sprite s = ArtePixel.Caja(w, h, new Color32(34, 20, 52, 245), new Color32(20, 12, 28, 255),
                                      new Color32(90, 60, 130, 255), new Color32(22, 12, 36, 255));
            SpriteRenderer p = ArtePixel.Crear("Panel", s, uiRaiz, O_PANEL + 1);
            p.transform.localPosition = ArtePixel.VirtualALocal(cx, cy, 0);
            objetosUI.Add(p.gameObject);
            // esquinas naranjas decorativas
            for (int i = 0; i < 4; i++)
            {
                SpriteRenderer d = ArtePixel.Crear("Esquina", ArtePixel.PixelEsquina, uiRaiz, O_PANEL + 2);
                float x = cx + ((i & 1) == 0 ? -w / 2 + 3 : w / 2 - 6);
                float y = cy + ((i & 2) == 0 ? -h / 2 + 3 : h / 2 - 6);
                d.transform.localPosition = ArtePixel.VirtualALocal(Mathf.Round(x), Mathf.Round(y), 0);
                d.transform.localScale = new Vector3(3, 3, 1);
                d.color = Naranja;
                objetosUI.Add(d.gameObject);
            }
        }

        void CrearBoton(string etiqueta, float cx, float cy, int w, int h, int escalaTexto, Action accion)
        {
            Boton b = new Boton
            {
                rect = new Rect(cx - w / 2f, cy - h / 2f, w, h),
                accion = accion,
                normal = ArtePixel.Caja(w, h, new Color32(232, 112, 26, 255), new Color32(20, 12, 28, 255),
                                        new Color32(255, 169, 77, 255), new Color32(168, 68, 10, 255)),
                encima = ArtePixel.Caja(w, h, new Color32(255, 169, 77, 255), new Color32(20, 12, 28, 255),
                                        new Color32(255, 230, 150, 255), new Color32(222, 110, 30, 255))
            };
            b.fondo = ArtePixel.Crear("Boton_" + etiqueta, b.normal, uiRaiz, O_PANEL + 3);
            b.fondo.transform.localPosition = ArtePixel.VirtualALocal(cx, cy, 0);
            b.texto = ArtePixel.Crear("TextoBoton", FuentePixel.Texto(etiqueta, Blanco, new Color32(255, 220, 180, 255), escalaTexto), uiRaiz, O_PANEL + 4);
            b.posTexto = ArtePixel.VirtualALocal(cx, cy, 0);
            b.texto.transform.localPosition = b.posTexto;
            botones.Add(b);
            objetosUI.Add(b.fondo.gameObject);
            objetosUI.Add(b.texto.gameObject);
        }

        void ActualizarBotones()
        {
            Mouse m = Mouse.current;
            bool click = m != null && m.leftButton.wasPressedThisFrame;
            Boton pulsado = null;
            foreach (Boton b in botones)
            {
                bool sobre = b.rect.Contains(ratonVirtual);
                if (sobre && !b.sobre) audioJuego.Efecto("hover", 0.6f, 0f);
                b.sobre = sobre;
                b.fondo.sprite = sobre ? b.encima : b.normal;
                int sube = sobre ? 1 : 0;
                b.fondo.transform.localPosition = ArtePixel.VirtualALocal(b.rect.center.x, b.rect.center.y + sube, 0);
                b.texto.transform.localPosition = b.posTexto + new Vector3(0, sube / (float)ArtePixel.PPU, 0);
                if (sobre && click) pulsado = b;
            }
            if (nuevoRecordSr != null) nuevoRecordSr.enabled = Mathf.FloorToInt(tiempoReal * 3f) % 2 == 0;
            if (pulsado != null)
            {
                audioJuego.Efecto("boton", 0.9f, 0f);
                pulsado.accion?.Invoke();
            }
        }

        void LimpiarUI()
        {
            foreach (GameObject g in objetosUI) if (g != null) Destroy(g);
            objetosUI.Clear();
            botones.Clear();
            tituloSr = null;
            titulo2Sr = null;
            nuevoRecordSr = null;
        }
    }
}
