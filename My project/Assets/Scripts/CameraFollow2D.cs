using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(1.5f, 0.7f, -10f);
    public float smoothSpeed = 8f;
    public float minY = 0.2f;

    [Header("Enquadramento Vertical (mantém o chão visível em saltos e plataformas)")]
    public float baseGroundPlayerY = -1.5f;
    [Range(0.15f, 0.7f)]
    public float verticalFollowFactor = 0.35f;
    public float verticalScreenMargin = 1.8f;

    [Header("Game Feel - Trauma Screen Shake")]
    public float traumaDecay = 1.6f;
    public Vector2 maxShakeOffset = new Vector2(0.38f, 0.26f);

    private float trauma;
    private float shakeTime;
    private Vector3 basePosition;
    private bool hasBasePosition;
    private Camera cachedCam;

    private void Awake()
    {
        cachedCam = GetComponent<Camera>();
    }

    public static void TriggerShake(float amount)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow != null)
        {
            follow.AddTrauma(amount);
        }
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        basePosition = ComputeDesiredPosition();
        hasBasePosition = true;
        transform.position = basePosition;
    }

    private Vector3 ComputeDesiredPosition()
    {
        float x = target.position.x + offset.x;
        // Acompanha apenas uma fração suave da subida vertical para manter o chão/inimigos abaixo visíveis.
        float gentleY = offset.y + (target.position.y - baseGroundPlayerY) * verticalFollowFactor;
        float desiredY = Mathf.Max(minY, gentleY);

        if (cachedCam == null) cachedCam = GetComponent<Camera>();
        float halfH = (cachedCam != null && cachedCam.orthographic) ? cachedCam.orthographicSize : 4.8f;
        float minSafeY = target.position.y - Mathf.Max(1.0f, halfH - verticalScreenMargin);
        float maxSafeY = target.position.y + Mathf.Max(1.0f, halfH - verticalScreenMargin);
        desiredY = Mathf.Clamp(desiredY, minSafeY, maxSafeY);

        return new Vector3(x, desiredY, offset.z);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (!hasBasePosition)
        {
            basePosition = transform.position;
            hasBasePosition = true;
        }

        Vector3 desiredPos = ComputeDesiredPosition();
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        basePosition = Vector3.Lerp(basePosition, desiredPos, t);

        Vector3 shakeOffset = Vector3.zero;
        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            float shake = trauma * trauma;
            shakeTime += Time.unscaledDeltaTime * 32f;
            shakeOffset.x = maxShakeOffset.x * shake * Mathf.Sin(shakeTime * 1.7f);
            shakeOffset.y = maxShakeOffset.y * shake * Mathf.Sin(shakeTime * 2.3f);
        }

        transform.position = basePosition + shakeOffset;
    }
}
