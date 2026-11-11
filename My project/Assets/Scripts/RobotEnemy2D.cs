using UnityEngine;

public enum EnemyBehaviorType
{
    PatrolWalker = 0,
    Jumper = 1,
    Turret = 2,
    FlyingDrone = 3
}

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class RobotEnemy2D : MonoBehaviour
{
    [Header("Tipo de Inimigo (Fase 4)")]
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.PatrolWalker;

    [Header("Patrulha (Estilo Goomba)")]
    public float moveSpeed = 2.4f;
    public float patrolRange = 3.2f;
    public int startDirection = -1;

    [Header("Robô Saltador (Jumper)")]
    public float jumpInterval = 1.65f;
    public float jumpTelegraphDuration = 0.28f;
    public float jumpHeight = 1.85f;
    public float jumpAirDuration = 0.62f;

    [Header("Torreta (Turret)")]
    public float turretFireInterval = 2.40f;
    public float turretTelegraphDuration = 0.50f;
    public float turretDetectionRange = 11.5f;
    public float turretProjectileSpeed = 8.2f;

    [Header("Robô Voador (Flying Drone)")]
    public float flyAmplitude = 0.52f;
    public float flyFrequency = 2.6f;

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
    private float behaviorTimer;
    private float jumpAirTimer;
    private bool isJumping;
    private float deadFallVelocityY;
    private float deadGroundTargetY;
    private bool isDeadFalling;
    private bool initializedStartPos;

    private static Sprite walkSprite1;
    private static Sprite walkSprite2;
    private static Sprite jumperSprite1;
    private static Sprite jumperSprite2;
    private static Sprite turretIdleSprite;
    private static Sprite turretChargeSprite;
    private static Sprite droneSprite1;
    private static Sprite droneSprite2;
    private static Sprite deadWalkerSprite;
    private static Sprite deadJumperSprite;
    private static Sprite deadTurretSprite;
    private static Sprite deadDroneSprite;
    private static bool robotSpritesReady;

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

    public void ConfigureSpawn(Vector3 spawnPos, float range, float speed, int dir = -1, EnemyBehaviorType type = EnemyBehaviorType.PatrolWalker)
    {
        behaviorType = type;
        startPosition = spawnPos;
        initializedStartPos = true;
        patrolRange = range;
        moveSpeed = speed;
        startDirection = dir;
        if (behaviorType == EnemyBehaviorType.FlyingDrone)
        {
            maxHealth = 1;
        }
        transform.position = spawnPos;
        ResetEnemy();
    }

    private void OnEnable()
    {
        EnsureSprites();
        if (!Application.isPlaying && spriteRenderer != null)
        {
            spriteRenderer.sprite = GetPrimaryIdleSprite();
            spriteRenderer.color = Color.white;
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying) return;
            EnsureSprites();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = GetPrimaryIdleSprite();
                spriteRenderer.color = Color.white;
            }
        };
