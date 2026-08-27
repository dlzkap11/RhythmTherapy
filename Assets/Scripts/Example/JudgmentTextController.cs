// JudgmentTextController.cs
// PERFECT/GREAT/MISS 텍스트 팝 연출.
using UnityEngine;
using UnityEngine.UI;
using TMP_Text = TMPro.TMP_Text;

namespace PixelBeat
{
    public class JudgmentTextController : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rt;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Color perfectColor = new Color(1f, 0.88f, 0.30f);
        [SerializeField] private Color greatColor = new Color(0.30f, 1f, 0.56f);
        [SerializeField] private Color missColor = new Color(1f, 0.30f, 0.30f);

        private float _timer;

        public void Show(JudgeRank rank)
        {
            if (text == null) return;
            switch (rank)
            {
                case JudgeRank.Perfect: text.text = "PERFECT"; text.color = perfectColor; break;
                case JudgeRank.Great:   text.text = "GREAT";   text.color = greatColor;   break;
                default:                text.text = "MISS";     text.color = missColor;    break;
            }
            _timer = duration;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (rt != null) rt.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (_timer <= 0f) return;
            _timer -= Time.deltaTime;
            float t = 1f - (_timer / duration); // 0 -> 1
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(_timer / duration * 1.2f);
            if (rt != null)
            {
                float scale = Mathf.Lerp(1.2f, 1f, Mathf.Clamp01(t * 3f));
                rt.localScale = new Vector3(scale, scale, 1f);
                rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 16f, t));
            }
            if (_timer <= 0f && canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
}
