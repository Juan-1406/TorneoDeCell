using UnityEngine;

[DisallowMultipleComponent]
public sealed class MateoSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float runFramesPerSecond = 8f;
    [SerializeField, Min(0f)] private float moveThreshold = 0.1f;
    [SerializeField, Min(0f)] private float dashThreshold = 12f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;

        float horizontalSpeed = body != null ? body.linearVelocity.x : 0f;
        float verticalSpeed = body != null ? body.linearVelocity.y : 0f;
        int frameIndex = 0;

        if (frames.Length >= 4 && Mathf.Abs(horizontalSpeed) >= dashThreshold)
            frameIndex = 3;
        else if (frames.Length >= 4 && verticalSpeed > 0.2f)
            frameIndex = 3;
        else if (frames.Length >= 3 && Mathf.Abs(horizontalSpeed) > moveThreshold)
            frameIndex = 1 + Mathf.FloorToInt(Time.time * runFramesPerSecond) % 2;

        spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
        if (Mathf.Abs(horizontalSpeed) > moveThreshold)
            spriteRenderer.flipX = horizontalSpeed < 0f;
    }
}
