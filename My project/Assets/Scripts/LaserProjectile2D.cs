using System.Collections.Generic;
using UnityEngine;

public class LaserProjectile2D : MonoBehaviour
{
    public float speed = 18f;
    public float maxLifetime = 1.6f;

    private int direction = 1;
    private float lifeTimer;
    private static Sprite cachedLaserSprite;
    private static Sprite cachedSparkSprite;

    public static void SpawnLaser(Vector3 spawnPosition, int dir)
    {
        GameObject laserObj = new GameObject("LaserBolt");
        laserObj.transform.position = spawnPosition;

        SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetLaserSprite();
        sr.sortingOrder = 8;
        sr.flipX = (dir < 0);

        BoxCollider2D col = laserObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.65f, 0.20f);

        Rigidbody2D rb = laserObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        LaserProjectile2D proj = laserObj.AddComponent<LaserProjectile2D>();
        proj.direction = dir >= 0 ? 1 : -1;

        SpawnFlash(spawnPosition, 0.06f, new Color(0.2f, 1f, 1f, 0.9f));
    }

    private void Update()
    {
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BoxPlayer2D>() != null || other.GetComponent<LaserProjectile2D>() != null)
        {
            return;
        }

        RobotEnemy2D robot = other.GetComponent<RobotEnemy2D>();
        if (robot != null)
        {
            if (!robot.IsDead)
            {
                robot.TakeLaserHit();
                SpawnFlash(transform.position, 0.10f, new Color(1f, 0.55f, 0.15f, 1f));
                Destroy(gameObject);
            }
            return;
        }

        Switch2D switchObj = other.GetComponent<Switch2D>();
        if (switchObj != null)
        {
            if (!switchObj.isActivated)
            {
                switchObj.ActivateSwitch();
            }
            SpawnFlash(transform.position, 0.12f, new Color(0.25f, 1.0f, 0.72f, 1f));
            Destroy(gameObject);
            return;
        }

        BreakableBlock2D breakable = other.GetComponent<BreakableBlock2D>();
        if (breakable != null && !breakable.IsBroken)
        {
            breakable.BreakBlock();
            SpawnFlash(transform.position, 0.12f, new Color(1.0f, 0.86f, 0.28f, 1f));
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            SpawnFlash(transform.position, 0.09f, new Color(1f, 0.92f, 0.25f, 1f));
            Destroy(gameObject);
        }
    }

    private static void SpawnFlash(Vector3 pos, float duration, Color color)
    {
        GameObject flashObj = new GameObject("LaserFlash");
        flashObj.transform.position = pos;

        SpriteRenderer sr = flashObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSparkSprite();
        sr.color = color;
        sr.sortingOrder = 9;

        Destroy(flashObj, duration);
    }

    private static Sprite GetLaserSprite()
    {
        if (cachedLaserSprite != null) return cachedLaserSprite;

        const int width = 24;
        const int height = 8;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color tail = new Color(0.05f, 0.65f, 0.95f, 0.75f);
        Color outer = new Color(0.05f, 0.90f, 1.00f, 1f);
        Color core = new Color(0.92f, 1.00f, 1.00f, 1f);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inCapsule = (y >= 1 && y <= 6) || (x >= 3 && x <= width - 3);
                bool cutCorners = (x == 0 || x == width - 1) && (y <= 1 || y >= 6);
                if (!inCapsule || cutCorners)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (y >= 3 && y <= 4 && x >= 4 && x <= width - 3)
                {
                    tex.SetPixel(x, y, core);
                }
                else if (x < 5)
                {
                    tex.SetPixel(x, y, tail);
                }
                else
                {
                    tex.SetPixel(x, y, outer);
                }
            }
        }

        tex.Apply();
        cachedLaserSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        cachedLaserSprite.name = "PixelLaserSprite32";
        return cachedLaserSprite;
    }

    private static Sprite GetSparkSprite()
    {
        if (cachedSparkSprite != null) return cachedSparkSprite;

        const int size = 12;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - 5);
                int dy = Mathf.Abs(y - 5);
                bool star = (dx <= 1 && dy <= 5) || (dy <= 1 && dx <= 5) || (dx + dy <= 3);
                tex.SetPixel(x, y, star ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        cachedSparkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        cachedSparkSprite.name = "PixelSparkSprite32";
        return cachedSparkSprite;
    }

    public static void SpawnImpactSpark(Vector3 pos, float duration, Color color)
    {
        SpawnFlash(pos, duration, color);
    }
}

