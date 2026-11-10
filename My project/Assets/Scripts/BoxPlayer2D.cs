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

    [Header("Arma de Laser (Apenas Menino - Tecla Q)")]
    public float fireCooldown = 0.15f;

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
    private float horizontalInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float fireTimer;
    private float invincibilityTimer;
    private float knockbackTimer;
    private int currentHearts;
    private bool isGrounded;
    private bool wasGrounded = true;
    private bool inStartScreen = true;
    private bool hasWon;
    private bool hasLost;
    private int attempts = 1;

    // Variáveis do efeito de borracha (mola amortecida)
    private float squash;
    private float squashVelocity;
    private float maxAirFallSpeed;

    // Texturas de Coração em Pixel Art para a HUD
    private static Texture2D fullHeartTex;
    private static Texture2D emptyHeartTex;

    private static readonly string[] HeartGrid =
    {
        ".OO...OO.",
        "ORRO.ORRO",
        "ORRRRRRRO",
        "ORRRRRRRO",
        "ORRRRRRRO",
        ".ORRRRRO.",
        "..ORRRO..",
        "...ORO...",
        "....O...."
    };

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
        ResetPlayer(countAttempt: false);
    }

    private void Update()
    {
        // 1. TELA DE INÍCIO (Seleção de Personagem)
        if (inStartScreen)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                selectedCharacter = PlayerCharacterType.BoyLaser;
                if (heroAnimator != null) heroAnimator.SetCharacterType(selectedCharacter);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                selectedCharacter = PlayerCharacterType.GirlKitty;
                if (heroAnimator != null) heroAnimator.SetCharacterType(selectedCharacter);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                SelectAndStartGame(selectedCharacter);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitGame();
            }

            UpdateVisualEffect();
            return;
        }

        // 2. TELA DE VITÓRIA OU DERROTA
        if (hasLost || hasWon)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ResetPlayer(countAttempt: hasLost);
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                inStartScreen = true;
                hasLost = false;
                hasWon = false;
                rb.simulated = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitGame();
            }

            UpdateVisualEffect();
            return;
        }

        // Permitir sair com ESC durante a partida
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
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

        // Leitura direta das teclas A e D (WASD)
        horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A)) horizontalInput -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontalInput += 1f;

        // Disparo da Arma de Laser com a tecla Q (APENAS para o Boneco Menino!)
        if (fireTimer > 0f) fireTimer -= Time.deltaTime;
        if (selectedCharacter == PlayerCharacterType.BoyLaser && Input.GetKeyDown(KeyCode.Q) && fireTimer <= 0f)
        {
            ShootLaser();
        }

        wasGrounded = isGrounded;
        CheckGrounded();

        // Registra a velocidade de queda no ar para calcular o impacto no chão
        if (!isGrounded)
        {
            if (-rb.velocity.y > maxAirFallSpeed)
            {
                maxAirFallSpeed = -rb.velocity.y;
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

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Executa o pulo com Barra de Espaço (ou W)
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && knockbackTimer <= 0f)
        {
            Jump();
        }

        // Pulo mais curto se soltar a tecla de pulo cedo
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.55f);
        }

        // S acelera a descida quando estiver no ar
        if (!isGrounded && Input.GetKey(KeyCode.S))
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
            currentHearts--;
            if (currentHearts <= 0)
            {
                currentHearts = 0;
                TriggerLose();
            }
            else
            {
                transform.position = new Vector3(44.5f, 0f, 0f);
                rb.velocity = Vector2.zero;
                invincibilityTimer = invincibilityDuration;
                squash = 0.50f;
            }
        }

        UpdateVisualEffect();
    }

    private void FixedUpdate()
    {
        if (inStartScreen || hasWon || hasLost)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (knockbackTimer > 0f)
        {
            return;
        }

        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
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

        squash = Mathf.Max(squash, 0.14f);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
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
            float animHoriz = (inStartScreen || hasLost || hasWon) ? 0f : horizontalInput;
            bool animGrounded = inStartScreen || isGrounded;
            heroAnimator.UpdateAnimation(animHoriz, animGrounded, Time.deltaTime);
        }

        // Efeito de piscar quando sofre dano (invencibilidade temporária)
        if (heroSpriteRenderer != null)
        {
            if (invincibilityTimer > 0f && !hasLost && !inStartScreen)
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

        if (isGrounded || inStartScreen)
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
        if (inStartScreen || hasWon || hasLost) return;

        if (other.gameObject.name.StartsWith("Goal"))
        {
            TriggerWin();
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
        if (inStartScreen || hasWon || hasLost) return;

        RobotEnemy2D robot = obj.GetComponent<RobotEnemy2D>();
        if (robot != null && !robot.IsDead)
        {
            // Se estiver jogando com a BONECA MENINA (Hello Kitty), pular por cima esmaga o robô!
            if (selectedCharacter == PlayerCharacterType.GirlKitty)
            {
                bool isAboveRobot = transform.position.y > robot.transform.position.y + 0.18f && rb.velocity.y <= 1.0f;
                if (isAboveRobot)
                {
                    robot.StompKill();
                    rb.velocity = new Vector2(rb.velocity.x, jumpForce * 0.85f);
                    isGrounded = false;
                    wasGrounded = false;

                    // Efeito elástico ao pisar na cabeça do robô
                    squash = 0.50f;
                    squashVelocity = 2.8f;
                    return;
                }
            }

            // Caso contrário (Menino ou batida lateral da Menina), sofre dano!
            if (invincibilityTimer <= 0f)
            {
                TakeDamage(robot.transform.position);
            }
            return;
        }

        // Obstáculos vermelhos sempre dão dano
        if (obj.name.StartsWith("Obstacle") && invincibilityTimer <= 0f)
        {
            TakeDamage(obj.transform.position);
        }
    }

    private void TakeDamage(Vector3 hazardPos)
    {
        if (invincibilityTimer > 0f || hasLost || hasWon) return;

        currentHearts--;
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
            rb.velocity = new Vector2(knockDir * 5.8f, 9.5f);
        }
    }

    private void TriggerLose()
    {
        hasLost = true;
        horizontalInput = 0f;
        rb.velocity = Vector2.zero;
        rb.simulated = false;
    }

    private void TriggerWin()
    {
        hasWon = true;
        horizontalInput = 0f;
        rb.velocity = Vector2.zero;
        rb.simulated = false;
    }

    public void ResetPlayer(bool countAttempt)
    {
        hasWon = false;
        hasLost = false;
        if (countAttempt) attempts++;

        currentHearts = maxHearts;
        invincibilityTimer = 0f;
        knockbackTimer = 0f;

        if (heroAnimator != null)
        {
            heroAnimator.SetCharacterType(selectedCharacter);
        }

        rb.simulated = true;
        transform.position = startPosition;
        rb.velocity = Vector2.zero;
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

        RobotEnemy2D[] robots = FindObjectsOfType<RobotEnemy2D>();
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

    private static void EnsureHeartTextures()
    {
        if (fullHeartTex != null && emptyHeartTex != null) return;

        fullHeartTex = BuildHeartTexture(new Color(0.95f, 0.18f, 0.25f, 1f));
        emptyHeartTex = BuildHeartTexture(new Color(0.28f, 0.30f, 0.36f, 0.85f));
    }

    private static Texture2D BuildHeartTexture(Color fillColor)
    {
        int width = 9;
        int height = 9;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color outline = new Color(0.10f, 0.10f, 0.14f, 1f);
        for (int row = 0; row < height; row++)
        {
            string line = HeartGrid[row];
            int y = height - 1 - row;
            for (int x = 0; x < width; x++)
            {
                char c = line[x];
                if (c == 'O') tex.SetPixel(x, y, outline);
                else if (c == 'R') tex.SetPixel(x, y, fillColor);
                else tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        return tex;
    }

    private void OnGUI()
    {
        EnsureHeartTextures();

        if (inStartScreen)
        {
            DrawStartScreenGUI();
            return;
        }

        GUIStyle hudStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 15,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(14, 14, 10, 10),
            richText = true
        };
        GUI.color = new Color(1f, 1f, 1f, 0.92f);

        string specialControl = (selectedCharacter == PlayerCharacterType.BoyLaser)
            ? "<b>[Q]</b> Atirar Laser"
            : "<b>[PULO]</b> Esmagar Robôs por Cima";

        string hudText =
            $"<b>[A / D]</b> Mover   |   <b>[ESPAÇO / W]</b> Pular   |   {specialControl}   |   <b>[S]</b> Descer\n" +
            $"Tentativa: <b>#{attempts}</b>          Vida:";

        GUI.Box(new Rect(16, 16, 650, 64), hudText, hudStyle);

        // Desenha os 3 corações em Pixel Art na HUD
        GUI.color = Color.white;
        for (int i = 0; i < maxHearts; i++)
        {
            Texture2D tex = (i < currentHearts) ? fullHeartTex : emptyHeartTex;
            GUI.DrawTexture(new Rect(195 + i * 34, 46, 26, 26), tex, ScaleMode.StretchToFill);
        }

        if (hasLost || hasWon)
        {
            GUIStyle modalStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            Rect modalRect = new Rect(Screen.width / 2f - 270f, Screen.height / 2f - 95f, 540f, 190f);

            if (hasLost)
            {
                GUI.backgroundColor = new Color(0.85f, 0.18f, 0.22f, 0.96f);
                GUI.Box(
                    modalRect,
                    "VOCÊ PERDEU\n\n<size=16>[ENTER] Reiniciar a fase\n[T] Trocar de Personagem\n[ESC] Sair do jogo</size>",
                    modalStyle
                );
            }
            else if (hasWon)
            {
                GUI.backgroundColor = new Color(0.15f, 0.72f, 0.36f, 0.96f);
                GUI.Box(
                    modalRect,
                    "VOCÊ GANHOU\n\n<size=16>[ENTER] Reiniciar a fase\n[T] Trocar de Personagem\n[ESC] Sair do jogo</size>",
                    modalStyle
                );
            }
        }
    }

    private void DrawStartScreenGUI()
    {
        float panelW = 680f;
        float panelH = 400f;
        float panelX = Screen.width / 2f - panelW / 2f;
        float panelY = Screen.height / 2f - panelH / 2f;

        GUIStyle mainBoxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(20, 20, 18, 18),
            richText = true
        };

        GUI.backgroundColor = new Color(0.10f, 0.13f, 0.20f, 0.98f);
        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "ESCOLHA SEU PERSONAGEM", mainBoxStyle);

        GUIStyle cardStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperCenter,
            padding = new RectOffset(12, 12, 12, 12),
            richText = true
        };

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        // Card 1: Boneco Menino
        Rect boyCard = new Rect(panelX + 30f, panelY + 62f, 295f, 265f);
        bool boySelected = (selectedCharacter == PlayerCharacterType.BoyLaser);
        GUI.backgroundColor = boySelected ? new Color(0.25f, 0.65f, 1.0f, 0.95f) : new Color(0.22f, 0.25f, 0.32f, 0.90f);
        GUI.Box(
            boyCard,
            "<b><size=17>1. BONECO MENINO</size></b>\n\n\n\n\n\n\n" +
            "<b>Mecânica:</b> Pistola de Laser <b>[Q]</b>\n" +
            "<size=12>Derrota os robôs com 2 tiros de laser.\nNão pode pular em cima dos robôs!</size>",
            cardStyle
        );
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(boyCard.x + boyCard.width / 2f - 48f, boyCard.y + 38f, 96f, 96f), PixelHeroAnimator2D.GetBoyPreviewTexture(), ScaleMode.StretchToFill);

        GUI.backgroundColor = boySelected ? new Color(1.0f, 0.85f, 0.15f, 1f) : new Color(0.75f, 0.78f, 0.85f, 1f);
        if (GUI.Button(new Rect(boyCard.x + 25f, boyCard.yMax - 44f, boyCard.width - 50f, 34f), "JOGAR COM MENINO [1]", btnStyle))
        {
            SelectAndStartGame(PlayerCharacterType.BoyLaser);
        }

        // Card 2: Boneca Menina (Hello Kitty)
        Rect girlCard = new Rect(panelX + panelW - 325f, panelY + 62f, 295f, 265f);
        bool girlSelected = (selectedCharacter == PlayerCharacterType.GirlKitty);
        GUI.backgroundColor = girlSelected ? new Color(1.0f, 0.42f, 0.72f, 0.95f) : new Color(0.22f, 0.25f, 0.32f, 0.90f);
        GUI.Box(
            girlCard,
            "<b><size=17>2. BONECA KITTY</size></b>\n\n\n\n\n\n\n" +
            "<b>Mecânica:</b> Pulo Esmagador!\n" +
            "<size=12>Pule na cabeça dos robôs para derrotá-los!\nNão utiliza arma de laser.</size>",
            cardStyle
        );
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(girlCard.x + girlCard.width / 2f - 48f, girlCard.y + 38f, 96f, 96f), PixelHeroAnimator2D.GetKittyPreviewTexture(), ScaleMode.StretchToFill);

        GUI.backgroundColor = girlSelected ? new Color(1.0f, 0.45f, 0.75f, 1f) : new Color(0.75f, 0.78f, 0.85f, 1f);
        if (GUI.Button(new Rect(girlCard.x + 25f, girlCard.yMax - 44f, girlCard.width - 50f, 34f), "JOGAR COM MENINA [2]", btnStyle))
        {
            SelectAndStartGame(PlayerCharacterType.GirlKitty);
        }

        // Rodapé de instrução
        GUIStyle footerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        GUI.color = new Color(0.9f, 0.94f, 1f, 0.95f);
        GUI.Label(
            new Rect(panelX, panelY + panelH - 54f, panelW, 40f),
            "Use <b>[A / D]</b> ou <b>[1 / 2]</b> para escolher e <b>[ENTER / ESPAÇO]</b> para iniciar (ou clique no botão)",
            footerStyle
        );
    }
}
