using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(1.5f, 1.2f, -10f);
    public float smoothSpeed = 8f;
    public float minY = 0f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        desiredPos.y = Mathf.Max(minY, desiredPos.y * 0.4f + offset.y);
        desiredPos.z = offset.z;

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
