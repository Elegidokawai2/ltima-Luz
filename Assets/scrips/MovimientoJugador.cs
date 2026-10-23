using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class MovimientoJugador : MonoBehaviour
{
    public GameObject linterna;
    public float velocidadMaxima = 8f;
    public GameObject explosionPrefab;
    public float tiempoInvulnerable = 3f;
    public float velocidadParpadeo = 0.15f;

    private Rigidbody2D rb;
    private bool gameOver;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;
    private bool invulnerable;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (linterna == null)
        {
            Transform hijaLinterna = transform.Find("Linterna");
            if (hijaLinterna != null) linterna = hijaLinterna.gameObject;
        }

        if (linterna != null) linterna.SetActive(false);
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool mantenerLinterna = (teclado != null && teclado.spaceKey.isPressed) ||
                                (mouse != null && mouse.leftButton.isPressed);

        if (linterna != null) linterna.SetActive(!gameOver && mantenerLinterna);
        if (!gameOver && mantenerLinterna) ApuntarLinterna(mouse);

        if (gameOver && teclado != null && teclado.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void FixedUpdate()
    {
        Keyboard teclado = Keyboard.current;
        Vector2 direccion = Vector2.zero;

        if (!gameOver && teclado != null)
        {
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) direccion.x -= 1f;
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) direccion.x += 1f;
            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) direccion.y -= 1f;
            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) direccion.y += 1f;
        }

        rb.linearVelocity = direccion.normalized * velocidadMaxima;
    }

    private void ApuntarLinterna(Mouse mouse)
    {
        Camera camara = Camera.main;
        if (linterna == null || mouse == null || camara == null) return;

        Vector2 posicionMouse = mouse.position.ReadValue();
        Vector3 puntoMouse = camara.ScreenToWorldPoint(
            new Vector3(posicionMouse.x, posicionMouse.y, -camara.transform.position.z));
        Vector2 direccion = (Vector2)puntoMouse - (Vector2)transform.position;
        if (direccion.sqrMagnitude <= 0.001f) return;

        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg - 90f;
        linterna.transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<Fantasma>() == null || invulnerable) return;

        GestorJuego gestor = FindFirstObjectByType<GestorJuego>();
        if (gestor == null) return;
        gestor.PerderVida();

        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(collision.gameObject);

        if (gestor.vidas <= 0)
        {
            gestor.GameOver();
            gameOver = true;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (playerCollider != null) playerCollider.enabled = false;
        }
        else
        {
            StartCoroutine(Reaparecer());
        }
    }

    private IEnumerator Reaparecer()
    {
        invulnerable = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (playerCollider != null) playerCollider.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        yield return new WaitForSeconds(1f);
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        yield return StartCoroutine(Parpadear());
        if (playerCollider != null) playerCollider.enabled = true;
        invulnerable = false;
    }

    private IEnumerator Parpadear()
    {
        float tiempo = 0f;
        while (tiempo < tiempoInvulnerable)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(velocidadParpadeo);
            tiempo += velocidadParpadeo;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }
}