/// <summary>
/// Efeito visual e hitbox em Pixel Art 32x32 do Ataque de Arranhão da Boneca Kitty (Novo Poder da Fase 2).
/// Causa 1 de dano aos robozinhos atingidos e exibe 3 frames de garras afiadas luminosas.
/// </summary>
public class ScratchSlashEffect2D : MonoBehaviour
{
    public float duration = 0.22f;
    private int direction = 1;
    private float timer;
    private SpriteRenderer sr;
    private readonly HashSet<RobotEnemy2D> hitRobots = new HashSet<RobotEnemy2D>();

    private static Sprite[] cachedSlashSprites;

    private static readonly string[] SlashFrame1Grid =
    {
        "................................",
        "......OOOO......................",
        ".....OaaaaOO....................",
        "....OaWWWWaaOO..................",
        ".....OaaWWWWaaOO................",
        ".......OOaaWWWWaOO..............",
        "....OOOO..OOaWWWWaO.............",
        "...OaaaaOO..OOaWWWaO............",
        "..OaWWWWaaOO..OOaWWaO...........",
        "...OaaWWWWaaOO..OOaWaO..........",
        ".....OOaaWWWWaOO..OOaO..........",
        ".......OOaWWWWaOO...OO..........",
        ".....OOOO.OOaWWWaO..............",
        "....OaaaaOO.OOaWWaO.............",
        "...OaWWWWaaOO.OOaWaO............",
        "....OaaWWWWaaOO.OOO.............",
        "......OOaaWWWWaOO...............",
        "........OOaWWWWaO...............",
        "..........OOaWWaO...............",
        "............OOOO................",
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
        "................................"
    };

