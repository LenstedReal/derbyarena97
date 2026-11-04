using UnityEditor;
using UnityEngine;

namespace DerbyArena.EditorTools
{
    /// <summary>
    /// Import settings for the code-built UI (4.0). The repository does not track .meta files, so the first import
    /// on the build machine gets these rules: UI sprites and icons under Resources/Derby/UI stay uncompressed,
    /// mip-free and transparent (9-slice gradients band badly with ETC/ASTC), the carbon pattern repeats, the
    /// cloud panorama wraps horizontally, and the bundled fonts are dynamic with Turkish characters included.
    /// </summary>
    public sealed class DerbyAssetImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string p = assetPath.Replace('\\', '/');
            var imp = (TextureImporter)assetImporter;
            if (p.Contains("/Resources/Derby/UI/"))
            {
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.filterMode = FilterMode.Bilinear;
                imp.wrapMode = p.EndsWith("carbon.png") || p.EndsWith("hazard.png") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                imp.sRGBTexture = true;
                imp.maxTextureSize = 512;
            }
            else if (p.Contains("/Resources/Derby/Textures/clouds"))
            {
                imp.wrapModeU = TextureWrapMode.Repeat;
                imp.wrapModeV = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (p.Contains("/Resources/Derby/keyart"))
            {
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 1024;
            }
        }

        void OnPreprocessAsset()
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/Derby/Fonts/") || !p.EndsWith(".ttf")) return;
            var imp = assetImporter as TrueTypeFontImporter;
            if (imp == null) return;
            imp.fontTextureCase = FontTextureCase.Dynamic;
            imp.includeFontData = true;
            imp.fontRenderingMode = FontRenderingMode.Smooth;
        }
    }
}
