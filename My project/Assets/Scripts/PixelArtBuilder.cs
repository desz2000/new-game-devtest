using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilitário central para geração procedural de texturas e sprites em Pixel Art (32x32 e UI),
/// garantindo FilterMode.Point e estilo coeso Duck Souls x Celeste.
/// </summary>
public static class PixelArtBuilder
{
    private static readonly Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Sprite> CachedSprites = new Dictionary<string, Sprite>();

    public static Texture2D CreateTextureFromGrid(string key, string[] grid, System.Func<char, Color> palette, int width = 32, int height = 32)
    {
        if (!string.IsNullOrEmpty(key) && CachedTextures.TryGetValue(key, out Texture2D existing) && existing != null)
        {
            return existing;
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = string.IsNullOrEmpty(key) ? "PixelGridTex" : key
        };

        for (int row = 0; row < height; row++)
        {
            string line = (grid != null && row < grid.Length) ? grid[row] : string.Empty;
            int y = height - 1 - row;
            for (int x = 0; x < width; x++)
            {
                char c = x < line.Length ? line[x] : '.';
                tex.SetPixel(x, y, palette(c));
            }
        }

        tex.Apply();

        if (!string.IsNullOrEmpty(key))
        {
            CachedTextures[key] = tex;
        }

        return tex;
    }

    public static Sprite CreateSpriteFromGrid(string key, string[] grid, System.Func<char, Color> palette, float pixelsPerUnit = 32f, int width = 32, int height = 32)
    {
        if (!string.IsNullOrEmpty(key) && CachedSprites.TryGetValue(key, out Sprite existing) && existing != null)
        {
            return existing;
        }

        Texture2D tex = CreateTextureFromGrid(key, grid, palette, width, height);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        sprite.name = string.IsNullOrEmpty(key) ? "PixelGridSprite" : key;

        if (!string.IsNullOrEmpty(key))
        {
            CachedSprites[key] = sprite;
        }

        return sprite;
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
