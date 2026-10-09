using System.Collections;
using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [SerializeField] private GameObject escudo;
    [SerializeField] private float ventanaParry = 0.2f;
    [SerializeField] private float recargaParry = 0.6f;

    private bool parryActivo;
    private float siguienteParry;

    private void Awake()
    {
        escudo.SetActive(false);
    }

    private void OnDisable()
    {
        parryActivo = false;
        if (escudo != null) escudo.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire2") && Time.time >= siguienteParry)
        {
            StartCoroutine(Parry());
        }
    }

    private IEnumerator Parry()
    {
        parryActivo = true;
        siguienteParry = Time.time + recargaParry;
        escudo.SetActive(true);

        yield return new WaitForSeconds(ventanaParry);

        escudo.SetActive(false);
        parryActivo = false;
    }

    public bool IntentarParry(Bullet bala)
    {
        if (!parryActivo) return false;

        bala.Reflejar();
        return true;
    }
}