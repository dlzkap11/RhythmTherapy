using TMPro;
using UnityEngine;

public class RainbowText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float speed = 1f;

    private void Update()
    {
        if (textMeshPro == null) return;

        textMeshPro.ForceMeshUpdate();
        TMP_MeshInfo[] meshInfo = textMeshPro.textInfo.meshInfo;

        for (int i = 0; i < meshInfo.Length; i++)
        {
            Vector3[] vertices = meshInfo[i].vertices;
            Color32[] colors = meshInfo[i].colors32;

            // 문자 수만큼 반복
            int characterCount = textMeshPro.textInfo.characterCount;
            for (int c = 0; c < characterCount; c++)
            {
                // 각 문자의 4 개 버텍스 인덱스 계산
                int vertexIndex = textMeshPro.textInfo.characterInfo[c].vertexIndex;

                // 무지개 색상 계산 (문자 위치 기반)
                float t = (c / (float)characterCount + Time.time * speed) % 1f;
                Color rainbow = Color.HSVToRGB(t, 1f, 1f);
                Color32 rainbow32 = rainbow;

                // 4 개 버텍스에 모두 적용
                colors[vertexIndex + 0] = rainbow32;
                colors[vertexIndex + 1] = rainbow32;
                colors[vertexIndex + 2] = rainbow32;
                colors[vertexIndex + 3] = rainbow32;
            }

            meshInfo[i].colors32 = colors;
        }

        textMeshPro.UpdateVertexData();
    }
}