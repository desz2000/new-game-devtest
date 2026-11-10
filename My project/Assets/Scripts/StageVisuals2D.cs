using UnityEngine;

public enum StageBlockStyle
{
    Ground = 0,
    Platform = 1,
    Obstacle = 2,
    GoalPortal = 3
}

public enum StageVisualTheme
{
    NightRuins = 1,
    DaylightMeadow = 2
}

/// <summary>
/// Renderiza blocos, plataformas, obstáculos e portal da fase em Pixel Art 32x32 real (sem esticar bordas),
/// suportando tanto o tema Noturno (Fase 1) quanto o tema Diurno e Alegre (Fase 2).
/// </summary>
[ExecuteAlways]
public class PixelTileBlock2D : MonoBehaviour
{
    public StageBlockStyle blockStyle = StageBlockStyle.Ground;
    public Vector2 blockSize = new Vector2(1f, 1f);
    public StageVisualTheme visualTheme = StageVisualTheme.NightRuins;

    private static Sprite groundBodySprite;
    private static Sprite groundTopSprite;
    private static Sprite groundEdgeCapSprite;
    private static Sprite platformSlabSprite;
    private static Sprite platformCorbelSprite;
    private static Sprite obstacleShaftSprite;
    private static Sprite obstacleCrownSprite;
    private static Sprite portalArchSprite;
    private static Sprite[] portalCoreSprites;
    private static Sprite glowCircleSprite;

    // Sprites do Tema Diurno (Fase 2)
    private static Sprite dayGroundBodySprite;
    private static Sprite dayGroundTopSprite;
    private static Sprite dayGroundEdgeCapSprite;
    private static Sprite dayPlatformSlabSprite;
    private static Sprite dayPlatformCorbelSprite;
    private static Sprite dayObstacleShaftSprite;
    private static Sprite dayPortalArchSprite;

    private float animTimer;

