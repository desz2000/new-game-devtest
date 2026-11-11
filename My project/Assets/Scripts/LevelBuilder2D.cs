using System.Collections.Generic;
using UnityEngine;

public static class LevelBuilder2D
{
    public const float GroundTopY = -2.0f;
    public const float Stage1MapOriginX = -12.0f;

    /// <summary>
    /// Mapa ASCII da Fase 1 — Level Design v2 + Mecânicas das Fases 2, 3 e 4 (1 caractere = 1 unidade horizontal, origem X = -12).
    /// Seções:
    ///   1. Ensino (x = 0..15 / col 12..27): Bloco sólido seguro (B), espinho baixo (^), vão curto (2u), Robô Patrulheiro (R) e Checkpoint 1 (C).
    ///   2. Verticalidade (x = 15..35 / col 27..47): Mola (M), plataformas one-way (=), Robô Saltador (J) e Bloco Frágil (F).
    ///   3. Checkpoint + Descanso (x = 35..40 / col 47..52): zona aberta segura com moedas e Checkpoint 2.
    ///   4. Combinação (x = 40..65 / col 52..77): vão de 4u com Plataforma Móvel (~~~), plataforma com robô (R), Torreta (T), Robô Voador (V) e Interruptor (S) + Porta Rúnica (D) + Checkpoint 3.
    ///   5. Clímax (x = 65..85 / col 77..97): vão longo de 4u, Mola (M), dupla de robôs (R...R) para Combo de Pisão da Kitty alcançar a plataforma alta, e Portal (G).
    /// Legenda:
    ///   # = chão sólido | = = plataforma one-way | ~ = plataforma móvel | B = bloco sólido seguro | F = bloco frágil
    ///   S = interruptor | D = porta rúnica | ^ = espinhos (dano) | M = mola (~3.5u)
    ///   R = robô patrulheiro | J = robô saltador | T = torreta | V = robô voador (drone) | C = checkpoint | G = meta | P = início | o = moeda | . = vazio
    /// </summary>
    public static readonly string[] Stage1AsciiMap =
    {
        "...........................................ooo......................................................",
        "...................oo..............oR.o...====.............o.R.o............................ooo.....",
        "..................====............=====...................======..........................=====.....",
        "..............oo......................................~~~...........................................",
        ".............====.............=====..............===..............====........===...====............",
        ".......................................................................V............................",
        "......P....B..^.........R...C.M.....J....^...F....C...........o....T.S..D.C.......M..R...R....G.....",
        "###################..#################################....####################....##################"
    };

    public static int CurrentStage { get; private set; } = 1;
    public static int CurrentSeed { get; private set; } = 0;
    public static int LastGeneratedObstacleCount { get; private set; } = 5;
    public static int LastGeneratedRobotCount { get; private set; } = 6;
    public static int LastGeneratedCoinCount { get; private set; } = 14;
    public static Vector3 LastParsedPlayerSpawn { get; private set; } = new Vector3(-6f, GroundTopY + 0.475f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureLevelOnPlay()
    {
        BoxPlayer2D existingPlayer = Object.FindAnyObjectByType<BoxPlayer2D>();
        bool hasPhase4Enemies = false;
        foreach (RobotEnemy2D r in Object.FindObjectsByType<RobotEnemy2D>())
        {
            if (r != null && r.behaviorType != EnemyBehaviorType.PatrolWalker)
            {
                hasPhase4Enemies = true;
                break;
            }
        }

        if (existingPlayer == null
            || Object.FindAnyObjectByType<ParallaxBackdrop2D>() == null
            || Object.FindAnyObjectByType<SpringPad2D>() == null
            || Object.FindAnyObjectByType<Switch2D>() == null
            || Object.FindAnyObjectByType<BreakableBlock2D>() == null
            || !hasPhase4Enemies)
        {
            BuildScene();
        }
        else
        {
            if (existingPlayer.GetComponent<GameUI2D>() == null)
            {
                existingPlayer.gameObject.AddComponent<GameUI2D>();
            }
            EnsureRobotsInScene();
            EnsureCheckpointsInScene();
        }
    }

    public static void BuildScene()
    {
        CurrentStage = 1;
        CurrentSeed = 0;

        GameObject oldLevel = GameObject.Find("Level_2D");
        if (oldLevel != null) Object.DestroyImmediate(oldLevel);

        GameObject oldPlayer = GameObject.Find("PlayerBox");
        if (oldPlayer != null) Object.DestroyImmediate(oldPlayer);

        GameObject levelRoot = new GameObject("Level_2D");
        BuildStage1AuthoredGeometry(levelRoot.transform);

        // Jogador (Bonecos Pixel Art 32x32 ancorados no solo)
        GameObject playerObj = new GameObject("PlayerBox");
        playerObj.transform.position = LastParsedPlayerSpawn;

        SpriteRenderer rootSr = playerObj.AddComponent<SpriteRenderer>();
        rootSr.enabled = false;

        BoxCollider2D col = playerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.95f, 0.95f);

        Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4.0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        GameObject visualChild = new GameObject("VisualBox");
        visualChild.transform.SetParent(playerObj.transform, false);
        SpriteRenderer childSr = visualChild.AddComponent<SpriteRenderer>();
        childSr.sortingOrder = 6;
        PixelHeroAnimator2D heroAnim = visualChild.AddComponent<PixelHeroAnimator2D>();
        heroAnim.EnsureSprites();

        BoxPlayer2D playerScript = playerObj.AddComponent<BoxPlayer2D>();
        playerScript.visualChild = visualChild.transform;
        playerScript.SetupHeroVisual();

        GameUI2D ui = playerObj.AddComponent<GameUI2D>();
        ui.player = playerScript;

        ConfigureCameraForStage(playerObj.transform, stageNumber: 1);
    }

