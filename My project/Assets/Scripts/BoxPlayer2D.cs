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

    [Header("Progressão de Fase e Poder Desbloqueável")]
    public int currentStage = 1;
    public bool hasUnlockedStage2Power;

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
    private float horizontalInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float fireTimer;
    private float scratchTimer;
    private float invincibilityTimer;
    private float knockbackTimer;
    private int currentHearts;
    private bool isGrounded;
    private bool wasGrounded = true;
    private bool inStartScreen = true;
    private bool isLoadingNextStage;
    private float loadingTimer;
    private const float LoadingDuration = 3.0f;
    private bool hasWon;
    private bool hasLost;
    private int attempts = 1;

    // Variáveis do efeito de borracha (mola amortecida)
    private float squash;
    private float squashVelocity;
    private float maxAirFallSpeed;

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
        currentHearts = maxHearts;
        SetupHeroVisual();
        LevelBuilder2D.EnsureRobotsInScene();

        // Inicia na Tela de Seleção de Personagem
        inStartScreen = true;
        rb.simulated = false;
    }

    private void OnValidate()
    {
        if (visualChild != null)
        {
            SetupHeroVisual();
        }
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

    private void SelectAndStartGame(PlayerCharacterType character)
    {
        selectedCharacter = character;
        if (heroAnimator != null)
        {
            heroAnimator.SetCharacterType(selectedCharacter);
        }
        inStartScreen = false;
        isLoadingNextStage = false;
        currentStage = 1;
        hasUnlockedStage2Power = false;
        attempts = 1;
        LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: 1);
        ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
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
        if (inStartScreen)
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
        if (isLoadingNextStage)
        {
            loadingTimer += Time.deltaTime;
            bool skipRequested = loadingTimer >= 0.85f && GameInput2D.MenuConfirmPressed();
            if (loadingTimer >= LoadingDuration || skipRequested)
            {
                StartProceduralStage2();
            }

            UpdateVisualEffect();
            GameInput2D.LateTickState();
            return;
        }

        // 2. TELA DE VITÓRIA OU DERROTA
        if (hasLost || hasWon)
        {
            if (GameInput2D.ModalRestartPressed())
            {
                ResetPlayer(countAttempt: hasLost, rebuildProceduralIfStage2: currentStage >= 2);
            }
            else if (currentStage >= 2 && Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentStage = 1;
                hasUnlockedStage2Power = false;
                LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: 1);
                ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
            }
            else if (GameInput2D.SwitchCharacterPressed())
            {
                inStartScreen = true;
                hasLost = false;
                hasWon = false;
                rb.simulated = false;
            }
            else if (GameInput2D.BackOrQuitPressed())
            {
                QuitGame();
            }

            UpdateVisualEffect();
            GameInput2D.LateTickState();
            return;
        }

        // Permitir sair com ESC / Select ou voltar ao menu com Start durante a partida
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
            GameInput2D.LateTickState();
            return;
        }
        if (GameInput2D.GameplayMenuPressed())
        {
            inStartScreen = true;
            horizontalInput = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
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

        // Leitura unificada: Teclado (A/D/Setas) + Controle Wireless (Analógico Esquerdo / D-Pad)
        horizontalInput = GameInput2D.ReadHorizontalMovement();

        // Ataque com Q ou X/Quadrado/RB/RT:
        // - Menino: Dispara Laser (Fase 1 e Fase 2)
        // - Kitty: Ataque de Arranhão com Garras (Novo Poder desbloqueado na Fase 2, causando 1 de dano!)
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

        // Salva último ponto seguro no chão para respawn caso caia em um abismo procedural
        if (isGrounded && knockbackTimer <= 0f)
        {
            RecordSafeGroundPositionIfClear();
        }

        // Registra a velocidade de queda no ar para calcular o impacto no chão
        if (!isGrounded)
        {
            if (-rb.linearVelocity.y > maxAirFallSpeed)
            {
                maxAirFallSpeed = -rb.linearVelocity.y;
            }
        }

        // Acabou de tocar o chão após um pulo/queda -> dispara o efeito de elástico/borracha!
        if (!wasGrounded && isGrounded)
        {
            float impactFactor = Mathf.Clamp(maxAirFallSpeed / 13f, 0.55f, 1.35f);
            squash = landingElasticIntensity * impactFactor;
            squashVelocity = 3.2f * impactFactor;
            maxAirFallSpeed = 0f;

            if (visualChild != null)
            {
                visualChild.localRotation = Quaternion.identity;
            }
        }

        // Timers de Coyote Time e Jump Buffer
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

        // Executa o pulo com Barra de Espaço / W ou Botão Sul do Controle (A / X)
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && knockbackTimer <= 0f)
        {
            Jump();
        }

        // Pulo mais curto se soltar a tecla/botão de pulo cedo
        if (GameInput2D.JumpReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.55f);
        }

        // S ou Baixo acelera a descida quando estiver no ar
        if (!isGrounded && GameInput2D.FastFallHeld())
        {
            rb.gravityScale = gravityScale * fastFallMultiplier;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }

        // Caiu no buraco / fora da fase -> perde 1 coração
        if (transform.position.y < -8f)
        {
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
                transform.position = lastSafeGroundPosition + new Vector3(0f, 0.35f, 0f);
                rb.linearVelocity = Vector2.zero;
                invincibilityTimer = invincibilityDuration;
                squash = 0.50f;
            }
        }

        UpdateVisualEffect();
        GameInput2D.LateTickState();
    }

    private void RecordSafeGroundPositionIfClear()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 1.35f);
        foreach (Collider2D col in nearby)
        {
            if (col.gameObject.name.StartsWith("Obstacle") || col.GetComponent<RobotEnemy2D>() != null)
            {
                return;
            }
        }

        // Garante que há chão firme também um pouco atrás/frente para não salvar bem na quina do abismo
        Vector2 downLeft = new Vector2(transform.position.x - 0.5f, transform.position.y - 0.75f);
        Vector2 downRight = new Vector2(transform.position.x + 0.5f, transform.position.y - 0.75f);
        if (Physics2D.OverlapPoint(downLeft) != null && Physics2D.OverlapPoint(downRight) != null)
        {
            lastSafeGroundPosition = transform.position;
        }
    }

    private void FixedUpdate()
    {
        if (inStartScreen || isLoadingNextStage || hasWon || hasLost)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (knockbackTimer > 0f)
        {
            return;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
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

        GameInput2D.TriggerRumble(0.18f, 0.35f, 0.07f);
        squash = Mathf.Max(squash, 0.24f);
        squashVelocity = 1.6f;
    }

    private void Jump()
    {
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

        foreach (Collider2D hit in hits)
        {
            if (hit != boxCollider && !hit.isTrigger && !hit.gameObject.name.StartsWith("Obstacle"))
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void UpdateVisualEffect()
    {
        if (visualChild == null) return;

        if (heroAnimator == null) SetupHeroVisual();
        if (heroAnimator != null)
        {
            float animHoriz = (inStartScreen || isLoadingNextStage || hasLost || hasWon) ? 0f : horizontalInput;
            bool animGrounded = inStartScreen || isLoadingNextStage || isGrounded;
            float vertVel = (rb != null && rb.simulated) ? rb.linearVelocity.y : 0f;
            heroAnimator.UpdateAnimation(animHoriz, animGrounded, Time.deltaTime, vertVel, hasUnlockedStage2Power);
        }

        // Efeito de piscar quando sofre dano (invencibilidade temporária)
        if (heroSpriteRenderer != null)
        {
            if (invincibilityTimer > 0f && !hasLost && !inStartScreen && !isLoadingNextStage)
            {
                bool flash = Mathf.FloorToInt(invincibilityTimer * 18f) % 2 == 0;
                heroSpriteRenderer.color = flash ? new Color(1f, 0.35f, 0.35f, 0.75f) : Color.white;
            }
            else
            {
                heroSpriteRenderer.color = Color.white;
            }
        }

        // Simulação de mola elástica (lei de Hooke com amortecimento)
        float springForce = (-springStiffness * squash) - (springDamping * squashVelocity);
        squashVelocity += springForce * Time.deltaTime;
        squash += squashVelocity * Time.deltaTime;

        float scaleX = Mathf.Clamp(1f + squash * 0.95f, 0.60f, 1.75f);
        float scaleY = Mathf.Clamp(1f - squash * 0.75f, 0.45f, 1.55f);

        visualChild.localScale = new Vector3(scaleX, scaleY, 1f);
        visualChild.localRotation = Quaternion.identity;

        if (isGrounded || inStartScreen || isLoadingNextStage)
        {
            visualChild.localPosition = new Vector3(0f, (scaleY - 1f) * 0.5f, 0f);
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
        if (inStartScreen || isLoadingNextStage || hasWon || hasLost) return;

        if (other.gameObject.name.StartsWith("Goal"))
        {
            if (currentStage == 1)
            {
                TriggerStage1CompleteLoading();
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
        CheckHazardObject(other.gameObject);
    }

    private void CheckHazardObject(GameObject obj)
    {
        if (inStartScreen || isLoadingNextStage || hasWon || hasLost) return;

        RobotEnemy2D robot = obj.GetComponent<RobotEnemy2D>();
        if (robot != null && !robot.IsDead)
        {
            // Pode esmagar pulando na cabeça se for a Kitty OU se for o Menino após desbloquear seu Novo Poder no fim da Fase 1!
            bool canStompEnemy = (selectedCharacter == PlayerCharacterType.GirlKitty)
                                 || (selectedCharacter == PlayerCharacterType.BoyLaser && hasUnlockedStage2Power);

            if (canStompEnemy)
            {
                bool isAboveRobot = transform.position.y > robot.transform.position.y + 0.18f && rb.linearVelocity.y <= 1.0f;
                if (isAboveRobot)
                {
                    robot.StompKill();
                    StompShockwaveEffect2D.SpawnStompWave(robot.transform.position + new Vector3(0f, 0.15f, 0f));

                    if (heroAnimator != null)
                    {
                        heroAnimator.TriggerStompEffect();
                    }

                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.86f);
                    isGrounded = false;
                    wasGrounded = false;

                    // Efeito elástico + tremor de câmera ao pisar na cabeça do robô
                    GameInput2D.TriggerRumble(0.25f, 0.42f, 0.09f);
                    CameraFollow2D.TriggerShake(0.28f);
                    squash = 0.52f;
                    squashVelocity = 2.9f;
                    return;
                }
            }

            // Caso contrário, sofre dano!
            if (invincibilityTimer <= 0f)
            {
                TakeDamage(robot.transform.position);
            }
            return;
        }

        // Obstáculos sempre dão dano
        if (obj.name.StartsWith("Obstacle") && invincibilityTimer <= 0f)
        {
            TakeDamage(obj.transform.position);
        }
    }

    private void TakeDamage(Vector3 hazardPos)
    {
        if (invincibilityTimer > 0f || hasLost || hasWon || isLoadingNextStage) return;

        currentHearts--;
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

    private void TriggerStage1CompleteLoading()
    {
        hasUnlockedStage2Power = true;
        isLoadingNextStage = true;
        loadingTimer = 0f;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GameInput2D.TriggerRumble(0.28f, 0.52f, 0.16f);
    }

    private void StartProceduralStage2()
    {
        isLoadingNextStage = false;
        currentStage = 2;
        hasUnlockedStage2Power = true;
        LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: 2);
        ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
    }

    private void TriggerLose()
    {
        hasLost = true;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    private void TriggerWin()
    {
        hasWon = true;
        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public void ResetPlayer(bool countAttempt, bool rebuildProceduralIfStage2 = false)
    {
        hasWon = false;
        hasLost = false;
        isLoadingNextStage = false;
        if (countAttempt) attempts++;

        if (rebuildProceduralIfStage2 && currentStage >= 2)
        {
            LevelBuilder2D.BuildStageForExistingPlayer(this, stageNumber: 2);
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

        rb.simulated = true;
        transform.position = startPosition;
        lastSafeGroundPosition = startPosition;
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
            visualChild.localPosition = Vector3.zero;
        }

        RobotEnemy2D[] robots = FindObjectsByType<RobotEnemy2D>();
        foreach (RobotEnemy2D r in robots)
        {
            r.ResetEnemy();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUI.backgroundColor = Color.white;

        if (inStartScreen)
        {
            DrawStartScreenGUI();
            return;
        }

        if (isLoadingNextStage)
        {
            DrawLoadingScreenGUI();
            return;
        }

        DrawGameplayHUD();

        if (hasLost || hasWon)
        {
            DrawEndModalGUI();
        }
    }

    private void DrawLoadingScreenGUI()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float panelW = Mathf.Clamp(Screen.width * 0.78f, 640f, 1120f);
        float panelH = Mathf.Clamp(Screen.height * 0.80f, 440f, 680f);
        float panelX = (Screen.width - panelW) * 0.5f;
        float panelY = (Screen.height - panelH) * 0.5f;
        float s = Mathf.Clamp(Mathf.Min(panelW / 920f, panelH / 580f), 0.78f, 1.38f);

        GUIStyle boxStyle = PixelUITheme.CreateFrameStyle(PixelUITheme.ModalWinTex, Mathf.RoundToInt(28 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        boxStyle.padding = new RectOffset(28, 28, Mathf.RoundToInt(24 * s), 20);

        GUI.Box(
            new Rect(panelX, panelY, panelW, panelH),
            "<color=#6BFFB8>FASE 1 CONCLUÍDA!</color>  •  <color=#F5D77A>NOVO PODER DESBLOQUEADO!</color>",
            boxStyle
        );

        bool isBoy = (selectedCharacter == PlayerCharacterType.BoyLaser);
        bool hasPad = GameInput2D.HasGamepad;

        // Animação ao vivo em 3 quadros do novo poder desbloqueado
        Texture2D[] animFrames = isBoy
            ? PixelHeroAnimator2D.GetBoyStompPreviewTextures()
            : PixelHeroAnimator2D.GetKittyScratchPreviewTextures();
        int frameIdx = (animFrames != null && animFrames.Length > 0)
            ? Mathf.FloorToInt(Time.unscaledTime * 6.5f) % animFrames.Length
            : 0;
        Texture2D activeFrame = (animFrames != null && animFrames.Length > 0)
            ? animFrames[frameIdx]
            : (isBoy ? PixelHeroAnimator2D.GetBoyPreviewTexture() : PixelHeroAnimator2D.GetKittyPreviewTexture());

        float cardW = panelW - 96f * s;
        float cardH = 215f * s;
        Rect powerCardRect = new Rect(panelX + (panelW - cardW) * 0.5f, panelY + 74f * s, cardW, cardH);
        Texture2D cardBg = isBoy ? PixelUITheme.CardBoySelectedTex : PixelUITheme.CardGirlSelectedTex;
        GUIStyle powerCardStyle = PixelUITheme.CreateFrameStyle(cardBg, Mathf.RoundToInt(20 * s), TextAnchor.UpperLeft, FontStyle.Bold);
        GUI.Box(powerCardRect, GUIContent.none, powerCardStyle);

        float spriteSize = Mathf.Clamp(cardH * 0.76f, 118f, 185f);
        float bounceY = isBoy
            ? -Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6.5f)) * (12f * s)
            : Mathf.Sin(Time.unscaledTime * 8f) * (4f * s);
        Rect spriteRect = new Rect(
            powerCardRect.x + 28f * s,
            powerCardRect.y + (cardH - spriteSize) * 0.5f + bounceY,
            spriteSize,
            spriteSize
        );
        GUI.DrawTexture(spriteRect, activeFrame, ScaleMode.ScaleToFit);

        GUIStyle powerTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(22 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            wordWrap = true
        };
        powerTitleStyle.normal.textColor = Color.white;

        GUIStyle powerBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16 * s),
            alignment = TextAnchor.UpperLeft,
            richText = true,
            wordWrap = true
        };
        powerBodyStyle.normal.textColor = new Color(0.90f, 0.94f, 1f, 1f);

        string powerTitle = isBoy
            ? "<color=#6BE3FF>NOVO PODER DO MENINO: BOTAS DE PULO ESMAGADOR!</color>"
            : "<color=#FF88C2>NOVO PODER DA KITTY: GARRAS DE ARRANHÃO!</color>";

        string powerDesc = isBoy
            ? "Agora o <b>Boneco Menino</b> também pode <b>pular sobre a cabeça dos robôs</b> para esmagá-los instantaneamente (igual à Kitty!), além de continuar com sua Pistola de Laser!\n" +
              (hasPad ? "<color=#F5D77A>Como usar:</color> Salte com <b>[A]</b> e caia em cima dos inimigos!" : "<color=#F5D77A>Como usar:</color> Salte com <b>[ESPAÇO / W]</b> e caia em cima dos inimigos!")
            : "Agora a <b>Boneca Kitty</b> ganhou o ataque de <b>Arranhar</b> à curta distância, causando <b>1 de dano</b> aos robozinhos e empurrando-os para trás!\n" +
              (hasPad ? "<color=#F5D77A>Como usar:</color> Pressione <b>[Q / X / RT]</b> perto dos robôs para arranhar!" : "<color=#F5D77A>Como usar:</color> Pressione <b>[Q]</b> perto dos robôs para arranhar!");

        float textLeft = spriteRect.xMax + 26f * s;
        float textWidth = powerCardRect.xMax - textLeft - 24f * s;
        GUI.Label(new Rect(textLeft, powerCardRect.y + 22f * s, textWidth, 34f * s), powerTitle, powerTitleStyle);
        GUI.Label(new Rect(textLeft, powerCardRect.y + 62f * s, textWidth, cardH - 76f * s), powerDesc, powerBodyStyle);

        // Seção de Loading da Fase 2 Procedural Diurna
        float progress = Mathf.Clamp01(loadingTimer / LoadingDuration);
        int percent = Mathf.RoundToInt(progress * 100f);

        string stageStep = progress < 0.35f
            ? "Sorteando semente aleatória e esculpindo ilhas ensolaradas..."
            : (progress < 0.72f
                ? "Posicionando obstáculos de mármore solar, girassóis e pontes..."
                : "Despertando patrulhas de robôs na Pradaria Diurna...");

        GUIStyle loadLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(18 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        loadLabelStyle.normal.textColor = new Color(0.96f, 0.94f, 0.84f, 1f);

        float statusY = powerCardRect.yMax + 24f * s;
        GUI.Label(
            new Rect(panelX + 36f * s, statusY, panelW - 72f * s, 30f * s),
            $"<color=#FFE066>CARREGANDO FASE 2 PROCEDURAL — PRADARIA SOLAR DIURNA ({percent}%)</color>",
            loadLabelStyle
        );

        GUIStyle stepStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * s),
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        stepStyle.normal.textColor = new Color(0.75f, 0.88f, 1f, 1f);
        GUI.Label(new Rect(panelX + 36f * s, statusY + 30f * s, panelW - 72f * s, 26f * s), stageStep, stepStyle);

        // Barra de progresso Pixel Art
        float barW = panelW - 140f * s;
        float barH = 34f * s;
        float barX = panelX + (panelW - barW) * 0.5f;
        float barY = statusY + 66f * s;

        GUI.DrawTexture(new Rect(barX, barY, barW, barH), PixelUITheme.LoadingBarTrackTex, ScaleMode.StretchToFill);
        float fillW = Mathf.Max(8f, (barW - 8f * s) * progress);
        GUI.DrawTexture(
            new Rect(barX + 4f * s, barY + 4f * s, fillW, barH - 8f * s),
            PixelUITheme.LoadingBarFillTex,
            ScaleMode.StretchToFill
        );

        bool canSkip = loadingTimer >= 1.15f;
        string skipHint = canSkip
            ? (hasPad ? "<color=#6BFFB8>Pressione [A / ENTER / ESPAÇO] para entrar agora na Fase 2!</color>" : "<color=#6BFFB8>Pressione [ENTER / ESPAÇO] para entrar agora na Fase 2!</color>")
            : "<color=#A8B8D8>Preparando mundo procedural...</color>";
        GUI.Label(new Rect(panelX + 36f * s, barY + barH + 14f * s, panelW - 72f * s, 26f * s), skipHint, stepStyle);
    }

    private void DrawGameplayHUD()
    {
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.85f, 1.55f);
        float margin = 18f * scale;

        // Painel esquerdo: Retrato + Fase/Tentativa + Corações 16x16 grandes
        float leftW = 410f * scale;
        float leftH = 104f * scale;
        Rect leftRect = new Rect(margin, margin, leftW, leftH);

        GUIStyle hudFrame = PixelUITheme.CreateFrameStyle(PixelUITheme.HudPanelTex, Mathf.RoundToInt(17 * scale), TextAnchor.UpperLeft, FontStyle.Bold);
        GUI.Box(leftRect, GUIContent.none, hudFrame);

        float portraitSize = 68f * scale;
        Texture2D portraitTex = (selectedCharacter == PlayerCharacterType.BoyLaser)
            ? PixelHeroAnimator2D.GetBoyPreviewTexture()
            : PixelHeroAnimator2D.GetKittyPreviewTexture();
        GUI.DrawTexture(new Rect(leftRect.x + 16f * scale, leftRect.y + (leftH - portraitSize) * 0.5f, portraitSize, portraitSize), portraitTex, ScaleMode.ScaleToFit);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        labelStyle.normal.textColor = new Color(0.92f, 0.95f, 1f, 1f);

        string stageBadge = (currentStage == 1)
            ? "<color=#8CE8FF>FASE 1: RUÍNAS</color>"
            : $"<color=#FFE169>FASE 2: SOLAR (#{LevelBuilder2D.CurrentSeed % 10000})</color>";

        float infoX = leftRect.x + portraitSize + 24f * scale;
        GUI.Label(
            new Rect(infoX, leftRect.y + 10f * scale, leftW - portraitSize - 32f * scale, 24f * scale),
            $"{stageBadge}  •  <color=#F2D16B>TENTATIVA #{attempts}</color>",
            labelStyle
        );

        float heartSize = 36f * scale;
        float heartGap = 8f * scale;
        for (int i = 0; i < maxHearts; i++)
        {
            Texture2D tex = (i < currentHearts) ? PixelUITheme.FullHeartTex : PixelUITheme.EmptyHeartTex;
            GUI.DrawTexture(
                new Rect(infoX + i * (heartSize + heartGap), leftRect.y + 38f * scale, heartSize, heartSize),
                tex,
                ScaleMode.ScaleToFit
            );
        }

        if (hasUnlockedStage2Power)
        {
            string powerBadge = (selectedCharacter == PlayerCharacterType.BoyLaser)
                ? "<color=#6BFFB8>★ PODER ATIVO: Pulo Esmagador + Laser</color>"
                : "<color=#FF94C8>★ PODER ATIVO: Arranhar [Q] (1 Dano) + Esmagar</color>";
            GUIStyle powerBadgeStyle = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.RoundToInt(12 * scale)
            };
            GUI.Label(
                new Rect(infoX, leftRect.y + 76f * scale, leftW - portraitSize - 28f * scale, 20f * scale),
                powerBadge,
                powerBadgeStyle
            );
        }

        // Painel superior direito: Controles ampliados e adaptados para Fase 1 / Fase 2
        bool hasPad = GameInput2D.HasGamepad;
        string specialControl;
        if (selectedCharacter == PlayerCharacterType.BoyLaser)
        {
            if (hasUnlockedStage2Power)
            {
                specialControl = hasPad
                    ? "<color=#5CE1E6>[Q/X/RT]</color> Laser  •  <color=#6BFFB8>[PULO]</color> Esmagar"
                    : "<color=#5CE1E6>[Q]</color> Laser  •  <color=#6BFFB8>[PULO]</color> Esmagar";
            }
            else
            {
                specialControl = hasPad ? "<color=#5CE1E6>[Q / X / RT]</color> Laser" : "<color=#5CE1E6>[Q]</color> Laser";
            }
        }
        else
        {
            if (hasUnlockedStage2Power)
            {
                specialControl = hasPad
                    ? "<color=#FF85B3>[Q/X/RT]</color> Arranhar (1 Dano)  •  <color=#6BFFB8>[PULO]</color> Esmagar"
                    : "<color=#FF85B3>[Q]</color> Arranhar (1 Dano)  •  <color=#6BFFB8>[PULO]</color> Esmagar";
            }
            else
            {
                specialControl = hasPad ? "<color=#FF85B3>[PULO / A]</color> Esmagar" : "<color=#FF85B3>[PULO]</color> Esmagar";
            }
        }

        string controlsText = hasPad
            ? $"<color=#6BFFB8>● CONTROLE</color>   <b>[Analógico/D-Pad]</b> Mover   •   <b>[A]</b> Pular   •   {specialControl}"
            : $"<b>[A/D]</b> Mover   •   <b>[ESPAÇO/W]</b> Pular   •   {specialControl}   •   <b>[S]</b> Descer";

        float maxCtrlW = hasPad ? 820f * scale : 740f * scale;
        float rightW = Mathf.Min(Screen.width - leftW - margin * 3f, maxCtrlW);
        if (rightW > 260f)
        {
            float rightH = 56f * scale;
            Rect rightRect = new Rect(Screen.width - rightW - margin, margin, rightW, rightH);
            GUIStyle ctrlStyle = PixelUITheme.CreateFrameStyle(PixelUITheme.HudPanelTex, Mathf.RoundToInt(14 * scale), TextAnchor.MiddleCenter);
            ctrlStyle.padding = new RectOffset(14, 14, 6, 6);
            GUI.Box(rightRect, controlsText, ctrlStyle);
        }
    }

    private void DrawEndModalGUI()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float modalW = Mathf.Clamp(Screen.width * 0.64f, 540f, 940f);
        float modalH = Mathf.Clamp(Screen.height * 0.64f, 390f, 600f);
        float modalX = (Screen.width - modalW) * 0.5f;
        float modalY = (Screen.height - modalH) * 0.5f;
        float s = Mathf.Clamp(Mathf.Min(modalW / 740f, modalH / 470f), 0.78f, 1.35f);

        Texture2D bg = hasLost ? PixelUITheme.ModalLoseTex : PixelUITheme.ModalWinTex;
        GUIStyle modalBox = PixelUITheme.CreateFrameStyle(bg, Mathf.RoundToInt(36 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        modalBox.padding = new RectOffset(28, 28, Mathf.RoundToInt(28 * s), 24);

        string title = hasLost
            ? $"<color=#FF7A8A>FIM DE JOGO NA FASE {currentStage}</color>"
            : "<color=#6BFFB8>VITÓRIA TOTAL! FASE 2 CONQUISTADA!</color>";
        GUI.Box(new Rect(modalX, modalY, modalW, modalH), title, modalBox);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(18 * s),
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        subStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f, 1f);

        string subtitle;
        if (hasLost)
        {
            subtitle = (currentStage == 1)
                ? $"As Ruínas da Noite cobraram seu preço na tentativa <b>#{attempts}</b>."
                : $"A Pradaria Solar Procedural (Seed <b>#{LevelBuilder2D.CurrentSeed % 10000}</b>) venceu na tentativa <b>#{attempts}</b>.";
        }
        else
        {
            subtitle = $"Você dominou o novo poder e venceu a Fase 2 Procedural (Seed <b>#{LevelBuilder2D.CurrentSeed % 10000}</b>) na tentativa <b>#{attempts}</b>!";
        }
        GUI.Label(new Rect(modalX + 24f, modalY + 82f * s, modalW - 48f, 40f * s), subtitle, subStyle);

        float btnW = modalW - 120f * s;
        float btnH = 50f * s;
        float btnX = modalX + (modalW - btnW) * 0.5f;
        float firstBtnY = modalY + 138f * s;
        float btnStep = 60f * s;

        GUIStyle primaryBtn = PixelUITheme.CreateButtonStyle(1, Mathf.RoundToInt(18 * s));
        GUIStyle secondaryBtn = PixelUITheme.CreateButtonStyle(0, Mathf.RoundToInt(17 * s));

        bool hasPad = GameInput2D.HasGamepad;
        string restartLabel = (currentStage == 2)
            ? (hasPad ? "GERAR NOVA FASE 2 ALEATÓRIA  [ENTER / A]" : "GERAR NOVA FASE 2 ALEATÓRIA  [ENTER]")
            : (hasPad ? "REINICIAR FASE 1  [ENTER / A]" : "REINICIAR FASE 1  [ENTER]");
        string backStage1Label = hasPad ? "VOLTAR PARA A FASE 1  [1]" : "VOLTAR PARA A FASE 1  [1]";
        string switchLabel = hasPad ? "TROCAR DE PERSONAGEM  [T / Y]" : "TROCAR DE PERSONAGEM  [T]";
        string quitLabel = hasPad ? "SAIR DO JOGO  [ESC / B]" : "SAIR DO JOGO  [ESC]";

        if (GUI.Button(new Rect(btnX, firstBtnY, btnW, btnH), restartLabel, primaryBtn))
        {
            ResetPlayer(countAttempt: hasLost, rebuildProceduralIfStage2: (currentStage == 2));
        }

        float currentY = firstBtnY + btnStep;
        if (currentStage == 2)
        {
            if (GUI.Button(new Rect(btnX, currentY, btnW, btnH), backStage1Label, secondaryBtn))
            {
                currentStage = 1;
                hasUnlockedStage2Power = false;
                LevelBuilder2D.BuildStageForExistingPlayer(this, 1);
                ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
            }
            currentY += btnStep;
        }

        if (GUI.Button(new Rect(btnX, currentY, btnW, btnH), switchLabel, secondaryBtn))
        {
            inStartScreen = true;
            hasLost = false;
            hasWon = false;
            rb.simulated = false;
        }
        currentY += btnStep;

        if (GUI.Button(new Rect(btnX, currentY, btnW, btnH), quitLabel, secondaryBtn))
        {
            QuitGame();
        }
    }

    private void DrawStartScreenGUI()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float panelW = Mathf.Clamp(Screen.width * 0.90f, 680f, 1440f);
        float panelH = Mathf.Clamp(Screen.height * 0.88f, 480f, 860f);
        float panelX = (Screen.width - panelW) * 0.5f;
        float panelY = (Screen.height - panelH) * 0.5f;

        float s = Mathf.Clamp(Mathf.Min(panelW / 1060f, panelH / 680f), 0.72f, 1.42f);

        GUIStyle mainBoxStyle = PixelUITheme.CreateFrameStyle(PixelUITheme.MainPanelTex, Mathf.RoundToInt(32 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        mainBoxStyle.padding = new RectOffset(24, 24, Mathf.RoundToInt(22 * s), 18);
        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "<color=#F5D77A>ESCOLHA SEU PERSONAGEM</color>", mainBoxStyle);

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(15 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        subtitleStyle.normal.textColor = new Color(0.66f, 0.76f, 0.94f, 1f);

        bool hasPad = GameInput2D.HasGamepad;
        string subtitleText = hasPad
            ? $"RUÍNAS DO MONTE  •  <color=#6BFFB8>● CONTROLE WIRELESS CONECTADO ({GameInput2D.ConnectedGamepadName})</color>"
            : "RUÍNAS DO MONTE  •  TECLADO E CONTROLE WIRELESS COMPATÍVEIS";

        GUI.Label(
            new Rect(panelX + 20f, panelY + 62f * s, panelW - 40f, 28f * s),
            subtitleText,
            subtitleStyle
        );

        float sidePad = 36f * s;
        float cardGap = 28f * s;
        float cardsTop = panelY + 102f * s;
        float footerH = 52f * s;
        float cardW = (panelW - sidePad * 2f - cardGap) * 0.5f;
        float cardH = panelH - (cardsTop - panelY) - footerH - 16f * s;

        bool boySelected = (selectedCharacter == PlayerCharacterType.BoyLaser);
        bool girlSelected = (selectedCharacter == PlayerCharacterType.GirlKitty);

        // Card 1: Boneco Menino
        Rect boyCard = new Rect(panelX + sidePad, cardsTop, cardW, cardH);
        DrawCharacterCard(
            boyCard,
            s,
            isSelected: boySelected,
            selectedTex: PixelUITheme.CardBoySelectedTex,
            title: "<color=#6BE3FF>1. BONECO MENINO</color>",
            badge: hasPad ? "<b>ARMAMENTO:</b> Pistola de Laser <b>[Q / X / RT]</b>" : "<b>ARMAMENTO:</b> Pistola de Laser <b>[Q]</b>",
            desc: "Derrota os robôs à distância com 2 tiros de laser.\nNão pode pular em cima dos robôs!",
            previewTex: PixelHeroAnimator2D.GetBoyPreviewTexture(),
            buttonLabel: hasPad ? "JOGAR COM MENINO  [1 / A]" : "JOGAR COM MENINO  [1]",
            buttonKind: boySelected ? 1 : 0,
            characterType: PlayerCharacterType.BoyLaser
        );

        // Card 2: Boneca Kitty
        Rect girlCard = new Rect(boyCard.xMax + cardGap, cardsTop, cardW, cardH);
        DrawCharacterCard(
            girlCard,
            s,
            isSelected: girlSelected,
            selectedTex: PixelUITheme.CardGirlSelectedTex,
            title: "<color=#FF88C2>2. BONECA KITTY</color>",
            badge: hasPad ? "<b>HABILIDADE:</b> Pulo Esmagador! <b>[ESPAÇO / A]</b>" : "<b>HABILIDADE:</b> Pulo Esmagador!",
            desc: "Salte sobre a cabeça dos robôs para esmagá-los!\nNão utiliza arma de laser.",
            previewTex: PixelHeroAnimator2D.GetKittyPreviewTexture(),
            buttonLabel: hasPad ? "JOGAR COM MENINA  [2 / A]" : "JOGAR COM MENINA  [2]",
            buttonKind: girlSelected ? 2 : 0,
            characterType: PlayerCharacterType.GirlKitty
        );

        // Rodapé de instrução ampliado
        GUIStyle footerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16 * s),
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        footerStyle.normal.textColor = new Color(0.88f, 0.92f, 1f, 0.96f);
        string footerText = hasPad
            ? "Controle: <b><color=#F5D77A>[Analógico / D-Pad]</color></b> escolher  •  <b><color=#6BFFB8>[A / X / Start]</color></b> iniciar  •  Teclado: <b><color=#F5D77A>[A/D + ENTER]</color></b>"
            : "Use <b><color=#F5D77A>[A / D]</color></b> ou <b><color=#F5D77A>[1 / 2]</color></b> (ou <b><color=#6BFFB8>Controle Wireless</color></b>) e <b><color=#6BFFB8>[ENTER / ESPAÇO / A]</color></b> para iniciar";
        GUI.Label(
            new Rect(panelX + 20f, panelY + panelH - footerH - 4f * s, panelW - 40f, footerH),
            footerText,
            footerStyle
        );
    }

    private void DrawCharacterCard(
        Rect cardRect,
        float scale,
        bool isSelected,
        Texture2D selectedTex,
        string title,
        string badge,
        string desc,
        Texture2D previewTex,
        string buttonLabel,
        int buttonKind,
        PlayerCharacterType characterType)
    {
        GUIStyle cardStyle = PixelUITheme.CreateFrameStyle(
            isSelected ? selectedTex : PixelUITheme.CardNormalTex,
            Mathf.RoundToInt(22 * scale),
            TextAnchor.UpperCenter,
            FontStyle.Bold
        );
        cardStyle.padding = new RectOffset(18, 18, Mathf.RoundToInt(18 * scale), 16);

        GUI.Box(cardRect, title, cardStyle);

        // Preview grande em Pixel Art nítido (FilterMode.Point)
        float previewSize = Mathf.Clamp(cardRect.height * 0.39f, 110f, 215f);
        float bob = isSelected ? Mathf.Sin(Time.unscaledTime * 4.5f) * (4f * scale) : 0f;
        Rect previewRect = new Rect(
            cardRect.x + (cardRect.width - previewSize) * 0.5f,
            cardRect.y + 52f * scale + bob,
            previewSize,
            previewSize
        );
        GUI.DrawTexture(previewRect, previewTex, ScaleMode.ScaleToFit);

        // Descrição ampla abaixo do retrato
        GUIStyle descStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(16 * scale),
            alignment = TextAnchor.UpperCenter,
            richText = true,
            wordWrap = true
        };
        descStyle.normal.textColor = new Color(0.92f, 0.95f, 1f, 1f);

        float textTop = cardRect.y + 58f * scale + previewSize + 8f * scale;
        float btnH = Mathf.Clamp(54f * scale, 40f, 72f);
        float textH = Mathf.Max(40f, (cardRect.yMax - btnH - 22f * scale) - textTop);

        GUI.Label(
            new Rect(cardRect.x + 18f * scale, textTop, cardRect.width - 36f * scale, textH),
            $"{badge}\n<size={Mathf.RoundToInt(14 * scale)}><color=#C5D0E6>{desc}</color></size>",
            descStyle
        );

        // Botão grande de ação
        GUIStyle btnStyle = PixelUITheme.CreateButtonStyle(buttonKind, Mathf.RoundToInt(18 * scale));
        Rect btnRect = new Rect(cardRect.x + 24f * scale, cardRect.yMax - btnH - 16f * scale, cardRect.width - 48f * scale, btnH);
        if (GUI.Button(btnRect, buttonLabel, btnStyle))
        {
            SelectAndStartGame(characterType);
        }
    }
}
