using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class ShakeAlRecibirDanio : MonoBehaviour
{
    [SerializeField] private Health vida;
    [SerializeField] private float fuerza = 0.5f;

    private CinemachineImpulseSource fuente;

    private void Awake()
    {
        fuente = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        vida.OnDanioRecibido += Sacudir;
    }

    private void OnDisable()
    {
        if (vida != null) vida.OnDanioRecibido -= Sacudir;
    }

    private void Sacudir()
    {
        fuente.GenerateImpulse(fuerza);
    }
}