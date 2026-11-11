using UnityEngine;

/// <summary>
/// Barreira Rúnica Vertical controlada por um Switch2D ('D' no mapa ASCII).
/// Possui pedestal no chão e feixe de energia de altura total (8.5u) para impedir que o jogador pule por cima.
/// Quando ativado pelo Switch2D, o feixe recolhe e a passagem é liberada.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class SwitchGate2D : MonoBehaviour
{
    [Header("Configuração da Barreira Rúnica")]
    public int gateGroupId = 1;
    public float openSpeed = 4.5f;
    public float gateWidth = 0.78f;
    public float gateHeight = 8.5f;

    private SpriteRenderer rootSr;
    private SpriteRenderer pedestalSr;
    private SpriteRenderer beamSr;
    private Transform beamTransform;
    private BoxCollider2D boxCollider;
    private Vector3 closedPosition;
    private bool isOpen;
    private float openProgress;

    private static Sprite pedestalClosedSprite;
    private static Sprite pedestalOpenSprite;
    private static Sprite beamClosedSprite;
    private static Sprite beamOpenSprite;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        rootSr = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        closedPosition = transform.position;
        EnsureVisuals();
    }

    public void ConfigureSpawn(Vector3 worldPos, int groupId)
    {
        closedPosition = worldPos;
        transform.position = worldPos;
        gateGroupId = groupId;
        isOpen = false;
        openProgress = 0f;
        EnsureVisuals();
    }

    public void EnsureVisuals()
    {
        if (rootSr == null) rootSr = GetComponent<SpriteRenderer>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();

        EnsureSprites();

        if (rootSr != null)
        {
            rootSr.enabled = false;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            boxCollider.size = new Vector2(gateWidth, gateHeight);
            boxCollider.enabled = !isOpen;
        }

        Transform pedObj = transform.Find("Visual_GatePedestal");
        if (pedObj == null)
        {
            GameObject go = new GameObject("Visual_GatePedestal");
            go.transform.SetParent(transform, false);
            pedObj = go.transform;
        }
        pedObj.localPosition = new Vector3(0f, -gateHeight * 0.5f + 0.375f, 0f);
        pedestalSr = pedObj.GetComponent<SpriteRenderer>();
        if (pedestalSr == null) pedestalSr = pedObj.gameObject.AddComponent<SpriteRenderer>();
        pedestalSr.sprite = isOpen ? pedestalOpenSprite : pedestalClosedSprite;
        pedestalSr.sortingOrder = 4;
        pedestalSr.color = Color.white;

        Transform beamObj = transform.Find("Visual_GateBeam");
        if (beamObj == null)
        {
            GameObject go = new GameObject("Visual_GateBeam");
            go.transform.SetParent(transform, false);
            beamObj = go.transform;
        }
        beamTransform = beamObj;
        beamObj.localPosition = new Vector3(0f, 0.25f, 0f);
        beamSr = beamObj.GetComponent<SpriteRenderer>();
        if (beamSr == null) beamSr = beamObj.gameObject.AddComponent<SpriteRenderer>();
        beamSr.sprite = isOpen ? beamOpenSprite : beamClosedSprite;
        beamSr.drawMode = SpriteDrawMode.Tiled;
        beamSr.tileMode = SpriteTileMode.Continuous;
        beamSr.size = new Vector2(0.56f, Mathf.Max(1f, gateHeight - 0.5f));
        beamSr.sortingOrder = 3;
        beamSr.color = isOpen ? new Color(0.35f, 1f, 0.72f, 0.15f) : Color.white;
        beamTransform.localScale = isOpen ? new Vector3(1f, 0f, 1f) : Vector3.one;
    }

    public void OpenGate()
    {
        if (isOpen) return;
        isOpen = true;

        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        if (pedestalSr != null && pedestalOpenSprite != null)
        {
            pedestalSr.sprite = pedestalOpenSprite;
        }
        if (beamSr != null && beamOpenSprite != null)
        {
            beamSr.sprite = beamOpenSprite;
        }

        Vector3 fxPos = closedPosition - new Vector3(0f, gateHeight * 0.5f - 0.6f, 0f);
        LaserProjectile2D.SpawnImpactSpark(fxPos, 0.18f, new Color(0.35f, 1f, 0.68f, 1f));
    }

    public void ResetGate()
    {
        isOpen = false;
        openProgress = 0f;
        transform.position = closedPosition;
        transform.localScale = Vector3.one;
        EnsureVisuals();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        float target = isOpen ? 1f : 0f;
        if (!Mathf.Approximately(openProgress, target))
        {
            openProgress = Mathf.MoveTowards(openProgress, target, Time.deltaTime * openSpeed);
        }

        if (beamTransform != null && beamSr != null)
        {
            if (isOpen)
            {
                float sy = Mathf.Lerp(1f, 0f, openProgress);
                beamTransform.localScale = new Vector3(1f, sy, 1f);
                beamTransform.localPosition = new Vector3(0f, Mathf.Lerp(0.25f, -gateHeight * 0.5f + 0.5f, openProgress), 0f);
                beamSr.color = new Color(0.35f, 1f, 0.72f, Mathf.Lerp(0.85f, 0f, openProgress));
            }
            else
            {
                float pulse = 0.84f + Mathf.Sin(Time.time * 6.5f) * 0.16f;
                beamSr.color = new Color(1f, pulse, pulse, 0.92f);
            }
        }
    }

    private static void EnsureSprites()
    {
        if (pedestalClosedSprite == null) pedestalClosedSprite = BuildPedestalSprite(isOpenState: false);
        if (pedestalOpenSprite == null) pedestalOpenSprite = BuildPedestalSprite(isOpenState: true);
        if (beamClosedSprite == null) beamClosedSprite = BuildBeamSprite(isOpenState: false);
        if (beamOpenSprite == null) beamOpenSprite = BuildBeamSprite(isOpenState: true);
    }

    private static Sprite BuildPedestalSprite(bool isOpenState)
    {
        const int w = 26;
        const int h = 24;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isOpenState ? "GatePedestalOpen" : "GatePedestalClosed", w, h);

        Color outline = new Color(0.08f, 0.09f, 0.14f, 1f);
        Color stoneDark = new Color(0.20f, 0.22f, 0.32f, 1f);
        Color stoneMid = new Color(0.34f, 0.38f, 0.52f, 1f);
        Color stoneLight = new Color(0.52f, 0.58f, 0.74f, 1f);
        Color rune = isOpenState ? new Color(0.22f, 0.96f, 0.66f, 1f) : new Color(1.00f, 0.28f, 0.36f, 1f);
        Color core = isOpenState ? new Color(0.86f, 1.00f, 0.94f, 1f) : new Color(1.00f, 0.84f, 0.55f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int margin = (y < 6) ? 1 : (y < 16 ? 3 : 5);
                if (x < margin || x >= w - margin)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                if (x == margin || x == w - margin - 1 || y == 0 || y == h - 1 || y == 6 || y == 16)
                {
                    tex.SetPixel(x, y, outline);
                }
                else if (x >= 10 && x <= 15 && y >= 9 && y <= 14)
                {
                    bool inner = (x >= 12 && x <= 13 && y >= 11 && y <= 12);
                    tex.SetPixel(x, y, inner ? core : rune);
                }
                else if (y >= 17 && x >= 8 && x <= 17)
                {
                    tex.SetPixel(x, y, (y == h - 2) ? core : rune);
                }
                else if (x <= margin + 2)
                {
                    tex.SetPixel(x, y, stoneLight);
                }
                else if (x >= w - margin - 3)
                {
                    tex.SetPixel(x, y, stoneDark);
                }
                else
                {
                    tex.SetPixel(x, y, stoneMid);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    private static Sprite BuildBeamSprite(bool isOpenState)
    {
        const int w = 18;
        const int h = 32;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(isOpenState ? "GateBeamOpen" : "GateBeamClosed", w, h, TextureWrapMode.Repeat);

        Color edge = isOpenState ? new Color(0.18f, 0.78f, 0.55f, 0.55f) : new Color(0.75f, 0.14f, 0.24f, 0.88f);
        Color glow = isOpenState ? new Color(0.32f, 0.98f, 0.72f, 0.72f) : new Color(1.00f, 0.30f, 0.42f, 0.90f);
        Color core = isOpenState ? new Color(0.88f, 1.00f, 0.96f, 0.92f) : new Color(1.00f, 0.86f, 0.72f, 0.96f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x < 2 || x >= w - 2)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool node = (y % 16 >= 6 && y % 16 <= 9);
                if (x >= 7 && x <= 10)
                {
                    tex.SetPixel(x, y, core);
                }
                else if (x >= 5 && x <= 12)
                {
                    tex.SetPixel(x, y, node ? core : glow);
                }
                else if (x == 3 || x == w - 4)
                {
                    tex.SetPixel(x, y, edge);
                }
                else
                {
                    tex.SetPixel(x, y, glow);
                }
            }
        }

        tex.Apply();
        return PixelArtBuilder.CreateSprite64FromTexture(tex, 32f, seamlessTile: true);
    }
}
