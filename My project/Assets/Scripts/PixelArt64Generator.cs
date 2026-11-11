using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Gerador Nativo de Sprites 64-Bit HD (Salto Geracional 64x64 @ 44 PPU ~ 1.45 unidades de mundo):
///   - Esculpe os personagens (Menino Laser e Boneca Kitty) e os 4 arquétipos de Robôs diretamente
///     em uma malha nativa 64x64 com proporções heroicas maiores, anatomia articulada, 6 níveis de
///     sombreado direcional, oclusão de ambiente (AO) entre camadas e contorno seletivo unificado (Sel-Out).
///   - Salva e carrega automaticamente os PNGs pré-gerados em Assets/Resources/Sprites64/ para
///     performance máxima (O(1) em memória e zero recomputação em runtime).
/// </summary>
public static class PixelArt64Generator
{
    public const int CanvasSize = 64;
    public const float HeroPixelsPerUnit = 42f; // 64 / 42 = 1.524u canvas -> ~1.38u-1.43u de altura heroica 64-bit!
    public static readonly Vector2 GroundedHeroPivot = new Vector2(0.5f, 0.2986f); // Pés exatamente em GroundTopY (0.455u abaixo do pivô)
    public const float EnemyPixelsPerUnit = 46f; // 64 / 46 = 1.391u canvas -> ~1.04u-1.24u de altura dos robôs 64-bit!
    public static readonly Vector2 GroundedEnemyPivot = new Vector2(0.5f, 0.3306f); // Base exatamente em GroundTopY (0.460u abaixo do pivô)
    public static readonly Vector2 CenteredPivot = new Vector2(0.5f, 0.5f);

    private const int MAT_EMPTY = 0;
    private const int MAT_BOOT = 1;
    private const int MAT_SOCK = 2;
    private const int MAT_JEAN = 3;
    private const int MAT_BELT = 4;
    private const int MAT_BUCKLE = 5;
    private const int MAT_BACK_ARM = 6;
    private const int MAT_SHIRT = 7;
    private const int MAT_TORSO = 8;
    private const int MAT_OVERALL = 9;
    private const int MAT_POCKET = 10;
    private const int MAT_FRONT_ARM = 11;
    private const int MAT_GUN_BODY = 12;
    private const int MAT_GUN_GLOW = 13;
    private const int MAT_HEAD = 14;
    private const int MAT_FACE_DETAIL = 16;
    private const int MAT_HAIR = 17;
    private const int MAT_BOW_LOOP = 18;
    private const int MAT_BOW_KNOT = 19;
    private const int MAT_FX_GLOW = 20;

    private static readonly Dictionary<string, Texture2D> CachedTex = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Sprite> CachedSpr = new Dictionary<string, Sprite>();

    public static Sprite GetBoySprite(int pose, string key)
    {
        if (CachedSpr.TryGetValue(key, out Sprite s) && s != null) return s;
        Texture2D tex = GetBoyTexture(pose, key);
        s = Sprite.Create(tex, new Rect(0, 0, CanvasSize, CanvasSize), GroundedHeroPivot, HeroPixelsPerUnit, 0, SpriteMeshType.FullRect);
        s.name = key;
        CachedSpr[key] = s;
        return s;
    }

    public static Texture2D GetBoyTexture(int pose, string key)
    {
        if (CachedTex.TryGetValue(key, out Texture2D t) && t != null) return t;
        Texture2D prebaked = Resources.Load<Texture2D>("Sprites64/" + key);
        if (prebaked != null && prebaked.width == CanvasSize && prebaked.height == CanvasSize)
        {
            prebaked.filterMode = FilterMode.Point;
            prebaked.wrapMode = TextureWrapMode.Clamp;
            CachedTex[key] = prebaked;
            return prebaked;
        }

        t = BuildBoyTextureInternal(pose, key);
        CachedTex[key] = t;
        TrySavePng(key, t);
        return t;
    }

    public static Sprite GetKittySprite(int pose, string key)
    {
        if (CachedSpr.TryGetValue(key, out Sprite s) && s != null) return s;
        Texture2D tex = GetKittyTexture(pose, key);
        s = Sprite.Create(tex, new Rect(0, 0, CanvasSize, CanvasSize), GroundedHeroPivot, HeroPixelsPerUnit, 0, SpriteMeshType.FullRect);
        s.name = key;
        CachedSpr[key] = s;
        return s;
    }

    public static Texture2D GetKittyTexture(int pose, string key)
    {
        if (CachedTex.TryGetValue(key, out Texture2D t) && t != null) return t;
        Texture2D prebaked = Resources.Load<Texture2D>("Sprites64/" + key);
        if (prebaked != null && prebaked.width == CanvasSize && prebaked.height == CanvasSize)
        {
            prebaked.filterMode = FilterMode.Point;
            prebaked.wrapMode = TextureWrapMode.Clamp;
            CachedTex[key] = prebaked;
            return prebaked;
        }

        t = BuildKittyTextureInternal(pose, key);
        CachedTex[key] = t;
        TrySavePng(key, t);
        return t;
    }

    public static Sprite GetRobotSprite(int type, string key, bool grounded = true)
    {
        if (CachedSpr.TryGetValue(key, out Sprite s) && s != null) return s;
        if (!CachedTex.TryGetValue(key, out Texture2D tex) || tex == null)
        {
            Texture2D prebaked = Resources.Load<Texture2D>("Sprites64/" + key);
            if (prebaked != null && prebaked.width == CanvasSize && prebaked.height == CanvasSize)
            {
                prebaked.filterMode = FilterMode.Point;
                prebaked.wrapMode = TextureWrapMode.Clamp;
                tex = prebaked;
            }
            else
            {
                tex = BuildRobotTextureInternal(type, key, grounded);
                TrySavePng(key, tex);
            }
            CachedTex[key] = tex;
        }

        Vector2 pivot = grounded ? GroundedEnemyPivot : CenteredPivot;
        s = Sprite.Create(tex, new Rect(0, 0, CanvasSize, CanvasSize), pivot, EnemyPixelsPerUnit, 0, SpriteMeshType.FullRect);
        s.name = key;
        CachedSpr[key] = s;
        return s;
    }

