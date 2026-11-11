using UnityEngine;

/// <summary>
/// Marcador e visual 32x32 de Checkpoint da fase.
/// Salva o ponto de respawn do jogador ao ser ativado por trigger.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Checkpoint2D : MonoBehaviour
{
    [Header("Configuração de Checkpoint")]
    public int checkpointIndex = 0;
    public Vector3 respawnOffset = new Vector3(0f, 0.55f, 0f);
    public bool isActivated = false;

    private SpriteRenderer spriteRenderer;
    private float pulseTimer;

    private static Sprite inactiveSprite;
    private static Sprite activeSprite;

    private static readonly string[] CheckpointGrid =
    {
        "................................",
        "..............OOOO..............",
        ".............OCCCCO.............",
        "............OCccccCO............",
        "............OCccccCO............",
        ".............OCCCCO.............",
        "..............OOOO..............",
        "..............OSSO..............",
        ".............OSSSSO.............",
        "............OSSssSSO............",
        "............OSSSSSSO............",
        ".............OSSSSO.............",
        "..............OSSO..............",
        "..............OSSO..............",
        "..............OSSO..............",
        "..............OSSO..............",
        "..............OSSO..............",
        "..............OSSO..............",
        "..............OSSO..............",
        ".............OSSSSO.............",
        "............OSSssSSO............",
        "...........OSSSSSSSSO...........",
        "...........OOOOOOOOOO...........",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    public Vector3 GetRespawnWorldPosition()
    {
        return new Vector3(transform.position.x + respawnOffset.x, LevelBuilder2D.GroundTopY + respawnOffset.y, 0f);
    }

    private void Awake()
    {
        EnsureVisuals();
    }

    private void OnEnable()
    {
        EnsureVisuals();
    }

    public void Activate()
    {
        if (isActivated) return;
        isActivated = true;
        UpdateVisualState();
    }

    public void ResetCheckpoint()
    {
        isActivated = false;
        UpdateVisualState();
    }

    private void Update()
    {
        if (!Application.isPlaying || !isActivated || spriteRenderer == null) return;

        pulseTimer += Time.deltaTime * 4.5f;
        float glow = 0.92f + Mathf.Sin(pulseTimer) * 0.08f;
        spriteRenderer.color = new Color(glow, 1f, glow, 1f);
    }

    public void EnsureVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector2(0.95f, 1.8f);
        }

        if (inactiveSprite == null)
        {
            inactiveSprite = PixelArtBuilder.CreateSpriteFromGrid("Checkpoint_Inactive", CheckpointGrid, c => MapColor(c, false), 32f, 32, 32, alignBottom: true);
        }
        if (activeSprite == null)
        {
            activeSprite = PixelArtBuilder.CreateSpriteFromGrid("Checkpoint_Active", CheckpointGrid, c => MapColor(c, true), 32f, 32, 32, alignBottom: true);
        }

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.sortingOrder = 3;
        spriteRenderer.sprite = isActivated ? activeSprite : inactiveSprite;
        spriteRenderer.color = Color.white;
    }

    private static Color MapColor(char c, bool active)
    {
        switch (c)
        {
            case 'O': return new Color(0.08f, 0.09f, 0.14f, 1f);
            case 'S': return new Color(0.36f, 0.40f, 0.52f, 1f);
            case 's': return new Color(0.56f, 0.62f, 0.76f, 1f);
            case 'C': return active ? new Color(0.18f, 0.92f, 0.56f, 1f) : new Color(0.28f, 0.36f, 0.48f, 1f);
            case 'c': return active ? new Color(0.72f, 1.00f, 0.86f, 1f) : new Color(0.45f, 0.54f, 0.68f, 1f);
            default: return Color.clear;
        }
    }
}
