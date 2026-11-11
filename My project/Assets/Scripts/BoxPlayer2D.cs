using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class BoxPlayer2D : MonoBehaviour
{
    [Header("Personagem Escolhido")]
    public PlayerCharacterType selectedCharacter = PlayerCharacterType.BoyLaser;

    [Header("Vida (Corações)")]
    public int maxHearts = 3;
    public float invincibilityDuration = 1.0f;

    [Header("Movimento (WASD)")]
    public float moveSpeed = 7.5f;
    public float fastFallMultiplier = 2.0f;

    [Header("Pulo (Barra de Espaço)")]
    public float jumpForce = 14.5f;
    public float gravityScale = 4.0f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("Armas e Poderes (Tecla Q / X / RT)")]
    public float fireCooldown = 0.15f;
    public float scratchCooldown = 0.26f;

    [Header("Combo de Pisão (Escalada da Kitty)")]
    public float stompComboBounce1 = 0.86f;
    public float stompComboBounce2 = 1.04f;
    public float stompComboBounce3 = 1.16f;

    [Header("Progressão de Fase e Poder Desbloqueável")]
    public int currentStage = 1;
    public bool hasUnlockedStage2Power;

    [Header("Respawn e Checkpoints")]
    public bool respawnAtCheckpointOnFullDeath = false;
    public float abyssDeathY = -8.0f;
    public float groundedVisualOffsetY = -0.02f;

    [Header("Efeito Elástico (Borracha)")]
    public float springStiffness = 190f;
    public float springDamping = 11f;
    public float landingElasticIntensity = 0.65f;

    [Header("Referências Visuais")]
    public Transform visualChild;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private PixelHeroAnimator2D heroAnimator;
    private SpriteRenderer heroSpriteRenderer;
    private Vector3 startPosition;
    private Vector3 lastSafeGroundPosition;
    private Vector3 activeCheckpointPosition;
    private bool hasActiveCheckpoint;
    private float horizontalInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float fireTimer;
    private float scratchTimer;
    private float invincibilityTimer;
    private float knockbackTimer;
    private int currentHearts;
    private int coinsCollected;
    private int score;
    private int stompComboCount;
    private MovingPlatform2D activeMovingPlatform;
    private Vector2 pausedVelocity;
    private bool isGrounded;
    private bool wasGrounded = true;
    private GameState currentGameState = GameState.CharacterSelect;
    private float loadingTimer;
    private const float LoadingDuration = 3.0f;
    private int attempts = 1;

    // Variáveis do efeito de borracha (mola amortecida)
    private float squash;
    private float squashVelocity;
    private float maxAirFallSpeed;

    public GameState CurrentGameState => currentGameState;
    public int CurrentHearts => currentHearts;
    public int CoinsCollected => coinsCollected;
    public int Score => score;
    public int StompComboCount => stompComboCount;
    public int Attempts => attempts;

    public void AddScore(int points)
    {
        score += Mathf.Max(0, points);
    }

    public void AddBonusCoinsAndScore(int bonusCoins, int bonusScore)
    {
        coinsCollected += Mathf.Max(0, bonusCoins);
        score += Mathf.Max(0, bonusScore);
    }
    public float LoadingProgress => Mathf.Clamp01(loadingTimer / LoadingDuration);
    public int NextStageNumber => Mathf.Min(3, currentStage + 1);
    public bool CanSkipLoading => loadingTimer >= 1.15f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("PlayerNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };
        boxCollider.sharedMaterial = noFriction;

        startPosition = transform.position;
        lastSafeGroundPosition = startPosition;
        activeCheckpointPosition = startPosition;
        hasActiveCheckpoint = false;
        currentHearts = maxHearts;
        SetupHeroVisual();
        LevelBuilder2D.EnsureRobotsInScene();
        LevelBuilder2D.EnsureCheckpointsInScene();

        GameUI2D ui = GetComponent<GameUI2D>();
        if (ui == null)
        {
            ui = gameObject.AddComponent<GameUI2D>();
        }
        ui.player = this;

        currentGameState = GameState.CharacterSelect;
        rb.simulated = false;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying) return;
            if (visualChild != null)
            {
                SetupHeroVisual();
            }
        };