    // =========================================================================
    // CONSTRUÇÃO NATIVA 64x64 DO BONECO MENINO (JAQUETA SOLAR + PISTOLA LASER)
    // pose: 0=Idle1, 1=Idle2, 2=Walk1, 3=Walk2, 4=Walk3, 5=Jump, 6=Shoot,
    //       7=StompPrep, 8=StompDive, 9=StompBounce
    // =========================================================================
    private static Texture2D BuildBoyTextureInternal(int pose, string key)
    {
        Color32[,] c = new Color32[CanvasSize, CanvasSize];
        int[,] m = new int[CanvasSize, CanvasSize];

        int bobY = (pose == 1 || pose == 3) ? -1 : ((pose == 5 || pose == 8) ? 2 : 0);

        Color32 jeanBase = new Color32(58, 134, 224, 255);
        Color32 jeanLight = new Color32(108, 182, 250, 255);
        Color32 jeanDark = new Color32(34, 84, 168, 255);

        Color32 bootBase = new Color32(56, 64, 86, 255);
        Color32 bootLight = new Color32(98, 110, 140, 255);
        Color32 bootDark = new Color32(32, 36, 52, 255);

        Color32 jktBase = new Color32(252, 204, 32, 255);
        Color32 jktLight = new Color32(255, 232, 92, 255);
        Color32 jktSpec = new Color32(255, 248, 168, 255);
        Color32 jktDark = new Color32(208, 144, 14, 255);
        Color32 jktDeep = new Color32(164, 104, 6, 255);

        Color32 skinBase = new Color32(255, 208, 164, 255);
        Color32 skinLight = new Color32(255, 228, 198, 255);
        Color32 skinDark = new Color32(220, 160, 116, 255);
        Color32 skinDeep = new Color32(188, 128, 86, 255);

        // 1. Braço Esquerdo (Traseiro) - Conectado continuamente ao ombro
        if (pose == 5 || pose == 7 || pose == 8 || pose == 9)
        {
            PaintCapsule(c, m, 22f, 34f + bobY, 13f, 39f + bobY, 3.6f, 3.2f, MAT_BACK_ARM, jktBase, jktLight, jktDark);
            PaintCapsule(c, m, 13f, 39f + bobY, 13f, 44f + bobY, 3.2f, 3.0f, MAT_BACK_ARM, skinBase, skinLight, skinDark);
        }
        else
        {
            int swing = (pose == 2) ? -3 : ((pose == 4) ? 3 : 0);
            PaintCapsule(c, m, 22f, 34f + bobY, 16f, 26f + bobY + swing, 3.6f, 3.0f, MAT_BACK_ARM, jktBase, jktLight, jktDark);
            PaintEllipse(c, m, 15.5f, 24.5f + bobY + swing, 3.0f, 2.8f, MAT_BACK_ARM, skinBase, skinLight, skinLight, skinDark, skinDeep);
        }

        // 2. Pernas e Pélvis em Jeans 64-bit (com separação nítida entre as pernas)
        float lHipX = 25.0f, rHipX = 36.0f;
        float lAnkleX = 24.5f, rAnkleX = 36.5f;
        int lFootY = 2, rFootY = 2;
        if (pose == 2) { lAnkleX = 19.5f; rAnkleX = 40.5f; lFootY = 5; rFootY = 2; }
        else if (pose == 4) { lAnkleX = 28.5f; rAnkleX = 32.5f; lFootY = 2; rFootY = 5; }
        else if (pose == 5 || pose >= 7) { lAnkleX = 23.0f; rAnkleX = 38.0f; lFootY = 5; rFootY = 5; }

        PaintEllipse(c, m, 30.5f, 22.0f + bobY, 8.2f, 4.0f, MAT_JEAN, jeanBase, jeanLight, jeanLight, jeanDark, jeanDark);
        PaintCapsule(c, m, lHipX, 21f + bobY, lAnkleX, lFootY + 5, 3.5f, 3.2f, MAT_JEAN, jeanBase, jeanLight, jeanDark);
        PaintCapsule(c, m, rHipX, 21f + bobY, rAnkleX, rFootY + 5, 3.5f, 3.2f, MAT_JEAN, jeanBase, jeanLight, jeanDark);

        // 3. Botas Cyber 64-bit com Sola Energética
        PaintRoundBox(c, m, Mathf.RoundToInt(lAnkleX) - 4, lFootY, Mathf.RoundToInt(lAnkleX) + 4, lFootY + 5, 2, MAT_BOOT, bootBase, bootLight, bootDark);
        PaintRoundBox(c, m, Mathf.RoundToInt(rAnkleX) - 4, rFootY, Mathf.RoundToInt(rAnkleX) + 4, rFootY + 5, 2, MAT_BOOT, bootBase, bootLight, bootDark);
        PaintRoundBox(c, m, Mathf.RoundToInt(lAnkleX) - 4, lFootY, Mathf.RoundToInt(lAnkleX) + 4, lFootY + 1, 0, MAT_BOOT, new Color32(30, 210, 255, 255), new Color32(255, 255, 255, 255), new Color32(10, 140, 200, 255));
        PaintRoundBox(c, m, Mathf.RoundToInt(rAnkleX) - 4, rFootY, Mathf.RoundToInt(rAnkleX) + 4, rFootY + 1, 0, MAT_BOOT, new Color32(30, 210, 255, 255), new Color32(255, 255, 255, 255), new Color32(10, 140, 200, 255));

        if (pose == 5 || pose >= 7)
        {
            Color32 cCyan = new Color32(25, 235, 255, 255);
            Color32 cWhite = new Color32(255, 255, 255, 255);
            Color32 cGold = new Color32(255, 210, 45, 255);
            Color32 cDeep = new Color32(0, 160, 220, 255);
            PaintEllipse(c, m, lAnkleX, lFootY - 0.5f, 5.5f, 2.5f, MAT_FX_GLOW, cCyan, cWhite, cWhite, cGold, cDeep);
            PaintEllipse(c, m, rAnkleX, rFootY - 0.5f, 5.5f, 2.5f, MAT_FX_GLOW, cCyan, cWhite, cWhite, cGold, cDeep);
        }

        // 4. Torso Atlético (Jaqueta Solar 64-bit + Cinto + Camisa Interna)
        PaintEllipse(c, m, 30.5f, 31.5f + bobY, 9.8f, 8.2f, MAT_TORSO, jktBase, jktLight, jktSpec, jktDark, jktDeep);
        PaintRoundBox(c, m, 22, 22 + bobY, 39, 25 + bobY, 1, MAT_BELT, new Color32(62, 40, 28, 255), new Color32(92, 62, 44, 255), new Color32(38, 24, 16, 255));
        PaintRoundBox(c, m, 29, 22 + bobY, 33, 25 + bobY, 0, MAT_BUCKLE, new Color32(250, 208, 52, 255), new Color32(255, 245, 150, 255), new Color32(196, 138, 18, 255));
        PaintRoundBox(c, m, 29, 33 + bobY, 32, 39 + bobY, 1, MAT_SHIRT, new Color32(40, 46, 68, 255), new Color32(64, 72, 98, 255), new Color32(24, 28, 44, 255));

        // 5. Braço Direito + Blaster Sci-Fi 64-bit
        int rec = (pose == 6) ? -2 : 0;
        PaintCapsule(c, m, 37f + rec, 33f + bobY, 44f + rec, 31f + bobY, 3.4f, 3.0f, MAT_FRONT_ARM, skinBase, skinLight, skinDark);
        PaintRoundBox(c, m, 43 + rec, 26 + bobY, 47 + rec, 32 + bobY, 1, MAT_GUN_BODY, new Color32(58, 68, 90, 255), new Color32(92, 106, 134, 255), new Color32(34, 40, 56, 255));
        PaintRoundBox(c, m, 43 + rec, 30 + bobY, 59 + rec, 37 + bobY, 2, MAT_GUN_BODY, new Color32(148, 168, 198, 255), new Color32(216, 228, 246, 255), new Color32(84, 98, 126, 255));
        PaintRoundBox(c, m, 46 + rec, 32 + bobY, 57 + rec, 35 + bobY, 1, MAT_GUN_GLOW, new Color32(20, 232, 255, 255), new Color32(255, 255, 255, 255), new Color32(0, 168, 220, 255));
        PaintRoundBox(c, m, 49 + rec, 33 + bobY, 55 + rec, 34 + bobY, 0, MAT_GUN_GLOW, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255));
        if (pose == 6)
        {
            PaintEllipse(c, m, 60.0f, 33.5f + bobY, 3.0f, 4.5f, MAT_FX_GLOW, new Color32(30, 240, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(0, 180, 240, 255), new Color32(0, 140, 200, 255));
        }

        // 6. Cabeça Esculpida e Olhos Anime 64-bit
        PaintEllipse(c, m, 30.5f, 46.5f + bobY, 11.2f, 9.2f, MAT_HEAD, skinBase, skinLight, skinLight, skinDark, skinDeep);

        Color32 white = new Color32(255, 255, 255, 255);
        Color32 pupil = new Color32(24, 32, 56, 255);
        Color32 iris = new Color32(58, 96, 168, 255);

        for (int ey = 43; ey <= 48; ey++)
        {
            for (int ex = 25; ex <= 28; ex++)
                Px(c, m, ex, ey + bobY, MAT_FACE_DETAIL, (ex >= 26) ? pupil : white);
            for (int ex = 34; ex <= 37; ex++)
                Px(c, m, ex, ey + bobY, MAT_FACE_DETAIL, (ex >= 35) ? pupil : white);
        }
        Px(c, m, 27, 44 + bobY, MAT_FACE_DETAIL, iris);
        Px(c, m, 36, 44 + bobY, MAT_FACE_DETAIL, iris);
        Px(c, m, 26, 47 + bobY, MAT_FACE_DETAIL, white);
        Px(c, m, 27, 48 + bobY, MAT_FACE_DETAIL, white);
        Px(c, m, 35, 47 + bobY, MAT_FACE_DETAIL, white);
        Px(c, m, 36, 48 + bobY, MAT_FACE_DETAIL, white);

        Color32 blush = new Color32(250, 132, 164, 255);
        Color32 smile = new Color32(168, 98, 72, 255);
        Px(c, m, 22, 43 + bobY, MAT_FACE_DETAIL, blush);
        Px(c, m, 23, 43 + bobY, MAT_FACE_DETAIL, blush);
        Px(c, m, 38, 43 + bobY, MAT_FACE_DETAIL, blush);
        Px(c, m, 39, 43 + bobY, MAT_FACE_DETAIL, blush);
        Px(c, m, 30, 41 + bobY, MAT_FACE_DETAIL, smile);
        Px(c, m, 31, 41 + bobY, MAT_FACE_DETAIL, smile);
        Px(c, m, 32, 42 + bobY, MAT_FACE_DETAIL, smile);

        // 7. Cabelo Castanho em Camadas Dinâmicas 64-bit
        Color32 hairBase = new Color32(98, 56, 30, 255);
        Color32 hairLight = new Color32(146, 88, 48, 255);
        Color32 hairSpec = new Color32(192, 128, 76, 255);
        Color32 hairDark = new Color32(64, 34, 18, 255);
        Color32 hairDeep = new Color32(40, 20, 10, 255);

        PaintEllipse(c, m, 30.5f, 54.0f + bobY, 13.0f, 6.2f, MAT_HAIR, hairBase, hairLight, hairSpec, hairDark, hairDeep);
        PaintCapsule(c, m, 20f, 53f + bobY, 19f, 45f + bobY, 2.8f, 1.8f, MAT_HAIR, hairDark, hairBase, hairDeep);
        PaintCapsule(c, m, 41f, 53f + bobY, 42f, 46f + bobY, 2.6f, 1.6f, MAT_HAIR, hairDark, hairBase, hairDeep);
        PaintCapsule(c, m, 24f, 54f + bobY, 26f, 50f + bobY, 3.2f, 2.0f, MAT_HAIR, hairBase, hairLight, hairDark);
        PaintCapsule(c, m, 31f, 55f + bobY, 32f, 50f + bobY, 3.4f, 2.0f, MAT_HAIR, hairBase, hairSpec, hairDark);
        PaintCapsule(c, m, 37f, 54f + bobY, 38f, 50f + bobY, 2.8f, 1.8f, MAT_HAIR, hairDark, hairBase, hairDeep);

        return FinalizeTexture64(c, m, key, alignBottom: true);
    }

