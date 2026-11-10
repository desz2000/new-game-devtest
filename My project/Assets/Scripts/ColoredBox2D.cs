using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class ColoredBox2D : MonoBehaviour
{
    public Color boxColor = Color.white;
    public bool hasBorder = true;

    private static Sprite cachedBorderedSprite;
    private static Sprite cachedSolidSprite;

    private void OnEnable()
    {
        ApplyVisuals();
    }

    private void OnValidate()
    {
        ApplyVisuals();
    }

    public void ApplyVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (sr.sprite == null)
        {
            sr.sprite = hasBorder ? GetBorderedSprite() : GetSolidSprite();
        }
        sr.color = boxColor;
    }

    public static Sprite GetBorderedSprite()
    {
        if (cachedBorderedSprite != null) return cachedBorderedSprite;

        const int size = 64;
        const int border = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isEdge = x < border || x >= size - border || y < border || y >= size - border;
                tex.SetPixel(x, y, isEdge ? new Color(0.82f, 0.82f, 0.82f, 1f) : Color.white);
            }
        }
        tex.Apply();

        cachedBorderedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cachedBorderedSprite.name = "ProceduralBoxSprite";
        return cachedBorderedSprite;
    }

    public static Sprite GetSolidSprite()
    {
        if (cachedSolidSprite != null) return cachedSolidSprite;

        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        cachedSolidSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        cachedSolidSprite.name = "ProceduralSolidSprite";
        return cachedSolidSprite;
    }
}
