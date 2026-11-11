using UnityEngine;

/// <summary>
/// Bloco Frágil ('F' no mapa ASCII) — Fase 3 (Diferenciação de Personagens).
/// Soluções por personagem:
///   - Boneco Menino: quebra à distância ou de perto com o disparo de Laser.
///   - Boneca Kitty: quebra com o Pisão-Martelo (segurando S / Seta Baixo ao cair sobre o bloco,
///     ou pressionando S sobre ele) ou com o Arranhão na Fase 2.
/// Ao quebrar, solta estilhaços em Pixel Art, libera a passagem/atalho e concede +1 Moeda (+100 PTS).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class BreakableBlock2D : MonoBehaviour
{
    [Header("Configuração do Bloco Frágil")]
    public int coinReward = 1;
    public int scoreBonus = 100;
    public Vector2 blockSize = new Vector2(0.95f, 0.95f);

    private SpriteRenderer sr;
    private BoxCollider2D boxCollider;
    private bool isBroken;

    private static Sprite cachedCrackedBlockSprite;
    private static Sprite cachedDebrisSprite;

    public bool IsBroken => isBroken;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        EnsureVisuals();
    }

    public void EnsureVisuals()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        EnsureSprites();

        if (sr != null)
        {
            sr.sprite = cachedCrackedBlockSprite;
            sr.sortingOrder = 3;
            sr.enabled = !isBroken;
            sr.color = Color.white;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            boxCollider.size = blockSize;
            boxCollider.enabled = !isBroken;
        }
    }

    public void BreakBlock(BoxPlayer2D player = null)
    {
        if (isBroken) return;
        isBroken = true;

        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
        }

        if (player == null)
        {
            player = FindAnyObjectByType<BoxPlayer2D>();
        }
        if (player != null)
        {
            player.AddBonusCoinsAndScore(coinReward, scoreBonus);
        }

        SpawnBreakDebris(transform.position);
        GameAudio2D.PlayBreak();
        LaserProjectile2D.SpawnImpactSpark(transform.position, 0.16f, new Color(1.0f, 0.85f, 0.30f, 1f));
        CameraFollow2D.TriggerShake(0.22f);
        GameInput2D.TriggerRumble(0.22f, 0.40f, 0.08f);
    }

    public void ResetBlock()
    {
        isBroken = false;

        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            EnsureSprites();
            sr.sprite = cachedCrackedBlockSprite;
            sr.enabled = true;
            sr.color = Color.white;
        }
    }

    private static void SpawnBreakDebris(Vector3 origin)
    {
        EnsureSprites();

        Vector2[] velocities =
        {
            new Vector2(-2.8f, 4.8f),
            new Vector2(2.8f, 4.8f),
            new Vector2(-1.6f, 6.2f),
            new Vector2(1.6f, 6.2f)
        };

        for (int i = 0; i < velocities.Length; i++)
        {
            GameObject shard = new GameObject($"BlockShard_{i}");
            shard.transform.position = origin + new Vector3((i % 2 == 0 ? -0.18f : 0.18f), (i >= 2 ? 0.18f : -0.12f), 0f);

            SpriteRenderer shardSr = shard.AddComponent<SpriteRenderer>();
            shardSr.sprite = cachedDebrisSprite;
            shardSr.sortingOrder = 8;
            shardSr.color = (i % 2 == 0) ? new Color(1f, 0.86f, 0.36f, 1f) : new Color(0.75f, 0.62f, 0.48f, 1f);

            BreakableShardParticle2D particle = shard.AddComponent<BreakableShardParticle2D>();
            particle.velocity = velocities[i];
            particle.angularSpeed = (i % 2 == 0) ? 380f : -380f;
        }
    }

    private static void EnsureSprites()
    {
        if (cachedCrackedBlockSprite != null && cachedDebrisSprite != null) return;

        cachedCrackedBlockSprite = BuildCrackedBlockSprite();
        cachedDebrisSprite = BuildDebrisSprite();
    }

    private static Sprite BuildCrackedBlockSprite()
    {
        const int size = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("BreakableBlock32", size, size);

        Color outline = new Color(0.10f, 0.08f, 0.06f, 1f);
        Color brickDark = new Color(0.42f, 0.28f, 0.18f, 1f);
        Color brickMid = new Color(0.58f, 0.40f, 0.25f, 1f);
        Color brickLight = new Color(0.76f, 0.56f, 0.36f, 1f);
        Color crackDark = new Color(0.16f, 0.11f, 0.08f, 1f);
        Color goldGlow = new Color(1.00f, 0.84f, 0.28f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                if (isBorder)
                {
                    tex.SetPixel(x, y, outline);
                    continue;
                }

                // Rachaduras diagonais com brilho dourado interno (sinalizando que guarda moeda/é quebrável)
                int diag1 = Mathf.Abs((x - 16) - (y - 16));
                int diag2 = Mathf.Abs((x - 16) + (y - 16));
                bool inCenterRange = (x >= 5 && x <= 26 && y >= 5 && y <= 26);

                if (inCenterRange && (diag1 <= 1 || diag2 <= 1))
                {
                    bool isGoldCore = (diag1 == 0 && (x + y) % 3 == 0) || (x >= 14 && x <= 17 && y >= 14 && y <= 17);
                    tex.SetPixel(x, y, isGoldCore ? goldGlow : crackDark);
                    continue;
                }

                // Argamassa de tijolos
                if (y == 10 || y == 21 || (y < 10 && x == 15) || (y >= 10 && y < 21 && (x == 8 || x == 23)) || (y >= 21 && x == 15))
                {
                    tex.SetPixel(x, y, brickDark);
                }
                else if (y == size - 2 || x == 1)
                {
                    tex.SetPixel(x, y, brickLight);
                }
                else
                {
                    tex.SetPixel(x, y, ((x + y) % 4 == 0) ? brickLight : brickMid);
                }
            }
        }

        tex.Apply();
        return PixelArtBuilder.CreateSprite64FromTexture(tex, 32f, seamlessTile: false);
    }

    private static Sprite BuildDebrisSprite()
    {
        const int s = 8;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("BlockDebris8", s, s);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                bool edge = (x == 0 || x == s - 1 || y == 0 || y == s - 1);
                if (edge && (x == 0 || x == s - 1) && (y == 0 || y == s - 1))
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    tex.SetPixel(x, y, edge ? new Color(0.25f, 0.18f, 0.12f, 1f) : Color.white);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
    }
}

/// <summary>
/// Partícula leve de estilhaço quando um BreakableBlock2D se rompe.
/// </summary>
public class BreakableShardParticle2D : MonoBehaviour
{
    public Vector2 velocity;
    public float angularSpeed = 320f;
    public float lifetime = 0.42f;
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        velocity.y -= 18f * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, angularSpeed * Time.deltaTime);

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
