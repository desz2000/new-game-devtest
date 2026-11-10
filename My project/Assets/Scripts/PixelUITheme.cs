using UnityEngine;

/// <summary>
/// Tema visual em Pixel Art para menus e HUD (bordas 9-slice nítidas e corações 16x16 sombreados).
/// </summary>
public static class PixelUITheme
{
    private static Texture2D darkBackdropTex;
    private static Texture2D mainPanelTex;
    private static Texture2D cardNormalTex;
    private static Texture2D cardBoySelectedTex;
    private static Texture2D cardGirlSelectedTex;
    private static Texture2D hudPanelTex;
    private static Texture2D modalLoseTex;
    private static Texture2D modalWinTex;

    private static Texture2D btnGoldTex;
    private static Texture2D btnGoldHoverTex;
    private static Texture2D btnSlateTex;
    private static Texture2D btnSlateHoverTex;
    private static Texture2D btnRoseTex;
    private static Texture2D btnRoseHoverTex;

    private static Texture2D fullHeartTex;
    private static Texture2D emptyHeartTex;
    private static Texture2D loadingBarTrackTex;
    private static Texture2D loadingBarFillTex;

    private static readonly string[] Heart16Grid =
    {
        "................",
        "..OOOO....OOOO..",
        ".ORhhRO..ORrrRO.",
        "ORhRRRROORrrrrRO",
        "ORhRRRRRRrRRRRRO",
        "ORRRRRRRRRRRRRRO",
        "ORRRRRRRRRrrrrRO",
        ".ORRRRRRRrrrrRO.",
        "..ORRRRRrrrrRO..",
        "...ORRRRrrrRO...",
        "....ORRRrrRO....",
        ".....ORRrrO.....",
        "......ORrO......",
        ".......OO.......",
        "................",
        "................"
    };

    public static Texture2D DarkBackdrop => darkBackdropTex != null ? darkBackdropTex : (darkBackdropTex = BuildSolidTex("UI_Backdrop", new Color(0.03f, 0.04f, 0.08f, 0.78f)));

    public static Texture2D MainPanelTex => mainPanelTex != null ? mainPanelTex : (mainPanelTex = BuildFrameTex("UI_MainPanel",
        new Color(0.07f, 0.09f, 0.16f, 0.96f),
        new Color(0.32f, 0.38f, 0.56f, 1f),
        new Color(0.76f, 0.64f, 0.34f, 1f),
        new Color(0.04f, 0.05f, 0.09f, 1f)));

    public static Texture2D CardNormalTex => cardNormalTex != null ? cardNormalTex : (cardNormalTex = BuildFrameTex("UI_CardNormal",
        new Color(0.11f, 0.13f, 0.22f, 0.94f),
        new Color(0.24f, 0.28f, 0.42f, 1f),
        new Color(0.42f, 0.48f, 0.64f, 1f),
        new Color(0.05f, 0.06f, 0.11f, 1f)));

    public static Texture2D CardBoySelectedTex => cardBoySelectedTex != null ? cardBoySelectedTex : (cardBoySelectedTex = BuildFrameTex("UI_CardBoySel",
        new Color(0.10f, 0.20f, 0.36f, 0.96f),
        new Color(0.24f, 0.72f, 0.98f, 1f),
        new Color(0.66f, 0.95f, 1.00f, 1f),
        new Color(0.04f, 0.09f, 0.18f, 1f)));

    public static Texture2D CardGirlSelectedTex => cardGirlSelectedTex != null ? cardGirlSelectedTex : (cardGirlSelectedTex = BuildFrameTex("UI_CardGirlSel",
        new Color(0.28f, 0.11f, 0.24f, 0.96f),
        new Color(0.95f, 0.35f, 0.66f, 1f),
        new Color(1.00f, 0.74f, 0.88f, 1f),
        new Color(0.12f, 0.04f, 0.10f, 1f)));

