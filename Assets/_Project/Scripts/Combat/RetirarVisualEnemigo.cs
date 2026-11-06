using UnityEngine;

// No contiene colliders, salud ni lógica de oleadas. Solo termina el gesto de caída.
public class RetirarVisualEnemigo : MonoBehaviour
{
    private SpriteRenderer[] imagenes;
    private float edad;
    public void Preparar()
    {
        imagenes = GetComponentsInChildren<SpriteRenderer>();
        foreach (var imagen in imagenes) imagen.color = Color.white;
        foreach (var animator in GetComponentsInChildren<Animator>())
        { animator.Play("Muerte", 0, 0f); animator.Update(0f); }
    }
    private void Update()
    {
        if (SesionJuego.EnPausa || Time.timeScale == 0f) return;
        edad += Time.deltaTime;
        foreach (var imagen in imagenes)
            imagen.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((edad - 0.22f) / 0.16f));
        if (edad >= 0.38f) Destroy(gameObject);
    }
}
