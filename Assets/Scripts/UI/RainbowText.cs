using TMPro;
using UnityEngine;

public class RainbowText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float speed = 1f;

    // 활성화되는 즉시 한 번 칠해서, 첫 렌더 프레임부터 무지개가 보이게 한다.
    private void OnEnable()
    {
        ApplyRainbow();
    }

    // Update 가 아니라 LateUpdate 에서 칠한다 — DOTween(DOFade 등)이 같은 프레임의 Update
    // 단계에서 tmp.color 를 바꾸면 TMP 가 내부적으로 메시를 다시 만들면서 여기서 칠한 무지개
    // 정점 색을 덮어써 버린다(판정 팝업 색이 안 나오던 원인). LateUpdate 는 그 모든 Update 이후에
    // 실행되는 게 보장되므로, 매 프레임 "마지막에 칠하는 쪽"이 항상 RainbowText 가 되게 한다.
    private void LateUpdate()
    {
        ApplyRainbow();
    }

    private void ApplyRainbow()
    {
        if (textMeshPro == null) return;

        textMeshPro.ForceMeshUpdate();
        TMP_MeshInfo[] meshInfo = textMeshPro.textInfo.meshInfo;

        // textMeshPro.color.a 를 그대로 사용 — DOTween(DOFade) 등으로 외부에서 페이드시켜도
        // 무지개 버텍스 컬러가 매 프레임 알파를 255로 덮어쓰지 않도록 함 (JudgementView 연출용).
        float alpha = textMeshPro.color.a;

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
                rainbow.a = alpha;
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
