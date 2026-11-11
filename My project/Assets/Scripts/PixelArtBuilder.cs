using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Motor central de Pixel Art 64-bit HD (64x64 @ 64 PPU):
///   - Converte grades base 32x32 e texturas procedurais em sprites 64-bit HD mantendo exatamente 1.0 unidade de mundo.
///   - Aplica suavização diagonal Scale2x, iluminação direcional multi-tom (bevel solar topo-esquerda +
///     oclusão de ambiente base-direita), contornos seletivos coloridos (Sel-Out), micro-textura de materiais
///     (tecido, metal escovado, pelagem, pedra, cristal) e brilho especular (catchlight) nos olhos.
///   - Faz cache estático O(1) em memória e pré-gera/carrega PNGs lossless em Assets/Resources/Sprites64/
///     para eliminar recomputação de CPU durante o gameplay.
/// </summary>
public static class PixelArtBuilder
{
    public const int UpscaleFactor = 2; // 32x32 -> 64x64 (64-bit HD Pixel Art)

    private static readonly Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

    public static void ClearCache()
    {
        CachedTextures.Clear();
        CachedSprites.Clear();
    }

    /// <summary>
    /// Desloca as linhas da grade para baixo de modo que a última linha contendo pixels opacos
    /// fique exatamente na base (height - 1), evitando que personagens, robôs ou totens "flutuem".
    /// </summary>
    public static string[] AlignGridToBottom(string[] grid, int width = 32, int height = 32)
    {
        if (grid == null || grid.Length == 0) return grid;

        int lastOpaqueRow = -1;
        int maxRow = Mathf.Min(height, grid.Length);
        for (int r = 0; r < maxRow; r++)
        {
            string line = grid[r];
            if (string.IsNullOrEmpty(line)) continue;
            for (int x = 0; x < line.Length; x++)
            {
                if (line[x] != '.' && line[x] != ' ')
                {
                    lastOpaqueRow = r;
                    break;
                }
            }
        }

        if (lastOpaqueRow < 0 || lastOpaqueRow == height - 1)
        {
            return grid;
        }

        int shiftDown = (height - 1) - lastOpaqueRow;
        string emptyLine = new string('.', width);
        string[] shifted = new string[height];
        for (int r = 0; r < height; r++)
        {
            int srcRow = r - shiftDown;
            shifted[r] = (srcRow >= 0 && srcRow < grid.Length && !string.IsNullOrEmpty(grid[srcRow]))
                ? grid[srcRow]
                : emptyLine;
        }
        return shifted;
    }

