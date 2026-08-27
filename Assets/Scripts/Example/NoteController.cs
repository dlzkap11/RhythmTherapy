// NoteController.cs
// 개별 노트의 이동/판정/제거를 담당. 노트 프리팹에 부착.
using UnityEngine;
using UnityEngine.UI;

namespace PixelBeat
{
    [RequireComponent(typeof(RectTransform))]
    public class NoteController : MonoBehaviour
    {
        public NoteData Data { get; private set; }
        public bool IsReleased { get; private set; }

        private RectTransform _rt;
        private RhythmGameConfig _config;
        private Image _image;

        public void Setup(NoteData data, RhythmGameConfig config, float notesLayerHeight, float laneWidth, Color color)
        {
            _rt = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            Data = data;
            _config = config;

            // 색상
            if (_image != null) _image.color = color;

            // x 위치: 레인 중앙 정렬
            float x = (data.lane - (config.laneCount - 1) * 0.5f) * laneWidth;
            _rt.anchoredPosition = new Vector2(x, 0f);
        }

        /// <summary> 곡 경과 시간(elapsed)을 받아 위치 갱신. true면 판정선 도달/통과로 처리 불필요. </summary>
        public void UpdatePosition(float elapsed)
        {
            if (IsReleased) return;
            // hit time에 y = judgeLineY. 그 이전에는 위, 이후에는 아래.
            float y = _config.judgeLineY + (Data.time - elapsed) * _config.noteSpeed;
            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, y);
        }

        /// <summary> 판정선을 넘어 miss 처리 대상인지 </summary>
        public bool IsPastJudgeLine(float elapsed)
        {
            return elapsed - Data.time > _config.greatWindow;
        }

        public void MarkHit() { IsReleased = true; Data.hit = true; }
        public void MarkMissed()
        {
            IsReleased = true;
            Data.missed = true;
            if (_image != null) _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 0.3f);
        }

        public void Release()
        {
            if (!IsReleased) IsReleased = true;
            // 풀링 사용 시 여기서 비활성화. 데모에서는 파괴.
            Destroy(gameObject);
        }
    }
}
