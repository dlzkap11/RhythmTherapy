using UnityEngine;
using UnityEngine.UI;

public sealed class HpBarView : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Reset()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        Apply(gm.Hp, gm.HpMax);
        gm.HpChanged += OnHpChanged;
    }
    /*
    private void OnEnable()
    {
        GameManager.Instance.HpChanged += OnHpChanged;
    }
    */
    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.HpChanged -= OnHpChanged;
    }

    private void OnHpChanged(int current)
    {
        Apply(current, GameManager.Instance.HpMax);
    }

    private void Apply(int current, int max)
    {
        if (slider == null)
            return;

        slider.value = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }
}