#endif
    }

    public void EnsureSprites()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        Hazard2D hazard = GetComponent<Hazard2D>();
        if (hazard == null)
        {
            hazard = gameObject.AddComponent<Hazard2D>();
        }
        hazard.damage = 1;
        hazard.stompable = (behaviorType != EnemyBehaviorType.FlyingDrone);
        hazard.blocksGroundCheck = true;

        if (robotSpritesReady && walkSprite1 != null && deadWalkerSprite != null && deadDroneSprite != null) return;

        walkSprite1 = PixelArt64Generator.GetRobotSprite(0, "Robot64_Walk1", grounded: true);
        walkSprite2 = PixelArt64Generator.GetRobotSprite(1, "Robot64_Walk2", grounded: true);
        jumperSprite1 = PixelArt64Generator.GetRobotSprite(2, "Robot64_Jumper1", grounded: true);
        jumperSprite2 = PixelArt64Generator.GetRobotSprite(3, "Robot64_Jumper2", grounded: true);
        turretIdleSprite = PixelArt64Generator.GetRobotSprite(4, "Robot64_TurretIdle", grounded: true);
        turretChargeSprite = PixelArt64Generator.GetRobotSprite(5, "Robot64_TurretCharge", grounded: true);
        droneSprite1 = PixelArt64Generator.GetRobotSprite(6, "Robot64_Drone1", grounded: false);
        droneSprite2 = PixelArt64Generator.GetRobotSprite(7, "Robot64_Drone2", grounded: false);
        deadWalkerSprite = PixelArt64Generator.GetRobotSprite(8, "Robot64_DeadWalker", grounded: true);
        deadJumperSprite = PixelArt64Generator.GetRobotSprite(9, "Robot64_DeadJumper", grounded: true);
        deadTurretSprite = PixelArt64Generator.GetRobotSprite(10, "Robot64_DeadTurret", grounded: true);
        deadDroneSprite = PixelArt64Generator.GetRobotSprite(11, "Robot64_DeadDrone", grounded: true);
        robotSpritesReady = true;
    }

    private Sprite GetPrimaryIdleSprite()
    {
        switch (behaviorType)
        {
            case EnemyBehaviorType.Jumper: return jumperSprite1;
            case EnemyBehaviorType.Turret: return turretIdleSprite;
            case EnemyBehaviorType.FlyingDrone: return droneSprite1;
            default: return walkSprite1;
        }
    }

    private Sprite GetDeadSprite()
    {
        switch (behaviorType)
        {
            case EnemyBehaviorType.Jumper: return deadJumperSprite;
            case EnemyBehaviorType.Turret: return deadTurretSprite;
            case EnemyBehaviorType.FlyingDrone: return deadDroneSprite;
            default: return deadWalkerSprite;
        }
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
        behaviorTimer = 0f;
        jumpAirTimer = 0f;
        isJumping = false;
        deadFallVelocityY = 0f;
        isDeadFalling = false;

        Hazard2D hazard = GetComponent<Hazard2D>();
        if (hazard != null)
        {
            hazard.stompable = (behaviorType != EnemyBehaviorType.FlyingDrone);
        }

        if (boxCollider != null)
        {
            boxCollider.enabled = true;
            boxCollider.isTrigger = true;
            boxCollider.size = behaviorType == EnemyBehaviorType.FlyingDrone
                ? new Vector2(0.86f, 0.72f)
                : new Vector2(0.82f, 0.88f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = GetPrimaryIdleSprite();
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
                    : (currentHealth == 1 && !isDead && maxHealth > 1 ? new Color(1f, 0.84f, 0.84f, 1f) : Color.white);
            }
        }

        if (isDead)
        {
            if (isDeadFalling)
            {
                deadFallVelocityY -= 38f * Time.deltaTime;
                float nextY = transform.position.y + deadFallVelocityY * Time.deltaTime;
                if (nextY <= deadGroundTargetY)
                {
                    nextY = deadGroundTargetY;
                    if (deadFallVelocityY < -4.5f)
                    {
                        deadFallVelocityY = -deadFallVelocityY * 0.28f;
                        deathBounceTimer = 0.85f;
                    }
                    else
                    {
                        deadFallVelocityY = 0f;
                        isDeadFalling = false;
                        deathBounceTimer = 0.85f;
                    }
                }
                transform.position = new Vector3(transform.position.x, nextY, transform.position.z);
            }

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
            if (behaviorType != EnemyBehaviorType.Turret)
            {
                float recoilStep = hurtRecoilDir * 3.8f * Time.deltaTime;
                if (behaviorType == EnemyBehaviorType.FlyingDrone || HasGroundAhead(hurtRecoilDir))
                {
                    transform.position += new Vector3(recoilStep, 0f, 0f);
                }
            }
            float squashAmt = hurtRecoilTimer / 0.14f;
            transform.localScale = new Vector3(1f - squashAmt * 0.18f, 1f + squashAmt * 0.22f, 1f);
            return;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        switch (behaviorType)
        {
            case EnemyBehaviorType.Jumper:
                UpdateJumperBehavior();
                break;

            case EnemyBehaviorType.Turret:
                UpdateTurretBehavior();
                break;

            case EnemyBehaviorType.FlyingDrone:
                UpdateFlyingDroneBehavior();
                break;

            default:
                UpdatePatrolWalkerBehavior();
                break;
        }
    }

    private void UpdatePatrolWalkerBehavior()
    {
        StepHorizontalPatrol(moveSpeed, checkGround: true);

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

    private void UpdateJumperBehavior()
    {
        if (isJumping)
        {
            jumpAirTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpAirTimer / jumpAirDuration);
            float arcY = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            StepHorizontalPatrol(moveSpeed * 1.15f, checkGround: false);
            transform.position = new Vector3(transform.position.x, startPosition.y + arcY, startPosition.z);

            float stretch = Mathf.Cos(t * Mathf.PI) * 0.14f;
            transform.localScale = new Vector3(1f - Mathf.Abs(stretch) * 0.5f, 1f + Mathf.Abs(stretch), 1f);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = jumperSprite1;
                spriteRenderer.flipX = (currentDirection < 0);
            }

            if (jumpAirTimer >= jumpAirDuration)
            {
                isJumping = false;
                jumpAirTimer = 0f;
                behaviorTimer = 0f;
                transform.position = new Vector3(transform.position.x, startPosition.y, startPosition.z);
                transform.localScale = Vector3.one;
            }
            return;
        }

        behaviorTimer += Time.deltaTime;
        float timeRemaining = jumpInterval - behaviorTimer;

        // Telegraphing do pulo: agacha nos últimos 0.28s antes de saltar
        if (timeRemaining <= jumpTelegraphDuration)
        {
            float prepT = 1f - Mathf.Clamp01(timeRemaining / jumpTelegraphDuration);
            transform.localScale = new Vector3(1f + prepT * 0.22f, 1f - prepT * 0.24f, 1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = jumperSprite2;
                spriteRenderer.flipX = (currentDirection < 0);
            }
        }
        else
        {
            StepHorizontalPatrol(moveSpeed * 0.75f, checkGround: true);
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = jumperSprite1;
                spriteRenderer.flipX = (currentDirection < 0);
            }
        }

        if (behaviorTimer >= jumpInterval)
        {
            isJumping = true;
            jumpAirTimer = 0f;
            behaviorTimer = 0f;
        }
    }

    private void UpdateTurretBehavior()
    {
        BoxPlayer2D player = FindAnyObjectByType<BoxPlayer2D>();
        bool playerInRange = false;
        if (player != null && player.CurrentGameState == GameState.Playing)
        {
            float dx = player.transform.position.x - transform.position.x;
            float dy = Mathf.Abs(player.transform.position.y - transform.position.y);
            currentDirection = (dx >= 0f) ? 1 : -1;
            playerInRange = (Mathf.Abs(dx) <= turretDetectionRange && dy <= 3.8f);
        }

        if (spriteRenderer != null)
        {
            // O sprite da torreta aponta o canhão para a direita (flipX = true quando olha para a esquerda)
            spriteRenderer.flipX = (currentDirection < 0);
        }

        if (!playerInRange)
        {
            behaviorTimer = Mathf.Min(behaviorTimer, turretFireInterval - turretTelegraphDuration - 0.1f);
            if (spriteRenderer != null) spriteRenderer.sprite = turretIdleSprite;
            return;
        }

        behaviorTimer += Time.deltaTime;
        float timeToShot = turretFireInterval - behaviorTimer;

        // Telegraphing de 0,5 s antes do disparo!
        if (timeToShot <= turretTelegraphDuration)
        {
            float chargePulse = 1f + Mathf.Sin(Time.time * 28f) * 0.08f;
            transform.localScale = new Vector3(chargePulse, 2f - chargePulse, 1f);
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = turretChargeSprite;
                if (hitFlashTimer <= 0f)
                {
                    bool flash = Mathf.FloorToInt(Time.time * 16f) % 2 == 0;
                    spriteRenderer.color = flash ? new Color(1f, 0.86f, 0.38f, 1f) : Color.white;
                }
            }
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = turretIdleSprite;
                if (hitFlashTimer <= 0f)
                {
                    spriteRenderer.color = (currentHealth == 1 && maxHealth > 1) ? new Color(1f, 0.84f, 0.84f, 1f) : Color.white;
                }
            }
        }

        if (behaviorTimer >= turretFireInterval)
        {
            behaviorTimer = 0f;
            transform.localScale = Vector3.one;
            if (spriteRenderer != null && hitFlashTimer <= 0f) spriteRenderer.color = Color.white;

            Vector3 muzzlePos = transform.position + new Vector3(currentDirection * 0.56f, 0.28f, 0f);
            TurretProjectile2D.SpawnProjectile(muzzlePos, currentDirection, turretProjectileSpeed);
        }
    }

    private void UpdateFlyingDroneBehavior()
    {
        behaviorTimer += Time.deltaTime;
        StepHorizontalPatrol(moveSpeed, checkGround: false);

        float bobY = Mathf.Sin(behaviorTimer * flyFrequency) * flyAmplitude;
        transform.position = new Vector3(transform.position.x, startPosition.y + bobY, startPosition.z);

        animTimer += Time.deltaTime * 10f;
        if (animTimer >= 1f)
        {
            animTimer -= 1f;
            animFrame = 1 - animFrame;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = (animFrame == 0) ? droneSprite1 : droneSprite2;
            spriteRenderer.flipX = (currentDirection < 0);
        }
    }

    private void StepHorizontalPatrol(float speed, bool checkGround)
    {
        transform.position += new Vector3(currentDirection * speed * Time.deltaTime, 0f, 0f);

        float offsetFromStart = transform.position.x - startPosition.x;
        if (offsetFromStart > patrolRange && currentDirection > 0)
        {
            currentDirection = -1;
        }
        else if (offsetFromStart < -patrolRange && currentDirection < 0)
        {
            currentDirection = 1;
        }

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

        if (blockedAhead || (checkGround && !HasGroundAhead(currentDirection)))
        {
            currentDirection = -currentDirection;
        }
    }

    private bool HasGroundAhead(int dir)
    {
        Vector2 probeCenter = new Vector2(transform.position.x + dir * 0.42f, startPosition.y - 0.62f);
        Collider2D[] groundHits = Physics2D.OverlapBoxAll(probeCenter, new Vector2(0.16f, 0.22f), 0f);
        foreach (Collider2D hit in groundHits)
        {
            if (hit != boxCollider && !hit.isTrigger && hit.GetComponent<Hazard2D>() == null)
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
        GameAudio2D.PlayStomp();

        // Se o robô morreu enquanto estava no ar (ex.: Robô Voador ou Robô Saltador no meio do pulo),
        // aplica gravidade para o corpo cair até o chão ou plataforma abaixo!
        deadGroundTargetY = FindSupportGroundY();
        if (transform.position.y > deadGroundTargetY + 0.04f)
        {
            isDeadFalling = true;
            deadFallVelocityY = 2.4f;
        }
        else
        {
            isDeadFalling = false;
            deadFallVelocityY = 0f;
            transform.position = new Vector3(transform.position.x, deadGroundTargetY, transform.position.z);
        }

        BoxPlayer2D player = FindAnyObjectByType<BoxPlayer2D>();
        if (player != null)
        {
            player.AddScore(250);
        }

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = GetDeadSprite();
            spriteRenderer.color = Color.white;
        }
    }

    private float FindSupportGroundY()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(transform.position.x, transform.position.y + 0.2f), Vector2.down, 18f);
        float bestSurfaceY = float.MinValue;

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D col = hit.collider;
            if (col == null || col == boxCollider || col.isTrigger) continue;
            if (col.GetComponent<BoxPlayer2D>() != null || col.GetComponent<Hazard2D>() != null) continue;

            float topY = col.bounds.max.y;
            if (topY <= transform.position.y + 0.35f && topY > bestSurfaceY)
            {
                bestSurfaceY = topY;
            }
        }

        if (bestSurfaceY > float.MinValue * 0.5f)
        {
            return bestSurfaceY + 0.46f;
        }

        return LevelBuilder2D.GroundTopY + 0.46f;
    }
}

