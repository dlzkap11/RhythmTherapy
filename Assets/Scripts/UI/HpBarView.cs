using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP 바 시각화. GameManager.HpChanged 를 받아 Slider.value(0~1) 로 반영한다.
/// Slider Direction = Left To Right 라서 value 가 줄면 오른쪽부터 비워진다.
///
/// 검증용 최소 구현 — 정식 HUD 통합 시 교체 예정.
/// </summary>
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

    private void OnDestroy()
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
