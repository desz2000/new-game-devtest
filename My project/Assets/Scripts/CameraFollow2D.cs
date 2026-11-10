using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(1.5f, 1.2f, -10f);
    public float smoothSpeed = 8f;
    public float minY = 0f;

    [Header("Game Feel - Trauma Screen Shake")]
    public float traumaDecay = 1.6f;
    public Vector2 maxShakeOffset = new Vector2(0.38f, 0.26f);

    private float trauma;
    private float shakeTime;
    private Vector3 basePosition;
    private bool hasBasePosition;

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
        Vector3 desiredPos = target.position + offset;
        desiredPos.y = Mathf.Max(minY, desiredPos.y * 0.4f + offset.y);
        desiredPos.z = offset.z;
        basePosition = desiredPos;
        hasBasePosition = true;
        transform.position = basePosition;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (!hasBasePosition)
        {
            basePosition = transform.position;
            hasBasePosition = true;
        }

        Vector3 desiredPos = target.position + offset;
        desiredPos.y = Mathf.Max(minY, desiredPos.y * 0.4f + offset.y);
        desiredPos.z = offset.z;

        basePosition = Vector3.Lerp(basePosition, desiredPos, smoothSpeed * Time.deltaTime);

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