    private static readonly string[] SlashFrame2Grid =
    {
        "................................",
        "........OOOO....................",
        ".......OaaaaOO..................",
        "......OaRRRRaaOO......OO........",
        ".......OaaWWWWaaOO...OYYO.......",
        ".........OOaaWWWWaOOOYYYYO......",
        "......OOOO.OOaWWWWWWWWYYO.......",
        ".....OaaaaOO.OOaWWWWWWaOO.......",
        "....OaRRRRaaOO.OOaWWWWWaO.......",
        ".....OaaWWWWaaOO.OOaWWWWaO......",
        ".......OOaaWWWWaOO.OOaWWWaO.....",
        ".........OOaWWWWaOO..OaWWaO.....",
        "......OOOO.OOaWWWWaO..OaWaO.....",
        ".....OaaaaOO.OOaWWWaO..OOO......",
        "....OaRRRRaaOO.OOaWWaO..........",
        ".....OaaWWWWaaOO.OaWaO..........",
        ".......OOaaWWWWaOO.OOO..........",
        ".........OOaWWWWaOO.............",
        "...........OOaWWWWaO....OO......",
        ".............OOaWWWaO..OYYO.....",
        "...............OOaWWaOOYYYYO....",
        ".................OOaWaOOYYO.....",
        "...................OOOO.OO......",
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

    private static readonly string[] SlashFrame3Grid =
    {
        "................................",
        "................................",
        "................................",
        "......................OO........",
        "..............OO.....OYYO.......",
        ".............OaaOO....OO........",
        "..............OWWaOO............",
        "...............OaWWaOO..........",
        ".................OaWWaO.........",
        "...........OO.....OaWaO.........",
        "..........OaaOO....OOaO.........",
        "...........OWWaOO....OO.........",
        "............OaWWaOO.............",
        "..............OaWWaOO...OO......",
        "................OaWWaO.OYYO.....",
        "...........OO....OaWaO..OO......",
        "..........OaaOO...OOaO..........",
        "...........OWWaOO...OO..........",
        "............OaWWaOO.............",
        "..............OaWWaOO...........",
        "................OaWWaO..........",
        "..................OaWaO.........",
        "...................OOaO.........",
        ".....................OO.........",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................",
        "................................"
    };

    public static void SpawnScratchSlash(Vector3 spawnPos, int dir)
    {
        EnsureSlashSprites();

        GameObject obj = new GameObject("KittyScratchSlash");
        obj.transform.position = spawnPos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = cachedSlashSprites[0];
        sr.sortingOrder = 9;
        sr.flipX = (dir < 0);

        ScratchSlashEffect2D effect = obj.AddComponent<ScratchSlashEffect2D>();
        effect.direction = dir >= 0 ? 1 : -1;
        effect.sr = sr;
        effect.CheckHitsInBox();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += new Vector3(direction * 2.4f * Time.deltaTime, 0f, 0f);

        CheckHitsInBox();

        if (sr != null && cachedSlashSprites != null && cachedSlashSprites.Length == 3)
        {
            float t = Mathf.Clamp01(timer / duration);
            int idx = Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, 2);
            sr.sprite = cachedSlashSprites[idx];
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void CheckHitsInBox()
    {
        Collider2D[] cols = Physics2D.OverlapBoxAll(transform.position, new Vector2(1.25f, 1.05f), 0f);
        foreach (Collider2D col in cols)
        {
            RobotEnemy2D robot = col.GetComponent<RobotEnemy2D>();
            if (robot != null && !robot.IsDead && !hitRobots.Contains(robot))
            {
                hitRobots.Add(robot);
                robot.TakeScratchHit(1, direction);
                LaserProjectile2D.SpawnImpactSpark(robot.transform.position + new Vector3(0f, 0.1f, 0f), 0.12f, new Color(1f, 0.45f, 0.78f, 1f));
                CameraFollow2D.TriggerShake(0.24f);
                GameInput2D.TriggerRumble(0.22f, 0.40f, 0.08f);
                continue;
            }

            Switch2D switchObj = col.GetComponent<Switch2D>();
            if (switchObj != null && !switchObj.isActivated)
            {
                switchObj.ActivateSwitch();
                continue;
            }

            BreakableBlock2D breakable = col.GetComponent<BreakableBlock2D>();
            if (breakable != null && !breakable.IsBroken)
            {
                breakable.BreakBlock();
            }
        }
    }

    private static void EnsureSlashSprites()
    {
        if (cachedSlashSprites != null && cachedSlashSprites.Length == 3 && cachedSlashSprites[0] != null)
        {
            return;
        }

        cachedSlashSprites = new[]
        {
            PixelArtBuilder.CreateSpriteFromGrid("KittySlash_1", SlashFrame1Grid, SlashCharToColor, 32f, 32, 32),
            PixelArtBuilder.CreateSpriteFromGrid("KittySlash_2", SlashFrame2Grid, SlashCharToColor, 32f, 32, 32),
            PixelArtBuilder.CreateSpriteFromGrid("KittySlash_3", SlashFrame3Grid, SlashCharToColor, 32f, 32, 32)
        };
    }

    private static Color SlashCharToColor(char c)
    {
        switch (c)
        {
            case 'O': return new Color(0.14f, 0.05f, 0.12f, 0.95f);
            case 'R': return new Color(0.98f, 0.18f, 0.52f, 1f);
            case 'a': return new Color(1.00f, 0.58f, 0.82f, 1f);
            case 'W': return new Color(0.98f, 0.99f, 1.00f, 1f);
            case 'Y': return new Color(1.00f, 0.90f, 0.28f, 1f);
            default:  return Color.clear;
        }
    }
}

/// <summary>
/// Onda de choque em Pixel Art 32x32 ao esmagar um robô com o Pulo Esmagador.
/// </summary>
public class StompShockwaveEffect2D : MonoBehaviour
{
    public float duration = 0.22f;
    private float timer;
    private SpriteRenderer sr;
    private static Sprite cachedRingSprite;

    public static void SpawnStompWave(Vector3 worldPos)
    {
        GameObject obj = new GameObject("StompShockwave");
        obj.transform.position = worldPos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.sortingOrder = 9;
        sr.color = new Color(0.45f, 0.96f, 1f, 0.95f);

        StompShockwaveEffect2D fx = obj.AddComponent<StompShockwaveEffect2D>();
        fx.sr = sr;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float scaleX = Mathf.Lerp(0.65f, 1.85f, t);
        float scaleY = Mathf.Lerp(0.45f, 0.95f, t);
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        if (sr != null)
        {
            sr.color = new Color(0.55f, 0.98f, 1f, 1f - t);
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetRingSprite()
    {
        if (cachedRingSprite != null) return cachedRingSprite;

        const int w = 32;
        const int h = 16;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("StompWave32x16", w, h);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 15.5f) / 14.5f;
                float ny = (y - 7.5f) / 6.5f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                if (r >= 0.68f && r <= 1.0f)
                {
                    tex.SetPixel(x, y, r > 0.84f ? new Color(1f, 0.92f, 0.35f, 1f) : Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return cachedRingSprite;
    }
}

/// <summary>
/// Partículas leves de poeira (no pouso/pulo) e cintilação dourada (ao coletar moedas) — Fase 5 ("Juice").
/// </summary>
public class PlayerDustEffect2D : MonoBehaviour
{
    public Vector2 velocity;
    public float duration = 0.28f;
    public float gravity = 0f;
    public Color baseColor = Color.white;

    private float timer;
    private SpriteRenderer sr;
    private static Sprite cachedPuffSprite;

    public static void SpawnLandingDust(Vector3 feetPosition, float intensity = 1f)
    {
        int count = 4;
        for (int i = 0; i < count; i++)
        {
            float side = (i < 2) ? -1f : 1f;
            float speedMag = (i % 2 == 0) ? 1.8f : 0.95f;
            Vector2 vel = new Vector2(side * speedMag * intensity, Random.Range(0.35f, 0.95f) * intensity);
            SpawnSinglePuff(
                feetPosition + new Vector3(side * 0.14f, 0.04f, 0f),
                vel,
                duration: 0.26f,
                gravity: -1.2f,
                col: new Color(0.84f, 0.88f, 0.95f, 0.72f),
                initialScale: Random.Range(0.75f, 1.15f)
            );
        }
    }

    public static void SpawnJumpDust(Vector3 feetPosition)
    {
        for (int i = 0; i < 3; i++)
        {
            float dx = (i - 1) * 0.9f;
            SpawnSinglePuff(
                feetPosition + new Vector3((i - 1) * 0.10f, 0.05f, 0f),
                new Vector2(dx, Random.Range(0.4f, 1.0f)),
                duration: 0.22f,
                gravity: -0.8f,
                col: new Color(0.88f, 0.92f, 0.98f, 0.65f),
                initialScale: 0.80f
            );
        }
    }

    public static void SpawnCoinSparkles(Vector3 coinPosition)
    {
        for (int i = 0; i < 5; i++)
        {
            float angle = (i / 5f) * Mathf.PI * 2f + 0.3f;
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.2f;
            SpawnSinglePuff(
                coinPosition,
                vel,
                duration: 0.28f,
                gravity: 2.5f,
                col: (i % 2 == 0) ? new Color(1.00f, 0.92f, 0.32f, 0.95f) : new Color(1.00f, 0.98f, 0.78f, 0.95f),
                initialScale: 0.70f
            );
        }
    }

    private static void SpawnSinglePuff(Vector3 pos, Vector2 vel, float duration, float gravity, Color col, float initialScale)
    {
        GameObject obj = new GameObject("JuiceDustPuff");
        obj.transform.position = pos;
        obj.transform.localScale = Vector3.one * initialScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetPuffSprite();
        sr.sortingOrder = 7;
        sr.color = col;

        PlayerDustEffect2D puff = obj.AddComponent<PlayerDustEffect2D>();
        puff.velocity = vel;
        puff.duration = duration;
        puff.gravity = gravity;
        puff.baseColor = col;
        puff.sr = sr;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        float t = Mathf.Clamp01(timer / duration);
        float scale = Mathf.Lerp(1.0f, 0.25f, t);
        transform.localScale = Vector3.one * scale;

        if (sr != null)
        {
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - t));
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetPuffSprite()
    {
        if (cachedPuffSprite != null) return cachedPuffSprite;

        const int s = 8;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("DustPuff8", s, s);
        Vector2 c = new Vector2(3.5f, 3.5f);
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= 3.4f ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        cachedPuffSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        return cachedPuffSprite;
    }
}
