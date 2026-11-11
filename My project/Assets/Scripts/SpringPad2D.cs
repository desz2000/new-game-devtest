using UnityEngine;

/// <summary>
/// Mola de impulso em Pixel Art 32x32 (símbolo 'M' no mapa ASCII).
/// Lança o jogador a ~3,5 unidades de altura (v ~= 16.6 u/s com gravityScale = 4),
/// permitindo alcançar rotas altas e segredos.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class SpringPad2D : MonoBehaviour
{
    [Header("Mola de Impulso (~3.5u de altura)")]
    public float bounceVelocity = 16.6f;
    public float activeVisualDuration = 0.28f;
    public float retriggerCooldown = 0.12f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private float activeTimer;
    private float cooldownTimer;

    private static Sprite idleSprite;
    private static Sprite extendedSprite;

    // Mola comprimida em repouso (alinhada à base por AlignGridToBottom)
    private static readonly string[] SpringIdleGrid =
    {
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "......OOOOOOOOOOOOOOOOOOOO......",
        ".....OYYYYYYYYhhhhhhhhYYYYO.....",
        ".....ORRRRRRRRrrrrrrrrRRRRO.....",
        "......OOOOOOOOOOOOOOOOOOOO......",
        ".........OSSO......OSSO.........",
        "........OSSSSOOOOOOSSSSO........",
        ".........OSSO......OSSO.........",
        "........OSSSSOOOOOOSSSSO........",
        ".....OOOOOOOOOOOOOOOOOOOOOO.....",
        "....OSSSSSSSSSssssSSSSSSSSSO....",
        "....OSSSSSSSSSssssSSSSSSSSSO....",
        "....OOOOOOOOOOOOOOOOOOOOOOOO...."
    };

    // Mola distendida ao impulsionar o jogador
    private static readonly string[] SpringExtendedGrid =
    {
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "......OOOOOOOOOOOOOOOOOOOO......",
        ".....OYYYYYYYYhhhhhhhhYYYYO.....",
        ".....ORRRRRRRRrrrrrrrrRRRRO.....",
        "......OOOOOOOOOOOOOOOOOOOO......",
        "..........OSSO....OSSO..........",
        ".........OSSO......OSSO.........",
        "........OSSSSOOOOOOSSSSO........",
        ".........OSSO......OSSO.........",
        "..........OSSO....OSSO..........",
        ".........OSSO......OSSO.........",
        "........OSSSSOOOOOOSSSSO........",
        ".........OSSO......OSSO.........",
        "..........OSSO....OSSO..........",
        ".........OSSO......OSSO.........",
        "........OSSSSOOOOOOSSSSO........",
        ".........OSSO......OSSO.........",
        ".....OOOOOOOOOOOOOOOOOOOOOO.....",
        "....OSSSSSSSSSssssSSSSSSSSSO....",
        "....OSSSSSSSSSssssSSSSSSSSSO....",
        "....OOOOOOOOOOOOOOOOOOOOOOOO...."
    };

    public bool CanTrigger => cooldownTimer <= 0f;

    private void Awake()
    {
        EnsureVisuals();
    }

    private void OnEnable()
    {
        EnsureVisuals();
    }

    public void EnsureVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(0.85f, 0.55f);
            boxCollider.offset = new Vector2(0f, -0.20f);
        }

        if (idleSprite == null)
        {
            idleSprite = PixelArtBuilder.CreateSpriteFromGrid("SpringPad32_Idle", SpringIdleGrid, MapSpringColor, 32f, 32, 32, alignBottom: true);
        }
        if (extendedSprite == null)
        {
            extendedSprite = PixelArtBuilder.CreateSpriteFromGrid("SpringPad32_Ext", SpringExtendedGrid, MapSpringColor, 32f, 32, 32, alignBottom: true);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 3;
            spriteRenderer.sprite = (activeTimer > 0f) ? extendedSprite : idleSprite;
            spriteRenderer.color = Color.white;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (activeTimer > 0f)
        {
            activeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(activeTimer / activeVisualDuration);
            float stretchY = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
            float squashX = 1f - Mathf.Sin(t * Mathf.PI) * 0.12f;
            transform.localScale = new Vector3(squashX, stretchY, 1f);

            if (activeTimer <= 0f)
            {
                transform.localScale = Vector3.one;
                if (spriteRenderer != null) spriteRenderer.sprite = idleSprite;
            }
        }
    }

    public void TriggerSpring()
    {
        activeTimer = activeVisualDuration;
        cooldownTimer = retriggerCooldown;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = extendedSprite;
        }
        LaserProjectile2D.SpawnImpactSpark(transform.position + new Vector3(0f, -0.1f, 0f), 0.12f, new Color(1.00f, 0.88f, 0.25f, 1f));
    }

    private static Color MapSpringColor(char c)
    {
        switch (c)
        {
            case 'O': return new Color(0.08f, 0.09f, 0.14f, 1f);
            case 'Y': return new Color(1.00f, 0.84f, 0.18f, 1f);
            case 'h': return new Color(1.00f, 0.96f, 0.65f, 1f);
            case 'R': return new Color(0.95f, 0.24f, 0.32f, 1f);
            case 'r': return new Color(1.00f, 0.56f, 0.62f, 1f);
            case 'S': return new Color(0.52f, 0.58f, 0.72f, 1f);
            case 's': return new Color(0.76f, 0.82f, 0.94f, 1f);
            default: return Color.clear;
        }
    }
}