    private void OnEnable()
    {
        RebuildVisuals();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (blockStyle == StageBlockStyle.GoalPortal)
        {
            animTimer += Time.deltaTime;
            Transform core = transform.Find("Visual_PortalCore");
            if (core != null)
            {
                SpriteRenderer sr = core.GetComponent<SpriteRenderer>();
                if (sr != null && portalCoreSprites != null && portalCoreSprites.Length > 0)
                {
                    int idx = Mathf.FloorToInt(animTimer * 8f) % portalCoreSprites.Length;
                    sr.sprite = portalCoreSprites[idx];
                }
                float pulse = 1f + Mathf.Sin(animTimer * 3.5f) * 0.04f;
                core.localScale = new Vector3(pulse, pulse, 1f);
            }

            Transform aura = transform.Find("Visual_PortalAura");
            if (aura != null)
            {
                SpriteRenderer sr = aura.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.28f + Mathf.Sin(animTimer * 2.8f) * 0.08f;
                    sr.color = visualTheme == StageVisualTheme.DaylightMeadow
                        ? new Color(1.00f, 0.88f, 0.28f, alpha + 0.06f)
                        : new Color(0.22f, 0.96f, 0.72f, alpha);
                }
            }
        }
        else if (blockStyle == StageBlockStyle.Obstacle)
        {
            animTimer += Time.deltaTime;
            Transform aura = transform.Find("Visual_ObstacleAura");
            if (aura != null)
            {
                SpriteRenderer sr = aura.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float alpha = 0.22f + Mathf.Sin(animTimer * 4.2f + transform.position.x) * 0.09f;
                    sr.color = visualTheme == StageVisualTheme.DaylightMeadow
                        ? new Color(1.00f, 0.28f, 0.14f, alpha + 0.08f)
                        : new Color(0.95f, 0.16f, 0.28f, alpha);
                }
            }
        }
    }

    public void Configure(StageBlockStyle style, Vector2 size, StageVisualTheme theme = StageVisualTheme.NightRuins)
    {
        blockStyle = style;
        blockSize = size;
        visualTheme = theme;
        RebuildVisuals();
    }

    public void RebuildVisuals()
    {
        EnsureSprites();

        // Remove sprite do root para que apenas os filhos em Tile mode renderizem
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            rootSr.enabled = false;
        }

        ClearVisualChildren();

        switch (blockStyle)
        {
            case StageBlockStyle.Ground:
                BuildGroundVisuals();
                break;
            case StageBlockStyle.Platform:
                BuildPlatformVisuals();
                break;
            case StageBlockStyle.Obstacle:
                BuildObstacleVisuals();
                break;
            case StageBlockStyle.GoalPortal:
                BuildGoalPortalVisuals();
                break;
        }
    }

    private void ClearVisualChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Visual_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void BuildGroundVisuals()
    {
        bool isDay = (visualTheme == StageVisualTheme.DaylightMeadow);
        float w = Mathf.Max(1f, blockSize.x);
        float h = Mathf.Max(1f, blockSize.y);
        float topH = Mathf.Min(1f, h);
        float bodyH = Mathf.Max(0f, h - topH);

        if (bodyH > 0.01f)
        {
            GameObject body = CreateVisualChild("Visual_BodyTiles", new Vector3(0f, -topH * 0.5f, 0f), sortingOrder: 1);
            SpriteRenderer sr = body.GetComponent<SpriteRenderer>();
            sr.sprite = isDay ? dayGroundBodySprite : groundBodySprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(w, bodyH);
        }

        GameObject top = CreateVisualChild("Visual_TopCapTiles", new Vector3(0f, (h * 0.5f) - (topH * 0.5f), 0f), sortingOrder: 2);
        SpriteRenderer topSr = top.GetComponent<SpriteRenderer>();
        topSr.sprite = isDay ? dayGroundTopSprite : groundTopSprite;
        topSr.drawMode = SpriteDrawMode.Tiled;
        topSr.tileMode = SpriteTileMode.Continuous;
        topSr.size = new Vector2(w, topH);

        // Bordas esculpidas de penhasco nas duas extremidades
        GameObject leftTrim = CreateVisualChild("Visual_LeftCliff", new Vector3(-w * 0.5f + 0.16f, 0f, 0f), sortingOrder: 3);
        SpriteRenderer leftSr = leftTrim.GetComponent<SpriteRenderer>();
        leftSr.sprite = isDay ? dayGroundEdgeCapSprite : groundEdgeCapSprite;
        leftSr.drawMode = SpriteDrawMode.Tiled;
        leftSr.tileMode = SpriteTileMode.Continuous;
        leftSr.size = new Vector2(0.32f, h);

        GameObject rightTrim = CreateVisualChild("Visual_RightCliff", new Vector3(w * 0.5f - 0.16f, 0f, 0f), sortingOrder: 3);
        SpriteRenderer rightSr = rightTrim.GetComponent<SpriteRenderer>();
        rightSr.sprite = isDay ? dayGroundEdgeCapSprite : groundEdgeCapSprite;
        rightSr.flipX = true;
        rightSr.drawMode = SpriteDrawMode.Tiled;
        rightSr.tileMode = SpriteTileMode.Continuous;
        rightSr.size = new Vector2(0.32f, h);
    }

    private void BuildPlatformVisuals()
    {
        bool isDay = (visualTheme == StageVisualTheme.DaylightMeadow);
        float w = Mathf.Max(1f, blockSize.x);
        float h = Mathf.Max(0.25f, blockSize.y);

        GameObject slab = CreateVisualChild("Visual_PlatformSlab", Vector3.zero, sortingOrder: 2);
        SpriteRenderer sr = slab.GetComponent<SpriteRenderer>();
        sr.sprite = isDay ? dayPlatformSlabSprite : platformSlabSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(w, h);

        // Suportes arquitetônicos de pedra abaixo da plataforma (profundidade Duck Souls)
        float corbelOffset = Mathf.Max(0.6f, w * 0.32f);
        CreateCorbel("Visual_CorbelLeft", new Vector3(-corbelOffset, -h * 0.5f - 0.28f, 0f), flipX: false, isDay: isDay);
        CreateCorbel("Visual_CorbelRight", new Vector3(corbelOffset, -h * 0.5f - 0.28f, 0f), flipX: true, isDay: isDay);
    }

    private void CreateCorbel(string name, Vector3 localPos, bool flipX, bool isDay)
    {
        GameObject corbel = CreateVisualChild(name, localPos, sortingOrder: 0);
        SpriteRenderer sr = corbel.GetComponent<SpriteRenderer>();
        sr.sprite = isDay ? dayPlatformCorbelSprite : platformCorbelSprite;
        sr.flipX = flipX;
    }

    private void BuildObstacleVisuals()
    {
        bool isDay = (visualTheme == StageVisualTheme.DaylightMeadow);
        float w = Mathf.Max(0.4f, blockSize.x);
        float h = Mathf.Max(0.5f, blockSize.y);

        float crownH = Mathf.Min(0.55f, h * 0.55f);
        float shaftH = Mathf.Max(0.2f, h - crownH * 0.65f);

        GameObject shaft = CreateVisualChild("Visual_ObstacleShaft", new Vector3(0f, -h * 0.5f + shaftH * 0.5f, 0f), sortingOrder: 2);
        SpriteRenderer shaftSr = shaft.GetComponent<SpriteRenderer>();
        shaftSr.sprite = isDay ? dayObstacleShaftSprite : obstacleShaftSprite;
        shaftSr.drawMode = SpriteDrawMode.Tiled;
        shaftSr.tileMode = SpriteTileMode.Continuous;
        shaftSr.size = new Vector2(w, shaftH);

        GameObject crown = CreateVisualChild("Visual_ObstacleCrown", new Vector3(0f, h * 0.5f - crownH * 0.5f + 0.05f, 0f), sortingOrder: 3);
        SpriteRenderer crownSr = crown.GetComponent<SpriteRenderer>();
        crownSr.sprite = obstacleCrownSprite;
        crown.transform.localScale = new Vector3(w * 1.08f, crownH / 0.55f, 1f);

        // Halo sutil avermelhado atrás do monólito amaldiçoado
        GameObject aura = CreateVisualChild("Visual_ObstacleAura", new Vector3(0f, 0.05f, 0f), sortingOrder: 0);
        SpriteRenderer auraSr = aura.GetComponent<SpriteRenderer>();
        auraSr.sprite = glowCircleSprite;
        auraSr.color = isDay ? new Color(1.00f, 0.28f, 0.14f, 0.30f) : new Color(0.95f, 0.16f, 0.28f, 0.24f);
        aura.transform.localScale = new Vector3(w * 2.1f, h * 1.55f, 1f);
    }

    private void BuildGoalPortalVisuals()
    {
        bool isDay = (visualTheme == StageVisualTheme.DaylightMeadow);

        // Aura mística atrás do portal
        GameObject aura = CreateVisualChild("Visual_PortalAura", new Vector3(0f, 0f, 0f), sortingOrder: 1);
        SpriteRenderer auraSr = aura.GetComponent<SpriteRenderer>();
        auraSr.sprite = glowCircleSprite;
        auraSr.color = isDay ? new Color(1.00f, 0.88f, 0.28f, 0.36f) : new Color(0.22f, 0.96f, 0.72f, 0.30f);
        aura.transform.localScale = new Vector3(3.6f, 4.8f, 1f);

        // Núcleo dimensional animado
        GameObject core = CreateVisualChild("Visual_PortalCore", new Vector3(0f, -0.15f, 0f), sortingOrder: 2);
        SpriteRenderer coreSr = core.GetComponent<SpriteRenderer>();
        coreSr.sprite = portalCoreSprites[0];
        coreSr.drawMode = SpriteDrawMode.Tiled;
        coreSr.tileMode = SpriteTileMode.Continuous;
        coreSr.size = new Vector2(1.05f, 2.85f);

        // Arco antigo de pedra com runas (moldura Duck Souls x Celeste)
        GameObject arch = CreateVisualChild("Visual_PortalArch", new Vector3(0f, -0.05f, 0f), sortingOrder: 3);
        SpriteRenderer archSr = arch.GetComponent<SpriteRenderer>();
        archSr.sprite = isDay ? dayPortalArchSprite : portalArchSprite;
        arch.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
    }

    private GameObject CreateVisualChild(string name, Vector3 localPos, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPos;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        return obj;
    }

    private static void EnsureSprites()
    {
        if (groundBodySprite == null) groundBodySprite = BuildGroundBodySprite(isDay: false);
        if (groundTopSprite == null) groundTopSprite = BuildGroundTopSprite(isDay: false);
        if (groundEdgeCapSprite == null) groundEdgeCapSprite = BuildGroundEdgeCapSprite(isDay: false);
        if (platformSlabSprite == null) platformSlabSprite = BuildPlatformSlabSprite(isDay: false);
        if (platformCorbelSprite == null) platformCorbelSprite = BuildPlatformCorbelSprite(isDay: false);
        if (obstacleShaftSprite == null) obstacleShaftSprite = BuildObstacleShaftSprite(isDay: false);
        if (obstacleCrownSprite == null) obstacleCrownSprite = BuildObstacleCrownSprite();
        if (portalArchSprite == null) portalArchSprite = BuildPortalArchSprite(isDay: false);

        if (dayGroundBodySprite == null) dayGroundBodySprite = BuildGroundBodySprite(isDay: true);
        if (dayGroundTopSprite == null) dayGroundTopSprite = BuildGroundTopSprite(isDay: true);
        if (dayGroundEdgeCapSprite == null) dayGroundEdgeCapSprite = BuildGroundEdgeCapSprite(isDay: true);
        if (dayPlatformSlabSprite == null) dayPlatformSlabSprite = BuildPlatformSlabSprite(isDay: true);
        if (dayPlatformCorbelSprite == null) dayPlatformCorbelSprite = BuildPlatformCorbelSprite(isDay: true);
        if (dayObstacleShaftSprite == null) dayObstacleShaftSprite = BuildObstacleShaftSprite(isDay: true);
        if (dayPortalArchSprite == null) dayPortalArchSprite = BuildPortalArchSprite(isDay: true);

        if (portalCoreSprites == null || portalCoreSprites.Length == 0 || portalCoreSprites[0] == null)
        {
            portalCoreSprites = new[]
            {
                BuildPortalCoreSprite(0),
                BuildPortalCoreSprite(1),
                BuildPortalCoreSprite(2)
            };
        }
        if (glowCircleSprite == null) glowCircleSprite = BuildGlowSprite();
    }

    private static Sprite BuildGroundBodySprite(bool isDay)
    {
        // Textura 64x64 (bloco 2x2 de 32x32) de alvenaria de pedra antiga (Noturno) ou arenito solar claro (Diurno)
        const int size = 64;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "GroundBodyDay64_Tile" : "GroundBody64_Tile", size, size, TextureWrapMode.Repeat);

        Color mortar = isDay ? new Color(0.42f, 0.34f, 0.24f, 1f) : new Color(0.08f, 0.09f, 0.16f, 1f);
        Color stoneDeep = isDay ? new Color(0.68f, 0.56f, 0.40f, 1f) : new Color(0.13f, 0.16f, 0.25f, 1f);
        Color stoneMid = isDay ? new Color(0.82f, 0.71f, 0.54f, 1f) : new Color(0.18f, 0.22f, 0.34f, 1f);
        Color stoneLight = isDay ? new Color(0.92f, 0.84f, 0.68f, 1f) : new Color(0.25f, 0.30f, 0.44f, 1f);
        Color stoneHighlight = isDay ? new Color(0.98f, 0.94f, 0.82f, 1f) : new Color(0.32f, 0.39f, 0.54f, 1f);
        Color mossSpeck = isDay ? new Color(0.32f, 0.78f, 0.38f, 1f) : new Color(0.18f, 0.44f, 0.40f, 1f);

        int[] rowHeights = { 16, 16, 16, 16 };
        for (int y = 0; y < size; y++)
        {
            int band = (y / 16) % 4;
            int localY = y % rowHeights[band];
            int brickOffset = (band % 2 == 0) ? 0 : 16;
            int brickWidth = (band % 2 == 0) ? 32 : 24;

            for (int x = 0; x < size; x++)
            {
                int shiftedX = (x + brickOffset) % size;
                int localX = shiftedX % brickWidth;
                int brickIdX = shiftedX / brickWidth;

                bool isMortar = (localY == 0 || localX == 0);
                if (isMortar)
                {
                    tex.SetPixel(x, y, mortar);
                    continue;
                }

                float n = PixelArtBuilder.Hash01(x, y, 42);
                float brickTone = PixelArtBuilder.Hash01(brickIdX, band, 99);

                Color c = stoneMid;
                if (brickTone < 0.30f) c = stoneDeep;
                else if (brickTone > 0.72f) c = stoneLight;

                // Chanfro de luz no topo do tijolo (y máximo dentro da fiada)
                if (localY == 15)
                {
                    c = (n > 0.25f) ? stoneHighlight : stoneLight;
                }
                else if (localY == 1)
                {
                    c = stoneDeep;
                }
                else if (localX == 1)
                {
                    c = stoneLight;
                }
                else if (localX == brickWidth - 1)
                {
                    c = stoneDeep;
                }

                // Dithering e rachaduras sutis de pedra realista em pixel art
                if (n > 0.94f) c = stoneDeep;
                else if (n < 0.05f) c = stoneHighlight;
                else if ((y > 42) && n > 0.88f && n < 0.91f) c = mossSpeck;

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildGroundTopSprite(bool isDay)
    {
        // Textura 64x32 com musgo azul-esmeralda (Noturno) ou grama verde-primavera alegre com florzinhas (Diurno)
        const int w = 64;
        const int h = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "GroundTopDay64x32_Tile" : "GroundTop64x32_Tile", w, h, TextureWrapMode.Repeat);

        Color outline = isDay ? new Color(0.14f, 0.24f, 0.12f, 1f) : new Color(0.06f, 0.08f, 0.14f, 1f);
        Color mossDark = isDay ? new Color(0.16f, 0.56f, 0.24f, 1f) : new Color(0.12f, 0.30f, 0.31f, 1f);
        Color mossMid = isDay ? new Color(0.26f, 0.76f, 0.34f, 1f) : new Color(0.19f, 0.47f, 0.45f, 1f);
        Color mossBright = isDay ? new Color(0.45f, 0.90f, 0.44f, 1f) : new Color(0.32f, 0.70f, 0.61f, 1f);
        Color mossTip = isDay ? new Color(0.72f, 0.98f, 0.54f, 1f) : new Color(0.54f, 0.88f, 0.75f, 1f);

        Color stoneDark = isDay ? new Color(0.68f, 0.55f, 0.38f, 1f) : new Color(0.13f, 0.16f, 0.26f, 1f);
        Color stoneMid = isDay ? new Color(0.84f, 0.73f, 0.55f, 1f) : new Color(0.22f, 0.26f, 0.40f, 1f);
        Color stoneLight = isDay ? new Color(0.94f, 0.86f, 0.70f, 1f) : new Color(0.33f, 0.39f, 0.56f, 1f);
        Color stoneRim = isDay ? new Color(0.99f, 0.95f, 0.82f, 1f) : new Color(0.45f, 0.54f, 0.72f, 1f);

        Color flowerYellow = new Color(1.00f, 0.92f, 0.24f, 1f);
        Color flowerPink = new Color(1.00f, 0.52f, 0.78f, 1f);
        Color flowerWhite = new Color(0.98f, 0.99f, 1.00f, 1f);

        for (int x = 0; x < w; x++)
        {
            float nCol = PixelArtBuilder.Hash01(x, 7, 303);
            int overhang = 21 + Mathf.RoundToInt(Mathf.Sin(x * 0.45f) * 2.2f + (nCol - 0.5f) * 3.0f);
            int topGrassStart = 30 - (nCol > 0.75f ? 0 : 1);

            for (int y = 0; y < h; y++)
            {
                float n = PixelArtBuilder.Hash01(x, y, 777);

                if (y > topGrassStart)
                {
                    if (isDay && (x % 13 == 4 || x % 19 == 9) && y == 31)
                    {
                        tex.SetPixel(x, y, (x % 2 == 0) ? flowerYellow : flowerPink);
                    }
                    else
                    {
                        tex.SetPixel(x, y, (nCol > 0.80f && y == 31) ? mossTip : Color.clear);
                    }
                }
                else if (y == topGrassStart)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (y >= overhang)
                {
                    if (isDay && y == topGrassStart - 2 && (x % 11 == 3 || x % 17 == 8))
                    {
                        tex.SetPixel(x, y, (x % 3 == 0) ? flowerPink : (x % 2 == 0 ? flowerYellow : flowerWhite));
                    }
                    else if (y == topGrassStart - 1 || y == topGrassStart - 2)
                        tex.SetPixel(x, y, (n > 0.4f) ? mossTip : mossBright);
                    else if (y == overhang)
                        tex.SetPixel(x, y, outline);
                    else if (y <= overhang + 2)
                        tex.SetPixel(x, y, mossDark);
                    else
                        tex.SetPixel(x, y, (n > 0.55f) ? mossBright : mossMid);
                }
                else
                {
                    // Pedra talhada logo abaixo do musgo
                    bool isMortar = (y == 0 || y == 11 || ((x + (y < 11 ? 16 : 0)) % 16 == 0));
                    if (isMortar)
                    {
                        tex.SetPixel(x, y, isDay ? new Color(0.42f, 0.34f, 0.24f, 1f) : outline);
                    }
                    else if (y == overhang - 1)
                    {
                        tex.SetPixel(x, y, stoneDark);
                    }
                    else if (y == 10 || y == 12)
                    {
                        tex.SetPixel(x, y, (y == 10) ? stoneRim : stoneDark);
                    }
                    else
                    {
                        Color c = stoneMid;
                        if (n > 0.85f) c = stoneLight;
                        else if (n < 0.16f) c = stoneDark;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildGroundEdgeCapSprite(bool isDay)
    {
        const int w = 10;
        const int h = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "GroundCliffEdgeDay_Tile" : "GroundCliffEdge_Tile", w, h, TextureWrapMode.Repeat);

        Color outline = isDay ? new Color(0.28f, 0.22f, 0.14f, 1f) : new Color(0.06f, 0.08f, 0.14f, 1f);
        Color rim = isDay ? new Color(0.96f, 0.88f, 0.72f, 1f) : new Color(0.45f, 0.54f, 0.72f, 1f);
        Color mid = isDay ? new Color(0.78f, 0.66f, 0.48f, 1f) : new Color(0.24f, 0.29f, 0.43f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x == 0) tex.SetPixel(x, y, outline);
                else if (x == 1 || x == 2) tex.SetPixel(x, y, rim);
                else if (x <= 4) tex.SetPixel(x, y, new Color(mid.r, mid.g, mid.b, 0.65f));
                else tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildPlatformSlabSprite(bool isDay)
    {
        const int w = 32;
        const int h = 16;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "PlatformSlabDay32x16" : "PlatformSlab32x16", w, h, TextureWrapMode.Repeat);

        Color outline = isDay ? new Color(0.22f, 0.18f, 0.12f, 1f) : new Color(0.06f, 0.08f, 0.14f, 1f);
        Color mossTop = isDay ? new Color(0.46f, 0.92f, 0.44f, 1f) : new Color(0.32f, 0.74f, 0.64f, 1f);
        Color mossDeep = isDay ? new Color(0.22f, 0.68f, 0.30f, 1f) : new Color(0.16f, 0.42f, 0.40f, 1f);
        Color stoneRim = isDay ? new Color(0.98f, 0.92f, 0.78f, 1f) : new Color(0.52f, 0.62f, 0.80f, 1f);
        Color stoneMid = isDay ? new Color(0.84f, 0.74f, 0.56f, 1f) : new Color(0.25f, 0.31f, 0.46f, 1f);
        Color stoneDark = isDay ? new Color(0.66f, 0.54f, 0.38f, 1f) : new Color(0.14f, 0.17f, 0.27f, 1f);
        Color runeCyan = isDay ? new Color(1.00f, 0.84f, 0.22f, 1f) : new Color(0.24f, 0.90f, 0.96f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float n = PixelArtBuilder.Hash01(x, y, 505);
                if (y == 0 || y == h - 1)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (y >= 12)
                {
                    tex.SetPixel(x, y, (y == 14) ? mossTop : mossDeep);
                }
                else if (y == 11)
                {
                    tex.SetPixel(x, y, stoneRim);
                }
                else if (y <= 2)
                {
                    tex.SetPixel(x, y, stoneDark);
                }
                else if ((x == 7 || x == 23) && (y == 6 || y == 7))
                {
                    tex.SetPixel(x, y, runeCyan);
                }
                else if (x % 16 == 0)
                {
                    tex.SetPixel(x, y, outline);
                }
                else
                {
                    tex.SetPixel(x, y, (n > 0.75f) ? stoneRim : (n < 0.2f ? stoneDark : stoneMid));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildPlatformCorbelSprite(bool isDay)
    {
        const int size = 24;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "PlatformCorbelDay24" : "PlatformCorbel24", size, size);
        Color outline = isDay ? new Color(0.28f, 0.22f, 0.14f, 1f) : new Color(0.06f, 0.08f, 0.14f, 1f);
        Color stone = isDay ? new Color(0.76f, 0.65f, 0.48f, 1f) : new Color(0.20f, 0.24f, 0.37f, 1f);
        Color highlight = isDay ? new Color(0.94f, 0.86f, 0.70f, 1f) : new Color(0.34f, 0.41f, 0.58f, 1f);

        for (int y = 0; y < size; y++)
        {
            int span = Mathf.Max(2, (y * 18) / size + 2);
            for (int x = 0; x < size; x++)
            {
                if (x >= 2 && x < 2 + span)
                {
                    bool edge = (x == 2 || x == 1 + span || y == 0 || y == size - 1);
                    tex.SetPixel(x, y, edge ? outline : (x == 3 ? highlight : stone));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildObstacleShaftSprite(bool isDay)
    {
        const int w = 32;
        const int h = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "ObstacleShaftDay32" : "ObstacleShaft32", w, h, TextureWrapMode.Repeat);

        Color outline = new Color(0.12f, 0.04f, 0.08f, 1f);
        Color obsidianDark = isDay ? new Color(0.32f, 0.12f, 0.16f, 1f) : new Color(0.15f, 0.09f, 0.16f, 1f);
        Color obsidianMid = isDay ? new Color(0.48f, 0.18f, 0.24f, 1f) : new Color(0.24f, 0.14f, 0.24f, 1f);
        Color ironTrim = isDay ? new Color(0.74f, 0.36f, 0.28f, 1f) : new Color(0.42f, 0.25f, 0.36f, 1f);
        Color rubyGlow = isDay ? new Color(1.00f, 0.24f, 0.28f, 1f) : new Color(0.96f, 0.20f, 0.36f, 1f);
        Color rubyCore = isDay ? new Color(1.00f, 0.86f, 0.42f, 1f) : new Color(1.00f, 0.65f, 0.72f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x <= 1 || x >= w - 2)
                {
                    tex.SetPixel(x, y, outline);
                    continue;
                }
                if (x == 2 || x == w - 3)
                {
                    tex.SetPixel(x, y, ironTrim);
                    continue;
                }

                // Veio de cristal rubi pulsante no centro do monólito
                int centerDist = Mathf.Abs(x - 16 + Mathf.RoundToInt(Mathf.Sin(y * 0.35f) * 2f));
                if (centerDist == 0 && (y % 7 != 0))
                {
                    tex.SetPixel(x, y, rubyCore);
                }
                else if (centerDist <= 2 && (y % 5 != 0))
                {
                    tex.SetPixel(x, y, rubyGlow);
                }
                else if (y % 8 == 0)
                {
                    tex.SetPixel(x, y, outline);
                }
                else
                {
                    float n = PixelArtBuilder.Hash01(x, y, 818);
                    tex.SetPixel(x, y, (n > 0.75f) ? ironTrim : (n < 0.25f ? obsidianDark : obsidianMid));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildObstacleCrownSprite()
    {
        // Coroa de espinhos metálicos + cristais rubi 32x24
        const int w = 32;
        const int h = 24;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("ObstacleCrown32x24", w, h);

        Color outline = new Color(0.08f, 0.04f, 0.09f, 1f);
        Color crimsonDark = new Color(0.55f, 0.08f, 0.20f, 1f);
        Color crimsonMid = new Color(0.92f, 0.16f, 0.34f, 1f);
        Color crimsonBright = new Color(1.00f, 0.52f, 0.62f, 1f);
        Color crimsonTip = new Color(1.00f, 0.88f, 0.92f, 1f);

        for (int x = 0; x < w; x++)
        {
            // 3 picos afiados de cristal
            float p1 = Mathf.Max(0f, 1f - Mathf.Abs(x - 6) / 5.5f) * 18f;
            float p2 = Mathf.Max(0f, 1f - Mathf.Abs(x - 16) / 6.5f) * 23f;
            float p3 = Mathf.Max(0f, 1f - Mathf.Abs(x - 26) / 5.5f) * 18f;
            int spikeH = Mathf.Max(5, Mathf.RoundToInt(Mathf.Max(p1, Mathf.Max(p2, p3))));

            for (int y = 0; y < h; y++)
            {
                if (y > spikeH)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (y == spikeH || x == 0 || x == w - 1 || y == 0)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (y >= spikeH - 2)
                {
                    tex.SetPixel(x, y, crimsonTip);
                }
                else if (x == 5 || x == 15 || x == 25)
                {
                    tex.SetPixel(x, y, crimsonBright);
                }
                else if (x > 16 && x < 21 || x > 6 && x < 10)
                {
                    tex.SetPixel(x, y, crimsonDark);
                }
                else
                {
                    tex.SetPixel(x, y, crimsonMid);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildPortalArchSprite(bool isDay)
    {
        // Arco rúnico antigo 56x108 em 32 PPU (~1.75 x 3.375 unidades)
        const int w = 56;
        const int h = 108;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "PortalArchDay56x108" : "PortalArch56x108", w, h);

        Color outline = isDay ? new Color(0.24f, 0.18f, 0.10f, 1f) : new Color(0.06f, 0.08f, 0.14f, 1f);
        Color stoneDark = isDay ? new Color(0.68f, 0.56f, 0.38f, 1f) : new Color(0.14f, 0.17f, 0.27f, 1f);
        Color stoneMid = isDay ? new Color(0.86f, 0.76f, 0.58f, 1f) : new Color(0.24f, 0.29f, 0.43f, 1f);
        Color stoneLight = isDay ? new Color(0.98f, 0.92f, 0.78f, 1f) : new Color(0.38f, 0.46f, 0.64f, 1f);
        Color runeGlow = isDay ? new Color(1.00f, 0.85f, 0.18f, 1f) : new Color(0.22f, 0.95f, 0.72f, 1f);
        Color moss = isDay ? new Color(0.32f, 0.84f, 0.38f, 1f) : new Color(0.25f, 0.62f, 0.54f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inLeftPillar = x >= 2 && x <= 11 && y >= 2 && y <= 90;
                bool inRightPillar = x >= w - 12 && x <= w - 3 && y >= 2 && y <= 90;
                bool inPedestal = y < 8 && x >= 1 && x <= w - 2;
                bool inArch = false;

                if (y > 82)
                {
                    float dx = (x - w * 0.5f) / 24f;
                    float dy = (y - 82f) / 23f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    inArch = r >= 0.55f && r <= 1.0f;
                }

                if (!inLeftPillar && !inRightPillar && !inPedestal && !inArch)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool isBorder = (x == 1 || x == w - 2 || y == 0 || y == h - 1 ||
                                 (inLeftPillar && (x == 2 || x == 11)) ||
                                 (inRightPillar && (x == w - 12 || x == w - 3)));

                if (isBorder)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (((inLeftPillar && x == 6) || (inRightPillar && x == w - 7)) && (y % 12 >= 4 && y % 12 <= 7))
                {
                    tex.SetPixel(x, y, runeGlow);
                }
                else if (y > 96 && Mathf.Abs(x - w / 2) <= 2 && y < 103)
                {
                    tex.SetPixel(x, y, runeGlow);
                }
                else if (y % 14 == 0)
                {
                    tex.SetPixel(x, y, outline);
                }
                else
                {
                    float n = PixelArtBuilder.Hash01(x, y, 616);
                    if (n > 0.88f && y > 60) tex.SetPixel(x, y, moss);
                    else if (x < 6 || (inRightPillar && x < w - 8) || n > 0.78f) tex.SetPixel(x, y, stoneLight);
                    else if (n < 0.22f) tex.SetPixel(x, y, stoneDark);
                    else tex.SetPixel(x, y, stoneMid);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildPortalCoreSprite(int phase)
    {
        const int w = 32;
        const int h = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("PortalCore32_" + phase, w, h, TextureWrapMode.Repeat);

        Color deep = new Color(0.06f, 0.42f, 0.36f, 0.85f);
        Color mid = new Color(0.14f, 0.78f, 0.58f, 0.92f);
        Color bright = new Color(0.45f, 0.98f, 0.82f, 0.98f);
        Color white = new Color(0.88f, 1.00f, 0.96f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float wave = Mathf.Sin((y + phase * 10) * 0.35f + x * 0.22f) + Mathf.Cos((x - phase * 6) * 0.28f);
                float edgeFade = 1f - Mathf.Abs(x - 15.5f) / 16f;
                if (edgeFade < 0.12f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (wave > 1.15f && edgeFade > 0.4f)
                {
                    tex.SetPixel(x, y, white);
                }
                else if (wave > 0.35f)
                {
                    tex.SetPixel(x, y, bright);
                }
                else if (wave > -0.45f)
                {
                    tex.SetPixel(x, y, mid);
                }
                else
                {
                    tex.SetPixel(x, y, deep);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite BuildGlowSprite()
    {
        const int size = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("PixelGlow32", size, size);
        Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - dist);
                // Gradiente escalonado sutil para combinar com estética pixel art
                float stepped = Mathf.Floor(a * a * 6f) / 6f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, stepped));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}

/// <summary>
/// Controla o céu em pixel art, lua, estrelas e 3 camadas de parallax (montanhas de Celeste + ruínas de Duck Souls).
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class ParallaxBackdrop2D : MonoBehaviour
{
    public StageVisualTheme visualTheme = StageVisualTheme.NightRuins;

    private Transform skyLayer;
    private Transform[] farChunks;
    private Transform[] midChunks;
    private Transform[] nearChunks;
    private StageVisualTheme builtTheme = StageVisualTheme.NightRuins;

    private const float ChunkWorldWidth = 16f;

    private static Sprite skySprite;
    private static Sprite farMountainSprite;
    private static Sprite midRuinsSprite;
    private static Sprite nearMistSprite;

    // Sprites Diurnos da Fase 2 (Pradaria Solar)
    private static Sprite daySkySprite;
    private static Sprite dayFarMountainSprite;
    private static Sprite dayMidRuinsSprite;
    private static Sprite dayNearMistSprite;

    private void OnEnable()
    {
        EnsureBuilt();
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        EnsureBuilt();

        Vector3 camPos = cam.transform.position;

        if (skyLayer != null)
        {
            skyLayer.position = new Vector3(camPos.x, camPos.y, 10f);
        }

        UpdateParallaxRow(farChunks, camPos, parallaxX: 0.14f, parallaxY: 0.08f, baseY: 1.5f);
        UpdateParallaxRow(midChunks, camPos, parallaxX: 0.34f, parallaxY: 0.16f, baseY: 0.2f);
        UpdateParallaxRow(nearChunks, camPos, parallaxX: 0.58f, parallaxY: 0.28f, baseY: -0.6f);
    }

    private void UpdateParallaxRow(Transform[] chunks, Vector3 camPos, float parallaxX, float parallaxY, float baseY)
    {
        if (chunks == null || chunks.Length == 0) return;

        float scrollOffset = camPos.x * (1f - parallaxX);
        float snappedCenter = Mathf.Round((camPos.x - scrollOffset) / ChunkWorldWidth) * ChunkWorldWidth + scrollOffset;
        float yPos = baseY + camPos.y * (1f - parallaxY) * 0.25f;

        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i] == null) continue;
            float offsetIndex = i - 1;
            chunks[i].position = new Vector3(snappedCenter + offsetIndex * ChunkWorldWidth, yPos, 5f);
        }
    }

    public void ConfigureTheme(StageVisualTheme theme)
    {
        visualTheme = theme;
        skyLayer = null;
        EnsureBuilt();
    }

    public void EnsureBuilt()
    {
        bool isDay = (visualTheme == StageVisualTheme.DaylightMeadow);

        if (skySprite == null) skySprite = BuildSkySprite(isDay: false);
        if (farMountainSprite == null) farMountainSprite = BuildFarMountainSprite(isDay: false);
        if (midRuinsSprite == null) midRuinsSprite = BuildMidRuinsSprite(isDay: false);
        if (nearMistSprite == null) nearMistSprite = BuildNearMistSprite(isDay: false);

        if (daySkySprite == null) daySkySprite = BuildSkySprite(isDay: true);
        if (dayFarMountainSprite == null) dayFarMountainSprite = BuildFarMountainSprite(isDay: true);
        if (dayMidRuinsSprite == null) dayMidRuinsSprite = BuildMidRuinsSprite(isDay: true);
        if (dayNearMistSprite == null) dayNearMistSprite = BuildNearMistSprite(isDay: true);

        if (skyLayer != null && farChunks != null && farChunks.Length == 3 && farChunks[0] != null && builtTheme == visualTheme)
        {
            return;
        }

        builtTheme = visualTheme;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }

        GameObject skyObj = new GameObject("Parallax_Sky");
        skyObj.transform.SetParent(transform, false);
        SpriteRenderer skySr = skyObj.AddComponent<SpriteRenderer>();
        skySr.sprite = isDay ? daySkySprite : skySprite;
        skySr.sortingOrder = -50;
        skyObj.transform.localScale = new Vector3(3.2f, 2.8f, 1f);
        skyLayer = skyObj.transform;

        farChunks = CreateRow("Parallax_FarMountains", isDay ? dayFarMountainSprite : farMountainSprite, -40);
        midChunks = CreateRow("Parallax_MidRuins", isDay ? dayMidRuinsSprite : midRuinsSprite, -30);
        nearChunks = CreateRow("Parallax_NearMist", isDay ? dayNearMistSprite : nearMistSprite, -20);
    }

    private Transform[] CreateRow(string prefix, Sprite sprite, int sortingOrder)
    {
        Transform[] arr = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = new GameObject($"{prefix}_{i}");
            obj.transform.SetParent(transform, false);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            arr[i] = obj.transform;
        }
        return arr;
    }

    private static Sprite BuildSkySprite(bool isDay)
    {
        const int w = 256;
        const int h = 144;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "CelesteSolar_DaySky256" : "CelesteSouls_Sky256", w, h);

        Color cTop = isDay ? new Color(0.16f, 0.62f, 0.98f, 1f) : new Color(0.04f, 0.05f, 0.12f, 1f);
        Color cMid = isDay ? new Color(0.36f, 0.78f, 1.00f, 1f) : new Color(0.09f, 0.11f, 0.22f, 1f);
        Color cLow = isDay ? new Color(0.64f, 0.90f, 1.00f, 1f) : new Color(0.17f, 0.19f, 0.35f, 1f);
        Color cHorizon = isDay ? new Color(0.92f, 0.97f, 0.86f, 1f) : new Color(0.24f, 0.26f, 0.44f, 1f);

        Vector2 orbCenter = new Vector2(198f, 112f);

        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            for (int x = 0; x < w; x++)
            {
                float dither = (((x + y) & 1) == 0 ? 0.03f : -0.03f);
                float td = Mathf.Clamp01(1f - t + dither);

                Color sky;
                if (td < 0.38f) sky = Color.Lerp(cTop, cMid, Mathf.Floor((td / 0.38f) * 5f) / 5f);
                else if (td < 0.72f) sky = Color.Lerp(cMid, cLow, Mathf.Floor(((td - 0.38f) / 0.34f) * 5f) / 5f);
                else sky = Color.Lerp(cLow, cHorizon, Mathf.Floor(((td - 0.72f) / 0.28f) * 4f) / 4f);

                if (!isDay)
                {
                    // Estrelas em pixel art (Noturno)
                    float starN = PixelArtBuilder.Hash01(x, y, 2026);
                    if (t > 0.38f && starN > 0.994f)
                    {
                        sky = (starN > 0.998f) ? new Color(0.86f, 0.92f, 1.00f, 1f) : new Color(0.55f, 0.65f, 0.88f, 1f);
                    }

                    // Lua crescente com halo suave
                    float distMoon = Vector2.Distance(new Vector2(x, y), orbCenter);
                    float distCut = Vector2.Distance(new Vector2(x, y), orbCenter + new Vector2(4.5f, 3.0f));
                    if (distMoon <= 11f && distCut > 9.5f)
                    {
                        sky = new Color(0.88f, 0.93f, 1.00f, 1f);
                    }
                    else if (distMoon <= 24f)
                    {
                        float glow = (1f - distMoon / 24f) * 0.18f;
                        sky = Color.Lerp(sky, new Color(0.55f, 0.68f, 0.95f, 1f), glow);
                    }
                }
                else
                {
                    // Nuvens fofinhas em pixel art no céu diurno (Fase 2)
                    bool inCloud1 = IsInsidePixelCloud(x, y, 48f, 114f, 26f, 9f);
                    bool inCloud2 = IsInsidePixelCloud(x, y, 124f, 98f, 32f, 10f);
                    bool inCloud3 = IsInsidePixelCloud(x, y, 228f, 92f, 22f, 8f);
                    if (inCloud1 || inCloud2 || inCloud3)
                    {
                        bool cloudBottom = IsInsidePixelCloud(x, y + 3, 48f, 114f, 26f, 9f) && !IsInsidePixelCloud(x, y - 2, 48f, 114f, 26f, 9f)
                                           || IsInsidePixelCloud(x, y + 3, 124f, 98f, 32f, 10f) && !IsInsidePixelCloud(x, y - 2, 124f, 98f, 32f, 10f);
                        sky = cloudBottom ? new Color(0.82f, 0.93f, 1.00f, 1f) : new Color(0.99f, 0.99f, 1.00f, 1f);
                    }

                    // Sol radiante dourado com halo alegre
                    float distSun = Vector2.Distance(new Vector2(x, y), orbCenter);
                    if (distSun <= 12.5f)
                    {
                        sky = distSun <= 8.5f ? new Color(1.00f, 0.99f, 0.82f, 1f) : new Color(1.00f, 0.88f, 0.28f, 1f);
                    }
                    else if (distSun <= 32f)
                    {
                        float glow = Mathf.Floor((1f - distSun / 32f) * 4f) / 4f * 0.34f;
                        sky = Color.Lerp(sky, new Color(1.00f, 0.95f, 0.55f, 1f), glow);
                    }
                }

                tex.SetPixel(x, y, sky);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 20f);
    }

    private static bool IsInsidePixelCloud(int x, int y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx;
        float dy = (y - cy) / ry;
        float puffs = Mathf.Sin((x - cx) * 0.32f) * 0.22f;
        return (dx * dx + (dy - puffs) * (dy - puffs)) <= 1.0f && y >= cy - ry * 0.65f;
    }

    private static Sprite BuildFarMountainSprite(bool isDay)
    {
        const int w = 512;
        const int h = 192;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "FarMountainsDay512" : "FarMountains512", w, h, TextureWrapMode.Repeat);

        Color farPeak = isDay ? new Color(0.28f, 0.58f, 0.78f, 1f) : new Color(0.09f, 0.11f, 0.21f, 1f);
        Color ridgeLight = isDay ? new Color(0.42f, 0.74f, 0.88f, 1f) : new Color(0.14f, 0.17f, 0.30f, 1f);
        Color snowRim = isDay ? new Color(0.95f, 0.99f, 1.00f, 0.92f) : new Color(0.32f, 0.40f, 0.60f, 0.75f);
        Color towerWindow = isDay ? new Color(1.00f, 0.92f, 0.36f, 0.95f) : new Color(0.95f, 0.72f, 0.30f, 0.75f);

        for (int x = 0; x < w; x++)
        {
            float rad = (x / (float)w) * Mathf.PI * 2f;
            float peak1 = 88f + Mathf.Sin(rad) * 32f + Mathf.Cos(rad * 3f) * 18f + Mathf.Abs(Mathf.Sin(rad * 5f)) * 12f;

            bool inTower1 = (x >= 116 && x <= 136);
            bool inTower2 = (x >= 362 && x <= 386);
            float towerH = 0f;
            if (inTower1)
            {
                int dx = Mathf.Abs(x - 126);
                towerH = (dx <= 2) ? 168f : ((x % 4 < 2) ? 152f : 146f);
            }
            else if (inTower2)
            {
                int dx = Mathf.Abs(x - 374);
                towerH = (dx <= 3) ? 174f : ((x % 4 < 2) ? 156f : 150f);
            }

            int height = Mathf.RoundToInt(Mathf.Max(peak1, towerH));
            for (int y = 0; y < h; y++)
            {
                if (y > height)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if ((inTower1 && x == 126 && y == 138) || (inTower2 && (x == 370 || x == 378) && y == 142))
                {
                    tex.SetPixel(x, y, towerWindow);
                }
                else if (y >= height - 3 && !inTower1 && !inTower2)
                {
                    tex.SetPixel(x, y, snowRim);
                }
                else if (y > height - 10)
                {
                    tex.SetPixel(x, y, ridgeLight);
                }
                else
                {
                    tex.SetPixel(x, y, farPeak);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildMidRuinsSprite(bool isDay)
    {
        const int w = 512;
        const int h = 160;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "MidRuinsDay512" : "MidRuins512", w, h, TextureWrapMode.Repeat);

        Color ruinDark = isDay ? new Color(0.68f, 0.62f, 0.48f, 1f) : new Color(0.11f, 0.14f, 0.24f, 1f);
        Color ruinLight = isDay ? new Color(0.86f, 0.80f, 0.64f, 1f) : new Color(0.17f, 0.21f, 0.34f, 1f);
        Color hillTop = isDay ? new Color(0.32f, 0.78f, 0.42f, 1f) : ruinLight;
        Color hillBody = isDay ? new Color(0.22f, 0.60f, 0.34f, 1f) : ruinDark;
        Color pineDark = isDay ? new Color(0.18f, 0.52f, 0.28f, 1f) : new Color(0.08f, 0.13f, 0.20f, 1f);

        for (int x = 0; x < w; x++)
        {
            float rad = (x / (float)w) * Mathf.PI * 2f;
            int hillH = Mathf.RoundToInt(46f + Mathf.Sin(rad * 2f + 1.1f) * 18f + Mathf.Cos(rad * 4f) * 9f);

            int archX = x % 128;
            bool isPillar = (archX < 18 || archX > 110);
            int bridgeTop = 104 - (x > 220 && x < 280 ? 18 : 0);

            int treeMod = (x + 32) % 64;
            int treeCenterDist = Mathf.Abs(treeMod - 32);
            int pineTop = 78 + ((x / 64) % 2) * 14;

            for (int y = 0; y < h; y++)
            {
                bool drawRuin = false;
                if (y <= bridgeTop)
                {
                    if (isPillar) drawRuin = true;
                    else if (y >= 88 && (x < 220 || x > 280))
                    {
                        float archR = Vector2.Distance(new Vector2(archX, y), new Vector2(64f, 64f));
                        if (archR >= 28f) drawRuin = true;
                    }
                }

                bool drawPine = false;
                if (y <= pineTop && y > hillH - 8)
                {
                    int tierW = Mathf.Max(1, ((pineTop - y) % 14) / 2 + (pineTop - y) / 6);
                    if (treeCenterDist <= tierW) drawPine = true;
                }

                if (y <= hillH)
                {
                    tex.SetPixel(x, y, (y > hillH - 3) ? hillTop : hillBody);
                }
                else if (drawRuin)
                {
                    tex.SetPixel(x, y, (archX == 2 || y == bridgeTop) ? ruinLight : ruinDark);
                }
                else if (drawPine)
                {
                    tex.SetPixel(x, y, pineDark);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildNearMistSprite(bool isDay)
    {
        const int w = 512;
        const int h = 144;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "NearMistDay512" : "NearMist512", w, h, TextureWrapMode.Repeat);

        Color mist = isDay ? new Color(1.00f, 0.96f, 0.76f, 0.22f) : new Color(0.24f, 0.30f, 0.48f, 0.24f);
        Color mistSoft = isDay ? new Color(1.00f, 0.96f, 0.76f, 0.11f) : new Color(0.24f, 0.30f, 0.48f, 0.12f);
        Color pillar = isDay ? new Color(0.78f, 0.72f, 0.58f, 0.75f) : new Color(0.11f, 0.13f, 0.22f, 0.85f);

        for (int x = 0; x < w; x++)
        {
            int colMod = x % 256;
            bool inColumn = (colMod >= 40 && colMod <= 54);
            bool inChain = (colMod == 168);

            for (int y = 0; y < h; y++)
            {
                if (inColumn && y < 130)
                {
                    tex.SetPixel(x, y, pillar);
                }
                else if (inChain && y > 84 && (y % 4 != 0))
                {
                    tex.SetPixel(x, y, pillar);
                }
                else if (y < 46)
                {
                    float wave = Mathf.Sin(x * 0.05f) * 6f;
                    if (y < 22 + wave) tex.SetPixel(x, y, mist);
                    else if (y < 36 + wave && ((x + y) % 2 == 0)) tex.SetPixel(x, y, mistSoft);
                    else tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }
}

/// <summary>
/// Tocha animada de ferro em pixel art 32x32 com halo quente cintilante.
/// </summary>
[ExecuteAlways]
public class TorchFlicker2D : MonoBehaviour
{
    private SpriteRenderer torchSr;
    private SpriteRenderer glowSr;
    private float timer;

    private static Sprite[] torchFrames;
    private static Sprite glowSprite;

    private void OnEnable()
    {
        EnsureBuilt();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        timer += Time.deltaTime * 9f;
        if (torchSr != null && torchFrames != null && torchFrames.Length > 0)
        {
            int idx = Mathf.FloorToInt(timer + transform.position.x) % torchFrames.Length;
            torchSr.sprite = torchFrames[idx];
        }
        if (glowSr != null)
        {
            float pulse = 0.24f + Mathf.Sin(timer * 1.4f + transform.position.x) * 0.06f;
            glowSr.color = new Color(1.0f, 0.56f, 0.16f, pulse);
            float scale = 2.4f + Mathf.Sin(timer * 1.9f) * 0.15f;
            glowSr.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void EnsureBuilt()
    {
        if (torchFrames == null || torchFrames.Length == 0 || torchFrames[0] == null)
        {
            torchFrames = new[]
            {
                BuildTorchSprite(0),
                BuildTorchSprite(1),
                BuildTorchSprite(2)
            };
        }
        if (glowSprite == null)
        {
            glowSprite = BuildWarmGlow();
        }

        torchSr = GetComponent<SpriteRenderer>();
        if (torchSr == null) torchSr = gameObject.AddComponent<SpriteRenderer>();
        torchSr.sprite = torchFrames[0];
        torchSr.sortingOrder = -2;

        Transform glowChild = transform.Find("TorchGlow");
        if (glowChild == null)
        {
            GameObject g = new GameObject("TorchGlow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            glowChild = g.transform;
        }
        glowSr = glowChild.GetComponent<SpriteRenderer>();
        if (glowSr == null) glowSr = glowChild.gameObject.AddComponent<SpriteRenderer>();
        glowSr.sprite = glowSprite;
        glowSr.color = new Color(1.0f, 0.56f, 0.16f, 0.26f);
        glowSr.sortingOrder = -3;
        glowChild.localScale = new Vector3(2.4f, 2.4f, 1f);
    }

    private static Sprite BuildTorchSprite(int phase)
    {
        const int size = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("Torch32_" + phase, size, size);

        Color outline = new Color(0.08f, 0.06f, 0.10f, 1f);
        Color iron = new Color(0.24f, 0.26f, 0.34f, 1f);
        Color ironLight = new Color(0.42f, 0.45f, 0.56f, 1f);
        Color wood = new Color(0.32f, 0.18f, 0.10f, 1f);
        Color fireRed = new Color(0.95f, 0.26f, 0.10f, 1f);
        Color fireOrange = new Color(1.00f, 0.60f, 0.12f, 1f);
        Color fireYellow = new Color(1.00f, 0.92f, 0.42f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = Color.clear;

                // Poste / suporte de ferro da tocha (base y=0..15)
                if (y >= 1 && y <= 14 && x >= 14 && x <= 17)
                {
                    c = (x == 14 || x == 17) ? outline : wood;
                }
                else if (y >= 13 && y <= 17 && x >= 12 && x <= 19)
                {
                    bool edge = (y == 13 || y == 17 || x == 12 || x == 19);
                    c = edge ? outline : (x < 15 ? ironLight : iron);
                }
                else if (y >= 18 && y <= 29)
                {
                    float sway = Mathf.Sin((y + phase * 3) * 0.6f) * 1.4f;
                    float dist = Mathf.Abs((x - 15.5f) - sway);
                    float maxR = Mathf.Lerp(4.2f, 0.4f, (y - 18f) / 11f);
                    if (dist <= maxR)
                    {
                        if (y <= 22 && dist <= maxR * 0.45f) c = fireYellow;
                        else if (dist <= maxR * 0.72f) c = fireOrange;
                        else c = fireRed;
                    }
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildWarmGlow()
    {
        const int size = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("TorchGlow32", size, size);
        Vector2 c = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / 16f;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}

/// <summary>
/// Adiciona props arquitetônicos de profundidade (pilares, cristais, névoa do abismo e partículas de neve/brasas).
/// </summary>
public static class AmbientAtmosphere2D
{
    private static Sprite pillarSprite;
    private static Sprite dayPillarSprite;
    private static Sprite crystalSprite;
    private static Sprite sunflowerSprite;
    private static Sprite abyssMistSprite;
    private static Sprite dayAbyssMistSprite;
    private static Material particleMat;

    public static void PopulateStageDepth(Transform levelRoot, float groundTopY)
    {
        if (levelRoot == null) return;

        EnsurePropSprites();

        // 1. Camada de Parallax (Céu + Lua + 3 camadas de montanhas/ruínas)
        GameObject backdropObj = new GameObject("Parallax_Backdrop");
        backdropObj.transform.SetParent(levelRoot, false);
        ParallaxBackdrop2D backdrop = backdropObj.AddComponent<ParallaxBackdrop2D>();
        backdrop.ConfigureTheme(StageVisualTheme.NightRuins);

        // 2. Pilares antigos de fundo (profundidade arquitetônica estilo Duck Souls)
        float[] pillarXs = { -5.2f, 10.0f, 24.5f, 37.5f, 54.0f, 67.5f, 82.5f };
        foreach (float px in pillarXs)
        {
            CreateProp($"BgPillar_{px:F0}", levelRoot, new Vector3(px, groundTopY + 2.2f, 0f), pillarSprite, sortingOrder: -5, Color.white);
        }

        // 3. Tochas acesas ao longo da fase
        float[] torchXs = { -3.5f, 5.5f, 14.5f, 23.8f, 34.5f, 42.8f, 54.8f, 62.8f, 71.2f, 79.8f };
        foreach (float tx in torchXs)
        {
            GameObject tObj = new GameObject($"WallTorch_{tx:F0}");
            tObj.transform.SetParent(levelRoot, false);
            tObj.transform.position = new Vector3(tx, groundTopY + 0.85f, 0f);
            TorchFlicker2D flicker = tObj.AddComponent<TorchFlicker2D>();
            flicker.EnsureBuilt();
        }

        // 4. Cristais luminosos estilo Celeste perto da plataforma segura e do portal
        CreateProp("CrystalCluster_SafeL", levelRoot, new Vector3(35.3f, groundTopY + 0.45f, 0f), crystalSprite, sortingOrder: -2, new Color(0.65f, 0.98f, 1f, 1f));
        CreateProp("CrystalCluster_SafeR", levelRoot, new Vector3(39.7f, groundTopY + 0.45f, 0f), crystalSprite, sortingOrder: -2, new Color(0.65f, 0.98f, 1f, 1f));
        CreateProp("CrystalCluster_GoalL", levelRoot, new Vector3(80.6f, groundTopY + 0.45f, 0f), crystalSprite, sortingOrder: -2, new Color(0.55f, 1f, 0.82f, 1f));
        CreateProp("CrystalCluster_GoalR", levelRoot, new Vector3(84.4f, groundTopY + 0.45f, 0f), crystalSprite, sortingOrder: -2, new Color(0.55f, 1f, 0.82f, 1f));

        // 5. Névoa no abismo entre Ground_Part1 e Ground_Part2
        SpawnAbyssMist(levelRoot, 48.6f, groundTopY, 8.5f, isDay: false);

        // 6. Partículas atmosféricas (neve/cinzas estilo Celeste + brasas suaves)
        CreateAmbientParticles(levelRoot, isDay: false, spanWidth: 105f);
    }

    public static void PopulateDaylightStageDepth(Transform levelRoot, float groundTopY, float goalX)
    {
        if (levelRoot == null) return;

        EnsurePropSprites();

        // 1. Camada de Parallax Diurna e Alegre (Céu Azul + Sol Radiante + Nuvens + Colinas Verdes)
        GameObject backdropObj = new GameObject("Parallax_Backdrop");
        backdropObj.transform.SetParent(levelRoot, false);
        ParallaxBackdrop2D backdrop = backdropObj.AddComponent<ParallaxBackdrop2D>();
        backdrop.ConfigureTheme(StageVisualTheme.DaylightMeadow);

        // 2. Cristais solares perto do portal da Fase 2
        CreateProp("CrystalCluster_GoalL", levelRoot, new Vector3(goalX - 1.9f, groundTopY + 0.45f, 0f), crystalSprite, sortingOrder: -2, new Color(1.00f, 0.92f, 0.38f, 1f));
        CreateProp("CrystalCluster_GoalR", levelRoot, new Vector3(goalX + 1.9f, groundTopY + 0.45f, 0f), crystalSprite, sortingOrder: -2, new Color(1.00f, 0.92f, 0.38f, 1f));

        // 3. Partículas diurnas alegres (pólen dourado e pétalas rosadas flutuando na brisa)
        CreateAmbientParticles(levelRoot, isDay: true, spanWidth: Mathf.Max(125f, goalX + 25f));
    }

    public static void SpawnDaylightPropOnIsland(Transform levelRoot, float x, float groundTopY, bool isPillar, bool isSunflower)
    {
        EnsurePropSprites();
        if (isPillar)
        {
            CreateProp($"DayPillar_{x:F1}", levelRoot, new Vector3(x, groundTopY + 2.2f, 0f), dayPillarSprite, sortingOrder: -5, Color.white);
        }
        if (isSunflower)
        {
            CreateProp($"Sunflower_{x:F1}", levelRoot, new Vector3(x, groundTopY + 0.50f, 0f), sunflowerSprite, sortingOrder: -2, Color.white);
        }
    }

    public static void SpawnAbyssMist(Transform levelRoot, float centerX, float groundTopY, float width, bool isDay)
    {
        EnsurePropSprites();
        GameObject mistObj = new GameObject($"AbyssMist_{centerX:F1}");
        mistObj.transform.SetParent(levelRoot, false);
        mistObj.transform.position = new Vector3(centerX, groundTopY - 2.3f, 0f);
        SpriteRenderer mistSr = mistObj.AddComponent<SpriteRenderer>();
        mistSr.sprite = isDay ? dayAbyssMistSprite : abyssMistSprite;
        mistSr.drawMode = SpriteDrawMode.Tiled;
        mistSr.tileMode = SpriteTileMode.Continuous;
        mistSr.size = new Vector2(Mathf.Max(3.5f, width + 1.4f), 3.2f);
        mistSr.sortingOrder = -1;
    }

    private static void CreateProp(string name, Transform parent, Vector3 pos, Sprite sprite, int sortingOrder, Color tint)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = tint;
    }

    private static void CreateAmbientParticles(Transform parent, bool isDay, float spanWidth)
    {
        if (particleMat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                particleMat = new Material(shader);
            }
        }

        GameObject snowObj = new GameObject(isDay ? "Ambient_DaylightPollen" : "Ambient_CelesteSnow");
        snowObj.transform.SetParent(parent, false);
        snowObj.transform.position = new Vector3(spanWidth * 0.4f, 3.5f, 0f);

        ParticleSystem ps = snowObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
        main.startColor = isDay
            ? new ParticleSystem.MinMaxGradient(
                new Color(1.00f, 0.94f, 0.45f, 0.72f),
                new Color(1.00f, 0.65f, 0.85f, 0.58f))
            : new ParticleSystem.MinMaxGradient(
                new Color(0.75f, 0.88f, 1.0f, 0.55f),
                new Color(0.50f, 0.78f, 0.96f, 0.30f));
        main.maxParticles = 260;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = isDay ? 30f : 26f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spanWidth, 12f, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.5f, -0.15f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 10;
            if (particleMat != null) renderer.sharedMaterial = particleMat;
        }
    }

    private static void EnsurePropSprites()
    {
        if (pillarSprite == null) pillarSprite = BuildPillarSprite(isDay: false);
        if (dayPillarSprite == null) dayPillarSprite = BuildPillarSprite(isDay: true);
        if (crystalSprite == null) crystalSprite = BuildCrystalSprite();
        if (sunflowerSprite == null) sunflowerSprite = BuildSunflowerSprite();
        if (abyssMistSprite == null) abyssMistSprite = BuildAbyssMistSprite(isDay: false);
        if (dayAbyssMistSprite == null) dayAbyssMistSprite = BuildAbyssMistSprite(isDay: true);
    }

    private static Sprite BuildPillarSprite(bool isDay)
    {
        const int w = 32;
        const int h = 144;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "BgPillarDay32x144" : "BgPillar32x144", w, h);

        Color outline = isDay ? new Color(0.32f, 0.26f, 0.18f, 0.88f) : new Color(0.06f, 0.07f, 0.12f, 0.85f);
        Color dark = isDay ? new Color(0.72f, 0.64f, 0.48f, 0.88f) : new Color(0.11f, 0.13f, 0.22f, 0.85f);
        Color mid = isDay ? new Color(0.86f, 0.78f, 0.62f, 0.88f) : new Color(0.16f, 0.19f, 0.30f, 0.85f);
        Color light = isDay ? new Color(0.96f, 0.92f, 0.78f, 0.88f) : new Color(0.22f, 0.27f, 0.41f, 0.85f);
        Color moss = isDay ? new Color(0.32f, 0.82f, 0.38f, 0.90f) : new Color(0.16f, 0.38f, 0.36f, 0.85f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isCap = (y < 14 || y > h - 16);
                int minX = isCap ? 3 : 7;
                int maxX = isCap ? 28 : 24;

                if (x < minX || x > maxX)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                if (x == minX || x == maxX || y == 0 || y == h - 1 || y == 14 || y == h - 16)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (y % 16 == 0)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (x <= minX + 2)
                {
                    tex.SetPixel(x, y, light);
                }
                else if (x >= maxX - 2)
                {
                    tex.SetPixel(x, y, dark);
                }
                else
                {
                    float n = PixelArtBuilder.Hash01(x, y, 909);
                    tex.SetPixel(x, y, (n > 0.85f && y < 64) ? moss : (n < 0.2f ? dark : mid));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildSunflowerSprite()
    {
        const int size = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("DaySunflower32", size, size);

        Color outline = new Color(0.14f, 0.22f, 0.10f, 1f);
        Color stem = new Color(0.24f, 0.72f, 0.30f, 1f);
        Color leaf = new Color(0.38f, 0.86f, 0.38f, 1f);
        Color petalGold = new Color(1.00f, 0.88f, 0.18f, 1f);
        Color petalOrange = new Color(0.98f, 0.64f, 0.12f, 1f);
        Color coreBrown = new Color(0.42f, 0.22f, 0.10f, 1f);

        Vector2 headCenter = new Vector2(15.5f, 22f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = Color.clear;
                float distHead = Vector2.Distance(new Vector2(x, y), headCenter);

                if (distHead <= 3.5f)
                {
                    c = coreBrown;
                }
                else if (distHead <= 7.2f)
                {
                    c = ((x + y) % 2 == 0) ? petalGold : petalOrange;
                }
                else if (distHead <= 8.2f)
                {
                    c = outline;
                }
                else if (y >= 1 && y <= 15 && x >= 14 && x <= 17)
                {
                    c = (x == 14 || x == 17) ? outline : stem;
                }
                else if (y >= 6 && y <= 10 && Mathf.Abs(x - 15.5f) >= 2f && Mathf.Abs(x - 15.5f) <= 6f)
                {
                    c = leaf;
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildCrystalSprite()
    {
        const int size = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture("Crystal32", size, size);

        Color outline = new Color(0.05f, 0.12f, 0.18f, 1f);
        Color deep = new Color(0.10f, 0.45f, 0.58f, 1f);
        Color mid = new Color(0.24f, 0.82f, 0.88f, 1f);
        Color bright = new Color(0.70f, 0.98f, 1.00f, 1f);

        for (int x = 0; x < size; x++)
        {
            float c1 = Mathf.Max(0f, 1f - Mathf.Abs(x - 10) / 5f) * 20f;
            float c2 = Mathf.Max(0f, 1f - Mathf.Abs(x - 17) / 6f) * 29f;
            float c3 = Mathf.Max(0f, 1f - Mathf.Abs(x - 23) / 4f) * 16f;
            int h = Mathf.RoundToInt(Mathf.Max(c1, Mathf.Max(c2, c3)));

            for (int y = 0; y < size; y++)
            {
                if (h <= 1 || y > h)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (y == h || y == 0)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (x == 16 || x == 9 || y >= h - 3)
                {
                    tex.SetPixel(x, y, bright);
                }
                else if (x > 18 || x < 9)
                {
                    tex.SetPixel(x, y, deep);
                }
                else
                {
                    tex.SetPixel(x, y, mid);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildAbyssMistSprite(bool isDay)
    {
        const int w = 64;
        const int h = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isDay ? "AbyssMistDay64x32" : "AbyssMist64x32", w, h, TextureWrapMode.Repeat);

        for (int y = 0; y < h; y++)
        {
            float t = 1f - (y / (float)(h - 1));
            for (int x = 0; x < w; x++)
            {
                float wave = Mathf.Sin(x * 0.2f) * 0.15f;
                float a = Mathf.Clamp01((t + wave) * 0.55f);
                float stepped = Mathf.Floor(a * 5f) / 5f;
                Color baseCol = isDay ? new Color(0.88f, 0.96f, 1.00f, stepped) : new Color(0.18f, 0.25f, 0.44f, stepped);
                tex.SetPixel(x, y, baseCol);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
    }
}
