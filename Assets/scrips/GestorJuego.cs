using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GestorJuego : MonoBehaviour
{
    public int puntuacion;
    public int vidas = 3;
    public TMP_Text textoPuntuacion;
    public TMP_Text textoVidas;
    public GameObject gameOverPanel;
    public TMP_Text textoPuntuacionFinal;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        ActualizarUI();
    }

    public void SumarPuntos(int puntos)
    {
        puntuacion += puntos;
        ActualizarUI();
    }

    public void PerderVida()
    {
        vidas--;
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoPuntuacion != null) textoPuntuacion.text = "PUNTOS: " + puntuacion;
        if (textoVidas != null) textoVidas.text = "VIDAS: " + vidas;
    }

    public void GameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (textoPuntuacionFinal != null) textoPuntuacionFinal.text = "PUNTUACIÓN: " + puntuacion;
        Time.timeScale = 0f;
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Juego");
    }

    public void IrMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
