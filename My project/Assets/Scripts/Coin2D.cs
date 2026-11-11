using UnityEngine;

/// <summary>
/// Moeda coletável em Pixel Art 32x32 (símbolo 'o' no mapa ASCII).
/// Recompensa rotas opcionais, guia o arco do pulo e reseta ao reiniciar a tentativa.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Coin2D : MonoBehaviour
{
    [Header("Moeda Coletável")]
    public int value = 1;
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 3.8f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Vector3 basePosition;
    private bool initializedBasePos;
    private bool isCollected;

    private static Sprite[] coinFrames;

    private static readonly string[] CoinFrame1Grid =
    {
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "............OOOOOOOO............",
        "..........OOYYYYYYYYOO..........",
        ".........OYYhhhhhhhhYYO.........",
        "........OYhhYYYYYYYYyyYO........",
        "........OYhYYOOOOOOYYyYO........",
        ".......OYhYYOOhhhhOOYyYYO.......",
        ".......OYhYOOhYYYYyOYyYYO.......",
        ".......OYhYOhYYYYYYyOYyYO.......",
        ".......OYhYOhYYYYYYyOYyYO.......",
        ".......OYhYOOhYYYYyOOYyYO.......",
        ".......OYhYYOOhhhhOOYyYYO.......",
        "........OYhYYOOOOOOYYyYO........",
        "........OYhhYYYYYYYYyyYO........",
        ".........OYYyyyyyyyyYYO.........",
        "..........OOYYYYYYYYOO..........",
        "............OOOOOOOO............",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    private static readonly string[] CoinFrame2Grid =
    {
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        ".............OOOOOO.............",
        "............OYYYYYYO............",
        "...........OYhhhhhhYO...........",
        "..........OYhYYYYYYyYO..........",
        "..........OYhYOOOOYyYO..........",
        "..........OYhYOhhyOYyYO.........",
        "..........OYhYOhYyOYyYO.........",
        "..........OYhYOhYyOYyYO.........",
        "..........OYhYOhYyOYyYO.........",
        "..........OYhYOhYyOYyYO.........",
        "..........OYhYOhhyOYyYO.........",
        "..........OYhYOOOOYyYO..........",
        "..........OYhYYYYYYyYO..........",
        "...........OYyyyyyyYO...........",
        "............OYYYYYYO............",
        ".............OOOOOO.............",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    public bool IsCollected => isCollected;

    private void Awake()
    {
        EnsureVisuals();
        if (Application.isPlaying && !initializedBasePos)
        {
            basePosition = transform.position;
            initializedBasePos = true;
        }
    }

    private void OnEnable()
    {
        EnsureVisuals();
    }

    public void ConfigureSpawn(Vector3 worldPos)
    {
        basePosition = worldPos;
        initializedBasePos = true;
        transform.position = worldPos;
        ResetCoin();
    }

    public void EnsureVisuals()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(0.55f, 0.55f);
        }

        if (coinFrames == null || coinFrames.Length != 2 || coinFrames[0] == null)
        {
            coinFrames = new[]
            {
                PixelArtBuilder.CreateSpriteFromGrid("RuneCoin32_1", CoinFrame1Grid, MapCoinColor, 32f, 32, 32),
                PixelArtBuilder.CreateSpriteFromGrid("RuneCoin32_2", CoinFrame2Grid, MapCoinColor, 32f, 32, 32)
            };
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 4;
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = coinFrames[0];
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || isCollected) return;

        if (!initializedBasePos)
        {
            basePosition = transform.position;
            initializedBasePos = true;
        }

        float phase = Time.time * bobSpeed + basePosition.x * 0.7f;
        transform.position = basePosition + new Vector3(0f, Mathf.Sin(phase) * bobAmplitude, 0f);

        if (spriteRenderer != null && coinFrames != null && coinFrames.Length == 2)
        {
            int idx = Mathf.FloorToInt(Time.time * 4.5f + Mathf.Abs(basePosition.x)) % 2;
            spriteRenderer.sprite = coinFrames[idx];
        }
    }

    public int Collect()
    {
        if (isCollected) return 0;
        isCollected = true;

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;

        GameAudio2D.PlayCoin();
        PlayerDustEffect2D.SpawnCoinSparkles(transform.position);
        LaserProjectile2D.SpawnImpactSpark(transform.position, 0.14f, new Color(1.00f, 0.92f, 0.28f, 1f));
        return value;
    }

    public void ResetCoin()
    {
        EnsureVisuals();
        isCollected = false;
        if (initializedBasePos)
        {
            transform.position = basePosition;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = coinFrames[0];
            spriteRenderer.color = Color.white;
        }
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }

    private static Color MapCoinColor(char c)
    {
        switch (c)
        {
            case 'O': return new Color(0.14f, 0.09f, 0.04f, 1f);
            case 'Y': return new Color(1.00f, 0.84f, 0.16f, 1f);
            case 'y': return new Color(0.84f, 0.56f, 0.08f, 1f);
            case 'h': return new Color(1.00f, 0.97f, 0.68f, 1f);
            default: return Color.clear;
        }
    }
}