    // =========================================================================
    // CONSTRUÇÃO NATIVA 64x64 DA BONECA KITTY (LAÇO RUBI + JARDINEIRA JEANS)
    // pose: 0=Idle1, 1=Idle2(Wink), 2=Walk1, 3=Walk2, 4=Walk3, 5=Jump,
    //       6=Scratch1, 7=Scratch2, 8=Scratch3
    // =========================================================================
    private static Texture2D BuildKittyTextureInternal(int pose, string key)
    {
        Color32[,] c = new Color32[CanvasSize, CanvasSize];
        int[,] m = new int[CanvasSize, CanvasSize];

        int bobY = (pose == 1 || pose == 3) ? -1 : ((pose == 5) ? 2 : 0);

        Color32 furPure = new Color32(252, 253, 255, 255);
        Color32 furLight = new Color32(255, 252, 250, 255);
        Color32 furShade = new Color32(214, 222, 240, 255);
        Color32 furDeep = new Color32(178, 188, 214, 255);

        Color32 jeanBase = new Color32(64, 144, 234, 255);
        Color32 jeanLight = new Color32(118, 192, 252, 255);
        Color32 jeanDark = new Color32(38, 92, 178, 255);

        Color32 pinkBase = new Color32(250, 116, 172, 255);
        Color32 pinkLight = new Color32(255, 172, 210, 255);
        Color32 pinkDark = new Color32(202, 70, 128, 255);

        float lAnkleX = 24.5f, rAnkleX = 37.5f;
        int lFootY = 2, rFootY = 2;
        if (pose == 2) { lAnkleX = 20.5f; rAnkleX = 40.5f; lFootY = 5; rFootY = 2; }
        else if (pose == 4) { lAnkleX = 28.0f; rAnkleX = 34.0f; lFootY = 2; rFootY = 5; }
        else if (pose >= 6) { lAnkleX = 21.0f; rAnkleX = 41.0f; }

        // 1. Pernas + Meias Brancas + Botinhas
        PaintCapsule(c, m, 25.5f, 20f + bobY, lAnkleX, lFootY + 4, 3.6f, 3.3f, MAT_OVERALL, jeanBase, jeanLight, jeanDark);
        PaintCapsule(c, m, 36.5f, 20f + bobY, rAnkleX, rFootY + 4, 3.6f, 3.3f, MAT_OVERALL, jeanBase, jeanLight, jeanDark);
        PaintRoundBox(c, m, Mathf.RoundToInt(lAnkleX) - 3, lFootY + 4, Mathf.RoundToInt(lAnkleX) + 3, lFootY + 7, 1, MAT_SOCK, furPure, furLight, furShade);
        PaintRoundBox(c, m, Mathf.RoundToInt(rAnkleX) - 3, rFootY + 4, Mathf.RoundToInt(rAnkleX) + 3, rFootY + 7, 1, MAT_SOCK, furPure, furLight, furShade);
        PaintRoundBox(c, m, Mathf.RoundToInt(lAnkleX) - 4, lFootY, Mathf.RoundToInt(lAnkleX) + 4, lFootY + 4, 2, MAT_BOOT, new Color32(52, 56, 78, 255), new Color32(96, 104, 134, 255), new Color32(30, 34, 48, 255));
        PaintRoundBox(c, m, Mathf.RoundToInt(rAnkleX) - 4, rFootY, Mathf.RoundToInt(rAnkleX) + 4, rFootY + 4, 2, MAT_BOOT, new Color32(52, 56, 78, 255), new Color32(96, 104, 134, 255), new Color32(30, 34, 48, 255));

        // 2. Patinhas + Blusinha Rosa + Jardineira Jeans 64-bit
        PaintCapsule(c, m, 22f, 31f + bobY, 16f, 26f + bobY, 3.5f, 3.2f, MAT_BACK_ARM, furPure, furLight, furShade);
        PaintEllipse(c, m, 31.0f, 29.5f + bobY, 9.8f, 7.5f, MAT_SHIRT, pinkBase, pinkLight, pinkLight, pinkDark, pinkDark);
        PaintRoundBox(c, m, 22, 18 + bobY, 40, 30 + bobY, 3, MAT_OVERALL, jeanBase, jeanLight, jeanDark);
        PaintRoundBox(c, m, 23, 29 + bobY, 26, 35 + bobY, 1, MAT_OVERALL, jeanBase, jeanLight, jeanDark);
        PaintRoundBox(c, m, 36, 29 + bobY, 39, 35 + bobY, 1, MAT_OVERALL, jeanBase, jeanLight, jeanDark);
        PaintRoundBox(c, m, 24, 29 + bobY, 25, 30 + bobY, 0, MAT_BUCKLE, new Color32(255, 216, 42, 255), new Color32(255, 255, 255, 255), new Color32(204, 142, 12, 255));
        PaintRoundBox(c, m, 37, 29 + bobY, 38, 30 + bobY, 0, MAT_BUCKLE, new Color32(255, 216, 42, 255), new Color32(255, 255, 255, 255), new Color32(204, 142, 12, 255));
        PaintRoundBox(c, m, 28, 21 + bobY, 34, 26 + bobY, 1, MAT_POCKET, jeanLight, new Color32(168, 220, 255, 255), jeanBase);

        if (pose >= 6)
        {
            float reachX = (pose == 6) ? 49f : ((pose == 7) ? 53f : 48f);
            PaintCapsule(c, m, 39f, 31f + bobY, reachX, 30f + bobY, 3.6f, 3.2f, MAT_FRONT_ARM, furPure, furLight, furShade);
            int yMin = (pose == 6) ? 22 : ((pose == 7) ? 14 : 16);
            int yMax = (pose == 6) ? 48 : ((pose == 7) ? 52 : 44);
            for (int sy = yMin; sy <= yMax; sy++)
            {
                float ny = (sy - 33f) / 19f;
                int ax = 55 + Mathf.RoundToInt((1f - ny * ny) * 6f);
                Px(c, m, ax - 2, sy, MAT_FX_GLOW, new Color32(255, 50, 120, 255));
                Px(c, m, ax - 1, sy, MAT_FX_GLOW, new Color32(255, 215, 55, 255));
                Px(c, m, ax, sy, MAT_FX_GLOW, new Color32(175, 255, 255, 255));
                Px(c, m, ax + 1, sy, MAT_FX_GLOW, new Color32(255, 255, 255, 255));
            }
        }
        else
        {
            PaintCapsule(c, m, 40f, 31f + bobY, 46f, 26f + bobY, 3.5f, 3.2f, MAT_FRONT_ARM, furPure, furLight, furShade);
        }

        // 3. Cabeça + Orelhas Unidas na Mesma Camada (MAT_HEAD) sem linha preta cortando as orelhas
        PaintCapsule(c, m, 21f, 49f + bobY, 18f, 58f + bobY, 5.8f, 3.6f, MAT_HEAD, furPure, furLight, furShade);
        PaintCapsule(c, m, 41f, 49f + bobY, 44f, 58f + bobY, 5.8f, 3.6f, MAT_HEAD, furPure, furLight, furShade);
        PaintEllipse(c, m, 31.0f, 44.5f + bobY, 15.8f, 10.8f, MAT_HEAD, furPure, furLight, furLight, furShade, furDeep);

        PaintCapsule(c, m, 20f, 52f + bobY, 19f, 56f + bobY, 2.2f, 1.2f, MAT_FACE_DETAIL, pinkLight, pinkLight, pinkBase);
        PaintCapsule(c, m, 42f, 53f + bobY, 43f, 56f + bobY, 2.0f, 1.2f, MAT_FACE_DETAIL, pinkLight, pinkLight, pinkBase);

        Color32 eyeDark = new Color32(20, 22, 36, 255);
        Color32 white = new Color32(255, 255, 255, 255);
        if (pose == 1)
        {
            for (int ex = 22; ex <= 26; ex++) Px(c, m, ex, 45 + bobY, MAT_FACE_DETAIL, eyeDark);
        }
        else
        {
            for (int ey = 42; ey <= 47; ey++)
                for (int ex = 23; ex <= 25; ex++)
                    Px(c, m, ex, ey + bobY, MAT_FACE_DETAIL, eyeDark);
            Px(c, m, 24, 46 + bobY, MAT_FACE_DETAIL, white);
        }

        for (int ey = 42; ey <= 47; ey++)
            for (int ex = 37; ex <= 39; ex++)
                Px(c, m, ex, ey + bobY, MAT_FACE_DETAIL, eyeDark);
        Px(c, m, 38, 46 + bobY, MAT_FACE_DETAIL, white);

        for (int ny = 41; ny <= 42; ny++)
            for (int nx = 30; nx <= 32; nx++)
                Px(c, m, nx, ny + bobY, MAT_FACE_DETAIL, (ny == 42) ? new Color32(255, 220, 50, 255) : new Color32(214, 152, 16, 255));

        PaintEllipse(c, m, 20.5f, 40.5f + bobY, 2.4f, 1.4f, MAT_FACE_DETAIL, pinkLight, pinkLight, pinkLight, pinkBase, pinkBase);
        PaintEllipse(c, m, 41.5f, 40.5f + bobY, 2.4f, 1.4f, MAT_FACE_DETAIL, pinkLight, pinkLight, pinkLight, pinkBase, pinkBase);

        for (int wy = 41; wy <= 47; wy += 3)
        {
            Px(c, m, 14, wy + bobY, MAT_FACE_DETAIL, eyeDark);
            Px(c, m, 15, wy + bobY, MAT_FACE_DETAIL, eyeDark);
            Px(c, m, 16, wy + bobY, MAT_FACE_DETAIL, eyeDark);
            Px(c, m, 46, wy + bobY, MAT_FACE_DETAIL, eyeDark);
            Px(c, m, 47, wy + bobY, MAT_FACE_DETAIL, eyeDark);
            Px(c, m, 48, wy + bobY, MAT_FACE_DETAIL, eyeDark);
        }

        // 4. Laço Rubi Acetinado 64-bit na Base da Orelha Direita
        Color32 bowBase = new Color32(242, 38, 90, 255);
        Color32 bowLight = new Color32(255, 118, 158, 255);
        Color32 bowSpec = new Color32(255, 188, 212, 255);
        Color32 bowDark = new Color32(186, 18, 64, 255);
        Color32 bowDeep = new Color32(138, 10, 44, 255);

        PaintEllipse(c, m, 38.0f, 51.5f + bobY, 4.0f, 3.2f, MAT_BOW_LOOP, bowBase, bowLight, bowSpec, bowDark, bowDeep);
        PaintEllipse(c, m, 45.5f, 49.5f + bobY, 4.0f, 3.2f, MAT_BOW_LOOP, bowBase, bowLight, bowSpec, bowDark, bowDeep);
        PaintEllipse(c, m, 41.8f, 50.5f + bobY, 2.3f, 2.3f, MAT_BOW_KNOT, bowDark, bowLight, bowSpec, bowDeep, bowDeep);

        return FinalizeTexture64(c, m, key, alignBottom: true);
    }