    public static Texture2D CreateTextureFromGrid(string key, string[] grid, System.Func<char, Color> palette, int width = 32, int height = 32, bool alignBottom = false)
    {
        string baseKey = alignBottom && !string.IsNullOrEmpty(key) ? (key + "_Grounded") : key;
        string cacheKey = !string.IsNullOrEmpty(baseKey) ? (baseKey + "_64bit") : baseKey;

        if (!string.IsNullOrEmpty(cacheKey) && CachedTextures.TryGetValue(cacheKey, out Texture2D existing) && existing != null)
        {
            return existing;
        }

        int outW = width * UpscaleFactor;
        int outH = height * UpscaleFactor;

        // 1. Tenta carregar PNG pré-gerado de Resources/Sprites64/ para evitar custo de CPU em runtime
        if (!string.IsNullOrEmpty(cacheKey))
        {
            Texture2D prebaked = Resources.Load<Texture2D>("Sprites64/" + cacheKey);
            if (prebaked != null && prebaked.width == outW && prebaked.height == outH)
            {
                prebaked.filterMode = FilterMode.Point;
                prebaked.wrapMode = TextureWrapMode.Clamp;
                CachedTextures[cacheKey] = prebaked;
                return prebaked;
            }
        }

        string[] effectiveGrid = alignBottom ? AlignGridToBottom(grid, width, height) : grid;

        // 2. Constrói matriz base (width x height) de caracteres e cores (y=0 na base, y=height-1 no topo)
        char[,] charMap = new char[width, height];
        Color[,] baseCol = new Color[width, height];

        for (int row = 0; row < height; row++)
        {
            string line = (effectiveGrid != null && row < effectiveGrid.Length) ? effectiveGrid[row] : string.Empty;
            int y = height - 1 - row;
            for (int x = 0; x < width; x++)
            {
                char c = x < line.Length ? line[x] : '.';
                charMap[x, y] = c;
                baseCol[x, y] = palette(c);
            }
        }

        // 3. Passo 1 (64x64): Expansão Scale2x / HQ-Pixel com suavização de quinas diagonais
        char[,] hiChar = new char[outW, outH];
        Color[,] hiCol = new Color[outW, outH];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char P = charMap[x, y];
                char U = GetChar(charMap, x, y + 1, width, height);
                char D = GetChar(charMap, x, y - 1, width, height);
                char L = GetChar(charMap, x - 1, y, width, height);
                char R = GetChar(charMap, x + 1, y, width, height);

                // Scale2x: suaviza diagonais serrilhadas preservando linhas horizontais/verticais
                bool diffUD = (U != D);
                bool diffLR = (L != R);

                char cTL = (diffUD && diffLR && U == L && ShouldSmoothCorner(P, U)) ? U : P;
                char cTR = (diffUD && diffLR && U == R && ShouldSmoothCorner(P, U)) ? U : P;
                char cBL = (diffUD && diffLR && D == L && ShouldSmoothCorner(P, D)) ? D : P;
                char cBR = (diffUD && diffLR && D == R && ShouldSmoothCorner(P, D)) ? D : P;

                int gx = x * 2;
                int gy = y * 2;

                hiChar[gx, gy + 1] = cTL;
                hiChar[gx + 1, gy + 1] = cTR;
                hiChar[gx, gy] = cBL;
                hiChar[gx + 1, gy] = cBR;

                hiCol[gx, gy + 1] = palette(cTL);
                hiCol[gx + 1, gy + 1] = palette(cTR);
                hiCol[gx, gy] = palette(cBL);
                hiCol[gx + 1, gy] = palette(cBR);
            }
        }

        // 4. Passo 2 (64-bit Shading): Iluminação Direcional, Bevel, Ambient Occlusion, Sel-Out e Brilho nos Olhos
        Texture2D tex = new Texture2D(outW, outH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = string.IsNullOrEmpty(cacheKey) ? "PixelGridTex64" : cacheKey
        };

        Color[] finalPixels = new Color[outW * outH];

        for (int y = 0; y < outH; y++)
        {
            float verticalNorm = y / (float)Mathf.Max(1, outH - 1);
            for (int x = 0; x < outW; x++)
            {
                char c = hiChar[x, y];
                Color col = hiCol[x, y];

                if (col.a <= 0.01f)
                {
                    finalPixels[y * outW + x] = Color.clear;
                    continue;
                }

                // A. Contorno Seletivo Colorido (Sel-Out 64-bit)
                if (c == 'O')
                {
                    Color neighborTint = SampleBrightestNeighbor(hiChar, hiCol, x, y, outW, outH);
                    Color selOut = Color.Lerp(col, new Color(neighborTint.r * 0.42f, neighborTint.g * 0.42f, neighborTint.b * 0.48f, 1f), 0.38f);
                    float outlineLight = Mathf.Lerp(0.78f, 1.22f, verticalNorm);
                    selOut.r = Mathf.Clamp01(selOut.r * outlineLight);
                    selOut.g = Mathf.Clamp01(selOut.g * outlineLight);
                    selOut.b = Mathf.Clamp01(selOut.b * outlineLight);

                    if (y <= 1)
                    {
                        selOut = new Color(selOut.r * 0.65f, selOut.g * 0.65f, selOut.b * 0.72f, 1f);
                    }
                    finalPixels[y * outW + x] = selOut;
                    continue;
                }

                // B. Olhos ('E'): Adiciona Catchlight (brilho vivo no canto superior-esquerdo da íris 64-bit)
                if (c == 'E')
                {
                    char upChar = GetChar(hiChar, x, y + 1, outW, outH);
                    char leftChar = GetChar(hiChar, x - 1, y, outW, outH);
                    char downChar = GetChar(hiChar, x, y - 1, outW, outH);
                    if (upChar != 'E' && leftChar != 'E')
                    {
                        finalPixels[y * outW + x] = new Color(0.92f, 0.96f, 1.00f, 1f);
                    }
                    else if (downChar != 'E')
                    {
                        finalPixels[y * outW + x] = new Color(0.20f, 0.28f, 0.46f, 1f);
                    }
                    else
                    {
                        finalPixels[y * outW + x] = col;
                    }
                    continue;
                }

                // C. Iluminação Direcional 64-bit (Luz Solar Topo-Esquerda / Sombra Profunda Base-Direita)
                char cUp = GetChar(hiChar, x, y + 1, outW, outH);
                char cDown = GetChar(hiChar, x, y - 1, outW, outH);
                char cLeft = GetChar(hiChar, x - 1, y, outW, outH);
                char cRight = GetChar(hiChar, x + 1, y, outW, outH);

                bool diffTop = !SameMaterialFamily(c, cUp);
                bool diffLeft = !SameMaterialFamily(c, cLeft);
                bool diffBottom = !SameMaterialFamily(c, cDown);
                bool diffRight = !SameMaterialFamily(c, cRight);

                float shadeDelta = 0f;

                if (diffTop && !diffBottom) shadeDelta += 0.11f;
                if (diffLeft && !diffRight) shadeDelta += 0.08f;

                if (diffBottom && !diffTop) shadeDelta -= 0.13f;
                if (diffRight && !diffLeft) shadeDelta -= 0.09f;

                if (cUp == 'O' && c != 'W' && c != 'S')
                {
                    shadeDelta -= 0.07f;
                }

                int subX = x & 1;
                int subY = (y + 1) & 1;
                if (subX == 0 && subY == 0) shadeDelta += 0.035f;
                else if (subX == 1 && subY == 1) shadeDelta -= 0.045f;

                float matNoise = (Hash01(x, y, 4099) - 0.5f) * GetMaterialTextureStrength(c);
                shadeDelta += matNoise;

                Color shaded = ApplyRichShading(col, shadeDelta);
                finalPixels[y * outW + x] = shaded;
            }
        }

        tex.SetPixels(finalPixels);
        tex.Apply();

        if (!string.IsNullOrEmpty(cacheKey))
        {
            CachedTextures[cacheKey] = tex;
            TrySavePrebakedPngInEditor(cacheKey, tex);
        }

        return tex;
    }

    public static Sprite CreateSpriteFromGrid(string key, string[] grid, System.Func<char, Color> palette, float pixelsPerUnit = 32f, int width = 32, int height = 32, bool alignBottom = false)
    {
        string baseKey = alignBottom && !string.IsNullOrEmpty(key) ? (key + "_Grounded") : key;
        string cacheKey = !string.IsNullOrEmpty(baseKey) ? (baseKey + "_64bit") : baseKey;

        if (!string.IsNullOrEmpty(cacheKey) && CachedSprites.TryGetValue(cacheKey, out Sprite existing) && existing != null)
        {
            return existing;
        }

        Texture2D tex = CreateTextureFromGrid(key, grid, palette, width, height, alignBottom);
        float scaledPpu = pixelsPerUnit * UpscaleFactor; // 32 PPU -> 64 PPU (mantém exatamente 1.0 unidade de mundo!)
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), scaledPpu, 0, SpriteMeshType.FullRect);
        sprite.name = string.IsNullOrEmpty(cacheKey) ? "PixelGridSprite64" : cacheKey;

        if (!string.IsNullOrEmpty(cacheKey))
        {
            CachedSprites[cacheKey] = sprite;
        }

        return sprite;
    }

    /// <summary>
    /// Converte uma textura procedural de cenário/prop 32-bit em uma textura 64-bit HD (2x resolução)
    /// com suavização Scale2x, bevel de luz topo-esquerda, sombra base-direita e micro-textura de superfície.
    /// </summary>
    public static Texture2D UpscaleTextureTo64Bit(Texture2D src, bool seamlessTile = false)
    {
        if (src == null) return null;

        string cacheKey = src.name + "_64bit";
        if (!string.IsNullOrEmpty(src.name) && CachedTextures.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        int w = src.width;
        int h = src.height;
        int outW = w * UpscaleFactor;
        int outH = h * UpscaleFactor;

        Color[] srcPixels = src.GetPixels();
        Color[,] hiCol = new Color[outW, outH];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color P = GetTexPixel(srcPixels, x, y, w, h, seamlessTile);
                Color U = GetTexPixel(srcPixels, x, y + 1, w, h, seamlessTile);
                Color D = GetTexPixel(srcPixels, x, y - 1, w, h, seamlessTile);
                Color L = GetTexPixel(srcPixels, x - 1, y, w, h, seamlessTile);
                Color R = GetTexPixel(srcPixels, x + 1, y, w, h, seamlessTile);

                bool diffUD = !ColorsClose(U, D, 0.02f);
                bool diffLR = !ColorsClose(L, R, 0.02f);

                Color cTL = (diffUD && diffLR && ColorsClose(U, L, 0.02f)) ? U : P;
                Color cTR = (diffUD && diffLR && ColorsClose(U, R, 0.02f)) ? U : P;
                Color cBL = (diffUD && diffLR && ColorsClose(D, L, 0.02f)) ? D : P;
                Color cBR = (diffUD && diffLR && ColorsClose(D, R, 0.02f)) ? D : P;

                int gx = x * 2;
                int gy = y * 2;
                hiCol[gx, gy + 1] = cTL;
                hiCol[gx + 1, gy + 1] = cTR;
                hiCol[gx, gy] = cBL;
                hiCol[gx + 1, gy] = cBR;
            }
        }

        Texture2D dst = new Texture2D(outW, outH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = src.wrapMode,
            name = cacheKey
        };

        Color[] outPixels = new Color[outW * outH];
        for (int y = 0; y < outH; y++)
        {
            for (int x = 0; x < outW; x++)
            {
                Color c = hiCol[x, y];
                if (c.a <= 0.01f)
                {
                    outPixels[y * outW + x] = Color.clear;
                    continue;
                }

                Color u = GetHiPixel(hiCol, x, y + 1, outW, outH, seamlessTile);
                Color d = GetHiPixel(hiCol, x, y - 1, outW, outH, seamlessTile);
                Color l = GetHiPixel(hiCol, x - 1, y, outW, outH, seamlessTile);
                Color r = GetHiPixel(hiCol, x + 1, y, outW, outH, seamlessTile);

                bool diffTop = !ColorsClose(c, u, 0.08f);
                bool diffBot = !ColorsClose(c, d, 0.08f);
                bool diffLft = !ColorsClose(c, l, 0.08f);
                bool diffRgt = !ColorsClose(c, r, 0.08f);

                float delta = 0f;
                if (diffTop && !diffBot) delta += 0.08f;
                if (diffLft && !diffRgt) delta += 0.06f;
                if (diffBot && !diffTop) delta -= 0.10f;
                if (diffRgt && !diffLft) delta -= 0.07f;

                int subX = x & 1;
                int subY = (y + 1) & 1;
                if (subX == 0 && subY == 0) delta += 0.025f;
                else if (subX == 1 && subY == 1) delta -= 0.032f;

                delta += (Hash01(x, y, 7919) - 0.5f) * 0.035f;

                outPixels[y * outW + x] = ApplyRichShading(c, delta);
            }
        }

        dst.SetPixels(outPixels);
        dst.Apply();

        if (!string.IsNullOrEmpty(src.name))
        {
            CachedTextures[cacheKey] = dst;
        }
        return dst;
    }

    public static Sprite CreateSprite64FromTexture(Texture2D src32, float basePpu = 32f, bool seamlessTile = false, SpriteMeshType meshType = SpriteMeshType.FullRect)
    {
        if (src32 == null) return null;
        string cacheKey = src32.name + "_Sprite64";
        if (!string.IsNullOrEmpty(src32.name) && CachedSprites.TryGetValue(cacheKey, out Sprite existing) && existing != null)
        {
            return existing;
        }

        Texture2D tex64 = UpscaleTextureTo64Bit(src32, seamlessTile);
        Sprite s = Sprite.Create(tex64, new Rect(0, 0, tex64.width, tex64.height), new Vector2(0.5f, 0.5f), basePpu * UpscaleFactor, 0, meshType);
        s.name = cacheKey;
        if (!string.IsNullOrEmpty(src32.name))
        {
            CachedSprites[cacheKey] = s;
        }
        return s;
    }

    public static Texture2D CreateBlankTexture(string name, int width, int height, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = wrapMode,
            name = name
        };
        return tex;
    }

    private static Color GetTexPixel(Color[] pixels, int x, int y, int w, int h, bool wrap)
    {
        if (wrap)
        {
            x = (x % w + w) % w;
            y = Mathf.Clamp(y, 0, h - 1);
            return pixels[y * w + x];
        }
        if (x < 0 || x >= w || y < 0 || y >= h) return Color.clear;
        return pixels[y * w + x];
    }

    private static Color GetHiPixel(Color[,] cols, int x, int y, int w, int h, bool wrap)
    {
        if (wrap)
        {
            x = (x % w + w) % w;
            y = Mathf.Clamp(y, 0, h - 1);
            return cols[x, y];
        }
        if (x < 0 || x >= w || y < 0 || y >= h) return Color.clear;
        return cols[x, y];
    }

    private static bool ColorsClose(Color a, Color b, float tol)
    {
        return Mathf.Abs(a.r - b.r) <= tol
            && Mathf.Abs(a.g - b.g) <= tol
            && Mathf.Abs(a.b - b.b) <= tol
            && Mathf.Abs(a.a - b.a) <= tol;
    }

    private static char GetChar(char[,] map, int x, int y, int w, int h)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return '.';
        return map[x, y];
    }

    private static bool ShouldSmoothCorner(char center, char neighbor)
    {
        if (center == 'E' || neighbor == 'E' || center == 'N' || center == 'n') return false;
        return true;
    }

    private static bool SameMaterialFamily(char a, char b)
    {
        if (a == b) return true;
        return char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
    }

    private static float GetMaterialTextureStrength(char c)
    {
        switch (c)
        {
            case 'B':
            case 'b':
            case 'Y':
            case 'y':
            case 'P':
            case 'p':
            case 'R':
            case 'r':
                return 0.055f;

            case 'H':
            case 'h':
            case 'l':
            case 'D':
                return 0.075f;

            case 'M':
            case 'm':
            case 'A':
            case 'K':
            case 'k':
                return 0.050f;

            case 'S':
            case 's':
            case 'W':
            case 'w':
            case 'q':
                return 0.022f;

            default:
                return 0.035f;
        }
    }

    private static Color ApplyRichShading(Color baseColor, float delta)
    {
        if (Mathf.Abs(delta) < 0.001f) return baseColor;

        if (delta > 0f)
        {
            Color warmLight = new Color(1.00f, 0.98f, 0.90f, baseColor.a);
            Color c = Color.Lerp(baseColor, warmLight, Mathf.Clamp01(delta * 0.85f));
            c.a = baseColor.a;
            return c;
        }
        else
        {
            float shadowAmt = Mathf.Clamp01(-delta);
            Color coolShadow = new Color(
                baseColor.r * 0.42f,
                baseColor.g * 0.38f,
                Mathf.Min(1f, baseColor.b * 0.56f + 0.04f),
                baseColor.a
            );
            Color c = Color.Lerp(baseColor, coolShadow, shadowAmt);
            c.a = baseColor.a;
            return c;
        }
    }

    private static Color SampleBrightestNeighbor(char[,] chars, Color[,] cols, int x, int y, int w, int h)
    {
        Color best = new Color(0.18f, 0.20f, 0.28f, 1f);
        float bestLum = -1f;

        int[] dx = { -1, 1, 0, 0, -1, 1, -1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, -1, 1, 1 };

        for (int i = 0; i < 8; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
            char nc = chars[nx, ny];
            if (nc == '.' || nc == ' ' || nc == 'O') continue;

            Color c = cols[nx, ny];
            if (c.a <= 0.1f) continue;
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            if (lum > bestLum)
            {
                bestLum = lum;
                best = c;
            }
        }

        return best;
    }

    private static void TrySavePrebakedPngInEditor(string cacheKey, Texture2D tex)
    {
#if UNITY_EDITOR
        try
        {
            string dir = Path.Combine(Application.dataPath, "Resources", "Sprites64");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string filePath = Path.Combine(dir, cacheKey + ".png");
            if (!File.Exists(filePath))
            {
                byte[] pngBytes = tex.EncodeToPNG();
                if (pngBytes != null && pngBytes.Length > 0)
                {
                    File.WriteAllBytes(filePath, pngBytes);
                }
            }
        }
        catch
        {
            // Ignora falhas de IO caso a pasta esteja bloqueada temporariamente
        }
#endif
    }

    public static float Hash01(int x, int y, int seed = 1337)
    {
        unchecked
        {
            int n = x * 374761393 + y * 668265263 + seed * 1442695041;
            n = (n ^ (n >> 13)) * 1274126177;
            n = n ^ (n >> 16);
            return (n & 0x7FFFFFFF) / 2147483647f;
        }
    }
}
