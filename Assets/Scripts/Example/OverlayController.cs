// OverlayController.cs
// 시작/일시정지/결과 오버레이.
using UnityEngine;
using UnityEngine.UI;
using TMP_Text = TMPro.TMP_Text;

namespace PixelBeat
{
    public class OverlayController : MonoBehaviour
    {
        [Header("Start")]
        public GameObject startRoot;
        public TMP_Text startTitleText;
        public Button startButton;

        [Header("Result")]
        public GameObject resultRoot;
        public TMP_Text resultTitleText;
        public TMP_Text rankText;
        public TMP_Text perfectText;
        public TMP_Text greatText;
        public TMP_Text missText;
        public TMP_Text maxComboText;
        public TMP_Text scoreText;
        public TMP_Text accText;
        public Button retryButton;

        private void Awake()
        {
            HideAll();
        }

        public void ShowStart(System.Action onStart)
        {
            HideAll();
            if (startRoot != null) startRoot.SetActive(true);
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => onStart?.Invoke());
            }
        }

        public void HideAll()
        {
            if (startRoot != null) startRoot.SetActive(false);
            if (resultRoot != null) resultRoot.SetActive(false);
        }

        public void ShowResult(bool win, int score, int perfect, int great, int miss, int maxCombo, float acc, System.Action onRetry)
        {
            HideAll();
            if (resultRoot != null) resultRoot.SetActive(true);
            if (resultTitleText != null) resultTitleText.text = win ? "CLEAR!" : "FAILED";
            string rank = acc >= 95f ? "S" : acc >= 85f ? "A" : acc >= 70f ? "B" : acc >= 50f ? "C" : "D";
            if (rankText != null) rankText.text = rank;
            if (perfectText != null) perfectText.text = "PERFECT  " + perfect;
            if (greatText != null) greatText.text = "GREAT  " + great;
            if (missText != null) missText.text = "MISS  " + miss;
            if (maxComboText != null) maxComboText.text = "MAX COMBO  " + maxCombo;
            if (scoreText != null) scoreText.text = "SCORE  " + score.ToString("D6");
            if (accText != null) accText.text = "ACC  " + acc.ToString("F1") + "%";
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() => onRetry?.Invoke());
            }
        }
    }
}