    // =========================================================================
    // CONSTRUÇÃO NATIVA 64x64 DOS INIMIGOS ROBÔS (MECHA-CLANK 64-BIT)
    // type: 0=Walk1, 1=Walk2, 2=Jumper1, 3=Jumper2, 4=TurretIdle, 5=TurretCharge,
    //       6=Drone1, 7=Drone2, 8=Dead
    // =========================================================================
    private static Texture2D BuildRobotTextureInternal(int type, string key, bool grounded)
    {
        Color32[,] c = new Color32[CanvasSize, CanvasSize];
        int[,] m = new int[CanvasSize, CanvasSize];

        Color32 chBase = new Color32(168, 186, 212, 255);
        Color32 chLight = new Color32(224, 236, 250, 255);
        Color32 chSpec = new Color32(248, 252, 255, 255);
        Color32 chDark = new Color32(98, 114, 142, 255);
        Color32 chDeep = new Color32(52, 62, 84, 255);
        Color32 white = new Color32(255, 255, 255, 255);

        if (type >= 0 && type <= 3)
        {
            bool isJumper = (type == 2 || type == 3);
            int bobY = (type == 1 || type == 3) ? -2 : 0;

            if (isJumper)
            {
                PaintRoundBox(c, m, 20, 1, 43, 5, 2, MAT_BOOT, chDark, chBase, chDeep);
                int step = (type == 3) ? 3 : 4;
                for (int sy = 6; sy <= 18 + bobY; sy += step)
                    PaintRoundBox(c, m, 23, sy, 40, sy + 2, 1, MAT_BUCKLE, new Color32(255, 208, 36, 255), new Color32(255, 245, 150, 255), new Color32(192, 132, 12, 255));
            }
            else
            {
                float lFootX = (type == 1) ? 20f : 23f;
                float rFootX = (type == 1) ? 43f : 40f;
                PaintCapsule(c, m, 25f, 18f + bobY, lFootX, 4f, 2.5f, 2.2f, MAT_JEAN, chDark, chBase, chDeep);
                PaintCapsule(c, m, 38f, 18f + bobY, rFootX, 4f, 2.5f, 2.2f, MAT_JEAN, chDark, chBase, chDeep);
                PaintRoundBox(c, m, Mathf.RoundToInt(lFootX) - 4, 1, Mathf.RoundToInt(lFootX) + 4, 5, 2, MAT_BOOT, chBase, chLight, chDark);
                PaintRoundBox(c, m, Mathf.RoundToInt(rFootX) - 4, 1, Mathf.RoundToInt(rFootX) + 4, 5, 2, MAT_BOOT, chBase, chLight, chDark);
            }

            PaintCapsule(c, m, 20f, 29f + bobY, 13f, 21f + bobY, 2.6f, 2.2f, MAT_BACK_ARM, chDark, chBase, chDeep);
            PaintCapsule(c, m, 43f, 29f + bobY, 50f, 21f + bobY, 2.6f, 2.2f, MAT_BACK_ARM, chDark, chBase, chDeep);

            PaintRoundBox(c, m, 21, 17 + bobY, 42, 31 + bobY, 3, MAT_TORSO, chBase, chLight, chDark);
            Color32 coreC = isJumper ? new Color32(255, 205, 35, 255) : new Color32(20, 225, 255, 255);
            Color32 coreL = isJumper ? new Color32(255, 240, 140, 255) : new Color32(165, 248, 255, 255);
            PaintEllipse(c, m, 31.5f, 24.0f + bobY, 4.2f, 3.2f, MAT_GUN_GLOW, coreC, coreL, white, coreC, chDark);

            PaintRoundBox(c, m, 15, 32 + bobY, 48, 50 + bobY, 6, MAT_HEAD, chBase, chLight, chDark);
            PaintRoundBox(c, m, 18, 35 + bobY, 45, 47 + bobY, 3, MAT_SHIRT, new Color32(20, 26, 40, 255), chDeep, new Color32(10, 12, 20, 255));

            Color32 eyeC = isJumper ? new Color32(255, 204, 32, 255) : new Color32(36, 236, 108, 255);
            Color32 eyeL = isJumper ? new Color32(255, 240, 140, 255) : new Color32(150, 255, 195, 255);
            PaintEllipse(c, m, 24.5f, 41.0f + bobY, 4.8f, 4.0f, MAT_GUN_GLOW, eyeC, eyeL, white, eyeC, chDeep);
            PaintEllipse(c, m, 38.5f, 41.0f + bobY, 4.8f, 4.0f, MAT_GUN_GLOW, eyeC, eyeL, white, eyeC, chDeep);

            PaintRoundBox(c, m, 30, 50 + bobY, 33, 55 + bobY, 0, MAT_BELT, chDark, chBase, chDeep);
            PaintEllipse(c, m, 31.5f, 58.0f + bobY, 3.8f, 3.0f, MAT_BOW_LOOP, new Color32(250, 48, 62, 255), new Color32(255, 140, 150, 255), white, new Color32(180, 20, 36, 255), chDeep);
            return FinalizeTexture64(c, m, key, alignBottom: true);
        }
        else if (type == 4 || type == 5)
        {
            bool charge = (type == 5);
            PaintRoundBox(c, m, 11, 1, 52, 7, 2, MAT_BOOT, chDark, chBase, chDeep);
            PaintRoundBox(c, m, 16, 7, 47, 21, 3, MAT_TORSO, chBase, chLight, chDark);
            Color32 coreC = charge ? new Color32(255, 215, 40, 255) : new Color32(250, 52, 68, 255);
            Color32 coreL = charge ? new Color32(255, 248, 160, 255) : new Color32(255, 140, 150, 255);
            PaintEllipse(c, m, 31.5f, 14.0f, 6.5f, 4.0f, MAT_GUN_GLOW, coreC, coreL, white, new Color32(180, 20, 38, 255), chDeep);
            PaintRoundBox(c, m, 36, 28, 61, 38, 2, MAT_GUN_BODY, chBase, chLight, chDark);
            PaintRoundBox(c, m, 55, 30, 60, 36, 1, MAT_GUN_GLOW, coreC, coreL, new Color32(190, 25, 40, 255));
            PaintEllipse(c, m, 29.0f, 33.0f, 13.5f, 10.5f, MAT_HEAD, chBase, chLight, chSpec, chDark, chDeep);
            Color32 visorC = charge ? new Color32(255, 195, 35, 255) : new Color32(36, 236, 108, 255);
            Color32 visorL = charge ? new Color32(255, 245, 155, 255) : new Color32(150, 255, 195, 255);
            PaintRoundBox(c, m, 20, 29, 36, 37, 2, MAT_GUN_GLOW, visorC, visorL, new Color32(18, 160, 72, 255));
            return FinalizeTexture64(c, m, key, alignBottom: true);
        }
        else if (type == 6 || type == 7)
        {
            bool pulse = (type == 7);
            Color32 spikeC = pulse ? new Color32(255, 210, 45, 255) : new Color32(255, 52, 68, 255);
            for (int sx = 19; sx <= 43; sx += 12)
                PaintCapsule(c, m, sx, 42f, sx, 53f, 3.0f, 1.2f, MAT_BOW_LOOP, spikeC, new Color32(255, 235, 120, 255), new Color32(180, 20, 40, 255));
            PaintEllipse(c, m, 31.5f, 34.0f, 19.5f, 9.0f, MAT_HEAD, chBase, chLight, chSpec, chDark, chDeep);
            PaintEllipse(c, m, 31.5f, 34.0f, 9.0f, 4.2f, MAT_GUN_GLOW, new Color32(255, 48, 62, 255), new Color32(255, 160, 120, 255), white, new Color32(255, 205, 50, 255), new Color32(175, 18, 36, 255));
            Color32 thrC = pulse ? new Color32(255, 215, 50, 255) : new Color32(20, 230, 255, 255);
            PaintEllipse(c, m, 21.0f, 22.0f, 3.8f, 4.2f, MAT_GUN_GLOW, thrC, white, white, thrC, chDeep);
            PaintEllipse(c, m, 42.0f, 22.0f, 3.8f, 4.2f, MAT_GUN_GLOW, thrC, white, white, thrC, chDeep);
            return FinalizeTexture64(c, m, key, alignBottom: false);
        }
        else
        {
            // Fallbacks distintos por arquétipo derrotado (8=Walker, 9=Jumper, 10=Turret, 11=Drone)
            PaintRoundBox(c, m, 12, 1, 52, 7, 2, MAT_BOOT, chDark, chBase, chDeep);
            if (type == 9)
            {
                PaintRoundBox(c, m, 22, 6, 41, 10, 1, MAT_BUCKLE, new Color32(142, 102, 24, 255), new Color32(180, 134, 34, 255), new Color32(92, 64, 16, 255));
                PaintEllipse(c, m, 31.5f, 15.0f, 13.0f, 7.5f, MAT_HEAD, chDark, chBase, chLight, chDeep, chDeep);
            }
            else if (type == 10)
            {
                PaintRoundBox(c, m, 34, 6, 58, 12, 2, MAT_GUN_BODY, chDark, chBase, chDeep);
                PaintEllipse(c, m, 27.0f, 16.0f, 11.0f, 8.0f, MAT_HEAD, chDark, chBase, chLight, chDeep, chDeep);
            }
            else if (type == 11)
            {
                PaintEllipse(c, m, 31.5f, 9.0f, 18.0f, 6.5f, MAT_HEAD, chDark, chBase, chLight, chDeep, chDeep);
                for (int sx = 20; sx <= 42; sx += 11)
                    PaintCapsule(c, m, sx, 13f, sx + 1, 18f, 2.2f, 1.0f, MAT_BOW_LOOP, new Color32(118, 32, 42, 255), new Color32(150, 48, 56, 255), new Color32(72, 18, 28, 255));
            }
            else
            {
                PaintEllipse(c, m, 26.0f, 13.0f, 12.0f, 7.0f, MAT_HEAD, chDark, chBase, chLight, chDeep, chDeep);
                PaintEllipse(c, m, 41.0f, 10.5f, 8.0f, 5.5f, MAT_TORSO, chDark, chBase, chLight, chDeep, chDeep);
            }
            return FinalizeTexture64(c, m, key, alignBottom: true);
        }
    }

