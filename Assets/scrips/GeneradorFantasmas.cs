using UnityEngine;

public class GeneradorFantasmas : MonoBehaviour
{
    public GameObject fantasmaPrefab;
    public float intervalo = 2f;
    public float limiteX = 9f;
    public float limiteY = 5f;
    public float intervaloMinimo = 0.1f;
    public float reduccionIntervalo = 0.1f;

    private float siguienteFantasma;

    private void Start()
    {
        siguienteFantasma = Time.time + 1f;
    }

    private void Update()
    {
        if (Time.time < siguienteFantasma) return;

        CrearFantasma();
        siguienteFantasma = Time.time + intervalo;
        if (intervalo > intervaloMinimo) intervalo -= reduccionIntervalo;
    }

    private void CrearFantasma()
    {
        if (fantasmaPrefab == null)
        {
            Debug.LogError("Asigna el prefab de fantasma en GeneradorFantasmas.", this);
            enabled = false;
            return;
        }

        Vector2 posicion;
        switch (Random.Range(0, 4))
        {
            case 0:
                posicion = new Vector2(-limiteX, Random.Range(-limiteY, limiteY));
                break;
            case 1:
                posicion = new Vector2(limiteX, Random.Range(-limiteY, limiteY));
                break;
            case 2:
                posicion = new Vector2(Random.Range(-limiteX, limiteX), limiteY);
                break;
            default:
                posicion = new Vector2(Random.Range(-limiteX, limiteX), -limiteY);
                break;
        }

        GameObject fantasma = Instantiate(fantasmaPrefab, posicion, Quaternion.identity);
        fantasma.name = "Fantasma";
    }
}