    public static Texture2D HudPanelTex => hudPanelTex != null ? hudPanelTex : (hudPanelTex = BuildFrameTex("UI_HudPanel",
        new Color(0.06f, 0.08f, 0.14f, 0.90f),
        new Color(0.28f, 0.34f, 0.50f, 1f),
        new Color(0.54f, 0.62f, 0.80f, 1f),
        new Color(0.03f, 0.04f, 0.08f, 1f)));

    public static Texture2D ModalLoseTex => modalLoseTex != null ? modalLoseTex : (modalLoseTex = BuildFrameTex("UI_ModalLose",
        new Color(0.18f, 0.06f, 0.09f, 0.97f),
        new Color(0.84f, 0.18f, 0.26f, 1f),
        new Color(1.00f, 0.58f, 0.62f, 1f),
        new Color(0.06f, 0.02f, 0.04f, 1f)));

    public static Texture2D ModalWinTex => modalWinTex != null ? modalWinTex : (modalWinTex = BuildFrameTex("UI_ModalWin",
        new Color(0.06f, 0.17f, 0.14f, 0.97f),
        new Color(0.20f, 0.82f, 0.58f, 1f),
        new Color(0.70f, 1.00f, 0.86f, 1f),
        new Color(0.03f, 0.07f, 0.06f, 1f)));

    public static Texture2D FullHeartTex => fullHeartTex != null ? fullHeartTex : (fullHeartTex = BuildHeartTex(isFull: true));
    public static Texture2D EmptyHeartTex => emptyHeartTex != null ? emptyHeartTex : (emptyHeartTex = BuildHeartTex(isFull: false));

    public static Texture2D LoadingBarTrackTex => loadingBarTrackTex != null ? loadingBarTrackTex : (loadingBarTrackTex = BuildSolidTex("UI_LoadTrack", new Color(0.05f, 0.07f, 0.13f, 0.96f)));
    public static Texture2D LoadingBarFillTex => loadingBarFillTex != null ? loadingBarFillTex : (loadingBarFillTex = BuildSolidTex("UI_LoadFill", new Color(0.35f, 0.96f, 0.68f, 1.00f)));