    private static void PaintCapsule(Color32[,] col, int[,] mat, float x0, float y0, float x1, float y1, float r0, float r1, int matId, Color32 cBase, Color32 cLight, Color32 cDark)
    {
        float minX = Mathf.Min(x0, x1) - Mathf.Max(r0, r1) - 1f;
        float maxX = Mathf.Max(x0, x1) + Mathf.Max(r0, r1) + 1f;
        float minY = Mathf.Min(y0, y1) - Mathf.Max(r0, r1) - 1f;
        float maxY = Mathf.Max(y0, y1) + Mathf.Max(r0, r1) + 1f;

        float dx = x1 - x0, dy = y1 - y0;
        float len2 = dx * dx + dy * dy;

        for (int y = Mathf.Max(1, (int)minY); y <= Mathf.Min(62, (int)maxY); y++)
        {
            for (int x = Mathf.Max(1, (int)minX); x <= Mathf.Min(62, (int)maxX); x++)
            {
                float t = len2 > 0.001f ? Mathf.Clamp01(((x - x0) * dx + (y - y0) * dy) / len2) : 0f;
                float px = x0 + t * dx;
                float py = y0 + t * dy;
                float rad = r0 + t * (r1 - r0);

                float dist = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                if (dist <= rad)
                {
                    float nx = (x - px) / Mathf.Max(1f, rad);
                    float ny = (y - py) / Mathf.Max(1f, rad);
                    float light = -nx * 0.6f + ny * 0.5f;
                    Color32 c = cBase;
                    if (light > 0.25f) c = cLight;
                    else if (light < -0.25f) c = cDark;
                    col[x, y] = c;
                    mat[x, y] = matId;
                }
            }
        }
    }

