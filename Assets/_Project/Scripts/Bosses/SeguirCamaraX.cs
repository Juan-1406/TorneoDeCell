using UnityEngine;

public class SeguirCamaraX : MonoBehaviour
{
    [SerializeField] private float desfaseX = 5f;
    [SerializeField] private float suavidad = 2f;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        float objetivoX = cam.transform.position.x + desfaseX;
        float nuevaX = Mathf.Lerp(transform.position.x, objetivoX, suavidad * Time.deltaTime);
        transform.position = new Vector3(nuevaX, transform.position.y, transform.position.z);
    }
}