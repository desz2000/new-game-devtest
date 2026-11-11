using UnityEngine;

/// <summary>
/// Tema visual minimalista e confortável aos olhos para HUD e menus,
/// combinando cartões arredondados translúcidos, tipografia suave (Segoe UI / Calibri / Verdana)
/// e ícones em Pixel Art.
/// </summary>
public static class PixelUITheme
{
    private static Font friendlyFont;
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

    public static Font FriendlyFont
    {
        get
        {
            if (friendlyFont == null)
            {
                friendlyFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Calibri", "Verdana", "Trebuchet MS", "Arial" },
                    18
                );
            }
            return friendlyFont;
        }
    }

    public static Texture2D DarkBackdrop => darkBackdropTex != null ? darkBackdropTex : (darkBackdropTex = BuildSolidTex("UI_Backdrop", new Color(0.04f, 0.06f, 0.10f, 0.72f)));

    public static Texture2D MainPanelTex => mainPanelTex != null ? mainPanelTex : (mainPanelTex = BuildMinimalCardTex("UI_MainPanel",
        new Color(0.08f, 0.10f, 0.16f, 0.94f),
        new Color(0.24f, 0.30f, 0.42f, 0.85f)));

    public static Texture2D CardNormalTex => cardNormalTex != null ? cardNormalTex : (cardNormalTex = BuildMinimalCardTex("UI_CardNormal",
        new Color(0.11f, 0.14f, 0.21f, 0.90f),
        new Color(0.22f, 0.27f, 0.38f, 0.75f)));

    public static Texture2D CardBoySelectedTex => cardBoySelectedTex != null ? cardBoySelectedTex : (cardBoySelectedTex = BuildMinimalCardTex("UI_CardBoySel",
        new Color(0.10f, 0.18f, 0.29f, 0.95f),
        new Color(0.34f, 0.76f, 0.96f, 0.95f)));

    public static Texture2D CardGirlSelectedTex => cardGirlSelectedTex != null ? cardGirlSelectedTex : (cardGirlSelectedTex = BuildMinimalCardTex("UI_CardGirlSel",
        new Color(0.23f, 0.12f, 0.21f, 0.95f),
        new Color(0.94f, 0.48f, 0.72f, 0.95f)));

    public static Texture2D HudPanelTex => hudPanelTex != null ? hudPanelTex : (hudPanelTex = BuildMinimalCardTex("UI_HudPanel",
        new Color(0.07f, 0.09f, 0.15f, 0.82f),
        new Color(0.24f, 0.30f, 0.42f, 0.55f)));

    public static Texture2D ModalLoseTex => modalLoseTex != null ? modalLoseTex : (modalLoseTex = BuildMinimalCardTex("UI_ModalLose",
        new Color(0.14f, 0.08f, 0.11f, 0.95f),
        new Color(0.82f, 0.32f, 0.40f, 0.85f)));

    public static Texture2D ModalWinTex => modalWinTex != null ? modalWinTex : (modalWinTex = BuildMinimalCardTex("UI_ModalWin",
        new Color(0.07f, 0.15f, 0.14f, 0.95f),
        new Color(0.28f, 0.78f, 0.60f, 0.85f)));

    public static Texture2D FullHeartTex => fullHeartTex != null ? fullHeartTex : (fullHeartTex = BuildHeartTex(isFull: true));
    public static Texture2D EmptyHeartTex => emptyHeartTex != null ? emptyHeartTex : (emptyHeartTex = BuildHeartTex(isFull: false));

    public static Texture2D LoadingBarTrackTex => loadingBarTrackTex != null ? loadingBarTrackTex : (loadingBarTrackTex = BuildMinimalCardTex("UI_LoadTrack", new Color(0.06f, 0.08f, 0.13f, 0.95f), new Color(0.20f, 0.25f, 0.36f, 0.80f)));
    public static Texture2D LoadingBarFillTex => loadingBarFillTex != null ? loadingBarFillTex : (loadingBarFillTex = BuildSolidTex("UI_LoadFill", new Color(0.32f, 0.86f, 0.66f, 1.00f)));

    public static GUIStyle CreateLabelStyle(int fontSize, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal, Color? textColor = null)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label)
        {
            font = FriendlyFont,
            fontSize = fontSize,
            fontStyle = fontStyle,
            alignment = alignment,
            richText = true,
            wordWrap = true
        };
        s.normal.textColor = textColor ?? new Color(0.92f, 0.95f, 0.98f, 1f);
        return s;
    }

    public static GUIStyle CreateFrameStyle(Texture2D bg, int fontSize, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal)
    {
        GUIStyle s = new GUIStyle
        {
            font = FriendlyFont,
            fontSize = fontSize,
            fontStyle = fontStyle,
            alignment = alignment,
            richText = true,
            wordWrap = true,
            border = new RectOffset(6, 6, 6, 6),
            padding = new RectOffset(16, 16, 14, 14)
        };
        s.normal.background = bg;
        s.normal.textColor = new Color(0.93f, 0.96f, 0.99f, 1f);
        return s;
    }

    public static GUIStyle CreateButtonStyle(int paletteKind, int fontSize)
    {
        Texture2D normal;
        Texture2D hover;
        Color textCol = new Color(0.95f, 0.97f, 1f, 1f);

        if (paletteKind == 1) // Azul-ciano suave / Primário
        {
            if (btnGoldTex == null) btnGoldTex = BuildMinimalCardTex("UI_BtnPrimary", new Color(0.16f, 0.48f, 0.68f, 0.96f), new Color(0.42f, 0.82f, 0.98f, 0.95f));
            if (btnGoldHoverTex == null) btnGoldHoverTex = BuildMinimalCardTex("UI_BtnPrimaryH", new Color(0.22f, 0.58f, 0.80f, 0.98f), new Color(0.68f, 0.92f, 1.00f, 1f));
            normal = btnGoldTex;
            hover = btnGoldHoverTex;
        }
        else if (paletteKind == 2) // Rosa suave / Kitty
        {
            if (btnRoseTex == null) btnRoseTex = BuildMinimalCardTex("UI_BtnRose", new Color(0.62f, 0.22f, 0.44f, 0.96f), new Color(0.95f, 0.52f, 0.76f, 0.95f));
            if (btnRoseHoverTex == null) btnRoseHoverTex = BuildMinimalCardTex("UI_BtnRoseH", new Color(0.74f, 0.28f, 0.54f, 0.98f), new Color(1.00f, 0.72f, 0.88f, 1f));
            normal = btnRoseTex;
            hover = btnRoseHoverTex;
        }
        else // Ardósia minimalista
        {
            if (btnSlateTex == null) btnSlateTex = BuildMinimalCardTex("UI_BtnSlate", new Color(0.15f, 0.19f, 0.28f, 0.92f), new Color(0.30f, 0.37f, 0.50f, 0.80f));
            if (btnSlateHoverTex == null) btnSlateHoverTex = BuildMinimalCardTex("UI_BtnSlateH", new Color(0.22f, 0.27f, 0.39f, 0.96f), new Color(0.48f, 0.56f, 0.74f, 0.95f));
            normal = btnSlateTex;
            hover = btnSlateHoverTex;
        }

        GUIStyle s = new GUIStyle
        {
            font = FriendlyFont,
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            border = new RectOffset(6, 6, 6, 6),
            padding = new RectOffset(14, 14, 9, 9)
        };
        s.normal.background = normal;
        s.normal.textColor = textCol;
        s.hover.background = hover;
        s.hover.textColor = Color.white;
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

    private static Texture2D BuildMinimalCardTex(string name, Color fill, Color border)
    {
        const int size = 20;
        Texture2D tex = PixelArtBuilder.CreateBlankTexture(name, size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = (x < 3) ? (2 - x) : ((x >= size - 3) ? (x - (size - 3)) : 0);
                int dy = (y < 3) ? (2 - y) : ((y >= size - 3) ? (y - (size - 3)) : 0);

                // Canto arredondado suave
                if (dx + dy >= 3)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dx + dy == 2 || x == 0 || y == 0 || x == size - 1 || y == size - 1)
                {
                    tex.SetPixel(x, y, border);
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
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
                    case 'O': return new Color(0.08f, 0.08f, 0.13f, isFull ? 0.95f : 0.70f);
                    case 'R': return isFull ? new Color(0.94f, 0.24f, 0.36f, 1f) : new Color(0.20f, 0.23f, 0.31f, 0.65f);
                    case 'r': return isFull ? new Color(0.72f, 0.12f, 0.24f, 1f) : new Color(0.15f, 0.18f, 0.25f, 0.65f);
                    case 'h': return isFull ? new Color(1.00f, 0.68f, 0.74f, 1f) : new Color(0.28f, 0.32f, 0.42f, 0.65f);
                    default:  return Color.clear;
                }
            },
            width: 16,
            height: 16
        );
    }
}
