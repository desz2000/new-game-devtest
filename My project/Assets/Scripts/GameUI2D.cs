using UnityEngine;

public enum GameState
{
    CharacterSelect = 0,
    Playing = 1,
    StageTransitionLoading = 2,
    Won = 3,
    Lost = 4,
    Paused = 5
}

/// <summary>
/// Gerencia a máquina de estados de tela (Seleção, Jogando, Pausa, Transição, Vitória, Derrota)
/// e toda a interface IMGUI com design ultra-minimalista, sem caixas desnecessárias na HUD
/// e com tipografia limpa e amigável aos olhos.
/// </summary>
public class GameUI2D : MonoBehaviour
{
    [Header("Referência do Jogador")]
    public BoxPlayer2D player;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<BoxPlayer2D>();
            if (player == null)
            {
                player = FindAnyObjectByType<BoxPlayer2D>();
            }
        }
    }

    private void OnGUI()
    {
        if (player == null)
        {
            player = GetComponent<BoxPlayer2D>();
            if (player == null) return;
        }

        GUI.color = Color.white;
        GUI.backgroundColor = Color.white;

        switch (player.CurrentGameState)
        {
            case GameState.CharacterSelect:
                DrawStartScreenGUI();
                break;

            case GameState.StageTransitionLoading:
                DrawLoadingScreenGUI();
                break;

            case GameState.Playing:
                DrawGameplayHUD();
                break;

            case GameState.Paused:
                DrawGameplayHUD();
                DrawPauseModalGUI();
                break;

            case GameState.Won:
            case GameState.Lost:
                DrawGameplayHUD();
                DrawEndModalGUI();
                break;
        }
    }

    /// <summary>
    /// Desenha texto limpo com sombra suave de 1px para leitura perfeita sobre o cenário sem precisar de caixas de fundo.
    /// </summary>
    private static void DrawShadowedLabel(Rect rect, string PlainText, string richText, GUIStyle baseStyle)
    {
        Color prevColor = GUI.color;
        bool prevRich = baseStyle.richText;

        baseStyle.richText = false;
        GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.82f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), PlainText, baseStyle);

        baseStyle.richText = prevRich;
        GUI.color = prevColor;
        GUI.Label(rect, richText, baseStyle);
    }

    /// <summary>
    /// HUD ultra-minimalista: apenas os corações e indicadores essenciais flutuando discretamente no canto superior esquerdo,
    /// sem molduras pesadas ou banners de controles poluindo a tela.
    /// </summary>
    private void DrawGameplayHUD()
    {
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.85f, 1.45f);
        float marginX = 22f * scale;
        float marginY = 18f * scale;

        float heartSize = 24f * scale;
        float heartGap = 6f * scale;
        for (int i = 0; i < player.maxHearts; i++)
        {
            Texture2D tex = (i < player.CurrentHearts) ? PixelUITheme.FullHeartTex : PixelUITheme.EmptyHeartTex;
            GUI.DrawTexture(
                new Rect(marginX + i * (heartSize + heartGap), marginY, heartSize, heartSize),
                tex,
                ScaleMode.ScaleToFit
            );
        }

        int totalCoins = Mathf.Max(player.CoinsCollected, LevelBuilder2D.LastGeneratedCoinCount);
        bool hasCombo = player.StompComboCount >= 2;
        string comboPlain = hasCombo ? $"   •   x{player.StompComboCount}" : "";
        string comboRich = hasCombo ? $"   <color=#64748B>•</color>   <color=#F472B6>x{player.StompComboCount}</color>" : "";

        string plainStats = $"{player.CoinsCollected}/{totalCoins}   •   {player.Score}{comboPlain}";
        string richStats = $"<color=#FCD34D>{player.CoinsCollected}/{totalCoins}</color>   <color=#64748B>•</color>   <color=#E2E8F0>{player.Score}</color>{comboRich}";

        GUIStyle statsStyle = PixelUITheme.CreateLabelStyle(
            Mathf.RoundToInt(15 * scale),
            TextAnchor.MiddleLeft,
            FontStyle.Bold,
            new Color(0.92f, 0.95f, 0.99f, 1f)
        );

        float statsX = marginX + player.maxHearts * (heartSize + heartGap) + 16f * scale;
        Rect statsRect = new Rect(statsX, marginY - 1f * scale, 360f * scale, heartSize + 2f * scale);
        DrawShadowedLabel(statsRect, plainStats, richStats, statsStyle);
    }

    private void DrawPauseModalGUI()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float modalW = Mathf.Clamp(Screen.width * 0.40f, 380f, 520f);
        float modalH = Mathf.Clamp(Screen.height * 0.56f, 340f, 440f);
        float modalX = (Screen.width - modalW) * 0.5f;
        float modalY = (Screen.height - modalH) * 0.5f;
        float s = Mathf.Clamp(Mathf.Min(modalW / 460f, modalH / 390f), 0.85f, 1.25f);

        GUIStyle modalBox = PixelUITheme.CreateFrameStyle(PixelUITheme.MainPanelTex, Mathf.RoundToInt(22 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        modalBox.padding = new RectOffset(24, 24, Mathf.RoundToInt(22 * s), 20);
        GUI.Box(new Rect(modalX, modalY, modalW, modalH), "Pausa", modalBox);

        float btnW = modalW - 84f * s;
        float btnH = 40f * s;
        float btnX = modalX + (modalW - btnW) * 0.5f;

        // Controle minimalista de Volume (Fase 5)
        float volY = modalY + 66f * s;
        int volPct = Mathf.RoundToInt(GameAudio2D.MasterVolume * 100f);
        GUIStyle volLabelStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(14 * s), TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.78f, 0.85f, 0.94f, 1f));
        GUIStyle smallBtn = PixelUITheme.CreateButtonStyle(0, Mathf.RoundToInt(14 * s));

        float stepBtnW = 38f * s;
        if (GUI.Button(new Rect(btnX, volY, stepBtnW, 32f * s), "-", smallBtn))
        {
            GameAudio2D.AdjustVolume(-0.1f);
            GameAudio2D.PlaySwitch();
        }
        GUI.Label(new Rect(btnX + stepBtnW, volY, btnW - stepBtnW * 2f, 32f * s), $"Volume: <b>{volPct}%</b>", volLabelStyle);
        if (GUI.Button(new Rect(btnX + btnW - stepBtnW, volY, stepBtnW, 32f * s), "+", smallBtn))
        {
            GameAudio2D.AdjustVolume(0.1f);
            GameAudio2D.PlaySwitch();
        }

        float firstBtnY = volY + 48f * s;
        float btnStep = 48f * s;

        GUIStyle primaryBtn = PixelUITheme.CreateButtonStyle(1, Mathf.RoundToInt(14 * s));
        GUIStyle secondaryBtn = PixelUITheme.CreateButtonStyle(0, Mathf.RoundToInt(14 * s));

        if (GUI.Button(new Rect(btnX, firstBtnY, btnW, btnH), "Continuar  [Esc / Enter]", primaryBtn))
        {
            player.ResumeFromPause();
        }

        if (GUI.Button(new Rect(btnX, firstBtnY + btnStep, btnW, btnH), "Reiniciar Fase  [R]", secondaryBtn))
        {
            Time.timeScale = 1f;
            player.ResetPlayer(countAttempt: false, rebuildProceduralIfStage2: false);
        }

        if (GUI.Button(new Rect(btnX, firstBtnY + btnStep * 2f, btnW, btnH), "Trocar Personagem  [T]", secondaryBtn))
        {
            player.EnterCharacterSelectScreen();
        }

        if (GUI.Button(new Rect(btnX, firstBtnY + btnStep * 3f, btnW, btnH), "Sair", secondaryBtn))
        {
            player.QuitGame();
        }
    }

    private void DrawLoadingScreenGUI()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float panelW = Mathf.Clamp(Screen.width * 0.64f, 540f, 860f);
        float panelH = Mathf.Clamp(Screen.height * 0.66f, 380f, 540f);
        float panelX = (Screen.width - panelW) * 0.5f;
        float panelY = (Screen.height - panelH) * 0.5f;
        float s = Mathf.Clamp(Mathf.Min(panelW / 780f, panelH / 480f), 0.80f, 1.25f);

        GUIStyle boxStyle = PixelUITheme.CreateFrameStyle(PixelUITheme.ModalWinTex, Mathf.RoundToInt(22 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        boxStyle.padding = new RectOffset(24, 24, Mathf.RoundToInt(22 * s), 20);

        GUI.Box(
            new Rect(panelX, panelY, panelW, panelH),
            $"<color=#6EE7B7>Fase {player.currentStage} Concluída</color>  <color=#64748B>•</color>  <color=#FDE68A>{(player.currentStage >= 2 ? "Prepare-se" : "Novo Poder")}</color>",
            boxStyle
        );

        bool isBoy = (player.selectedCharacter == PlayerCharacterType.BoyLaser);
        bool hasPad = GameInput2D.HasGamepad;

        Texture2D[] animFrames = isBoy
            ? PixelHeroAnimator2D.GetBoyStompPreviewTextures()
            : PixelHeroAnimator2D.GetKittyScratchPreviewTextures();
        int frameIdx = (animFrames != null && animFrames.Length > 0)
            ? Mathf.FloorToInt(Time.unscaledTime * 6.5f) % animFrames.Length
            : 0;
        Texture2D activeFrame = (animFrames != null && animFrames.Length > 0)
            ? animFrames[frameIdx]
            : (isBoy ? PixelHeroAnimator2D.GetBoyPreviewTexture() : PixelHeroAnimator2D.GetKittyPreviewTexture());

        float cardW = panelW - 76f * s;
        float cardH = 175f * s;
        Rect powerCardRect = new Rect(panelX + (panelW - cardW) * 0.5f, panelY + 66f * s, cardW, cardH);
        Texture2D cardBg = isBoy ? PixelUITheme.CardBoySelectedTex : PixelUITheme.CardGirlSelectedTex;
        GUIStyle powerCardStyle = PixelUITheme.CreateFrameStyle(cardBg, Mathf.RoundToInt(16 * s), TextAnchor.UpperLeft, FontStyle.Bold);
        GUI.Box(powerCardRect, GUIContent.none, powerCardStyle);

        float spriteSize = Mathf.Clamp(cardH * 0.70f, 96f, 148f);
        float bounceY = isBoy
            ? -Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6.5f)) * (8f * s)
            : Mathf.Sin(Time.unscaledTime * 8f) * (3.5f * s);
        Rect spriteRect = new Rect(
            powerCardRect.x + 22f * s,
            powerCardRect.y + (cardH - spriteSize) * 0.5f + bounceY,
            spriteSize,
            spriteSize
        );
        GUI.DrawTexture(spriteRect, activeFrame, ScaleMode.ScaleToFit);

        GUIStyle powerTitleStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(18 * s), TextAnchor.UpperLeft, FontStyle.Bold, Color.white);
        GUIStyle powerBodyStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(14 * s), TextAnchor.UpperLeft, FontStyle.Normal, new Color(0.84f, 0.89f, 0.96f, 1f));

        string powerTitle = isBoy
            ? "<color=#7DD3FC>Botas de Pulo Esmagador</color>"
            : "<color=#F9A8D4>Garras de Arranhão</color>";

        string powerDesc = isBoy
            ? "Agora você também pode saltar sobre os robôs para esmagá-los, além de manter o Laser.\n\n" +
              (hasPad ? "<color=#94A3B8>Usar:</color> <b>[A]</b> sobre inimigos." : "<color=#94A3B8>Usar:</color> <b>[Espaço / W]</b> sobre inimigos.")
            : "Ataque de curta distância que causa dano aos robôs e ativa interruptores.\n\n" +
              (hasPad ? "<color=#94A3B8>Usar:</color> <b>[Q / X / RT]</b>." : "<color=#94A3B8>Usar:</color> <b>[Q]</b>.");

        float textLeft = spriteRect.xMax + 20f * s;
        float textWidth = powerCardRect.xMax - textLeft - 20f * s;
        GUI.Label(new Rect(textLeft, powerCardRect.y + 22f * s, textWidth, 28f * s), powerTitle, powerTitleStyle);
        GUI.Label(new Rect(textLeft, powerCardRect.y + 52f * s, textWidth, cardH - 64f * s), powerDesc, powerBodyStyle);

        float progress = player.LoadingProgress;
        int percent = Mathf.RoundToInt(progress * 100f);

        GUIStyle loadLabelStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(15 * s), TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.90f, 0.94f, 0.98f, 1f));
        GUIStyle stepStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(13 * s), TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.64f, 0.72f, 0.84f, 1f));

        float statusY = powerCardRect.yMax + 22f * s;
        GUI.Label(
            new Rect(panelX + 32f * s, statusY, panelW - 64f * s, 24f * s),
            $"Fase {player.NextStageNumber} — {(player.NextStageNumber >= 3 ? "Montanhas Nevadas" : "Pradaria Solar")} ({percent}%)",
            loadLabelStyle
        );

        float barW = panelW - 140f * s;
        float barH = 14f * s;
        float barX = panelX + (panelW - barW) * 0.5f;
        float barY = statusY + 32f * s;

        GUI.DrawTexture(new Rect(barX, barY, barW, barH), PixelUITheme.LoadingBarTrackTex, ScaleMode.StretchToFill);
        float fillW = Mathf.Max(6f, (barW - 4f * s) * progress);
        GUI.DrawTexture(
            new Rect(barX + 2f * s, barY + 2f * s, fillW, barH - 4f * s),
            PixelUITheme.LoadingBarFillTex,
            ScaleMode.StretchToFill
        );

        bool canSkip = player.CanSkipLoading;
        string skipHint = canSkip
            ? (hasPad ? "<color=#6EE7B7>Pressione [A / Enter] para continuar</color>" : "<color=#6EE7B7>Pressione [Enter / Espaço] para continuar</color>")
            : "Gerando fase...";
        GUI.Label(new Rect(panelX + 32f * s, barY + barH + 12f * s, panelW - 64f * s, 22f * s), skipHint, stepStyle);
    }

    private void DrawEndModalGUI()
    {
        bool hasLost = (player.CurrentGameState == GameState.Lost);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float modalW = Mathf.Clamp(Screen.width * 0.46f, 420f, 640f);
        float modalH = Mathf.Clamp(Screen.height * 0.54f, 320f, 460f);
        float modalX = (Screen.width - modalW) * 0.5f;
        float modalY = (Screen.height - modalH) * 0.5f;
        float s = Mathf.Clamp(Mathf.Min(modalW / 580f, modalH / 400f), 0.80f, 1.25f);

        Texture2D bg = hasLost ? PixelUITheme.ModalLoseTex : PixelUITheme.ModalWinTex;
        GUIStyle modalBox = PixelUITheme.CreateFrameStyle(bg, Mathf.RoundToInt(22 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        modalBox.padding = new RectOffset(24, 24, Mathf.RoundToInt(24 * s), 20);

        string title = hasLost
            ? $"<color=#FDA4AF>Fim de Jogo</color>"
            : "<color=#6EE7B7>Vitória!</color>";
        GUI.Box(new Rect(modalX, modalY, modalW, modalH), title, modalBox);

        GUIStyle subStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(14 * s), TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.78f, 0.84f, 0.92f, 1f));

        string subtitle = $"Fase {player.currentStage}   •   {player.Score} pts   •   Tentativa #{player.Attempts}";
        GUI.Label(new Rect(modalX + 24f, modalY + 62f * s, modalW - 48f, 28f * s), subtitle, subStyle);

        float btnW = modalW - 100f * s;
        float btnH = 42f * s;
        float btnX = modalX + (modalW - btnW) * 0.5f;
        float firstBtnY = modalY + 108f * s;
        float btnStep = 50f * s;

        GUIStyle primaryBtn = PixelUITheme.CreateButtonStyle(1, Mathf.RoundToInt(14 * s));
        GUIStyle secondaryBtn = PixelUITheme.CreateButtonStyle(0, Mathf.RoundToInt(14 * s));

        bool hasPad = GameInput2D.HasGamepad;
        string restartLabel = (player.currentStage >= 2)
            ? (hasPad ? $"Nova Fase {player.currentStage}  [Enter / A]" : $"Nova Fase {player.currentStage}  [Enter]")
            : (hasPad ? "Jogar Novamente  [Enter / A]" : "Jogar Novamente  [Enter]");
        string backStage1Label = "Fase 1  [1]";
        string switchLabel = hasPad ? "Personagem  [T / Y]" : "Personagem  [T]";
        string quitLabel = hasPad ? "Sair  [Esc / B]" : "Sair  [Esc]";

        if (GUI.Button(new Rect(btnX, firstBtnY, btnW, btnH), restartLabel, primaryBtn))
        {
            player.ResetPlayer(countAttempt: hasLost, rebuildProceduralIfStage2: (player.currentStage >= 2));
        }

        float currentY = firstBtnY + btnStep;
        if (player.currentStage >= 2)
        {
            if (GUI.Button(new Rect(btnX, currentY, btnW, btnH), backStage1Label, secondaryBtn))
            {
                player.ReturnToStage1FromModal();
            }
            currentY += btnStep;
        }

        if (GUI.Button(new Rect(btnX, currentY, btnW, btnH), switchLabel, secondaryBtn))
        {
            player.EnterCharacterSelectScreen();
        }
        currentY += btnStep;

        if (GUI.Button(new Rect(btnX, currentY, btnW, btnH), quitLabel, secondaryBtn))
        {
            player.QuitGame();
        }
    }

    private void DrawStartScreenGUI()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), PixelUITheme.DarkBackdrop, ScaleMode.StretchToFill);

        float panelW = Mathf.Clamp(Screen.width * 0.72f, 600f, 1020f);
        float panelH = Mathf.Clamp(Screen.height * 0.74f, 420f, 640f);
        float panelX = (Screen.width - panelW) * 0.5f;
        float panelY = (Screen.height - panelH) * 0.5f;

        float s = Mathf.Clamp(Mathf.Min(panelW / 900f, panelH / 560f), 0.76f, 1.25f);

        GUIStyle mainBoxStyle = PixelUITheme.CreateFrameStyle(PixelUITheme.MainPanelTex, Mathf.RoundToInt(22 * s), TextAnchor.UpperCenter, FontStyle.Bold);
        mainBoxStyle.padding = new RectOffset(22, 22, Mathf.RoundToInt(22 * s), 16);
        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "Ruínas do Monte", mainBoxStyle);

        GUIStyle subtitleStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(13 * s), TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.60f, 0.68f, 0.80f, 1f));

        bool hasPad = GameInput2D.HasGamepad;
        string subtitleText = hasPad
            ? $"Escolha seu personagem  •  <color=#6EE7B7>{GameInput2D.ConnectedGamepadName}</color>"
            : "Escolha seu personagem";

        GUI.Label(
            new Rect(panelX + 20f, panelY + 52f * s, panelW - 40f, 22f * s),
            subtitleText,
            subtitleStyle
        );

        float sidePad = 30f * s;
        float cardGap = 20f * s;
        float cardsTop = panelY + 84f * s;
        float footerH = 38f * s;
        float cardW = (panelW - sidePad * 2f - cardGap) * 0.5f;
        float cardH = panelH - (cardsTop - panelY) - footerH - 12f * s;

        bool boySelected = (player.selectedCharacter == PlayerCharacterType.BoyLaser);
        bool girlSelected = (player.selectedCharacter == PlayerCharacterType.GirlKitty);

        Rect boyCard = new Rect(panelX + sidePad, cardsTop, cardW, cardH);
        DrawCharacterCard(
            boyCard,
            s,
            isSelected: boySelected,
            selectedTex: PixelUITheme.CardBoySelectedTex,
            title: "<color=#7DD3FC>Menino</color>",
            badge: hasPad ? "Laser  [Q / X / RT]" : "Laser  [Q]",
            desc: "Ataque à distância.",
            previewTex: PixelHeroAnimator2D.GetBoyPreviewTexture(),
            buttonLabel: hasPad ? "Jogar  [1 / A]" : "Jogar  [1]",
            buttonKind: boySelected ? 1 : 0,
            characterType: PlayerCharacterType.BoyLaser
        );

        Rect girlCard = new Rect(boyCard.xMax + cardGap, cardsTop, cardW, cardH);
        DrawCharacterCard(
            girlCard,
            s,
            isSelected: girlSelected,
            selectedTex: PixelUITheme.CardGirlSelectedTex,
            title: "<color=#F9A8D4>Kitty</color>",
            badge: hasPad ? "Pisão  [Espaço / A]" : "Pisão  [Espaço]",
            desc: "Combo de pulo e impacto.",
            previewTex: PixelHeroAnimator2D.GetKittyPreviewTexture(),
            buttonLabel: hasPad ? "Jogar  [2 / A]" : "Jogar  [2]",
            buttonKind: girlSelected ? 2 : 0,
            characterType: PlayerCharacterType.GirlKitty
        );

        GUIStyle footerStyle = PixelUITheme.CreateLabelStyle(Mathf.RoundToInt(13 * s), TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.60f, 0.68f, 0.80f, 1f));
        string footerText = hasPad
            ? "[A / D] Alternar   •   [Enter / A] Iniciar   •   [Esc] Pausa"
            : "[A / D] Alternar   •   [Enter / Espaço] Iniciar   •   [Esc] Pausa";
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
            Mathf.RoundToInt(18 * scale),
            TextAnchor.UpperCenter,
            FontStyle.Bold
        );
        cardStyle.padding = new RectOffset(16, 16, Mathf.RoundToInt(16 * scale), 14);

        GUI.Box(cardRect, title, cardStyle);

        float previewSize = Mathf.Clamp(cardRect.height * 0.40f, 92f, 175f);
        float bob = isSelected ? Mathf.Sin(Time.unscaledTime * 4.0f) * (3.0f * scale) : 0f;
        Rect previewRect = new Rect(
            cardRect.x + (cardRect.width - previewSize) * 0.5f,
            cardRect.y + 46f * scale + bob,
            previewSize,
            previewSize
        );
        GUI.DrawTexture(previewRect, previewTex, ScaleMode.ScaleToFit);

        GUIStyle descStyle = PixelUITheme.CreateLabelStyle(
            Mathf.RoundToInt(14 * scale),
            TextAnchor.UpperCenter,
            FontStyle.Bold,
            new Color(0.90f, 0.94f, 0.98f, 1f)
        );

        float textTop = cardRect.y + 52f * scale + previewSize + 6f * scale;
        float btnH = Mathf.Clamp(42f * scale, 36f, 54f);
        float textH = Mathf.Max(32f, (cardRect.yMax - btnH - 18f * scale) - textTop);

        GUI.Label(
            new Rect(cardRect.x + 16f * scale, textTop, cardRect.width - 32f * scale, textH),
            $"{badge}\n<size={Mathf.RoundToInt(12 * scale)}><color=#94A3B8>{desc}</color></size>",
            descStyle
        );

        GUIStyle btnStyle = PixelUITheme.CreateButtonStyle(buttonKind, Mathf.RoundToInt(14 * scale));
        Rect btnRect = new Rect(cardRect.x + 22f * scale, cardRect.yMax - btnH - 14f * scale, cardRect.width - 44f * scale, btnH);
        if (GUI.Button(btnRect, buttonLabel, btnStyle))
        {
            player.SelectAndStartGame(characterType);
        }
    }
}
