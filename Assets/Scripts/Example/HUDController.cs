// HUDController.cs
// 상단 HUD: 점수 / 정확도 / 콤보 갱신.
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시. 일반 Text면 아래 타입을 UnityEngine.UI.Text 로 교체.

namespace PixelBeat
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text accText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text comboLabel;

        private int _total;

        public void Init(int total) { _total = total; Refresh(0, 0, 0, 0); }

        public void Refresh(int score, int combo, int perfect, int great)
        {
            if (scoreText != null) scoreText.text = score.ToString("D6");
            if (comboText != null) comboText.text = combo.ToString();

            float acc = (perfect + great) == 0 ? 100f : (perfect + great * 0.5f) / Mathf.Max(1, _total) * 100f;
            if (accText != null) accText.text = acc.ToString("F1") + "%";
        }

        public void SetComboColor(Color color)
        {
            if (comboText != null) comboText.color = color;
        }
    }
}
