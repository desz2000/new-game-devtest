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

        const int width = 12;
        const int height = 4;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color outer = new Color(0.00f, 0.88f, 1.00f, 1f);
        Color core = new Color(1.00f, 1.00f, 1.00f, 1f);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isTip = (x == 0 || x == width - 1) && (y == 0 || y == height - 1);
                if (isTip)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (y == 1 || y == 2)
                {
                    tex.SetPixel(x, y, (x > 1 && x < width - 1) ? core : outer);
                }
                else
                {
                    tex.SetPixel(x, y, outer);
                }
            }
        }

        tex.Apply();
        cachedLaserSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
        cachedLaserSprite.name = "PixelLaserSprite";
        return cachedLaserSprite;
    }

    private static Sprite GetSparkSprite()
    {
        if (cachedSparkSprite != null) return cachedSparkSprite;

        const int size = 6;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool centerCross = (x == 2 || x == 3 || y == 2 || y == 3);
                tex.SetPixel(x, y, centerCross ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        cachedSparkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        cachedSparkSprite.name = "PixelSparkSprite";
        return cachedSparkSprite;
    }
}
