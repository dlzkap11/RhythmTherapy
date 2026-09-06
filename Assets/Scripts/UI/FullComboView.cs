using DG.Tweening;
using RhythmTherapy.Core;
using TMPro;
using UnityEngine;

/// <summary>
/// GameScene 풀콤보/올퍼펙트 연출. 곡 종료(GameManager.Finished) 시 fullCombo 면
/// ResultScene 전환 전 1.5초 대기 창 안에서 배너를 잠깐 띄운다.
/// </summary>
public sealed class FullComboView : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private RectTransform burst;

    private Sequence sequence;

    private void Start()
    {
        if (group != null)
            group.alpha = 0f;

        if (GameManager.Instance != null)
            GameManager.Instance.Finished += OnFinished;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Finished -= OnFinished;

        sequence?.Kill();
    }

    private void OnFinished(GameResult result)
    {
        if (!result.fullCombo || group == null)
            return;

        if (label != null)
            label.text = result.allPerfect ? "ALL PERFECT" : "FULL COMBO";

        sequence?.Kill();
        group.alpha = 0f;

        sequence = DOTween.Sequence();
        sequence.Append(group.DOFade(1f, 0.15f));

        if (burst != null)
        {
            burst.localScale = Vector3.zero;
            sequence.Join(burst.DOScale(1.15f, 0.25f).SetEase(Ease.OutBack));
        }

        if (label != null)
        {
            label.transform.localScale = Vector3.one * 0.6f;
            sequence.Join(label.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
        }

        sequence.AppendInterval(GameConfig.FullComboHoldSeconds);
        sequence.Append(group.DOFade(0f, 0.3f));
    }
}