    private static void PaintEllipse(Color32[,] col, int[,] mat, float cx, float cy, float rx, float ry, int matId, Color32 cBase, Color32 cLight, Color32 cSpec, Color32 cDark, Color32 cDeep)
    {
        for (int y = Mathf.Max(1, (int)(cy - ry - 1f)); y <= Mathf.Min(62, (int)(cy + ry + 1f)); y++)
        {
            for (int x = Mathf.Max(1, (int)(cx - rx - 1f)); x <= Mathf.Min(62, (int)(cx + rx + 1f)); x++)
            {
                float nx = (x - cx) / rx;
                float ny = (y - cy) / ry;
                if (nx * nx + ny * ny <= 1.0f)
                {
                    float l = -nx * 0.52f + ny * 0.68f;
                    Color32 c = cBase;
                    if (l > 0.52f) c = cSpec;
                    else if (l > 0.16f) c = cLight;
                    else if (l < -0.45f) c = cDeep;
                    else if (l < -0.16f) c = cDark;
                    col[x, y] = c;
                    mat[x, y] = matId;
                }
            }
        }
    }

    private static void PaintRoundBox(Color32[,] col, int[,] mat, int x0, int y0, int x1, int y1, int rad, int matId, Color32 cBase, Color32 cLight, Color32 cDark)
    {
        for (int y = Mathf.Max(1, y0); y <= Mathf.Min(62, y1); y++)
        {
            for (int x = Mathf.Max(1, x0); x <= Mathf.Min(62, x1); x++)
            {
                int dx = 0, dy = 0;
                if (x < x0 + rad) dx = (x0 + rad) - x;
                else if (x > x1 - rad) dx = x - (x1 - rad);
                if (y < y0 + rad) dy = (y0 + rad) - y;
                else if (y > y1 - rad) dy = y - (y1 - rad);
                if (rad > 0 && dx * dx + dy * dy > rad * rad) continue;

                float nx = (x - x0) / (float)Mathf.Max(1, x1 - x0) * 2f - 1f;
                float ny = (y - y0) / (float)Mathf.Max(1, y1 - y0) * 2f - 1f;
                float l = -nx * 0.5f + ny * 0.6f;
                Color32 c = cBase;
                if (l > 0.28f) c = cLight;
                else if (l < -0.25f) c = cDark;
                col[x, y] = c;
                mat[x, y] = matId;
            }
        }
    }

