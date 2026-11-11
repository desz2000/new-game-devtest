#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Garante que todos os sprites 64-bit gerados em Assets/Resources/Sprites64/ e Assets/Sprites64/
/// sejam importados sem compressão com perda (RGBA32 Uncompressed), FilterMode.Point,
/// sem MipMaps e com 64 Pixels Per Unit (64 PPU = 1.0 unidade de mundo).
/// </summary>
public class Sprites64Importer : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath == null) return;
        if (!assetPath.Contains("Sprites64/")) return;

        TextureImporter importer = assetImporter as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.npotScale = TextureImporterNPOTScale.None;
    }
}
#endif
