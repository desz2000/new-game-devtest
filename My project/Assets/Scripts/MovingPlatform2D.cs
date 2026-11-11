using UnityEngine;

/// <summary>
/// Plataforma móvel cinemática 2D (símbolo '~' no mapa ASCII).
/// Move-se suavemente entre dois pontos em FixedUpdate e expõe CurrentVelocity
/// para que o BoxPlayer2D herde sua velocidade sem jitter.
/// </summary>
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [Header("Trajetória da Plataforma Móvel")]
    public Vector2 travelOffset = new Vector2(2.6f, 0f);
    public float cycleSpeed = 1.35f;
    public float phaseOffset = 0f;

    private Rigidbody2D rb;
    private Vector2 startPos;
    private bool initialized;
    private Vector2 currentVelocity;

    public Vector2 CurrentVelocity => currentVelocity;

    private void Awake()
    {
        EnsurePhysicsSetup();
        if (Application.isPlaying)
        {
            startPos = transform.position;
            initialized = true;
        }
    }

    public void Configure(Vector2 origin, Vector2 offset, float speed = 1.35f, float phase = 0f)
    {
        EnsurePhysicsSetup();
        startPos = origin;
        travelOffset = offset;
        cycleSpeed = speed;
        phaseOffset = phase;
        initialized = true;
        transform.position = new Vector3(origin.x, origin.y, 0f);
        currentVelocity = Vector2.zero;
    }

    private void EnsurePhysicsSetup()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            col.usedByEffector = true;
        }

        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
        if (effector == null)
        {
            effector = gameObject.AddComponent<PlatformEffector2D>();
        }
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
    }

    public void ResetPlatform()
    {
        if (!initialized) return;
        currentVelocity = Vector2.zero;
        if (rb != null) rb.position = startPos;
        transform.position = new Vector3(startPos.x, startPos.y, 0f);
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying) return;
        if (!initialized)
        {
            startPos = rb != null ? rb.position : (Vector2)transform.position;
            initialized = true;
        }

        float dt = Time.fixedDeltaTime;
        if (dt <= 0.0001f) return;

        // Oscilação senoidal suave entre startPos - travelOffset*0.5 e startPos + travelOffset*0.5
        float angle = Time.time * cycleSpeed + phaseOffset;
        float s = Mathf.Sin(angle) * 0.5f;
        Vector2 targetPos = startPos + travelOffset * s;

        Vector2 prevPos = rb != null ? rb.position : (Vector2)transform.position;
        currentVelocity = (targetPos - prevPos) / dt;

        if (rb != null)
        {
            rb.MovePosition(targetPos);
        }
        else
        {
            transform.position = new Vector3(targetPos.x, targetPos.y, 0f);
        }
    }
}
