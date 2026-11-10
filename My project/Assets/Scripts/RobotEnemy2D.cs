using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class RobotEnemy2D : MonoBehaviour
{
    [Header("Patrulha (Estilo Goomba)")]
    public float moveSpeed = 2.4f;
    public float patrolRange = 3.2f;
    public int startDirection = -1;

    [Header("Vida (Tiros de Laser)")]
    public int maxHealth = 2;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Vector3 startPosition;
    private int currentDirection;
    private int currentHealth;
    private bool isDead;
    private float animTimer;
    private int animFrame;
    private float hitFlashTimer;
    private float deathBounceTimer;
    private float hurtRecoilTimer;
    private int hurtRecoilDir;
    private bool initializedStartPos;

    private static Sprite walkSprite1;
    private static Sprite walkSprite2;
    private static Sprite deadSprite;

    // Robozinho estilo Clank em 32x32 (olhos verdes luminosos, metal prateado sombreado, antena rubi)
    // Legenda:
    // . = Transparente | O = Contorno escuro
    // R/r = Luz da antena (vermelho vivo / brilho)
    // A = Juntas metálicas escuras | M/m/H = Metal prateado (base / sombra / brilho)
    // G/g = Olhos verdes estilo Clank (base / brilho)
    // C/c = Núcleo ciano no peito | X = Olhos apagados

    private static readonly string[] Walk1Grid =
    {
        "..............OOOO..............",
        ".............OrRRrO.............",
        ".............ORRRRO.............",
        "..............OOOO..............",
        "..............OAAO..............",
        "..............OAAO..............",
        "........OOOOOOOOOOOOOOOO........",
        ".......OOHHHHHHHHHHHHHmOO.......",
        "......OHHMMMMMMMMMMMMMmmMO......",
        ".....OHMMOOOOOOOOOOOOOOMmmO.....",
        ".....OMMOgGGGGOMMMOgGGGOMmO.....",
        ".....OMMOGGGGGOMMMOGGGGGMmO.....",
        ".....OMMOGGGGGOMMMOGGGGGMmO.....",
        ".....OMMOgGGmmOMMMOgGGmmMmO.....",
        ".....OMMMOOOOOOOOOOOOOOMmmO.....",
        "......OMMMMMMMMMMMMMMMmmmO......",
        ".......OOOOOAAAAAAAOOOOOO.......",
        "........OOOHMMMMMMmOOO..........",
        ".......OAAOMMMCCMMmOAAO.........",
        "......OAAOOMMcCCcMmOOAAO........",
        ".....OAAO.OMMMMMMMmO.OAAO.......",
        ".....OAO..OMMmmmmmmO..OAO.......",
        ".....OO...OAAAAAAAAO...OO.......",
        "..........OAAO..OAAO............",
        ".........OAAO....OAAO...........",
        ".........OMMO....OMMO...........",
        "........OAAAO....OAAAO..........",
        ".......OHMMMO....OHMMMO.........",
        ".......OAAAAO....OAAAAO.........",
        ".......OOOOOO....OOOOOO.........",
        "................................",
        "................................"
    };

    private static readonly string[] Walk2Grid =
    {
        "................................",
        "..............OOOO..............",
        ".............OrRRrO.............",
        ".............ORRRRO.............",
        "..............OOOO..............",
        "..............OAAO..............",
        "..............OAAO..............",
        "........OOOOOOOOOOOOOOOO........",
        ".......OOHHHHHHHHHHHHHmOO.......",
        "......OHHMMMMMMMMMMMMMmmMO......",
        ".....OHMMOOOOOOOOOOOOOOMmmO.....",
        ".....OMMOgGGGGOMMMOgGGGOMmO.....",
        ".....OMMOGGGGGOMMMOGGGGGMmO.....",
        ".....OMMOGGGGGOMMMOGGGGGMmO.....",
        ".....OMMOgGGmmOMMMOgGGmmMmO.....",
        ".....OMMMOOOOOOOOOOOOOOMmmO.....",
        "......OMMMMMMMMMMMMMMMmmmO......",
        ".......OOOOOAAAAAAAOOOOOO.......",
        ".......OAAOHMMMMMMmOAAO.........",
        "......OAAOOMMMCCMMmOOAAO........",
        "......OAO.OMMcCCcMmO.OAO........",
        "......OO..OMMMMMMMmO..OO........",
        "..........OMMmmmmmmO............",
        "..........OAAAAAAAAO............",
        "...........OAAOOAAO.............",
        "...........OMMOOMMO.............",
        "..........OAAAO.OAAAO...........",
        ".........OHMMMO.OHMMMO..........",
        ".........OAAAAO.OAAAAO..........",
        ".........OOOOOO.OOOOOO..........",
        "................................",
        "................................"
    };

    private static readonly string[] DeadGrid =
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
        "....OOOOOOOOOOOOOO..............",
        "...OHHHHHHHHHHHHHmO..OOOOOOOO...",
        "..OHMMOOOOOOOOOOOMmOOHMMMMMMmO..",
        ".OOMMOXXXOMMMOXXXOMmOMMCCCCMMmO.",
        "ORROMMXXXOMMMOXXXOMmOMMcCCcMMmO.",
        ".OOMMMOOOOOOOOOOOMmOOMMMMMMmmOO.",
        "..OMMMMMMMMMMMMMMmOOAAAAAAAAO...",
        "...OOOOOAAAAAAOOOOOOAAOOOOAAO...",
        ".......OAAOOAAO....OMMO..OMMO...",
        "......OHMMOOHMMO..OAAAO..OAAAO..",
        "......OAAAAOAAAAO.OAAAO..OAAAO..",
        "......OOOOOOOOOOOOOOOOOOOOOOOO..",
        "................................",
        "................................",
        "................................"
    };

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        EnsureSprites();
        if (Application.isPlaying)
        {
            if (!initializedStartPos)
            {
                startPosition = transform.position;
                initializedStartPos = true;
            }
            ResetEnemy();
        }
    }

    public void ConfigureSpawn(Vector3 spawnPos, float range, float speed, int dir = -1)
    {
        startPosition = spawnPos;
        initializedStartPos = true;
        patrolRange = range;
        moveSpeed = speed;
        startDirection = dir;
        transform.position = spawnPos;
        ResetEnemy();
    }

    private void OnEnable()
    {
        EnsureSprites();
        if (!Application.isPlaying && spriteRenderer != null)
        {
            spriteRenderer.sprite = walkSprite1;
            spriteRenderer.color = Color.white;
        }
    }

    private void OnValidate()
    {
        EnsureSprites();
        if (!Application.isPlaying && spriteRenderer != null)
        {
            spriteRenderer.sprite = walkSprite1;
            spriteRenderer.color = Color.white;
        }
    }

    public void EnsureSprites()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        if (walkSprite1 == null) walkSprite1 = BuildSprite(Walk1Grid, "ClankRobot32_Walk1");
        if (walkSprite2 == null) walkSprite2 = BuildSprite(Walk2Grid, "ClankRobot32_Walk2");
        if (deadSprite == null) deadSprite = BuildSprite(DeadGrid, "ClankRobot32_Dead");
    }

    public void ResetEnemy()
    {
        EnsureSprites();
        if (!initializedStartPos)
        {
            startPosition = transform.position;
            initializedStartPos = true;
        }
        transform.position = startPosition;
        transform.localScale = Vector3.one;
        currentDirection = startDirection != 0 ? (int)Mathf.Sign(startDirection) : -1;
        currentHealth = maxHealth;
        isDead = false;
        hitFlashTimer = 0f;
        deathBounceTimer = 0f;
        hurtRecoilTimer = 0f;

        if (boxCollider != null)
        {
            boxCollider.enabled = true;
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(0.82f, 0.88f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = walkSprite1;
            spriteRenderer.color = Color.white;
            spriteRenderer.flipX = (currentDirection < 0);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitFlashTimer > 0f
                    ? new Color(1f, 0.42f, 0.55f, 1f)
                    : (currentHealth == 1 && !isDead ? new Color(1f, 0.84f, 0.84f, 1f) : Color.white);
            }
        }

        if (isDead)
        {
            if (deathBounceTimer > 0f)
            {
                deathBounceTimer -= Time.deltaTime * 6f;
                float s = Mathf.Max(0f, deathBounceTimer);
                transform.localScale = new Vector3(1f + s * 0.28f, 1f - s * 0.22f, 1f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
            return;
        }

        if (hurtRecoilTimer > 0f)
        {
            hurtRecoilTimer -= Time.deltaTime;
            float recoilStep = hurtRecoilDir * 3.8f * Time.deltaTime;
            if (HasGroundAhead(hurtRecoilDir))
            {
                transform.position += new Vector3(recoilStep, 0f, 0f);
            }
            float squashAmt = hurtRecoilTimer / 0.14f;
            transform.localScale = new Vector3(1f - squashAmt * 0.18f, 1f + squashAmt * 0.22f, 1f);
            return;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        transform.position += new Vector3(currentDirection * moveSpeed * Time.deltaTime, 0f, 0f);

        float offsetFromStart = transform.position.x - startPosition.x;
        if (offsetFromStart > patrolRange && currentDirection > 0)
        {
            currentDirection = -1;
        }
        else if (offsetFromStart < -patrolRange && currentDirection < 0)
        {
            currentDirection = 1;
        }

        // Vira ao encontrar parede/obstáculo ou beira de abismo/plataforma
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position + new Vector3(currentDirection * 0.45f, 0f, 0f),
            new Vector2(0.12f, 0.6f),
            0f
        );
        bool blockedAhead = false;
        foreach (Collider2D hit in hits)
        {
            if (hit != boxCollider && !hit.isTrigger && hit.GetComponent<BoxPlayer2D>() == null)
            {
                blockedAhead = true;
                break;
            }
        }

        if (blockedAhead || !HasGroundAhead(currentDirection))
        {
            currentDirection = -currentDirection;
        }

        animTimer += Time.deltaTime * (currentHealth == 1 ? 9.5f : 7f);
        if (animTimer >= 1f)
        {
            animTimer -= 1f;
            animFrame = 1 - animFrame;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = (animFrame == 0) ? walkSprite1 : walkSprite2;
            spriteRenderer.flipX = (currentDirection < 0);
        }
    }

    private bool HasGroundAhead(int dir)
    {
        Vector2 probeCenter = new Vector2(transform.position.x + dir * 0.42f, transform.position.y - 0.62f);
        Collider2D[] groundHits = Physics2D.OverlapBoxAll(probeCenter, new Vector2(0.16f, 0.22f), 0f);
        foreach (Collider2D hit in groundHits)
        {
            if (hit != boxCollider && !hit.isTrigger && !hit.gameObject.name.StartsWith("Obstacle"))
            {
                return true;
            }
        }
        return false;
    }

    public void TakeLaserHit()
    {
        if (isDead) return;

        currentHealth--;
        hitFlashTimer = 0.14f;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeScratchHit(int damage = 1, int hitDirection = 1)
    {
        if (isDead) return;

        currentHealth -= Mathf.Max(1, damage);
        hitFlashTimer = 0.18f;
        hurtRecoilTimer = 0.14f;
        hurtRecoilDir = hitDirection >= 0 ? 1 : -1;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void StompKill()
    {
        if (isDead) return;
        currentHealth = 0;
        Die();
    }

    private void Die()
    {
        isDead = true;
        hitFlashTimer = 0f;
        hurtRecoilTimer = 0f;
        deathBounceTimer = 1f;

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = deadSprite;
            spriteRenderer.color = Color.white;
        }
    }

    private static Sprite BuildSprite(string[] grid, string name)
    {
        return PixelArtBuilder.CreateSpriteFromGrid(name, grid, CharToColor, pixelsPerUnit: 32f, width: 32, height: 32);
    }

    private static Color CharToColor(char c)
    {
        switch (c)
        {
            case 'O': return new Color(0.08f, 0.09f, 0.14f, 1f); // Contorno pixel art
            case 'R': return new Color(0.98f, 0.20f, 0.22f, 1f); // Luz vermelha da antena
            case 'r': return new Color(1.00f, 0.64f, 0.54f, 1f); // Brilho da antena
            case 'A': return new Color(0.21f, 0.25f, 0.34f, 1f); // Juntas metálicas escuras
            case 'M': return new Color(0.72f, 0.78f, 0.86f, 1f); // Metal prateado estilo Clank
            case 'm': return new Color(0.47f, 0.54f, 0.64f, 1f); // Sombra metálica
            case 'H': return new Color(0.90f, 0.94f, 0.99f, 1f); // Realce metálico superior
            case 'G': return new Color(0.16f, 0.90f, 0.42f, 1f); // Olhos verdes estilo Clank
            case 'g': return new Color(0.72f, 1.00f, 0.80f, 1f); // Brilho dos olhos
            case 'C': return new Color(0.05f, 0.86f, 1.00f, 1f); // Núcleo no peito
            case 'c': return new Color(0.62f, 0.97f, 1.00f, 1f); // Brilho do núcleo
            case 'X': return new Color(0.14f, 0.16f, 0.22f, 1f); // Olhos apagados
            default:  return new Color(0f, 0f, 0f, 0f);
        }
    }
}
