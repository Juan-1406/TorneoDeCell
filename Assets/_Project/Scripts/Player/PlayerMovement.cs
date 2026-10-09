using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 6f;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float radioSuelo = 0.2f;
    [SerializeField] private LayerMask capaSuelo;

    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool enSuelo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && enSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }
    }

    private void FixedUpdate()
    {
        enSuelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
        rb.linearVelocity = new Vector2(inputHorizontal * velocidad, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, radioSuelo);
    }
}