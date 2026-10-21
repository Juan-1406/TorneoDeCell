using UnityEngine;

[System.Serializable]
public class FaseJefe
{
    public string nombre = "Fase";
    [Range(0f, 1f)] public float activaBajoVida = 1f;
    public BossAttack[] ataques;
    public float pausaEntreAtaques = 1.5f;
    public Color color = Color.white;

    [Header("Movimiento")]
    [Min(0f)] public float amplitudHorizontal;
    [Min(0f)] public float amplitudVertical;
    [Min(0f)] public float ciclosPorSegundo;
}
