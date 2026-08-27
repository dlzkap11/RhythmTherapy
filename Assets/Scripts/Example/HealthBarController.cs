// HealthBarController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PixelBeat
{
    public class HealthBarController : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Color healthyColor = new Color(0.30f, 1f, 0.56f);
        [SerializeField] private Color warnColor = new Color(1f, 0.88f, 0.30f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.30f, 0.30f);
        [SerializeField] private float maxHealth = 100f;

        private float _current;

        public void Init(float max)
        {
            maxHealth = max;
            _current = max;
            Refresh();
        }

        public void Set(float value)
        {
            _current = Mathf.Clamp(value, 0f, maxHealth);
            Refresh();
        }

        public void ApplyDelta(float delta)
        {
            _current = Mathf.Clamp(_current + delta, 0f, maxHealth);
            Refresh();
        }

        public bool IsDead => _current <= 0f;

        private void Refresh()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = _current / maxHealth;
                float pct = _current / maxHealth;
                fillImage.color = pct > 0.5f ? healthyColor : (pct > 0.25f ? warnColor : dangerColor);
            }
            if (valueText != null)
                valueText.text = Mathf.RoundToInt(_current) + "/" + Mathf.RoundToInt(maxHealth);
        }
    }
}
