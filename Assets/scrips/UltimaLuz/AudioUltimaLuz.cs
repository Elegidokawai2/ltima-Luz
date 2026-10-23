using System.Collections.Generic;
using UnityEngine;

namespace UltimaLuz
{
    /// <summary>Música, efectos y el zumbido de carga de la linterna.</summary>
    public class AudioUltimaLuz
    {
        readonly AudioSource musica;
        readonly AudioSource carga;
        readonly AudioSource[] efectos;
        int siguiente;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        string musicaActual;

        public float volumenMusica = 0.45f;
        public float volumenEfectos = 0.8f;

        public AudioUltimaLuz(GameObject duenio)
        {
            musica = duenio.AddComponent<AudioSource>();
            musica.loop = true;
            musica.playOnAwake = false;
            musica.volume = volumenMusica;

            carga = duenio.AddComponent<AudioSource>();
            carga.loop = true;
            carga.playOnAwake = false;
            carga.clip = Clip("carga");
            carga.volume = 0f;

            efectos = new AudioSource[10];
            for (int i = 0; i < efectos.Length; i++)
            {
                efectos[i] = duenio.AddComponent<AudioSource>();
                efectos[i].playOnAwake = false;
            }
        }

        AudioClip Clip(string nombre)
        {
            if (clips.TryGetValue(nombre, out AudioClip c)) return c;
            c = Resources.Load<AudioClip>("UltimaLuz/Audio/" + nombre);
            if (c == null) Debug.LogWarning("[Última Luz] Falta el sonido Resources/UltimaLuz/Audio/" + nombre + ".wav");
            clips[nombre] = c;
            return c;
        }

        public void Efecto(string nombre, float volumen = 1f, float variacionTono = 0.05f)
        {
            AudioClip c = Clip(nombre);
            if (c == null) return;
            AudioSource s = efectos[siguiente];
            siguiente = (siguiente + 1) % efectos.Length;
            s.pitch = 1f + Random.Range(-variacionTono, variacionTono);
            s.PlayOneShot(c, volumen * volumenEfectos);
        }

        public void Musica(string nombre)
        {
            if (musicaActual == nombre && musica.isPlaying) return;
            musicaActual = nombre;
            AudioClip c = Clip(nombre);
            musica.Stop();
            if (c == null) return;
            musica.clip = c;
            musica.volume = volumenMusica;
            musica.pitch = 1f;
            musica.Play();
        }

        public void DetenerMusica()
        {
            musica.Stop();
            musicaActual = null;
        }

        public void TonoMusica(float tono) { musica.pitch = tono; }

        public void PausarMusica(bool pausa)
        {
            if (pausa) musica.Pause();
            else musica.UnPause();
        }

        /// <summary>progreso 0..1 ; activo = hay un fantasma cargándose.</summary>
        public void Carga(bool activo, float progreso)
        {
            if (carga.clip == null) return;
            if (activo)
            {
                if (!carga.isPlaying) carga.Play();
                carga.volume = Mathf.MoveTowards(carga.volume, 0.35f * volumenEfectos, Time.unscaledDeltaTime * 4f);
                carga.pitch = 0.75f + progreso * 0.9f;
            }
            else
            {
                carga.volume = Mathf.MoveTowards(carga.volume, 0f, Time.unscaledDeltaTime * 6f);
                if (carga.volume <= 0.001f && carga.isPlaying) carga.Stop();
            }
        }
    }
}