    public static GUIStyle CreateFrameStyle(Texture2D bg, int fontSize, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal)
    {
        GUIStyle s = new GUIStyle
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            alignment = alignment,
            richText = true,
            wordWrap = true,
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(16, 16, 14, 14)
        };
        s.normal.background = bg;
        s.normal.textColor = new Color(0.95f, 0.97f, 1f, 1f);
        return s;
    }

    public static GUIStyle CreateButtonStyle(int paletteKind, int fontSize)
    {
        Texture2D normal;
        Texture2D hover;
        Color textCol = Color.white;

        if (paletteKind == 1) // Dourado / Amarelo
        {
            if (btnGoldTex == null) btnGoldTex = BuildFrameTex("UI_BtnGold", new Color(0.78f, 0.56f, 0.10f, 1f), new Color(1.00f, 0.84f, 0.25f, 1f), new Color(1.00f, 0.96f, 0.72f, 1f), new Color(0.20f, 0.12f, 0.02f, 1f));
            if (btnGoldHoverTex == null) btnGoldHoverTex = BuildFrameTex("UI_BtnGoldH", new Color(0.92f, 0.68f, 0.14f, 1f), new Color(1.00f, 0.92f, 0.45f, 1f), Color.white, new Color(0.24f, 0.15f, 0.03f, 1f));
            normal = btnGoldTex;
            hover = btnGoldHoverTex;
            textCol = new Color(0.10f, 0.08f, 0.04f, 1f);
        }
        else if (paletteKind == 2) // Rosa / Kitty
        {
            if (btnRoseTex == null) btnRoseTex = BuildFrameTex("UI_BtnRose", new Color(0.78f, 0.22f, 0.48f, 1f), new Color(0.98f, 0.46f, 0.72f, 1f), new Color(1.00f, 0.82f, 0.92f, 1f), new Color(0.18f, 0.04f, 0.10f, 1f));
            if (btnRoseHoverTex == null) btnRoseHoverTex = BuildFrameTex("UI_BtnRoseH", new Color(0.92f, 0.30f, 0.58f, 1f), new Color(1.00f, 0.62f, 0.82f, 1f), Color.white, new Color(0.22f, 0.05f, 0.12f, 1f));
            normal = btnRoseTex;
            hover = btnRoseHoverTex;
        }
        else // Ardósia padrão
        {
            if (btnSlateTex == null) btnSlateTex = BuildFrameTex("UI_BtnSlate", new Color(0.18f, 0.22f, 0.34f, 1f), new Color(0.42f, 0.48f, 0.66f, 1f), new Color(0.70f, 0.78f, 0.94f, 1f), new Color(0.06f, 0.08f, 0.14f, 1f));
            if (btnSlateHoverTex == null) btnSlateHoverTex = BuildFrameTex("UI_BtnSlateH", new Color(0.26f, 0.32f, 0.48f, 1f), new Color(0.58f, 0.66f, 0.86f, 1f), Color.white, new Color(0.08f, 0.10f, 0.18f, 1f));
            normal = btnSlateTex;
            hover = btnSlateHoverTex;
        }

        GUIStyle s = new GUIStyle
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(14, 14, 10, 10)
        };
        s.normal.background = normal;
        s.normal.textColor = textCol;
        s.hover.background = hover;
        s.hover.textColor = textCol;
        s.active.background = normal;
        s.active.textColor = textCol;
        return s;
    }

    private static Texture2D BuildSolidTex(string name, Color col)
    {
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(name, 4, 4);
        Color[] arr = new Color[16];
        for (int i = 0; i < arr.Length; i++) arr[i] = col;
        tex.SetPixels(arr);
        tex.Apply();
        return tex;
    }

    private static Texture2D BuildFrameTex(string name, Color fill, Color borderMid, Color cornerLight, Color outerLine)
    {
        const int size = 24;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(name, size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isOuter = (x == 0 || y == 0 || x == size - 1 || y == size - 1);
                bool isRim = (x == 1 || y == 1 || x == size - 2 || y == size - 2 || x == 2 || y == 2 || x == size - 3 || y == size - 3);
                bool isInnerLine = (x == 3 || y == 3 || x == size - 4 || y == size - 4);
                bool isCornerRivet = (x <= 4 || x >= size - 5) && (y <= 4 || y >= size - 5) && !isOuter;

                if (isOuter) tex.SetPixel(x, y, outerLine);
                else if (isCornerRivet) tex.SetPixel(x, y, cornerLight);
                else if (isRim) tex.SetPixel(x, y, (y >= size - 3 || x <= 2) ? cornerLight : borderMid);
                else if (isInnerLine) tex.SetPixel(x, y, outerLine);
                else tex.SetPixel(x, y, fill);
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D BuildHeartTex(bool isFull)
    {
        return PixelArtBuilder.CreateTextureFromGrid(
            isFull ? "UI_Heart16_Full" : "UI_Heart16_Empty",
            Heart16Grid,
            c =>
            {
                switch (c)
                {
                    case 'O': return new Color(0.07f, 0.06f, 0.11f, isFull ? 1f : 0.85f);
                    case 'R': return isFull ? new Color(0.95f, 0.16f, 0.28f, 1f) : new Color(0.22f, 0.24f, 0.32f, 0.78f);
                    case 'r': return isFull ? new Color(0.68f, 0.07f, 0.18f, 1f) : new Color(0.15f, 0.17f, 0.24f, 0.78f);
                    case 'h': return isFull ? new Color(1.00f, 0.62f, 0.68f, 1f) : new Color(0.32f, 0.35f, 0.44f, 0.78f);
                    default:  return Color.clear;
                }
            },
            width: 16,
            height: 16
        );
    }
}
