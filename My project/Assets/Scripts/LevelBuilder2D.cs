using UnityEngine;

public static class LevelBuilder2D
{
    private const float GroundTopY = -2.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureLevelOnPlay()
    {
        if (Object.FindObjectOfType<BoxPlayer2D>() == null)
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
        // Remove versão anterior se estiver recriando
        GameObject oldLevel = GameObject.Find("Level_2D");
        if (oldLevel != null) Object.DestroyImmediate(oldLevel);

        GameObject oldPlayer = GameObject.Find("PlayerBox");
        if (oldPlayer != null) Object.DestroyImmediate(oldPlayer);

        // Raiz da fase
        GameObject levelRoot = new GameObject("Level_2D");

        // Cores da paleta
        Color groundColor = new Color(0.18f, 0.22f, 0.30f);
        Color platformColor = new Color(0.27f, 0.36f, 0.50f);
        Color obstacleColor = new Color(0.95f, 0.30f, 0.38f);
        Color goalColor = new Color(0.20f, 0.90f, 0.52f);

        // 1. Chão (2 blocos com um buraco/gap no meio para pular)
        CreateBlock("Ground_Part1", levelRoot.transform, new Vector2(17.5f, -3.5f), new Vector2(59f, 3f), groundColor, isTrigger: false);
        CreateBlock("Ground_Part2", levelRoot.transform, new Vector2(69.1f, -3.5f), new Vector2(37.8f, 3f), groundColor, isTrigger: false);

        // 2. Obstáculos ao longo da fase (todos começam em GroundTopY = -2.0f)
        CreateObstacle("Obstacle_01", levelRoot.transform, x: 3f, height: 1.0f, width: 0.85f, obstacleColor);
        CreateObstacle("Obstacle_02", levelRoot.transform, x: 12f, height: 1.35f, width: 0.9f, obstacleColor);

        CreateObstacle("Obstacle_03A", levelRoot.transform, x: 21f, height: 0.95f, width: 0.8f, obstacleColor);
        CreateObstacle("Obstacle_03B", levelRoot.transform, x: 21.85f, height: 0.95f, width: 0.8f, obstacleColor);

        CreateObstacle("Obstacle_04", levelRoot.transform, x: 30f, height: 1.15f, width: 0.85f, obstacleColor);
        CreateBlock("Platform_Safe", levelRoot.transform, new Vector2(37.5f, -0.9f), new Vector2(4.5f, 0.5f), platformColor, isTrigger: false);
        CreateObstacle("Obstacle_05_AfterPlatform", levelRoot.transform, x: 41.2f, height: 1.3f, width: 0.85f, obstacleColor);

        CreateObstacle("Obstacle_06", levelRoot.transform, x: 57f, height: 1.1f, width: 0.85f, obstacleColor);
        CreateObstacle("Obstacle_07", levelRoot.transform, x: 65f, height: 1.4f, width: 0.9f, obstacleColor);
        CreateObstacle("Obstacle_08A", levelRoot.transform, x: 73.5f, height: 1.05f, width: 0.8f, obstacleColor);
        CreateObstacle("Obstacle_08B", levelRoot.transform, x: 74.35f, height: 1.05f, width: 0.8f, obstacleColor);

        // 3. Portal de Chegada (Goal)
        CreateBlock("Goal_Portal", levelRoot.transform, new Vector2(82.5f, GroundTopY + 2.0f), new Vector2(1.2f, 4.0f), goalColor, isTrigger: true);

        // 4. Inimigos Robozinhos estilo Clank (Goombas que morrem com 2 tiros de laser)
        EnsureRobotsInScene(levelRoot.transform);

        // 5. Jogador (Boneco Pixel Art)
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
        childSr.sortingOrder = 5;
        PixelHeroAnimator2D heroAnim = visualChild.AddComponent<PixelHeroAnimator2D>();
        heroAnim.EnsureSprites();

        BoxPlayer2D playerScript = playerObj.AddComponent<BoxPlayer2D>();
        playerScript.visualChild = visualChild.transform;
        playerScript.SetupHeroVisual();

        // 6. Configurar Câmera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = new Color(0.09f, 0.11f, 0.16f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.5f;

            CameraFollow2D follow = mainCam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = mainCam.gameObject.AddComponent<CameraFollow2D>();
            follow.target = playerObj.transform;
            mainCam.transform.position = new Vector3(playerObj.transform.position.x + 1.5f, 0.6f, -10f);
        }
    }

    public static void EnsureRobotsInScene(Transform parent = null)
    {
        if (Object.FindObjectOfType<RobotEnemy2D>() != null) return;

        if (parent == null)
        {
            GameObject levelRoot = GameObject.Find("Level_2D");
            if (levelRoot != null) parent = levelRoot.transform;
        }

        // Posiciona robozinhos patrulhando nos trechos entre os obstáculos
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
        obj.transform.position = new Vector3(x, GroundTopY + 0.5f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.82f, 0.88f);

        RobotEnemy2D robot = obj.AddComponent<RobotEnemy2D>();
        robot.patrolRange = patrolRange;
        robot.moveSpeed = speed;
        robot.EnsureSprites();

        return obj;
    }

    private static GameObject CreateObstacle(string name, Transform parent, float x, float height, float width, Color color)
    {
        Vector2 pos = new Vector2(x, GroundTopY + height * 0.5f);
        Vector2 size = new Vector2(width, height);
        return CreateBlock(name, parent, pos, size, color, isTrigger: false);
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool isTrigger)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = isTrigger ? 2 : 1;

        ColoredBox2D colored = obj.AddComponent<ColoredBox2D>();
        colored.boxColor = color;
        colored.hasBorder = true;
        colored.ApplyVisuals();

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = isTrigger;

        return obj;
    }
}