/// <summary>
/// Projétil de plasma disparado pela Torreta (EnemyBehaviorType.Turret) após o telegraphing de 0,5 s.
/// </summary>
public class TurretProjectile2D : MonoBehaviour
{
    public float speed = 8.2f;
    public float maxLifetime = 2.2f;
    private int direction = -1;
    private float timer;
    private static Sprite cachedOrbSprite;

    public static void SpawnProjectile(Vector3 spawnPos, int dir, float projSpeed)
    {
        GameObject obj = new GameObject("TurretPlasmaShot");
        obj.transform.position = spawnPos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrbSprite();
        sr.sortingOrder = 8;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.36f, 0.36f);

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        Hazard2D hazard = obj.AddComponent<Hazard2D>();
        hazard.damage = 1;
        hazard.stompable = false;
        hazard.blocksGroundCheck = true;

        TurretProjectile2D proj = obj.AddComponent<TurretProjectile2D>();
        proj.direction = dir >= 0 ? 1 : -1;
        proj.speed = projSpeed;

        LaserProjectile2D.SpawnImpactSpark(spawnPos, 0.08f, new Color(1f, 0.55f, 0.20f, 1f));
    }

    private void Update()
    {
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        timer += Time.deltaTime;
        if (timer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<RobotEnemy2D>() != null || other.GetComponent<TurretProjectile2D>() != null)
        {
            return;
        }

        if (other.GetComponent<BoxPlayer2D>() != null)
        {
            LaserProjectile2D.SpawnImpactSpark(transform.position, 0.10f, new Color(1f, 0.35f, 0.25f, 1f));
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            LaserProjectile2D.SpawnImpactSpark(transform.position, 0.08f, new Color(1f, 0.65f, 0.25f, 1f));
            Destroy(gameObject);
        }
    }

    private static Sprite GetOrbSprite()
    {
        if (cachedOrbSprite != null) return cachedOrbSprite;

        const int s = 14;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("TurretPlasmaOrb14", s, s);
        Vector2 c = new Vector2(6.5f, 6.5f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d > 6.2f) tex.SetPixel(x, y, Color.clear);
                else if (d > 5.0f) tex.SetPixel(x, y, new Color(0.18f, 0.05f, 0.08f, 1f));
                else if (d > 3.2f) tex.SetPixel(x, y, new Color(0.98f, 0.24f, 0.22f, 1f));
                else if (d > 1.6f) tex.SetPixel(x, y, new Color(1.00f, 0.76f, 0.22f, 1f));
                else tex.SetPixel(x, y, new Color(1.00f, 0.98f, 0.86f, 1f));
            }
        }

        tex.Apply();
        cachedOrbSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        return cachedOrbSprite;
    }
}