    private static void Px(Color32[,] col, int[,] mat, int x, int y, int matId, Color32 c)
    {
        if (x < 0 || x >= CanvasSize || y < 0 || y >= CanvasSize) return;
        col[x, y] = c;
        mat[x, y] = matId;
    }

    private static Texture2D FinalizeTexture64(Color32[,] col, int[,] mat, string name, bool alignBottom)
    {
        if (alignBottom)
        {
            int minY = CanvasSize;
            for (int y = 0; y < CanvasSize; y++)
                for (int x = 0; x < CanvasSize; x++)
                    if (mat[x, y] != MAT_EMPTY && y < minY) minY = y;

            if (minY > 1 && minY < CanvasSize)
            {
                int shift = minY - 1;
                Color32[,] nCol = new Color32[CanvasSize, CanvasSize];
                int[,] nMat = new int[CanvasSize, CanvasSize];
                for (int y = shift; y < CanvasSize; y++)
                {
                    for (int x = 0; x < CanvasSize; x++)
                    {
                        nCol[x, y - shift] = col[x, y];
                        nMat[x, y - shift] = mat[x, y];
                    }
                }
                col = nCol;
                mat = nMat;
            }
        }

        Color32[] outPixels = new Color32[CanvasSize * CanvasSize];
        int[] dx4 = { -1, 1, 0, 0 };
        int[] dy4 = { 0, 0, -1, 1 };

        for (int y = 0; y < CanvasSize; y++)
        {
            float vNorm = y / 63f;
            for (int x = 0; x < CanvasSize; x++)
            {
                int m = mat[x, y];
                if (m == MAT_EMPTY)
                {
                    bool touchesBody = false;
                    Color32 brightest = new Color32(30, 34, 52, 255);
                    float bestLum = -1f;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx4[d], ny = y + dy4[d];
                        if (nx >= 0 && nx < CanvasSize && ny >= 0 && ny < CanvasSize && mat[nx, ny] != MAT_EMPTY && mat[nx, ny] != MAT_FX_GLOW)
                        {
                            touchesBody = true;
                            Color32 nc = col[nx, ny];
                            float lum = nc.r * 0.299f + nc.g * 0.587f + nc.b * 0.114f;
                            if (lum > bestLum) { bestLum = lum; brightest = nc; }
                        }
                    }

                    if (touchesBody)
                    {
                        float light = 0.75f + 0.40f * vNorm;
                        byte r = (byte)Mathf.Clamp((int)((16 + brightest.r * 0.22f) * light), 0, 255);
                        byte g = (byte)Mathf.Clamp((int)((18 + brightest.g * 0.22f) * light), 0, 255);
                        byte b = (byte)Mathf.Clamp((int)((30 + brightest.b * 0.26f) * light), 0, 255);
                        outPixels[y * CanvasSize + x] = new Color32(r, g, b, 255);
                    }
                    else
                    {
                        outPixels[y * CanvasSize + x] = new Color32(0, 0, 0, 0);
                    }
                    continue;
                }

                Color32 baseC = col[x, y];
                if (m == MAT_FACE_DETAIL || m == MAT_FX_GLOW || m == MAT_GUN_GLOW)
                {
                    outPixels[y * CanvasSize + x] = baseC;
                    continue;
                }

                int mUp = (y + 1 < CanvasSize) ? mat[x, y + 1] : MAT_EMPTY;
                int mDn = (y - 1 >= 0) ? mat[x, y - 1] : MAT_EMPTY;
                int mLf = (x - 1 >= 0) ? mat[x - 1, y] : MAT_EMPTY;
                int mRt = (x + 1 < CanvasSize) ? mat[x + 1, y] : MAT_EMPTY;

                float delta = 0f;
                if (mUp != m && mUp != MAT_FACE_DETAIL) delta += 0.10f;
                if (mLf != m && mLf != MAT_FACE_DETAIL) delta += 0.07f;
                if (mDn != m && mDn != MAT_FACE_DETAIL) delta -= 0.12f;
                if (mRt != m && mRt != MAT_FACE_DETAIL) delta -= 0.08f;

                if (mUp > m && mUp != MAT_FACE_DETAIL)
                {
                    delta -= 0.22f;
                }
                if (mLf > m && mLf != MAT_FACE_DETAIL && (mLf == MAT_FRONT_ARM || mLf == MAT_BOW_LOOP || mLf == MAT_BOW_KNOT))
                {
                    delta -= 0.16f;
                }

                delta += (PixelArtBuilder.Hash01(x, y, 5151) - 0.5f) * 0.045f;

                float rf = baseC.r / 255f, gf = baseC.g / 255f, bf = baseC.b / 255f;
                if (delta > 0f)
                {
                    float t = Mathf.Clamp01(delta * 0.85f);
                    rf += (1.00f - rf) * t;
                    gf += (0.98f - gf) * t;
                    bf += (0.90f - bf) * t;
                }
                else if (delta < 0f)
                {
                    float t = Mathf.Clamp01(-delta);
                    float sr = rf * 0.36f, sg = gf * 0.32f, sb = Mathf.Min(1f, bf * 0.52f + 0.04f);
                    rf += (sr - rf) * t;
                    gf += (sg - gf) * t;
                    bf += (sb - bf) * t;
                }

                outPixels[y * CanvasSize + x] = new Color32(
                    (byte)Mathf.Clamp((int)(rf * 255f), 0, 255),
                    (byte)Mathf.Clamp((int)(gf * 255f), 0, 255),
                    (byte)Mathf.Clamp((int)(bf * 255f), 0, 255),
                    255
                );
            }
        }

        Texture2D tex = new Texture2D(CanvasSize, CanvasSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = name
        };
        tex.SetPixels32(outPixels);
        tex.Apply();
        return tex;
    }

    private static void TrySavePng(string key, Texture2D tex)
    {
#if UNITY_EDITOR
        try
        {
            string dir = Path.Combine(Application.dataPath, "Resources", "Sprites64");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, key + ".png");
            if (!File.Exists(filePath))
            {
                byte[] bytes = tex.EncodeToPNG();
                if (bytes != null && bytes.Length > 0) File.WriteAllBytes(filePath, bytes);
            }
        }
        catch { }
#endif
    }
}