    /// <summary>
    /// Reconstrói apenas a geometria da fase (Fase 1 em mapa ASCII noturna ou Fase 2 procedural diurna)
    /// preservando o objeto do jogador e seus poderes.
    /// </summary>
    public static void BuildStageForExistingPlayer(BoxPlayer2D player, int stageNumber, int? customSeed = null)
    {
        CurrentStage = stageNumber;

        foreach (LaserProjectile2D laser in Object.FindObjectsByType<LaserProjectile2D>())
        {
            if (laser != null) Object.Destroy(laser.gameObject);
        }

        GameObject oldLevel = GameObject.Find("Level_2D");
        if (oldLevel != null)
        {
            if (Application.isPlaying) Object.Destroy(oldLevel);
            else Object.DestroyImmediate(oldLevel);
        }

        GameObject levelRoot = new GameObject("Level_2D");

        if (stageNumber <= 1)
        {
            CurrentSeed = 0;
            BuildStage1AuthoredGeometry(levelRoot.transform);
            if (player != null)
            {
                player.SetStageStartPosition(LastParsedPlayerSpawn);
                ConfigureCameraForStage(player.transform, stageNumber: 1);
            }
        }
        else
        {
            int seed = customSeed ?? Random.Range(10000, 99999);
            CurrentSeed = seed;
            BuildProceduralStageGeometry(levelRoot.transform, seed, stageNumber);
            if (player != null)
            {
                player.SetStageStartPosition(new Vector3(-6f, GroundTopY + 0.475f, 0f));
                ConfigureCameraForStage(player.transform, stageNumber);
            }
        }
    }

    private static void ConfigureCameraForStage(Transform playerTransform, int stageNumber)
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = stageNumber == 2
                ? new Color(0.36f, 0.78f, 1.00f, 1f)
                : (stageNumber >= 3 ? new Color(0.05f, 0.08f, 0.20f, 1f) : new Color(0.04f, 0.05f, 0.12f, 1f));
            mainCam.orthographic = true;
            mainCam.orthographicSize = 4.8f;

