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

    private static Sprite walkSprite1;
    private static Sprite walkSprite2;
    private static Sprite deadSprite;

    // Robozinho amigável estilo Clank (olhos verdes grandes, corpo metálico, antena)
    private static readonly string[] Walk1Grid =
    {
        ".......RR.......",
        ".......AA.......",
        "....AAAAAAAA....",
        "...AMMMMMMMMA...",
        "...AMgGMMMgGMA..",
        "...AMGGMMMGGMA..",
        "...AMMMMMMMMA...",
        "....AAMMMMAA....",
        "....AMMMMMMA....",
        "...AAMCMMCMAA...",
        "..AA.AMMMMMA.AA.",
        ".....AMMMMMA....",
        ".....AA..AA.....",
        "....AA....AA....",
        "...AAA....AAA...",
        "................"
    };

    private static readonly string[] Walk2Grid =
    {
        "................",
        ".......RR.......",
        ".......AA.......",
        "....AAAAAAAA....",
        "...AMMMMMMMMA...",
        "...AMgGMMMgGMA..",
        "...AMGGMMMGGMA..",
        "...AMMMMMMMMA...",
        "....AAMMMMAA....",
        "...AAMCMMCMAA...",
        "...A.AMMMMMA.A..",
        ".....AMMMMMA....",
        "......AA.AA.....",
        ".....AA...AA....",
        ".....AAA..AAA...",
        "................"
    };

    // Corpo do robozinho estirado no chão após morrer
    private static readonly string[] DeadGrid =
    {
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "..AAAAAAA.......",
        ".AMMMMMMMA.AAAA.",
        "AMXXMMMXXMAMMMMA",
        "AMMMMMMMMAMCMMMA",
        "RAAAMMMMMMAMMMAA",
        "...AAAAAAAAAAAAA",
        "................"
    };

    public bool IsDead => isDead;

    private void Awake()
    {
        EnsureSprites();
        if (Application.isPlaying)
        {
            startPosition = transform.position;
            ResetEnemy();
        }
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

        if (walkSprite1 == null) walkSprite1 = BuildSprite(Walk1Grid, "ClankRobot_Walk1");
        if (walkSprite2 == null) walkSprite2 = BuildSprite(Walk2Grid, "ClankRobot_Walk2");
        if (deadSprite == null) deadSprite = BuildSprite(DeadGrid, "ClankRobot_Dead");
    }

    public void ResetEnemy()
    {
        EnsureSprites();
        transform.position = startPosition;
        transform.localScale = Vector3.one;
        currentDirection = startDirection != 0 ? (int)Mathf.Sign(startDirection) : -1;
        currentHealth = maxHealth;
        isDead = false;
        hitFlashTimer = 0f;
        deathBounceTimer = 0f;

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
                spriteRenderer.color = hitFlashTimer > 0f ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
            }
        }

        if (isDead)
        {
            // Pequena animação elástica de queda ao se estirar no chão
            if (deathBounceTimer > 0f)
            {
                deathBounceTimer -= Time.deltaTime * 6f;
                float s = Mathf.Max(0f, deathBounceTimer);
                transform.localScale = new Vector3(1f + s * 0.25f, 1f - s * 0.20f, 1f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
            return;
        }

        // Movimento estilo Goomba (patrulha de um lado para o outro)
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

        // Verifica se bateu em algum obstáculo lateral para virar como Goomba
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position + new Vector3(currentDirection * 0.45f, 0f, 0f),
            new Vector2(0.12f, 0.6f),
            0f
        );
        foreach (Collider2D hit in hits)
        {
            if (hit != boxCollider && !hit.isTrigger && hit.GetComponent<BoxPlayer2D>() == null)
            {
                currentDirection = -currentDirection;
                break;
            }
        }

        // Animação de caminhada em Pixel Art
        animTimer += Time.deltaTime * 7f;
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
        deathBounceTimer = 1f;

        // Desativa o colisor para que o corpo estirado no chão não cause dano nem bloqueie tiros
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
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int row = 0; row < size; row++)
        {
            string line = grid[row];
            int y = size - 1 - row;
            for (int x = 0; x < size; x++)
            {
                char c = x < line.Length ? line[x] : '.';
                tex.SetPixel(x, y, CharToColor(c));
            }
        }

        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        s.name = name;
        return s;
    }

    private static Color CharToColor(char c)
    {
        switch (c)
        {
            case 'R': return new Color(1.00f, 0.22f, 0.20f, 1f); // Luz vermelha da antena
            case 'A': return new Color(0.22f, 0.26f, 0.33f, 1f); // Contorno / juntas de metal escuro
            case 'M': return new Color(0.74f, 0.80f, 0.86f, 1f); // Metal prateado estilo Clank
            case 'm': return new Color(0.54f, 0.60f, 0.68f, 1f); // Sombra metálica
            case 'G': return new Color(0.18f, 0.92f, 0.42f, 1f); // Olhos verdes grandes estilo Clank
            case 'g': return new Color(0.72f, 1.00f, 0.80f, 1f); // Brilho dos olhos verdes
            case 'C': return new Color(0.00f, 0.85f, 1.00f, 1f); // Luz no peito do robô
            case 'X': return new Color(0.14f, 0.16f, 0.20f, 1f); // Olhos apagados quando derrotado
            default:  return new Color(0f, 0f, 0f, 0f);
        }
    }
}
