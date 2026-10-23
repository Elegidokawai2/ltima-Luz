using UnityEngine;

public class RayoLuz : MonoBehaviour
{
    public float velocidad = 12f;
    public float tiempoVida = 2f;
    public GameObject explosionPrefab;
    public AudioClip sonidoExplosion;

    private void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * velocidad;
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Fantasma>() == null) return;

        GestorJuego gestor = FindFirstObjectByType<GestorJuego>();
        if (gestor != null) gestor.SumarPuntos(100);

        if (sonidoExplosion != null)
        {
            AudioSource.PlayClipAtPoint(sonidoExplosion, collision.transform.position);
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, collision.transform.position, Quaternion.identity);
        }

        Destroy(collision.gameObject);
        Destroy(gameObject);
    }
}
