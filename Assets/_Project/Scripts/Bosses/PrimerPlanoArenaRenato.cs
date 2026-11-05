using UnityEngine;

// Reutiliza los mismos píxeles del fondo: solo cambia su orden de dibujo.
// La cancha tapa la base del busto, mientras jugador y proyectiles permanecen delante.
[DisallowMultipleComponent]
public class PrimerPlanoArenaRenato : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fondo;
    [SerializeField] private float bordeY = -2.376f;
    private Sprite recorte;
    public SpriteRenderer Franja { get; private set; }
    public float BordeY => Franja != null ? Franja.bounds.max.y : bordeY;

    private void Awake()
    {
        if (fondo == null) fondo = GetComponent<SpriteRenderer>();
        if (fondo == null || fondo.sprite == null) return;
        Sprite original = fondo.sprite;
        float localY = fondo.transform.InverseTransformPoint(new Vector3(fondo.transform.position.x, bordeY, fondo.transform.position.z)).y;
        int alto = Mathf.Clamp(Mathf.RoundToInt((localY - original.bounds.min.y) * original.pixelsPerUnit), 1, (int)original.rect.height);
        Rect rect = original.rect; rect.height = alto;
        recorte = Sprite.Create(original.texture, rect, new Vector2(0.5f, 0f), original.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        recorte.name = "Cancha original - primer plano";
        var go = new GameObject("Cancha delante de Renato", typeof(SpriteRenderer));
        go.transform.SetParent(fondo.transform, false);
        go.transform.localPosition = new Vector3(original.bounds.center.x, original.bounds.min.y, 0f);
        Franja = go.GetComponent<SpriteRenderer>();
        Franja.sprite = recorte;
        Franja.sharedMaterials = fondo.sharedMaterials;
        Franja.color = fondo.color;
        Franja.sortingLayerID = fondo.sortingLayerID;
        Franja.sortingOrder = 16;
    }
    private void OnDestroy()
    {
        if (Franja != null) Destroy(Franja.gameObject);
        if (recorte != null) Destroy(recorte);
    }
}