            CameraFollow2D follow = mainCam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = mainCam.gameObject.AddComponent<CameraFollow2D>();
            follow.target = playerTransform;
            if (playerTransform != null)
            {
                mainCam.transform.position = new Vector3(playerTransform.position.x + 1.5f, 0.6f, -10f);
                follow.SnapToTarget();
            }
        }
    }

    private static void BuildStage1AuthoredGeometry(Transform levelRoot)
    {
        AmbientAtmosphere2D.PopulateStageDepth(levelRoot, GroundTopY);
        BuildStageFromAsciiMap(levelRoot, Stage1AsciiMap, Stage1MapOriginX, StageVisualTheme.NightRuins);
    }

    /// <summary>
    /// Calcula a altura Y exata da superfície de apoio para uma célula (r, c) do mapa ASCII.
    /// Quando uma entidade (robô, moeda, mola) está na linha imediatamente acima de uma plataforma ('=' ou '~'),
    /// retorna a superfície superior exata dessa plataforma para que o objeto nunca fique flutuando.
    /// </summary>
    private static float GetEntitySupportY(string[] asciiMap, int r, int c, int groundRow)
    {
        if (r + 1 == groundRow)
        {
            return GroundTopY;
        }

        if (r + 1 < groundRow)
        {
            string belowLine = asciiMap[r + 1];
            if (c >= 0 && c < belowLine.Length)
            {
                char belowChar = belowLine[c];
                if (belowChar == '=' || belowChar == '~')
                {
                    int platHeightAboveGround = (groundRow - 1) - (r + 1);
                    return GroundTopY + Mathf.Max(1f, platHeightAboveGround);
                }
            }
        }

        int heightAboveGround = (groundRow - 1) - r;
        return GroundTopY + heightAboveGround;
    }

    /// <summary>
    /// Interpreta uma grade ASCII (string[]) e instancia chão, plataformas one-way, plataformas móveis,
    /// blocos sólidos, espinhos, molas, robôs, moedas, checkpoints e portal.
    /// </summary>
    public static void BuildStageFromAsciiMap(Transform levelRoot, string[] asciiMap, float originX, StageVisualTheme theme)
    {
        if (asciiMap == null || asciiMap.Length == 0) return;

        int rowCount = asciiMap.Length;
        int groundRow = rowCount - 1;
        int obstacleCount = 0;
        int solidBlockCount = 0;
        int breakableBlockCount = 0;
        int switchCount = 0;
        int gateCount = 0;
        int springCount = 0;
        int robotCount = 0;
        int checkpointCount = 0;
        int coinCount = 0;
        int groundPartCount = 0;
        int platformCount = 0;
        int movingPlatformCount = 0;

        // 1. Constrói segmentos contínuos de chão ('#') e névoa nos abismos ('.') na linha base
        string bottomLine = asciiMap[groundRow];
        int col = 0;
        while (col < bottomLine.Length)
        {
            if (bottomLine[col] == '#')
            {
                int startCol = col;
                while (col < bottomLine.Length && bottomLine[col] == '#') col++;
                int endCol = col;

                groundPartCount++;
                float startX = originX + startCol;
                float endX = originX + endCol;
                float width = endX - startX;
                float centerX = startX + width * 0.5f;

                CreateStyledBlock(
                    $"Ground_Part{groundPartCount}",
                    levelRoot,
                    new Vector2(centerX, GroundTopY - 1.5f),
                    new Vector2(width, 3f),
                    StageBlockStyle.Ground,
                    isTrigger: false,
                    theme
                );
            }
            else
            {
                int startGap = col;
                while (col < bottomLine.Length && bottomLine[col] != '#') col++;
                int endGap = col;
                float gapWidth = endGap - startGap;
                float gapCenterX = originX + (startGap + endGap) * 0.5f;
                AmbientAtmosphere2D.SpawnAbyssMist(
                    levelRoot,
                    gapCenterX,
                    GroundTopY,
                    gapWidth + 1.5f,
                    isDay: (theme == StageVisualTheme.DaylightMeadow)
                );
            }
        }

        // 2. Percorre as linhas superiores
        for (int r = 0; r < groundRow; r++)
        {
            string line = asciiMap[r];
            int heightAboveGround = (groundRow - 1) - r;

            int c = 0;
            while (c < line.Length)
            {
                char ch = line[c];

                // Plataforma de mão única ('=')
                if (ch == '=')
                {
                    int startCol = c;
                    while (c < line.Length && line[c] == '=') c++;
                    int endCol = c;

                    platformCount++;
                    float width = Mathf.Max(1.8f, (endCol - startCol) * 0.95f);
                    float centerX = originX + (startCol + endCol) * 0.5f - 0.5f;
                    float platTopY = GroundTopY + Mathf.Max(1f, heightAboveGround);
                    float platCenterY = platTopY - 0.25f;

                    CreateOneWayPlatform(
                        $"Platform_{platformCount:00}",
                        levelRoot,
                        new Vector2(centerX, platCenterY),
                        new Vector2(width, 0.5f),
                        theme
                    );
                    continue;
                }

                // Plataforma móvel ('~')
                if (ch == '~')
                {
                    int startCol = c;
                    while (c < line.Length && line[c] == '~') c++;
                    int endCol = c;

                    movingPlatformCount++;
                    float width = Mathf.Max(2.2f, (endCol - startCol) * 0.95f);
                    float centerX = originX + (startCol + endCol) * 0.5f - 0.5f;
                    float platTopY = GroundTopY + Mathf.Max(1f, heightAboveGround);
                    float platCenterY = platTopY - 0.25f;

                    CreateMovingPlatform(
                        $"MovingPlatform_{movingPlatformCount:00}",
                        levelRoot,
                        new Vector2(centerX, platCenterY),
                        new Vector2(width, 0.5f),
                        new Vector2(2.4f, 0f),
                        speed: 1.45f,
                        theme: theme
                    );
                    continue;
                }

                float cellX = originX + c;
                float supportY = GetEntitySupportY(asciiMap, r, c, groundRow);

                switch (ch)
                {
                    case 'P':
                        LastParsedPlayerSpawn = new Vector3(cellX, supportY + 0.475f, 0f);
                        break;

                    case 'B':
                        solidBlockCount++;
                        CreateSolidBlock($"SolidBlock_{solidBlockCount:00}", levelRoot, x: cellX, height: 0.95f, width: 0.95f, theme, baseY: supportY);
                        break;

                    case 'F':
                        breakableBlockCount++;
                        CreateBreakableBlock($"BreakableBlock_{breakableBlockCount:00}", levelRoot, x: cellX, baseY: supportY);
                        break;

                    case 'S':
                        switchCount++;
                        CreateSwitch($"Switch_{switchCount:00}", levelRoot, x: cellX, baseY: supportY, groupId: switchCount);
                        break;

                    case 'D':
                        gateCount++;
                        CreateSwitchGate($"SwitchGate_{gateCount:00}", levelRoot, x: cellX, baseY: supportY, groupId: gateCount);
                        break;

                    case 'M':
                        springCount++;
                        CreateSpringPad($"SpringPad_{springCount:00}", levelRoot, x: cellX, baseY: supportY);
                        break;

                    case '^':
                        obstacleCount++;
                        bool isPaired = (c + 1 < line.Length && line[c + 1] == '^') || (c > 0 && line[c - 1] == '^');
                        // Espinhos baixos e afiados (distintos dos blocos sólidos 'B')
                        float spikeHeight = 0.62f;
                        float spikeWidth = isPaired ? 0.80f : 0.85f;
                        float spikeX = cellX;
                        if (c > 0 && line[c - 1] == '^')
                        {
                            spikeX = originX + (c - 1) + 0.82f;
                        }
                        CreateObstacle($"Spikes_{obstacleCount:00}", levelRoot, x: spikeX, height: spikeHeight, width: spikeWidth, theme, baseY: supportY);
                        break;

                    case 'R':
                        robotCount++;
                        bool onPlatform = (r + 1 < groundRow && (asciiMap[r + 1][c] == '=' || asciiMap[r + 1][c] == '~'));
                        float speed = (robotCount == 1) ? 2.0f : (2.2f + (robotCount % 3) * 0.2f);
                        float range = onPlatform ? 1.5f : 2.2f;
                        CreateRobotEnemy($"ClankRobot_{robotCount:00}", levelRoot, x: cellX, patrolRange: range, speed: speed, baseY: supportY, type: EnemyBehaviorType.PatrolWalker);
                        break;

                    case 'J':
                        robotCount++;
                        CreateRobotEnemy($"JumperRobot_{robotCount:00}", levelRoot, x: cellX, patrolRange: 1.6f, speed: 2.0f, baseY: supportY, type: EnemyBehaviorType.Jumper);
                        break;

                    case 'T':
                        robotCount++;
                        CreateRobotEnemy($"TurretRobot_{robotCount:00}", levelRoot, x: cellX, patrolRange: 0f, speed: 0f, baseY: supportY, type: EnemyBehaviorType.Turret);
                        break;

                    case 'V':
                        robotCount++;
                        CreateRobotEnemy($"FlyingDrone_{robotCount:00}", levelRoot, x: cellX, patrolRange: 2.2f, speed: 2.2f, baseY: supportY + 0.15f, type: EnemyBehaviorType.FlyingDrone);
                        break;

                    case 'C':
                        checkpointCount++;
                        CreateCheckpoint($"Checkpoint_{checkpointCount:00}", levelRoot, x: cellX, baseY: supportY, index: checkpointCount);
                        break;

                    case 'o':
                        coinCount++;
                        CreateCoin($"Coin_{coinCount:00}", levelRoot, x: cellX, baseY: supportY);
                        break;

                    case 'G':
                        CreateStyledBlock(
                            "Goal_Portal",
                            levelRoot,
                            new Vector2(cellX + 0.5f, supportY + 2.0f),
                            new Vector2(1.2f, 4.0f),
                            StageBlockStyle.GoalPortal,
                            isTrigger: true,
                            theme
                        );
                        break;
                }

                c++;
            }
        }

        LastGeneratedObstacleCount = obstacleCount;
        LastGeneratedRobotCount = robotCount;
        LastGeneratedCoinCount = coinCount + breakableBlockCount;
    }

    /// <summary>
    /// Gera as Fases 2 (Pradaria Solar, diurna) e 3 (Abismo Cristalino, noturna e mais difícil) de forma 100% procedural.
    /// A cada seed variam: quantidade e tamanho das ilhas, altura (degraus) do terreno, tipo de travessia dos abismos
    /// (salto simples, plataforma móvel ou pedras flutuantes), pesos dos 10 padrões de encontro e inimigos usados.
    /// </summary>
    private static void BuildProceduralStageGeometry(Transform levelRoot, int seed, int stageNumber)
    {
        System.Random rng = new System.Random(seed);
        bool nightStage = stageNumber >= 3;
        StageVisualTheme theme = nightStage ? StageVisualTheme.CrystalAbyss : StageVisualTheme.DaylightMeadow;
        string tag = "S" + stageNumber;
        float hard = nightStage ? 1f : 0f;
        float speedMul = 1f + 0.12f * hard;
        Color crystalTint = (rng.NextDouble() < 0.5) ? new Color(0.65f, 0.98f, 1f, 1f) : new Color(1f, 0.62f, 0.95f, 1f);

        int obstacleCount = 0, robotCount = 0, checkpointCount = 0, coinCount = 0;
        int springCount = 0, solidCount = 0, breakableCount = 0, platformCount = 0;

        void Spike(float x, float baseY, float w = 0.85f)
        {
            obstacleCount++;
            CreateObstacle($"Spikes_{tag}_{obstacleCount:00}", levelRoot, x: x, height: 0.65f, width: w, theme, baseY: baseY);
        }
        void Coin(float x, float baseY)
        {
            coinCount++;
            CreateCoin($"Coin_{tag}_{coinCount:00}", levelRoot, x: x, baseY: baseY);
        }
        void Robot(float x, float range, float speed, float baseY, EnemyBehaviorType type)
        {
            robotCount++;
            CreateRobotEnemy($"Robot_{tag}_{robotCount:00}", levelRoot, x: x, patrolRange: range, speed: speed * speedMul, baseY: baseY, type: type);
        }
        void Block(float x, float h, float baseY)
        {
            solidCount++;
            CreateSolidBlock($"SolidBlock_{tag}_{solidCount:00}", levelRoot, x: x, height: h, width: 0.95f, theme, baseY: baseY);
        }
        void Platform(float cx, float topY, float w)
        {
            platformCount++;
            CreateOneWayPlatform($"Platform_{tag}_{platformCount:00}", levelRoot, new Vector2(cx, topY - 0.25f), new Vector2(w, 0.5f), theme);
        }
        void Checkpoint(float x, float baseY)
        {
            checkpointCount++;
            CreateCheckpoint($"Checkpoint_{tag}_{checkpointCount:00}", levelRoot, x: x, baseY: baseY, index: checkpointCount);
        }
        EnemyBehaviorType PickGroundEnemy()
        {
            double roll = rng.NextDouble();
            double pJumper = 0.28 + 0.10 * hard;
            double pTurret = 0.20 + 0.14 * hard;
            if (roll < pJumper) return EnemyBehaviorType.Jumper;
            if (roll < pJumper + pTurret) return EnemyBehaviorType.Turret;
            return EnemyBehaviorType.PatrolWalker;
        }
        void Decorate(float x, float gy, bool pillar, bool flora)
        {
            if (nightStage)
            {
                // Fase 3 (Montanhas Nevadas): pinheiros cobertos de neve e cristais de gelo apoiados no chão
                Color icyTint = (rng.NextDouble() < 0.5) ? new Color(0.68f, 0.96f, 1.00f, 1f) : new Color(0.82f, 0.94f, 1.00f, 1f);
                if (pillar) AmbientAtmosphere2D.SpawnNightPropOnIsland(levelRoot, x, gy, 3, icyTint);
                if (flora) AmbientAtmosphere2D.SpawnNightPropOnIsland(levelRoot, x + 1.4f, gy, 2, icyTint);
            }
            else
            {
                AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, x, gy, isPillar: pillar, isSunflower: flora);
            }
        }

        // ---- Padrões de encontro: [id] = largura mínima necessária ----
        float[] need = { 10.0f, 8.0f, 9.0f, 7.8f, 8.0f, 10.0f, 8.4f, 8.0f, 8.6f, 4.0f, 9.5f, 11.0f };

        float RunPattern(int id, float x, float usableEnd, float gy)
        {
            switch (id)
            {
                case 0: // Mola + plataforma alta + espinhos embaixo + robô na saída
                {
                    springCount++;
                    CreateSpringPad($"SpringPad_{tag}_{springCount:00}", levelRoot, x: x + 0.2f, baseY: gy);
                    float platW = NextRange(rng, 4.0f, 5.2f);
                    float platX = x + 1.4f + platW * 0.5f;
                    float platTop = gy + NextRange(rng, 1.8f, 2.2f);
                    Platform(platX, platTop, platW);
                    Coin(platX - 0.8f, platTop);
                    Coin(platX + 0.8f, platTop);
                    Spike(platX, gy);
                    float robX = Mathf.Min(usableEnd - 1.2f, platX + platW * 0.5f + 2.2f);
                    Robot(robX, NextRange(rng, 2.0f, 2.8f), NextRange(rng, 2.6f, 3.3f), gy, EnemyBehaviorType.PatrolWalker);
                    return robX + NextRange(rng, 3.4f, 4.4f);
                }
                case 1: // Espinhos duplos + moeda alta + robô
                {
                    Spike(x, gy, 0.78f);
                    Spike(x + 0.82f, gy, 0.78f);
                    Coin(x + 0.4f, gy + 1.2f);
                    Robot(x + 3.8f, NextRange(rng, 2.0f, 2.7f), NextRange(rng, 2.6f, 3.3f), gy, EnemyBehaviorType.PatrolWalker);
                    return x + NextRange(rng, 6.4f, 7.4f);
                }
                case 2: // Inimigo variado (+ drone) e bloco (frágil ou sólido)
                {
                    Robot(x + 1.0f, NextRange(rng, 1.8f, 2.6f), NextRange(rng, 2.4f, 3.1f), gy, PickGroundEnemy());
                    if (rng.NextDouble() < 0.35 + 0.2 * hard)
                    {
                        Robot(x + 2.8f, 2.0f, 2.3f, gy + 1.45f, EnemyBehaviorType.FlyingDrone);
                    }
                    if (rng.NextDouble() < 0.55)
                    {
                        breakableCount++;
                        CreateBreakableBlock($"BreakableBlock_{tag}_{breakableCount:00}", levelRoot, x: x + 4.4f, baseY: gy);
                    }
                    else
                    {
                        Block(x + 4.4f, 1.05f, gy);
                    }
                    return x + NextRange(rng, 7.4f, 8.4f);
                }
                case 3: // Espinho simples + robô
                {
                    Spike(x, gy);
                    Robot(x + 3.6f, NextRange(rng, 2.1f, 2.8f), NextRange(rng, 2.6f, 3.3f), gy, EnemyBehaviorType.PatrolWalker);
                    return x + NextRange(rng, 6.4f, 7.2f);
                }
                case 4: // Escadaria de blocos sólidos com moeda no topo
                {
                    Block(x, 0.95f, gy);
                    Block(x + 1.0f, 1.9f, gy);
                    Coin(x + 1.0f, gy + 1.9f);
                    Spike(x + 2.7f, gy);
                    if (rng.NextDouble() < 0.65)
                    {
                        Robot(x + 5.0f, 2.0f, NextRange(rng, 2.5f, 3.2f), gy, PickGroundEnemy());
                    }
                    return x + NextRange(rng, 6.6f, 7.4f);
                }
                case 5: // Estrada alta: plataforma longa com moedas, robô patrulhando por baixo
                {
                    float platW = NextRange(rng, 5.0f, 7.0f);
                    float cx = x + 1.0f + platW * 0.5f;
                    float top = gy + NextRange(rng, 1.7f, 2.1f);
                    Platform(cx, top, platW);
                    Coin(cx - platW * 0.3f, top);
                    Coin(cx, top);
                    Coin(cx + platW * 0.3f, top);
                    Robot(cx, Mathf.Min(2.4f, platW * 0.4f), NextRange(rng, 2.6f, 3.2f), gy, EnemyBehaviorType.PatrolWalker);
                    return x + 1.0f + platW + NextRange(rng, 1.5f, 2.5f);
                }
                case 6: // Torreta com bloco de cobertura
                {
                    Block(x + 2.0f, 0.95f, gy);
                    Coin(x + 2.0f, gy + 0.95f);
                    Spike(x + 3.9f, gy);
                    Robot(x + 5.8f, 0f, 0f, gy, EnemyBehaviorType.Turret);
                    return x + NextRange(rng, 7.2f, 7.8f);
                }
                case 7: // Dois saltadores com trilha de moedas
                {
                    Robot(x + 1.0f, 1.5f, 2.0f, gy, EnemyBehaviorType.Jumper);
                    Coin(x + 2.4f, gy + 1.3f);
                    Coin(x + 3.0f, gy + 1.6f);
                    Robot(x + 4.8f, 1.4f, 2.0f, gy, EnemyBehaviorType.Jumper);
                    return x + NextRange(rng, 6.8f, 7.6f);
                }
                case 8: // Corredor de drones (derrubáveis por baixo)
                {
                    Robot(x + 1.5f, 2.0f, 2.3f, gy + 1.3f, EnemyBehaviorType.FlyingDrone);
                    Spike(x + 3.6f, gy);
                    Coin(x + 3.6f, gy + 1.1f);
                    Robot(x + 5.8f, 2.0f, 2.5f, gy + 1.5f, EnemyBehaviorType.FlyingDrone);
                    return x + NextRange(rng, 7.4f, 8.2f);
                }
                case 10: // Torre de plataformas em zigue-zague até o topo (vertical), com recompensa e guardião
                {
                    float[] offs = { 1.6f, 3.2f, 4.8f };
                    int tiers = nightStage ? 3 : rng.Next(2, 4);
                    float dir = (rng.NextDouble() < 0.5) ? 1f : -1f;
                    float cx0 = x + 3.0f;
                    float topY = gy;
                    for (int t = 0; t < tiers; t++)
                    {
                        topY = gy + offs[t];
                        float px = cx0 + dir * ((t % 2 == 0) ? -1.4f : 1.4f);
                        Platform(px, topY, 2.6f);
                        if (t < tiers - 1) Coin(px, topY);
                    }
                    float topX = cx0 + dir * (((tiers - 1) % 2 == 0) ? -1.4f : 1.4f);
                    Coin(topX - 0.5f, topY);
                    Coin(topX + 0.5f, topY);
                    Coin(topX, topY + 0.9f);
                    if (rng.NextDouble() < 0.5 + 0.3 * hard)
                    {
                        Robot(topX, 0.8f, 1.6f, topY, EnemyBehaviorType.PatrolWalker);
                    }
                    Spike(cx0, gy);
                    Robot(x + 7.2f, 1.8f, NextRange(rng, 2.4f, 3.0f), gy, PickGroundEnemy());
                    return x + 8.8f;
                }
                case 11: // Ponte aérea: mola lança até uma passarela alta, com drone e moedas
                {
                    springCount++;
                    CreateSpringPad($"SpringPad_{tag}_{springCount:00}", levelRoot, x: x + 0.3f, baseY: gy);
                    float len = NextRange(rng, 6.0f, 7.5f);
                    float top = gy + NextRange(rng, 2.3f, 2.8f);
                    float cx = x + 1.8f + len * 0.5f;
                    Platform(cx, top, len);
                    Coin(cx - len * 0.3f, top);
                    Coin(cx, top);
                    Coin(cx + len * 0.3f, top);
                    Robot(cx, 2.2f, 2.4f, top + 1.15f, EnemyBehaviorType.FlyingDrone);
                    Spike(x + 3.0f, gy);
                    return x + 2.4f + len + NextRange(rng, 0.8f, 1.8f);
                }
                default: // Descanso com arco de moedas
                {
                    Coin(x, gy);
                    Coin(x + 1.0f, gy + 0.8f);
                    Coin(x + 2.0f, gy + 0.8f);
                    Coin(x + 3.0f, gy);
                    return x + NextRange(rng, 3.8f, 4.6f);
                }
            }
        }

        // Pesos base por fase, embaralhados por seed para dar "personalidade" a cada fase gerada
        float[] baseWeights =
        {
            1.0f, 1.0f, 1.2f, 0.9f, 1.0f, 0.9f,
            0.6f + 0.7f * hard, 0.6f + 0.7f * hard, 0.45f + 0.7f * hard, 0.5f,
            1.1f + 0.5f * hard, 0.9f + 0.5f * hard
        };
        float[] levelWeights = new float[baseWeights.Length];
        for (int i = 0; i < baseWeights.Length; i++)
        {
            levelWeights[i] = baseWeights[i] * NextRange(rng, 0.35f, 1.65f);
        }

        int lastPattern = -1;

        void FillIsland(float usableStart, float usableEnd, float gy, bool gentle)
        {
            float x = usableStart;
            while (true)
            {
                float total = 0f;
                float[] w = new float[levelWeights.Length];
                for (int i = 0; i < w.Length; i++)
                {
                    if (x + need[i] > usableEnd) continue;
                    if (gentle && (i == 6 || i == 7 || i == 8)) continue;
                    w[i] = levelWeights[i] * (i == lastPattern ? 0.25f : 1f);
                    total += w[i];
                }
                if (total <= 0f) break;

                float pick = (float)rng.NextDouble() * total;
                int chosen = 0;
                for (int i = 0; i < w.Length; i++)
                {
                    if (w[i] <= 0f) continue;
                    pick -= w[i];
                    if (pick <= 0f) { chosen = i; break; }
                    chosen = i;
                }

                lastPattern = chosen;
                float next = RunPattern(chosen, x, usableEnd, gy);
                x = next + NextRange(rng, 0.4f, 2.4f);
            }
        }

        // ---- Ilha 0 (segura no início, spawn em x = -6) ----
        float islandStartX = -12.0f;
        float islandLen = NextRange(rng, 18.0f, 25.0f);
        float islandEndX = islandStartX + islandLen;
        float gyCurrent = GroundTopY;
        CreateGroundSegment($"Ground_{tag}_Island0", levelRoot, islandStartX, islandEndX, theme, gyCurrent);

        Decorate(-8.5f, gyCurrent, pillar: true, flora: false);
        Decorate(-3.2f, gyCurrent, pillar: false, flora: true);
        FillIsland(-0.8f, islandEndX - 3.4f, gyCurrent, gentle: true);
        Checkpoint(islandEndX - 1.8f, gyCurrent);

        // ---- Abismos e ilhas seguintes ----
        int totalChasms = nightStage ? rng.Next(4, 8) : rng.Next(3, 7);
        float[] deltaChoices = { -2.0f, -1.0f, -0.5f, 0.5f, 1.0f, 1.5f, 2.0f, 1.0f, 2.0f };
        float minTop = GroundTopY - 1.0f;
        float maxTop = GroundTopY + (nightStage ? 6.0f : 4.5f);

        for (int islandIdx = 1; islandIdx <= totalChasms; islandIdx++)
        {
            bool isFinalIsland = (islandIdx == totalChasms);
            float prevTop = gyCurrent;
            float nextTop = Mathf.Clamp(prevTop + deltaChoices[rng.Next(0, deltaChoices.Length)], minTop, maxTop);
            float stepUp = nextTop - prevTop;

            double crossRoll = rng.NextDouble();
            double pPlain = nightStage ? 0.25 : 0.35;
            double pMover = nightStage ? 0.30 : 0.25;

            float chasmStart = islandEndX;
            float chasmWidth;

            if (crossRoll < pPlain)
            {
                chasmWidth = NextRange(rng, 2.6f, 4.0f);
                if (stepUp > 1.4f) chasmWidth = Mathf.Min(chasmWidth, 2.8f);
                else if (stepUp > 0.4f) chasmWidth = Mathf.Min(chasmWidth, 3.4f);
            }
            else if (crossRoll < pPlain + pMover)
            {
                chasmWidth = NextRange(rng, 6.2f, 7.6f);
                float midY = (prevTop + nextTop) * 0.5f + NextRange(rng, 0.5f, 0.8f);
                float cx = chasmStart + chasmWidth * 0.5f;
                platformCount++;
                CreateMovingPlatform(
                    $"MovingPlatform_{tag}_{islandIdx}",
                    levelRoot,
                    new Vector2(cx, midY - 0.25f),
                    new Vector2(NextRange(rng, 2.8f, 3.5f), 0.5f),
                    new Vector2(2.2f, 0f),
                    speed: NextRange(rng, 1.3f, 1.7f),
                    theme: theme
                );
                Coin(cx, midY + 0.35f);
            }
            else
            {
                int stones = rng.Next(1, 4);
                float gap = NextRange(rng, 1.6f, 2.2f);
                float cursor = chasmStart + gap;
                for (int s = 0; s < stones; s++)
                {
                    float stoneW = NextRange(rng, 1.8f, 2.4f);
                    float t = (s + 1f) / (stones + 1f);
                    float stoneTop = Mathf.Lerp(prevTop, nextTop, t) + NextRange(rng, 0.6f, 1.2f);
                    Platform(cursor + stoneW * 0.5f, stoneTop, stoneW);
                    if (rng.NextDouble() < 0.7) Coin(cursor + stoneW * 0.5f, stoneTop);
                    cursor += stoneW + NextRange(rng, 1.6f, 2.2f);
                }
                chasmWidth = cursor - chasmStart;
            }

            AmbientAtmosphere2D.SpawnAbyssMist(levelRoot, chasmStart + chasmWidth * 0.5f, GroundTopY, chasmWidth, isDay: !nightStage);

            float islandStart = chasmStart + chasmWidth;
            islandLen = isFinalIsland ? NextRange(rng, 22.0f, 28.0f) : NextRange(rng, 14.0f, 28.0f);
            islandEndX = islandStart + islandLen;
            gyCurrent = nextTop;

            CreateGroundSegment($"Ground_{tag}_Island{islandIdx}", levelRoot, islandStart, islandEndX, theme, gyCurrent);
            Checkpoint(islandStart + 1.6f, gyCurrent);
            if (islandLen > 20.5f)
            {
                Checkpoint(islandStart + islandLen * 0.55f, gyCurrent);
            }

            Decorate(islandStart + 2.2f, gyCurrent, pillar: true, flora: true);
            Decorate(islandStart + islandLen * 0.5f, gyCurrent, pillar: false, flora: true);
            Decorate(islandEndX - 2.4f, gyCurrent, pillar: true, flora: false);

            float usableStart = islandStart + 3.6f;
            float usableEnd = isFinalIsland ? (islandEndX - 7.0f) : (islandEndX - 3.2f);
            FillIsland(usableStart, usableEnd, gyCurrent, gentle: false);

            if (isFinalIsland)
            {
                float gateX = islandEndX - 5.6f;
                float switchX = gateX - 2.6f;
                CreateSwitch($"Switch_{tag}_01", levelRoot, x: switchX, baseY: gyCurrent, groupId: 1);
                CreateSwitchGate($"SwitchGate_{tag}_01", levelRoot, x: gateX, baseY: gyCurrent, groupId: 1);
            }
        }

        float goalX = islandEndX - 3.2f;
        CreateStyledBlock("Goal_Portal", levelRoot, new Vector2(goalX, gyCurrent + 2.0f), new Vector2(1.2f, 4.0f), StageBlockStyle.GoalPortal, isTrigger: true, theme);

        if (nightStage)
        {
            AmbientAtmosphere2D.PopulateProceduralNightDepth(levelRoot, gyCurrent, goalX, seed);
        }
        else
        {
            AmbientAtmosphere2D.PopulateDaylightStageDepth(levelRoot, gyCurrent, goalX);
        }

        LastGeneratedObstacleCount = obstacleCount;
        LastGeneratedRobotCount = robotCount;
        LastGeneratedCoinCount = coinCount + breakableCount;
    }

    private static float NextRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static GameObject CreateGroundSegment(string name, Transform parent, float startX, float endX, StageVisualTheme theme, float topY = GroundTopY)
    {
        float width = Mathf.Max(2f, endX - startX);
        float centerX = startX + width * 0.5f;
        float height = Mathf.Max(3f, topY + 6.0f);
        return CreateStyledBlock(name, parent, new Vector2(centerX, topY - height * 0.5f), new Vector2(width, height), StageBlockStyle.Ground, isTrigger: false, theme);
    }

    public static void EnsureRobotsInScene(Transform parent = null)
    {
        if (Object.FindAnyObjectByType<RobotEnemy2D>() != null) return;

        if (parent == null)
        {
            GameObject levelRoot = GameObject.Find("Level_2D");
            if (levelRoot != null) parent = levelRoot.transform;
        }

        SpawnStage1Robots(parent);
    }

    public static void EnsureCheckpointsInScene(Transform parent = null)
    {
        if (Object.FindAnyObjectByType<Checkpoint2D>() != null) return;

        if (parent == null)
        {
            GameObject levelRoot = GameObject.Find("Level_2D");
            if (levelRoot != null) parent = levelRoot.transform;
        }

        CreateCheckpoint("Checkpoint_01", parent, x: 15f, baseY: GroundTopY, index: 1);
        CreateCheckpoint("Checkpoint_02", parent, x: 39f, baseY: GroundTopY, index: 2);
        CreateCheckpoint("Checkpoint_03", parent, x: 64f, baseY: GroundTopY, index: 3);
    }

    private static void SpawnStage1Robots(Transform parent)
    {
        CreateRobotEnemy("ClankRobot_01", parent, x: 12f, patrolRange: 2.2f, speed: 2.0f);
        CreateRobotEnemy("ClankRobot_02", parent, x: 24f, patrolRange: 2.2f, speed: 2.3f);
        CreateRobotEnemy("ClankRobot_03", parent, x: 51f, patrolRange: 1.5f, speed: 2.3f, baseY: GroundTopY + 1.0f);
        CreateRobotEnemy("ClankRobot_04", parent, x: 72f, patrolRange: 2.2f, speed: 2.5f);
        CreateRobotEnemy("ClankRobot_05", parent, x: 76f, patrolRange: 2.2f, speed: 2.6f);
    }

    private static GameObject CreateOneWayPlatform(string name, Transform parent, Vector2 position, Vector2 size, StageVisualTheme theme)
    {
        GameObject obj = CreateStyledBlock(name, parent, position, size, StageBlockStyle.Platform, isTrigger: false, theme);
        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.usedByEffector = true;
        }
        PlatformEffector2D effector = obj.GetComponent<PlatformEffector2D>();
        if (effector == null)
        {
            effector = obj.AddComponent<PlatformEffector2D>();
        }
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
        return obj;
    }

    private static GameObject CreateMovingPlatform(string name, Transform parent, Vector2 position, Vector2 size, Vector2 travelOffset, float speed, StageVisualTheme theme)
    {
        GameObject obj = CreateOneWayPlatform(name, parent, position, size, theme);
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody2D>();
        }
        MovingPlatform2D moving = obj.AddComponent<MovingPlatform2D>();
        moving.Configure(position, travelOffset, speed);
        return obj;
    }

    private static GameObject CreateSpringPad(string name, Transform parent, float x, float baseY)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(x, baseY + 0.46f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        SpringPad2D spring = obj.AddComponent<SpringPad2D>();
        spring.EnsureVisuals();
        return obj;
    }

    private static GameObject CreateSolidBlock(string name, Transform parent, float x, float height, float width, StageVisualTheme theme = StageVisualTheme.NightRuins, float baseY = GroundTopY)
    {
        Vector2 pos = new Vector2(x, baseY + height * 0.5f);
        Vector2 size = new Vector2(width, height);
        return CreateStyledBlock(name, parent, pos, size, StageBlockStyle.SolidBlock, isTrigger: false, theme);
    }

    private static GameObject CreateBreakableBlock(string name, Transform parent, float x, float baseY = GroundTopY)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(x, baseY + 0.475f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.size = new Vector2(0.95f, 0.95f);

        BreakableBlock2D breakable = obj.AddComponent<BreakableBlock2D>();
        breakable.EnsureVisuals();
        return obj;
    }

    private static GameObject CreateSwitch(string name, Transform parent, float x, float baseY, int groupId)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(x, baseY + 0.46f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Switch2D sw = obj.AddComponent<Switch2D>();
        sw.targetGateGroupId = groupId;
        sw.EnsureVisuals();
        return obj;
    }

    private static GameObject CreateSwitchGate(string name, Transform parent, float x, float baseY, int groupId)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        const float gateHeight = 8.5f;
        Vector3 worldPos = new Vector3(x, baseY + gateHeight * 0.5f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        SwitchGate2D gate = obj.AddComponent<SwitchGate2D>();
        gate.ConfigureSpawn(worldPos, groupId);
        return obj;
    }

    private static GameObject CreateCoin(string name, Transform parent, float x, float baseY)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        Vector3 worldPos = new Vector3(x, baseY + 0.55f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.55f, 0.55f);

        Coin2D coin = obj.AddComponent<Coin2D>();
        coin.ConfigureSpawn(worldPos);
        return obj;
    }

    private static GameObject CreateCheckpoint(string name, Transform parent, float x, float baseY, int index)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(x, baseY + 0.46f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.95f, 1.8f);

        Checkpoint2D cp = obj.AddComponent<Checkpoint2D>();
        cp.checkpointIndex = index;
        cp.EnsureVisuals();

        return obj;
    }

    private static GameObject CreateRobotEnemy(string name, Transform parent, float x, float patrolRange, float speed, float baseY = GroundTopY, EnemyBehaviorType type = EnemyBehaviorType.PatrolWalker)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        Vector3 spawnPos = new Vector3(x, baseY + 0.46f, 0f);
        obj.transform.position = spawnPos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = (type == EnemyBehaviorType.FlyingDrone) ? new Vector2(0.86f, 0.72f) : new Vector2(0.82f, 0.88f);

        Hazard2D hazard = obj.AddComponent<Hazard2D>();
        hazard.damage = 1;
        hazard.stompable = (type != EnemyBehaviorType.FlyingDrone);
        hazard.blocksGroundCheck = true;

        RobotEnemy2D robot = obj.AddComponent<RobotEnemy2D>();
        robot.ConfigureSpawn(spawnPos, patrolRange, speed, -1, type);

        return obj;
    }

    private static GameObject CreateObstacle(string name, Transform parent, float x, float height, float width, StageVisualTheme theme = StageVisualTheme.NightRuins, float baseY = GroundTopY)
    {
        Vector2 pos = new Vector2(x, baseY + height * 0.5f);
        Vector2 size = new Vector2(width, height);
        GameObject obj = CreateStyledBlock(name, parent, pos, size, StageBlockStyle.Obstacle, isTrigger: false, theme);
        Hazard2D hazard = obj.GetComponent<Hazard2D>();
        if (hazard == null) hazard = obj.AddComponent<Hazard2D>();
        hazard.damage = 1;
        hazard.stompable = false;
        hazard.blocksGroundCheck = true;
        return obj;
    }

    private static GameObject CreateStyledBlock(string name, Transform parent, Vector2 position, Vector2 size, StageBlockStyle style, bool isTrigger, StageVisualTheme theme = StageVisualTheme.NightRuins)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = Vector3.one;

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = isTrigger;

        if (style == StageBlockStyle.Obstacle)
        {
            Hazard2D hazard = obj.AddComponent<Hazard2D>();
            hazard.damage = 1;
            hazard.stompable = false;
            hazard.blocksGroundCheck = true;
        }
        else if (style == StageBlockStyle.GoalPortal)
        {
            obj.AddComponent<Goal2D>();
        }

        PixelTileBlock2D tileBlock = obj.AddComponent<PixelTileBlock2D>();
        tileBlock.Configure(style, size, theme);

        return obj;
    }
}
