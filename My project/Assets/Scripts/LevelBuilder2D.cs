using UnityEngine;

public static class LevelBuilder2D
{
    public const float GroundTopY = -2.0f;

    public static int CurrentStage { get; private set; } = 1;
    public static int CurrentSeed { get; private set; } = 0;
    public static int LastGeneratedObstacleCount { get; private set; } = 9;
    public static int LastGeneratedRobotCount { get; private set; } = 6;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureLevelOnPlay()
    {
        if (Object.FindAnyObjectByType<BoxPlayer2D>() == null || Object.FindAnyObjectByType<ParallaxBackdrop2D>() == null)
        {
            BuildScene();
        }
        else
        {
            EnsureRobotsInScene();
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

        // Jogador (Bonecos Pixel Art 32x32 reestilizados)
        GameObject playerObj = new GameObject("PlayerBox");
        playerObj.transform.position = new Vector3(-6f, GroundTopY + 0.55f, 0f);

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

        ConfigureCameraForStage(playerObj.transform, isDaylight: false);
    }

    /// <summary>
    /// Reconstrói apenas a geometria da fase (Fase 1 autoral noturna ou Fase 2 procedural e aleatória diurna)
    /// preservando o objeto do jogador e seus poderes.
    /// </summary>
    public static void BuildStageForExistingPlayer(BoxPlayer2D player, int stageNumber, int? customSeed = null)
    {
        CurrentStage = stageNumber;

        // Limpa projéteis ou efeitos temporários ativos
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
                ConfigureCameraForStage(player.transform, isDaylight: false);
            }
        }
        else
        {
            int seed = customSeed ?? Random.Range(10000, 99999);
            CurrentSeed = seed;
            BuildStage2ProceduralGeometry(levelRoot.transform, seed);
            if (player != null)
            {
                ConfigureCameraForStage(player.transform, isDaylight: true);
            }
        }
    }

    private static void ConfigureCameraForStage(Transform playerTransform, bool isDaylight)
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = isDaylight
                ? new Color(0.36f, 0.78f, 1.00f, 1f)
                : new Color(0.04f, 0.05f, 0.12f, 1f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.6f;

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
        // 1. Profundidade de fundo Noturna (Céu Pixel Art, Parallax de Montanhas/Ruínas, Tochas, Pilares, Cristais, Neve)
        AmbientAtmosphere2D.PopulateStageDepth(levelRoot, GroundTopY);

        // 2. Chão em blocos 32x32 estilizados com abismo no meio
        CreateStyledBlock("Ground_Part1", levelRoot, new Vector2(17.5f, -3.5f), new Vector2(59f, 3f), StageBlockStyle.Ground, isTrigger: false, StageVisualTheme.NightRuins);
        CreateStyledBlock("Ground_Part2", levelRoot, new Vector2(69.1f, -3.5f), new Vector2(37.8f, 3f), StageBlockStyle.Ground, isTrigger: false, StageVisualTheme.NightRuins);

        // 3. Obstáculos (Monólitos amaldiçoados com espinhos e cristais rubi 32x32)
        CreateObstacle("Obstacle_01", levelRoot, x: 3f, height: 1.0f, width: 0.85f, StageVisualTheme.NightRuins);
        CreateObstacle("Obstacle_02", levelRoot, x: 12f, height: 1.35f, width: 0.9f, StageVisualTheme.NightRuins);

        CreateObstacle("Obstacle_03A", levelRoot, x: 21f, height: 0.95f, width: 0.8f, StageVisualTheme.NightRuins);
        CreateObstacle("Obstacle_03B", levelRoot, x: 21.85f, height: 0.95f, width: 0.8f, StageVisualTheme.NightRuins);

        CreateObstacle("Obstacle_04", levelRoot, x: 30f, height: 1.15f, width: 0.85f, StageVisualTheme.NightRuins);
        CreateStyledBlock("Platform_Safe", levelRoot, new Vector2(37.5f, -0.9f), new Vector2(4.5f, 0.5f), StageBlockStyle.Platform, isTrigger: false, StageVisualTheme.NightRuins);
        CreateObstacle("Obstacle_05_AfterPlatform", levelRoot, x: 41.2f, height: 1.3f, width: 0.85f, StageVisualTheme.NightRuins);

        CreateObstacle("Obstacle_06", levelRoot, x: 57f, height: 1.1f, width: 0.85f, StageVisualTheme.NightRuins);
        CreateObstacle("Obstacle_07", levelRoot, x: 65f, height: 1.4f, width: 0.9f, StageVisualTheme.NightRuins);
        CreateObstacle("Obstacle_08A", levelRoot, x: 73.5f, height: 1.05f, width: 0.8f, StageVisualTheme.NightRuins);
        CreateObstacle("Obstacle_08B", levelRoot, x: 74.35f, height: 1.05f, width: 0.8f, StageVisualTheme.NightRuins);

        // 4. Portal de Chegada (Santuário Rúnico Celestial)
        CreateStyledBlock("Goal_Portal", levelRoot, new Vector2(82.5f, GroundTopY + 2.0f), new Vector2(1.2f, 4.0f), StageBlockStyle.GoalPortal, isTrigger: true, StageVisualTheme.NightRuins);

        // 5. Inimigos Robozinhos estilo Clank em 32x32
        SpawnStage1Robots(levelRoot);

        LastGeneratedObstacleCount = 10;
        LastGeneratedRobotCount = 6;
    }

    /// <summary>
    /// Gera a Fase 2 de forma 100% procedural e aleatória, com estética diurna alegre e dificuldade elevada
    /// (mais abismos, plataformas, obstáculos e robozinhos mais velozes), respeitando as métricas de pulo.
    /// </summary>
    private static void BuildStage2ProceduralGeometry(Transform levelRoot, int seed)
    {
        System.Random rng = new System.Random(seed);
        const StageVisualTheme theme = StageVisualTheme.DaylightMeadow;

        int obstacleCount = 0;
        int robotCount = 0;

        // Ilha Inicial (Warm-up onde o jogador nasce em x = -6 e já pode testar seu Novo Poder!)
        float currentStartX = -12.0f;
        float firstIslandLength = NextRange(rng, 23.0f, 26.0f);
        float firstIslandEndX = currentStartX + firstIslandLength;
        CreateGroundSegment("Ground_Stage2_Island0", levelRoot, currentStartX, firstIslandEndX, theme);

        // Decoração diurna na ilha inicial
        AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, -8.5f, GroundTopY, isPillar: true, isSunflower: false);
        AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, -3.2f, GroundTopY, isPillar: false, isSunflower: true);
        AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, 5.0f, GroundTopY, isPillar: true, isSunflower: true);

        // Robô de boas-vindas para testar o novo poder (Pulo Esmagador do Menino ou Garras da Kitty)
        robotCount++;
        CreateRobotEnemy($"ClankRobot_S2_{robotCount:00}", levelRoot, x: 1.2f, patrolRange: 2.2f, speed: 2.5f);

        // Primeiro obstáculo + segundo robô na ilha inicial
        obstacleCount++;
        CreateObstacle($"Obstacle_S2_{obstacleCount:00}", levelRoot, x: firstIslandEndX - 4.2f, height: NextRange(rng, 1.05f, 1.28f), width: 0.85f, theme);

        float cursorEndX = firstIslandEndX;
        int totalChasms = rng.Next(3, 5); // 3 a 4 abismos procedurais

        for (int islandIdx = 1; islandIdx <= totalChasms; islandIdx++)
        {
            bool bridgedCanyon = (rng.NextDouble() < 0.45);
            float chasmWidth = bridgedCanyon ? NextRange(rng, 6.4f, 7.8f) : NextRange(rng, 3.1f, 4.0f);
            float chasmCenterX = cursorEndX + chasmWidth * 0.5f;

            // Névoa luminosa no abismo procedural
            AmbientAtmosphere2D.SpawnAbyssMist(levelRoot, chasmCenterX, GroundTopY, chasmWidth, isDay: true);

            // Se for um cânion largo, cria uma plataforma flutuante no meio para travessia dinâmica
            if (bridgedCanyon)
            {
                float bridgePlatW = NextRange(rng, 3.0f, 3.8f);
                float bridgePlatY = GroundTopY + NextRange(rng, 0.85f, 1.35f);
                CreateStyledBlock(
                    $"Platform_Bridge_{islandIdx}",
                    levelRoot,
                    new Vector2(chasmCenterX, bridgePlatY),
                    new Vector2(bridgePlatW, 0.5f),
                    StageBlockStyle.Platform,
                    isTrigger: false,
                    theme
                );
            }

            float islandStart = cursorEndX + chasmWidth;
            bool isFinalIsland = (islandIdx == totalChasms);
            float islandLength = isFinalIsland ? NextRange(rng, 24.0f, 28.0f) : NextRange(rng, 22.0f, 27.5f);
            float islandEnd = islandStart + islandLength;

            CreateGroundSegment($"Ground_Stage2_Island{islandIdx}", levelRoot, islandStart, islandEnd, theme);

            // Props diurnos (pilares solares e girassóis)
            AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, islandStart + 2.2f, GroundTopY, isPillar: true, isSunflower: true);
            AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, islandStart + islandLength * 0.5f, GroundTopY, isPillar: false, isSunflower: true);
            AmbientAtmosphere2D.SpawnDaylightPropOnIsland(levelRoot, islandEnd - 2.4f, GroundTopY, isPillar: true, isSunflower: false);

            // Preenche a ilha com encontros procedurais respeitando margens seguras de pouso/pulo
            float usableStart = islandStart + 3.0f;
            float usableEnd = isFinalIsland ? (islandEnd - 6.8f) : (islandEnd - 3.2f);
            float x = usableStart;

            while (x < usableEnd - 2.5f)
            {
                int pattern = rng.Next(0, 4);

                if (pattern == 0 && (x + 6.5f < usableEnd))
                {
                    // Padrão A: Plataforma elevada com obstáculo embaixo e robô veloz logo adiante
                    float platW = NextRange(rng, 4.0f, 5.2f);
                    float platX = x + platW * 0.5f;
                    float platY = GroundTopY + NextRange(rng, 1.20f, 1.55f);
                    CreateStyledBlock($"Platform_S2_{islandIdx}_{obstacleCount}", levelRoot, new Vector2(platX, platY), new Vector2(platW, 0.5f), StageBlockStyle.Platform, isTrigger: false, theme);

                    obstacleCount++;
                    CreateObstacle($"Obstacle_S2_{obstacleCount:00}", levelRoot, x: platX, height: NextRange(rng, 0.95f, 1.15f), width: 0.85f, theme);

                    robotCount++;
                    float robX = Mathf.Min(usableEnd - 1.2f, platX + platW * 0.5f + 2.2f);
                    CreateRobotEnemy($"ClankRobot_S2_{robotCount:00}", levelRoot, x: robX, patrolRange: NextRange(rng, 2.0f, 2.8f), speed: NextRange(rng, 2.8f, 3.45f));

                    x = robX + NextRange(rng, 4.0f, 4.8f);
                }
                else if (pattern == 1 && (x + 5.2f < usableEnd))
                {
                    // Padrão B: Obstáculo Duplo (Gêmeos) seguido por Robô de Patrulha
                    float h = NextRange(rng, 0.95f, 1.18f);
                    obstacleCount++;
                    CreateObstacle($"Obstacle_S2_{obstacleCount:00}A", levelRoot, x: x, height: h, width: 0.78f, theme);
                    obstacleCount++;
                    CreateObstacle($"Obstacle_S2_{obstacleCount:00}B", levelRoot, x: x + 0.82f, height: h, width: 0.78f, theme);

                    if (x + 4.4f < usableEnd)
                    {
                        robotCount++;
                        CreateRobotEnemy($"ClankRobot_S2_{robotCount:00}", levelRoot, x: x + 3.8f, patrolRange: NextRange(rng, 2.0f, 2.7f), speed: NextRange(rng, 2.7f, 3.4f));
                        x += NextRange(rng, 6.8f, 7.6f);
                    }
                    else
                    {
                        x += NextRange(rng, 4.6f, 5.4f);
                    }
                }
                else if (pattern == 2)
                {
                    // Padrão C: Robô Ágil + Monólito Alto
                    robotCount++;
                    CreateRobotEnemy($"ClankRobot_S2_{robotCount:00}", levelRoot, x: x + 1.0f, patrolRange: NextRange(rng, 2.2f, 3.0f), speed: NextRange(rng, 2.75f, 3.5f));

                    if (x + 4.8f < usableEnd)
                    {
                        obstacleCount++;
                        CreateObstacle($"Obstacle_S2_{obstacleCount:00}", levelRoot, x: x + 4.4f, height: NextRange(rng, 1.20f, 1.45f), width: 0.88f, theme);
                        x += NextRange(rng, 7.6f, 8.4f);
                    }
                    else
                    {
                        x += NextRange(rng, 4.5f, 5.2f);
                    }
                }
                else
                {
                    // Padrão D: Obstáculo Simples + Robô Guardião
                    obstacleCount++;
                    CreateObstacle($"Obstacle_S2_{obstacleCount:00}", levelRoot, x: x, height: NextRange(rng, 1.05f, 1.38f), width: 0.85f, theme);

                    if (x + 4.2f < usableEnd)
                    {
                        robotCount++;
                        CreateRobotEnemy($"ClankRobot_S2_{robotCount:00}", levelRoot, x: x + 3.6f, patrolRange: NextRange(rng, 2.1f, 2.8f), speed: NextRange(rng, 2.7f, 3.35f));
                        x += NextRange(rng, 6.6f, 7.4f);
                    }
                    else
                    {
                        x += NextRange(rng, 4.2f, 5.0f);
                    }
                }
            }

            cursorEndX = islandEnd;
        }

        // Portal Solar de Chegada no final da última ilha
        float goalX = cursorEndX - 3.2f;
        CreateStyledBlock("Goal_Portal", levelRoot, new Vector2(goalX, GroundTopY + 2.0f), new Vector2(1.2f, 4.0f), StageBlockStyle.GoalPortal, isTrigger: true, theme);

        // Atmosfera diurna completa (Céu com Sol e Nuvens, Cristais Solares e Partículas de Pólen/Pétalas)
        AmbientAtmosphere2D.PopulateDaylightStageDepth(levelRoot, GroundTopY, goalX);

        LastGeneratedObstacleCount = obstacleCount;
        LastGeneratedRobotCount = robotCount;
    }

    private static float NextRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static GameObject CreateGroundSegment(string name, Transform parent, float startX, float endX, StageVisualTheme theme)
    {
        float width = Mathf.Max(2f, endX - startX);
        float centerX = startX + width * 0.5f;
        return CreateStyledBlock(name, parent, new Vector2(centerX, -3.5f), new Vector2(width, 3f), StageBlockStyle.Ground, isTrigger: false, theme);
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

    private static void SpawnStage1Robots(Transform parent)
    {
        CreateRobotEnemy("ClankRobot_01", parent, x: 7.5f, patrolRange: 2.8f, speed: 2.2f);
        CreateRobotEnemy("ClankRobot_02", parent, x: 16.5f, patrolRange: 2.8f, speed: 2.5f);
        CreateRobotEnemy("ClankRobot_03", parent, x: 26.0f, patrolRange: 2.6f, speed: 2.4f);
        CreateRobotEnemy("ClankRobot_04", parent, x: 53.5f, patrolRange: 2.2f, speed: 2.5f);
        CreateRobotEnemy("ClankRobot_05", parent, x: 61.0f, patrolRange: 2.6f, speed: 2.6f);
        CreateRobotEnemy("ClankRobot_06", parent, x: 69.2f, patrolRange: 2.6f, speed: 2.8f);
    }

    private static GameObject CreateRobotEnemy(string name, Transform parent, float x, float patrolRange, float speed)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        Vector3 spawnPos = new Vector3(x, GroundTopY + 0.5f, 0f);
        obj.transform.position = spawnPos;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.82f, 0.88f);

        RobotEnemy2D robot = obj.AddComponent<RobotEnemy2D>();
        robot.ConfigureSpawn(spawnPos, patrolRange, speed, -1);

        return obj;
    }

    private static GameObject CreateObstacle(string name, Transform parent, float x, float height, float width, StageVisualTheme theme = StageVisualTheme.NightRuins)
    {
        Vector2 pos = new Vector2(x, GroundTopY + height * 0.5f);
        Vector2 size = new Vector2(width, height);
        return CreateStyledBlock(name, parent, pos, size, StageBlockStyle.Obstacle, isTrigger: false, theme);
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

        PixelTileBlock2D tileBlock = obj.AddComponent<PixelTileBlock2D>();
        tileBlock.Configure(style, size, theme);

        return obj;
    }
}
