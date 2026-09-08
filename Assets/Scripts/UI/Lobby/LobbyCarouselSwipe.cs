using RhythmTherapy.Core;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 로비 캐러셀 위에서 마우스/터치를 좌우로 끌면 곡을 넘긴다(스와이프). 가로 이동량이
/// GameConfig.LobbySwipeStepPixels 를 넘으면 한 칸 이동을 요청한다. 한 프레임에 아무리 크게
/// 끌어도 1칸만 넘기고, 계속 끌면 이후 프레임들에서 순차적으로 넘어간다.
///
/// 추후 "손가락 추적형"(끄는 동안 원들이 연속으로 따라 도는) 방식으로 바꿀 때는 이 컴포넌트만
/// 교체하면 된다. LobbyController 의 슬라이드 로직은 건드리지 않는다.
/// </summary>
public sealed class LobbyCarouselSwipe : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private LobbyController controller;

    private float _accum;

    /// <summary>LobbyController 가 런타임에 자기 자신을 연결한다(씬 배선 불필요).</summary>
    public void Bind(LobbyController owner)
    {
        controller = owner;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _accum = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller == null)
            return;

        _accum += eventData.delta.x;
        if (Mathf.Abs(_accum) < GameConfig.LobbySwipeStepPixels)
            return;

        // 내용을 오른쪽으로 끌면 왼쪽(이전) 곡으로 이동 = dir -1.
        // 한 프레임에 크게 끌어도 1칸만: 잔여 이월 없이 리셋한다.
        controller.SwipeStep(_accum > 0f ? -1 : 1);
        _accum = 0f;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _accum = 0f;
    }
}
