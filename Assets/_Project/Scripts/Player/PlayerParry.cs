using System;
using System.Collections;
using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [SerializeField] private GameObject escudo;
    [SerializeField] private float ventanaParry = 0.2f;
    [SerializeField] private float recargaParry = 0.6f;
    [SerializeField, Min(0f)] private float ventanaPerfecta = 0.1f;
    public event Action ParryPerfecto;
    private float inicioParry;
    private bool premioNotificado;

    private bool parryActivo;
    private float siguienteParry;

    private void Awake()
    {
        if (escudo != null) escudo.SetActive(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        parryActivo = false;
        if (escudo != null) escudo.SetActive(false);
    }

    private void Update()
    {
        if (SesionJuego.EnPausa) return;
        if (Input.GetButtonDown("Fire2") && Time.time >= siguienteParry)
        {
            StartCoroutine(Parry());
        }
    }

    private IEnumerator Parry()
    {
        parryActivo = true;
        inicioParry = Time.time;
        premioNotificado = false;
        siguienteParry = Time.time + recargaParry;
        if (escudo != null) escudo.SetActive(true);

        yield return new WaitForSeconds(ventanaParry);

        if (escudo != null) escudo.SetActive(false);
        parryActivo = false;
    }

    public bool IntentarParry(Bullet bala)
    {
        if (!isActiveAndEnabled || !parryActivo || SesionJuego.EnPausa || Time.timeScale == 0f ||
            bala == null || !bala.EsReflejable) return false;

        Vector2 direccion = Vector2.zero;
        if (bala.EsProyectilRenato && Camera.main != null)
            direccion = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)bala.transform.position;
        bala.Reflejar(direccion);
        if (!premioNotificado && Time.time - inicioParry <= Mathf.Min(ventanaPerfecta, ventanaParry))
        {
            premioNotificado = true;
            ParryPerfecto?.Invoke();
        }
        return true;
    }
}
