// LaneController.cs
// 레인 단위 시각 요소(키캡, 하이라이트). 노트 컨테이너 참조 보유.
using UnityEngine;
using UnityEngine.UI;

namespace PixelBeat
{
    public class LaneController : MonoBehaviour
    {
        public RectTransform noteContainer;   // 이 레인에 속한 노트들이 들어갈 부모
        public Image keycap;
        public Image keycapBorder;
        public Image highlight;

        private float _highlightTimer;

        public void Configure(int laneIndex, RhythmGameConfig config)
        {
            Color c = config.laneColors[laneIndex];
            if (keycap != null)
            {
                keycap.color = Color.white;
                if (keycapBorder != null) keycapBorder.color = c;
            }
            if (highlight != null) highlight.color = new Color(c.r, c.g, c.b, 0f);
        }

        public void Flash(Color color)
        {
            if (highlight != null)
            {
                highlight.color = new Color(color.r, color.g, color.b, 0.25f);
                _highlightTimer = 0.09f;
            }
        }

        private void Update()
        {
            if (_highlightTimer > 0f)
            {
                _highlightTimer -= Time.deltaTime;
                if (_highlightTimer <= 0f && highlight != null)
                    highlight.color = new Color(highlight.color.r, highlight.color.g, highlight.color.b, 0f);
            }
        }
    }
}
