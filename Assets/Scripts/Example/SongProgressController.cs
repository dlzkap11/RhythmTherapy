// SongProgressController.cs
using UnityEngine;
using UnityEngine.UI;
using TMP_Text = TMPro.TMP_Text;

namespace PixelBeat
{
    public class SongProgressController : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text songNameText;

        public void SetSongName(string name) { if (songNameText != null) songNameText.text = "▶ " + name; }

        public void Refresh(float elapsed, float length)
        {
            float p = Mathf.Clamp01(elapsed / length);
            if (fillImage != null) fillImage.fillAmount = p;
            if (timeText != null)
            {
                int m = Mathf.FloorToInt(elapsed / 60f);
                int s = Mathf.FloorToInt(elapsed % 60f);
                timeText.text = m + ":" + s.ToString("D2");
            }
        }
    }
}
