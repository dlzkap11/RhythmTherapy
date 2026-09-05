using System.IO;

using UnityEditor;
using UnityEngine;

namespace RhythmTherapy.EditorTools
{
    /// <summary>
    /// 결과 연출(STAGE CLEAR / FAILED / FULL COMBO) 배너 뒤에 깔리는 샤프 스타버스트 스프라이트를
    /// 절차적으로 생성해 Assets/Resources/Arts/result_burst.png 로 저장한다. 전부 흰색 + 알파만
    /// 변조하므로 런타임에서 Image.color 로 티팅한다. 외부 아트 없이 재생성 가능.
    /// </summary>
    public static class BurstSpriteGenerator
    {
        const string OutputPath = "Assets/Resources/Arts/result_burst.png";
        const int Size = 1024;
        const int Supersample = 2;

        [MenuItem("RhythmTherapy/Art/Generate Result Burst")]
        public static void Generate()
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[Size * Size];

            float half = Size * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float acc = 0f;

                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            float px = x + (sx + 0.5f) / Supersample;
                            float py = y + (sy + 0.5f) / Supersample;

                            float dx = (px - half) / half;
                            float dy = (py - half) / half;
                            acc += SampleAlpha(dx, dy);
                        }
                    }

                    float a = Mathf.Clamp01(acc / (Supersample * Supersample));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings();

            Debug.Log("[BurstSpriteGenerator] 생성 완료 — " + OutputPath);
        }

        /// <summary>정규화 좌표(-1~1)에서 버스트 알파를 계산. 여러 성분을 max 합성.</summary>
        static float SampleAlpha(float dx, float dy)
        {
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r >= 1f)
                return 0f;

            float ang = Mathf.Atan2(dy, dx);

            // 중앙 글로우
            float glow = 0f;
            if (r < 0.32f)
            {
                float t = 1f - r / 0.32f;
                glow = t * t * 0.85f;
            }

            // 장축 광선 4개 (0/90/180/270)
            float primary = RayAlpha(ang, r, Mathf.PI * 0.5f, 0f, 0.16f, 0.006f, 0.95f, 0.62f);
            // 대각 보조 광선 4개 (45/135/...)
            float secondary = RayAlpha(ang, r, Mathf.PI * 0.5f, Mathf.PI * 0.25f, 0.10f, 0.004f, 0.55f, 0.34f);

            // 얇은 링
            float ringD = (r - 0.5f) / 0.012f;
            float ring = Mathf.Exp(-ringD * ringD) * 0.16f;

            return Mathf.Max(Mathf.Max(glow, primary), Mathf.Max(secondary, ring));
        }

        /// <summary>
        /// 축 간격 axisStep, 위상 phase 인 방사 광선의 알파.
        /// widthNear→widthFar 로 반경에 따라 뾰족해지고, reach 반경에서 페이드.
        /// </summary>
        static float RayAlpha(float ang, float r, float axisStep, float phase,
            float widthNear, float widthFar, float peak, float reach)
        {
            float a = Mathf.Repeat(ang - phase + axisStep * 0.5f, axisStep) - axisStep * 0.5f;
            float d = Mathf.Abs(a);

            float width = Mathf.Lerp(widthNear, widthFar, Mathf.SmoothStep(0f, 1f, r));
            float core = Mathf.Clamp01(1f - d / width);
            core = Mathf.Pow(core, 1.5f);

            // 반경 엔벌로프: 중심 근처에서 살짝 죽이고 reach 부근에서 끝을 뾰족하게 페이드
            float rise = Mathf.Clamp01(r / 0.04f);
            float fall = 1f - Mathf.SmoothStep(reach * 0.82f, reach, r);
            return core * rise * fall * peak;
        }

        static void ApplyImportSettings()
        {
            TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;

            importer.SaveAndReimport();
        }
    }
}