#endif
    }

    public void SetStageStartPosition(Vector3 stageStart)
    {
        startPosition = stageStart;
    }

    public void SetupHeroVisual()
    {
        if (visualChild == null)
        {
            Transform found = transform.Find("VisualBox");
            if (found != null) visualChild = found;
        }

        if (visualChild == null) return;

        ColoredBox2D oldBox = visualChild.GetComponent<ColoredBox2D>();
        if (oldBox != null)
        {
            oldBox.enabled = false;
        }

        heroSpriteRenderer = visualChild.GetComponent<SpriteRenderer>();
        heroAnimator = visualChild.GetComponent<PixelHeroAnimator2D>();
        if (heroAnimator == null)
        {
            heroAnimator = visualChild.gameObject.AddComponent<PixelHeroAnimator2D>();
        }
        heroAnimator.SetCharacterType(selectedCharacter);
    }

    public void SelectAndStartGame(PlayerCharacterType character)
    {
        selectedCharacter = character;
        if (heroAnimator != null)
        {
            heroAnimator.SetCharacterType(selectedCharacter);
        }
        currentGameState = GameState.Playing;
        currentStage = 1;
        hasUnlockedStage2Power = false;
        attempts = 1;
        hasActiveCheckpoint = false;
        LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: 1);
        ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
        GameAudio2D.EnsureMusicTheme(1);
    }

    public void ReturnToStage1FromModal()
    {
        currentStage = 1;
        hasUnlockedStage2Power = false;
        hasActiveCheckpoint = false;
        LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: 1);
        ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
        GameAudio2D.EnsureMusicTheme(1);
    }

    public void EnterCharacterSelectScreen()
    {
        currentGameState = GameState.CharacterSelect;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public void PauseGame()
    {
        if (currentGameState != GameState.Playing) return;
        currentGameState = GameState.Paused;
        pausedVelocity = rb.linearVelocity;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public void ResumeFromPause()
    {
        if (currentGameState != GameState.Paused) return;
        currentGameState = GameState.Playing;
        rb.simulated = true;
        rb.linearVelocity = pausedVelocity;
    }

    private void OnDisable()
    {
        GameInput2D.StopRumble();
    }

    private void OnApplicationQuit()
    {
        GameInput2D.StopRumble();
    }

    private void Update()
    {
        GameInput2D.Tick(Time.deltaTime);

        // 1. TELA DE INÍCIO (Seleção de Personagem)
        if (currentGameState == GameState.CharacterSelect)
        {
            if (GameInput2D.MenuLeftPressed())
            {
                selectedCharacter = PlayerCharacterType.BoyLaser;
                if (heroAnimator != null) heroAnimator.SetCharacterType(selectedCharacter);
                GameInput2D.TriggerRumble(0.08f, 0.14f, 0.04f);
            }
            else if (GameInput2D.MenuRightPressed())
            {
                selectedCharacter = PlayerCharacterType.GirlKitty;
                if (heroAnimator != null) heroAnimator.SetCharacterType(selectedCharacter);
                GameInput2D.TriggerRumble(0.08f, 0.14f, 0.04f);
            }
            else if (GameInput2D.MenuConfirmPressed())
            {
                GameInput2D.TriggerRumble(0.15f, 0.25f, 0.06f);
                SelectAndStartGame(selectedCharacter);
            }
            else if (GameInput2D.BackOrQuitPressed())
            {
                QuitGame();
            }

            UpdateVisualEffect();
            GameInput2D.LateTickState();
            return;
        }

        // 1.5 TELA DE LOADING E NOVO PODER (Transição Fase 1 -> Fase 2 Procedural Diurna)
        if (currentGameState == GameState.StageTransitionLoading)
        {
            loadingTimer += Time.deltaTime;
            bool skipRequested = loadingTimer >= 0.85f && GameInput2D.MenuConfirmPressed();
            if (loadingTimer >= LoadingDuration || skipRequested)
            {
                StartProceduralStage(currentStage + 1);
            }

            UpdateVisualEffect();
            GameInput2D.LateTickState();
            return;
        }

        // 2. TELA DE VITÓRIA OU DERROTA
        if (currentGameState == GameState.Lost || currentGameState == GameState.Won)
        {
            bool wasLost = (currentGameState == GameState.Lost);
            if (GameInput2D.ModalRestartPressed())
            {
                ResetPlayer(countAttempt: wasLost, rebuildProceduralIfStage2: currentStage >= 2);
            }
            else if (currentStage >= 2 && Input.GetKeyDown(KeyCode.Alpha1))
            {
                ReturnToStage1FromModal();
            }
            else if (GameInput2D.SwitchCharacterPressed())
            {
                EnterCharacterSelectScreen();
            }
            else if (GameInput2D.BackOrQuitPressed())
            {
                QuitGame();
            }

            UpdateVisualEffect();
            GameInput2D.LateTickState();
            return;
        }

        // 2.5 MENU DE PAUSA (Fase 5)
        if (currentGameState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || GameInput2D.MenuConfirmPressed())
            {
                ResumeFromPause();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
            }
            else if (GameInput2D.SwitchCharacterPressed())
            {
                EnterCharacterSelectScreen();
            }
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus) || GameInput2D.MenuLeftPressed())
            {
                GameAudio2D.MasterVolume = Mathf.Clamp01(GameAudio2D.MasterVolume - 0.10f);
                GameAudio2D.PlayCoin();
            }
            else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus) || GameInput2D.MenuRightPressed())
            {
                GameAudio2D.MasterVolume = Mathf.Clamp01(GameAudio2D.MasterVolume + 0.10f);
                GameAudio2D.PlayCoin();
            }

            UpdateVisualEffect();
            GameInput2D.LateTickState();
            return;
        }

        // Pausar com ESC ou P / Start durante a partida
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            PauseGame();
            GameInput2D.LateTickState();
            return;
        }
        if (GameInput2D.GameplayMenuPressed())
        {
            PauseGame();
            GameInput2D.LateTickState();
            return;
        }

        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
        }

        horizontalInput = GameInput2D.ReadHorizontalMovement();

        if (fireTimer > 0f) fireTimer -= Time.deltaTime;
        if (scratchTimer > 0f) scratchTimer -= Time.deltaTime;

        if (GameInput2D.FirePressedThisFrame())
        {
            if (selectedCharacter == PlayerCharacterType.BoyLaser && fireTimer <= 0f)
            {
                ShootLaser();
            }
            else if (selectedCharacter == PlayerCharacterType.GirlKitty && hasUnlockedStage2Power && scratchTimer <= 0f)
            {
                PerformKittyScratch();
            }
        }

        wasGrounded = isGrounded;
        CheckGrounded();

        if (isGrounded && knockbackTimer <= 0f)
        {
            RecordSafeGroundPositionIfClear();
        }

        if (!isGrounded)
        {
            if (-rb.linearVelocity.y > maxAirFallSpeed)
            {
                maxAirFallSpeed = -rb.linearVelocity.y;
            }
        }

        if (!wasGrounded && isGrounded)
        {
            float impactFactor = Mathf.Clamp(maxAirFallSpeed / 13f, 0.55f, 1.35f);
            squash = landingElasticIntensity * impactFactor;
            squashVelocity = 3.2f * impactFactor;
            PlayerDustEffect2D.SpawnLandingDust(transform.position - new Vector3(0f, 0.45f, 0f), impactFactor);
            GameAudio2D.PlayLand();
            maxAirFallSpeed = 0f;

            if (visualChild != null)
            {
                visualChild.localRotation = Quaternion.identity;
            }
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (GameInput2D.JumpPressedThisFrame())
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f && knockbackTimer <= 0f)
        {
            Jump();
        }

        if (GameInput2D.JumpReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.55f);
        }

        if (!isGrounded && GameInput2D.FastFallHeld())
        {
            rb.gravityScale = gravityScale * fastFallMultiplier;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }

        // Caiu no buraco / fora da fase -> perde 1 coração e volta ao Checkpoint ativo (ou último ponto seguro)
        if (transform.position.y < abyssDeathY)
        {
            GameAudio2D.PlayHurt();
            GameInput2D.TriggerRumble(0.55f, 0.70f, 0.20f);
            CameraFollow2D.TriggerShake(0.35f);
            currentHearts--;
            if (currentHearts <= 0)
            {
                currentHearts = 0;
                TriggerLose();
            }
            else
            {
                Vector3 respawnPos = GetActiveRespawnPosition();
                transform.position = respawnPos + new Vector3(0f, 0.35f, 0f);
                rb.linearVelocity = Vector2.zero;
                invincibilityTimer = invincibilityDuration;
                squash = 0.50f;
            }
        }

        UpdateVisualEffect();
        GameInput2D.LateTickState();
    }

    private Vector3 GetActiveRespawnPosition()
    {
        if (hasActiveCheckpoint)
        {
            return activeCheckpointPosition;
        }
        return lastSafeGroundPosition;
    }

    private void RecordSafeGroundPositionIfClear()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 1.35f);
        foreach (Collider2D col in nearby)
        {
            if (col.GetComponent<Hazard2D>() != null || col.GetComponent<RobotEnemy2D>() != null)
            {
                return;
            }
        }

        Vector2 downLeft = new Vector2(transform.position.x - 0.5f, transform.position.y - 0.75f);
        Vector2 downRight = new Vector2(transform.position.x + 0.5f, transform.position.y - 0.75f);
        if (Physics2D.OverlapPoint(downLeft) != null && Physics2D.OverlapPoint(downRight) != null)
        {
            lastSafeGroundPosition = transform.position;
        }
    }

    private void FixedUpdate()
    {
        if (currentGameState != GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (knockbackTimer > 0f)
        {
            return;
        }

        Vector2 platformVel = (isGrounded && activeMovingPlatform != null)
            ? activeMovingPlatform.CurrentVelocity
            : Vector2.zero;

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed + platformVel.x, rb.linearVelocity.y);
    }

    private void ShootLaser()
    {
        fireTimer = fireCooldown;

        if (heroAnimator == null) SetupHeroVisual();

        int dir = heroAnimator != null ? heroAnimator.FacingDirection : 1;
        Vector3 muzzlePos = heroAnimator != null
            ? heroAnimator.GetGunMuzzleWorldPosition()
            : transform.position + new Vector3(dir * 0.5f, 0f, 0f);

        LaserProjectile2D.SpawnLaser(muzzlePos, dir);
        GameAudio2D.PlayLaser();

        if (heroAnimator != null)
        {
            heroAnimator.TriggerShootEffect();
        }

        GameInput2D.TriggerRumble(0.15f, 0.30f, 0.06f);
        squash = Mathf.Max(squash, 0.14f);
    }

    private void PerformKittyScratch()
    {
        scratchTimer = scratchCooldown;

        if (heroAnimator == null) SetupHeroVisual();

        int dir = heroAnimator != null ? heroAnimator.FacingDirection : 1;
        Vector3 slashPos = heroAnimator != null
            ? heroAnimator.GetScratchSpawnWorldPosition()
            : transform.position + new Vector3(dir * 0.68f, 0.02f, 0f);

        if (heroAnimator != null)
        {
            heroAnimator.TriggerScratchAttack();
        }

        ScratchSlashEffect2D.SpawnScratchSlash(slashPos, dir);
        GameAudio2D.PlayLaser();

        GameInput2D.TriggerRumble(0.18f, 0.35f, 0.07f);
        squash = Mathf.Max(squash, 0.24f);
        squashVelocity = 1.6f;
    }

    private void Jump()
    {
        PlayerDustEffect2D.SpawnJumpDust(transform.position - new Vector3(0f, 0.45f, 0f));
        GameAudio2D.PlayJump();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;
        wasGrounded = false;
        maxAirFallSpeed = 0f;

        squash = -0.35f;
        squashVelocity = -1.2f;
    }

    private void CheckGrounded()
    {
        Bounds bounds = boxCollider.bounds;
        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - 0.06f);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.88f, 0.1f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        isGrounded = false;
        activeMovingPlatform = null;

        foreach (Collider2D hit in hits)
        {
            if (hit == boxCollider || hit.isTrigger) continue;

            Hazard2D hazard = hit.GetComponent<Hazard2D>();
            if (hazard != null && hazard.blocksGroundCheck) continue;

            // Plataforma de mão única (PlatformEffector2D): só considera pousado se não estiver subindo
            // e se a sola do jogador estiver acima da superfície superior da plataforma
            if (hit.GetComponent<PlatformEffector2D>() != null)
            {
                if (rb.linearVelocity.y > 0.15f || bounds.min.y < hit.bounds.max.y - 0.16f)
                {
                    continue;
                }
            }

            // Bloco Frágil (BreakableBlock2D): Kitty (ou Menino com poder da Fase 2) quebra com Pisão-Martelo (S / Baixo no ar ou sobre o bloco)
            BreakableBlock2D breakable = hit.GetComponent<BreakableBlock2D>();
            if (breakable != null && !breakable.IsBroken)
            {
                bool canHammerStomp = (selectedCharacter == PlayerCharacterType.GirlKitty)
                    || (selectedCharacter == PlayerCharacterType.BoyLaser && hasUnlockedStage2Power);
                if (canHammerStomp && GameInput2D.FastFallHeld() && bounds.min.y >= breakable.transform.position.y + 0.08f)
                {
                    TriggerHammerStompBreak(breakable);
                    return;
                }
            }

            isGrounded = true;
            stompComboCount = 0;
            MovingPlatform2D movingPlat = hit.GetComponent<MovingPlatform2D>();
            if (movingPlat != null)
            {
                activeMovingPlatform = movingPlat;
            }
            break;
        }
    }

    private void TriggerHammerStompBreak(BreakableBlock2D breakable)
    {
        Vector3 blockPos = breakable.transform.position;
        breakable.BreakBlock(this);
        StompShockwaveEffect2D.SpawnStompWave(blockPos + new Vector3(0f, 0.20f, 0f));

        if (heroAnimator != null)
        {
            heroAnimator.TriggerStompEffect();
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.72f);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;
        wasGrounded = false;
        maxAirFallSpeed = 0f;
        squash = 0.50f;
        squashVelocity = 2.6f;
    }

    private void UpdateVisualEffect()
    {
        if (visualChild == null) return;

        bool isMenuOrModal = (currentGameState != GameState.Playing);
        bool isIdlePoseState = (currentGameState == GameState.CharacterSelect || currentGameState == GameState.StageTransitionLoading);

        if (heroAnimator == null) SetupHeroVisual();
        if (heroAnimator != null)
        {
            float animHoriz = isMenuOrModal ? 0f : horizontalInput;
            bool animGrounded = isIdlePoseState || isGrounded;
            float vertVel = (rb != null && rb.simulated) ? rb.linearVelocity.y : 0f;
            heroAnimator.UpdateAnimation(animHoriz, animGrounded, Time.deltaTime, vertVel, hasUnlockedStage2Power);
        }

        if (heroSpriteRenderer != null)
        {
            if (invincibilityTimer > 0f && currentGameState == GameState.Playing)
            {
                bool flash = Mathf.FloorToInt(invincibilityTimer * 18f) % 2 == 0;
                heroSpriteRenderer.color = flash ? new Color(1f, 0.35f, 0.35f, 0.75f) : Color.white;
            }
            else
            {
                heroSpriteRenderer.color = Color.white;
            }
        }

        float springForce = (-springStiffness * squash) - (springDamping * squashVelocity);
        squashVelocity += springForce * Time.deltaTime;
        squash += squashVelocity * Time.deltaTime;

        float scaleX = Mathf.Clamp(1f + squash * 0.95f, 0.60f, 1.75f);
        float scaleY = Mathf.Clamp(1f - squash * 0.75f, 0.45f, 1.55f);

        visualChild.localScale = new Vector3(scaleX, scaleY, 1f);
        visualChild.localRotation = Quaternion.identity;

        if (isGrounded || isIdlePoseState)
        {
            visualChild.localPosition = new Vector3(0f, groundedVisualOffsetY + (scaleY - 1f) * 0.5f, 0f);
        }
        else
        {
            visualChild.localPosition = Vector3.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckHazardObject(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckHazardObject(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentGameState != GameState.Playing) return;

        Coin2D coin = other.GetComponent<Coin2D>();
        if (coin != null && !coin.IsCollected)
        {
            int val = coin.Collect();
            coinsCollected += val;
            score += val * 100;
            GameInput2D.TriggerRumble(0.06f, 0.12f, 0.03f);
            squash = Mathf.Max(squash, 0.12f);
            return;
        }

        SpringPad2D spring = other.GetComponent<SpringPad2D>();
        if (spring != null && spring.CanTrigger && rb.linearVelocity.y <= 3.0f)
        {
            TriggerSpringBounce(spring);
            return;
        }

        Switch2D switchObj = other.GetComponent<Switch2D>();
        if (switchObj != null && !switchObj.isActivated)
        {
            bool canStepActivate = (selectedCharacter == PlayerCharacterType.GirlKitty)
                || (selectedCharacter == PlayerCharacterType.BoyLaser && hasUnlockedStage2Power)
                || (rb.linearVelocity.y <= 0.5f && transform.position.y >= switchObj.transform.position.y);
            if (canStepActivate)
            {
                switchObj.ActivateSwitch(this);
                squash = Mathf.Max(squash, 0.25f);
                return;
            }
        }

        Checkpoint2D checkpoint = other.GetComponent<Checkpoint2D>();
        if (checkpoint != null)
        {
            if (!checkpoint.isActivated)
            {
                checkpoint.Activate();
                GameInput2D.TriggerRumble(0.10f, 0.20f, 0.05f);
            }
            activeCheckpointPosition = checkpoint.GetRespawnWorldPosition();
            lastSafeGroundPosition = activeCheckpointPosition;
            hasActiveCheckpoint = true;
            return;
        }

        Goal2D goal = other.GetComponent<Goal2D>();
        if (goal != null)
        {
            if (currentStage < 3)
            {
                TriggerStageCompleteLoading();
            }
            else
            {
                TriggerWin();
            }
            return;
        }

        CheckHazardObject(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (currentGameState != GameState.Playing) return;

        SpringPad2D spring = other.GetComponent<SpringPad2D>();
        if (spring != null && spring.CanTrigger && rb.linearVelocity.y <= 1.5f)
        {
            TriggerSpringBounce(spring);
            return;
        }

        Switch2D switchObj = other.GetComponent<Switch2D>();
        if (switchObj != null && !switchObj.isActivated)
        {
            bool canStepActivate = (selectedCharacter == PlayerCharacterType.GirlKitty)
                || (selectedCharacter == PlayerCharacterType.BoyLaser && hasUnlockedStage2Power);
            if (canStepActivate)
            {
                switchObj.ActivateSwitch(this);
                squash = Mathf.Max(squash, 0.25f);
                return;
            }
        }

        CheckHazardObject(other.gameObject);
    }

    private void TriggerSpringBounce(SpringPad2D spring)
    {
        spring.TriggerSpring();
        GameAudio2D.PlaySpring();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, spring.bounceVelocity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;
        wasGrounded = false;
        activeMovingPlatform = null;
        maxAirFallSpeed = 0f;

        GameInput2D.TriggerRumble(0.20f, 0.36f, 0.08f);
        CameraFollow2D.TriggerShake(0.18f);
        squash = -0.45f;
        squashVelocity = -1.6f;
    }

    private void CheckHazardObject(GameObject obj)
    {
        if (currentGameState != GameState.Playing) return;

        Hazard2D hazard = obj.GetComponent<Hazard2D>();
        RobotEnemy2D robot = obj.GetComponent<RobotEnemy2D>();

        if (robot != null)
        {
            if (robot.IsDead) return;

            // Robô Voador (FlyingDrone): seu topo tem espinhos elétricos, mas a Kitty (ou o Menino)
            // pode derrubá-lo pulando por baixo dele!
            if (robot.behaviorType == EnemyBehaviorType.FlyingDrone)
            {
                bool hitFromBelow = transform.position.y < robot.transform.position.y - 0.08f && rb.linearVelocity.y > -0.8f;
                if (hitFromBelow)
                {
                    robot.StompKill();
                    StompShockwaveEffect2D.SpawnStompWave(robot.transform.position - new Vector3(0f, 0.18f, 0f));
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -1.6f);
                    GameInput2D.TriggerRumble(0.22f, 0.38f, 0.08f);
                    CameraFollow2D.TriggerShake(0.24f);
                    squash = -0.35f;
                    squashVelocity = -1.4f;
                    return;
                }
            }

            bool isStompable = (hazard == null || hazard.stompable);
            bool canStompEnemy = isStompable && (
                (selectedCharacter == PlayerCharacterType.GirlKitty)
                || (selectedCharacter == PlayerCharacterType.BoyLaser && hasUnlockedStage2Power)
            );

            if (canStompEnemy)
            {
                bool isAboveRobot = transform.position.y > robot.transform.position.y + 0.18f && rb.linearVelocity.y <= 1.0f;
                if (isAboveRobot)
                {
                    robot.StompKill();
                    stompComboCount++;

                    float comboMultiplier = stompComboCount == 1
                        ? stompComboBounce1
                        : (stompComboCount == 2 ? stompComboBounce2 : stompComboBounce3);

                    if (stompComboCount >= 2)
                    {
                        score += (stompComboCount - 1) * 150;
                    }

                    StompShockwaveEffect2D.SpawnStompWave(robot.transform.position + new Vector3(0f, 0.15f, 0f));

                    if (heroAnimator != null)
                    {
                        heroAnimator.TriggerStompEffect();
                    }

                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * comboMultiplier);
                    isGrounded = false;
                    wasGrounded = false;

                    GameInput2D.TriggerRumble(0.25f, 0.42f, 0.09f);
                    CameraFollow2D.TriggerShake(0.28f + Mathf.Min(0.18f, (stompComboCount - 1) * 0.08f));
                    squash = 0.52f;
                    squashVelocity = 2.9f;
                    return;
                }
            }

            if (invincibilityTimer <= 0f)
            {
                int dmg = hazard != null ? Mathf.Max(1, hazard.damage) : 1;
                TakeDamage(robot.transform.position, dmg);
            }
            return;
        }

        if (hazard != null && invincibilityTimer <= 0f)
        {
            TakeDamage(obj.transform.position, Mathf.Max(1, hazard.damage));
        }
    }

    private void TakeDamage(Vector3 hazardPos, int damageAmount = 1)
    {
        if (invincibilityTimer > 0f || currentGameState != GameState.Playing) return;

        currentHearts -= Mathf.Max(1, damageAmount);
        GameAudio2D.PlayHurt();
        GameInput2D.TriggerRumble(0.55f, 0.72f, 0.18f);
        CameraFollow2D.TriggerShake(0.38f);
        squash = 0.55f;
        squashVelocity = 2.5f;

        if (currentHearts <= 0)
        {
            currentHearts = 0;
            TriggerLose();
        }
        else
        {
            invincibilityTimer = invincibilityDuration;
            knockbackTimer = 0.18f;
            float knockDir = (transform.position.x < hazardPos.x) ? -1f : 1f;
            rb.linearVelocity = new Vector2(knockDir * 5.8f, 9.5f);
        }
    }

    private void TriggerStageCompleteLoading()
    {
        hasUnlockedStage2Power = true;
        GameAudio2D.PlayWin();
        currentGameState = GameState.StageTransitionLoading;
        loadingTimer = 0f;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GameInput2D.TriggerRumble(0.28f, 0.52f, 0.16f);
    }

    private void StartProceduralStage(int stageNumber)
    {
        currentGameState = GameState.Playing;
        currentStage = stageNumber;
        hasUnlockedStage2Power = true;
        hasActiveCheckpoint = false;
        LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: stageNumber);
        ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
        GameAudio2D.EnsureMusicTheme(stageNumber);
    }

    private void TriggerLose()
    {
        currentGameState = GameState.Lost;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    private void TriggerWin()
    {
        GameAudio2D.PlayWin();
        currentGameState = GameState.Won;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public void ResetPlayer(bool countAttempt, bool rebuildProceduralIfStage2 = false)
    {
        currentGameState = GameState.Playing;
        if (countAttempt) attempts++;

        if (rebuildProceduralIfStage2 && currentStage >= 2)
        {
            hasActiveCheckpoint = false;
            LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: currentStage);
        }

        currentHearts = maxHearts;
        invincibilityTimer = 0f;
        knockbackTimer = 0f;
        fireTimer = 0f;
        scratchTimer = 0f;

        if (heroAnimator != null)
        {
            heroAnimator.SetCharacterType(selectedCharacter);
        }

        bool useCheckpoint = countAttempt && respawnAtCheckpointOnFullDeath && hasActiveCheckpoint && !rebuildProceduralIfStage2;
        Vector3 spawnPos = useCheckpoint ? activeCheckpointPosition : startPosition;

        if (!useCheckpoint)
        {
            coinsCollected = 0;
            score = 0;
            hasActiveCheckpoint = false;
            activeCheckpointPosition = startPosition;
            Checkpoint2D[] checkpoints = FindObjectsByType<Checkpoint2D>();
            foreach (Checkpoint2D cp in checkpoints)
            {
                cp.ResetCheckpoint();
            }
            Coin2D[] coins = FindObjectsByType<Coin2D>();
            foreach (Coin2D coin in coins)
            {
                coin.ResetCoin();
            }
            MovingPlatform2D[] movingPlatforms = FindObjectsByType<MovingPlatform2D>();
            foreach (MovingPlatform2D mp in movingPlatforms)
            {
                mp.ResetPlatform();
            }
            Switch2D[] switches = FindObjectsByType<Switch2D>();
            foreach (Switch2D sw in switches)
            {
                sw.ResetSwitch();
            }
            SwitchGate2D[] gates = FindObjectsByType<SwitchGate2D>();
            foreach (SwitchGate2D gate in gates)
            {
                gate.ResetGate();
            }
            BreakableBlock2D[] breakables = FindObjectsByType<BreakableBlock2D>();
            foreach (BreakableBlock2D bb in breakables)
            {
                bb.ResetBlock();
            }
        }

        stompComboCount = 0;
        rb.simulated = true;
        transform.position = spawnPos;
        lastSafeGroundPosition = spawnPos;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = gravityScale;

        squash = 0f;
        squashVelocity = 0f;
        maxAirFallSpeed = 0f;
        isGrounded = true;
        wasGrounded = true;

        if (visualChild != null)
        {
            visualChild.localRotation = Quaternion.identity;
            visualChild.localScale = Vector3.one;
            visualChild.localPosition = new Vector3(0f, groundedVisualOffsetY, 0f);
        }

        RobotEnemy2D[] robots = FindObjectsByType<RobotEnemy2D>();
        foreach (RobotEnemy2D r in robots)
        {
            r.ResetEnemy();
        }
        TurretProjectile2D[] turretShots = FindObjectsByType<TurretProjectile2D>();
        foreach (TurretProjectile2D shot in turretShots)
        {
            if (shot != null) Destroy(shot.gameObject);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
