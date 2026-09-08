using DG.Tweening;
using RhythmTherapy.Core;
using RhythmTherapy.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로비 곡 캐러셀. 방향키(←/→)로 곡을 넘긴다(wrap-around). 원들이 호(원) 위를 도는 무한 로터리로,
/// 앞(중앙) 원이 선택곡이며 크고 밝게, 옆으로 갈수록 물러나며 작고 어둡게 표시된다. 넘길 때 모든
/// 원이 한 칸씩만 회전하고, 화면 밖 버퍼 원이 재활용된다. Play 버튼으로 선택곡을 게임씬에 넘긴다.
/// </summary>
public sealed class LobbyController : MonoBehaviour
{
    private const int CircleCount = 7;                  // offset {3,2,1,0,-1,-2,-3}
    private static readonly int[] SlotOffset = { 3, 2, 1, 0, -1, -2, -3 };

    [Header("캐러셀 원 (Image)")]
    [SerializeField] private Image centerImage;
    [SerializeField] private Image leftImage;
    [SerializeField] private Image rightImage;
    [SerializeField] private Image[] bufferImages;      // {BufferL2, BufferL1, BufferR1, BufferR2}
    [SerializeField] private Sprite circleSprite;

    [Header("정보")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Scrollbar positionBar;

    [Header("미리듣기")]
    [SerializeField] private AudioSource previewSource;
    [SerializeField] private float previewVolume = 0.5f;

    private readonly List<SongDataConfig> _songs = new List<SongDataConfig>();
    private int _index;
    private int _virtualIndex;          // 언랩(누적) 인덱스. _index = Mod(_virtualIndex, n)
    private int _targetVirtual;
    private int _maxVisibleOffset;      // 중복 없이 보여줄 수 있는 최대 슬롯 오프셋
    private bool _sliding;
    private bool _suppressBarCallback;
    private Sequence _slideSeq;
    private Sequence _previewFade;

    private Vector2 _leftPos;
    private Vector2 _centerPos;
    private Vector2 _rightPos;

    private Image[] _circles;
    private float[] _ang;
    private int[] _circleSong;
    private Vector3[] _circleBaseScale;
    private Vector2 _ringCenter;
    private float _ringRadius;
    private float _frontAngle;
    private float _stepAngle;
    private bool _ringValid;

    private void Start()
    {
        _songs.Clear();
        _songs.AddRange(SongCatalog.Songs);

        if (centerImage != null) _centerPos = centerImage.rectTransform.anchoredPosition;
        if (leftImage != null) _leftPos = leftImage.rectTransform.anchoredPosition;
        if (rightImage != null) _rightPos = rightImage.rectTransform.anchoredPosition;

        // 중복 없이 보여줄 수 있는 최대 슬롯 오프셋. 곡 3개 → 1(원 3개), 5개 → 2(원 5개), 7개 이상 → 3(원 7개).
        _maxVisibleOffset = Mathf.Min(CircleCount / 2, Mathf.Max(0, (_songs.Count - 1) / 2));

        BuildRing();
        EnsureSwipeHandler();

        if (playButton != null)
            playButton.onClick.AddListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (positionBar != null)
        {
            positionBar.transform.SetAsLastSibling();
            positionBar.interactable = _songs.Count > 1;
            positionBar.numberOfSteps = Mathf.Max(0, _songs.Count);
            positionBar.onValueChanged.AddListener(OnPositionBar);
        }

        if (_songs.Count == 0)
        {
            if (titleText != null) titleText.text = "(곡 없음)";
            return;
        }

        // 로비는 항상 첫 곡에서 시작한다(스크롤바 핸들 왼쪽 끝). 이전 선택 위치는 복원하지 않는다.
        _index = 0;
        _virtualIndex = _index;
        _targetVirtual = _virtualIndex;
        ApplySelection();
        PlayPreview();
    }

    /// <summary>
    /// 캐러셀 패널에 마우스 스와이프 핸들러를 보장한다. 씬 배선(에디터 메뉴)에 의존하지 않도록
    /// 이미 배선된 centerImage 의 부모를 그대로 쓴다.
    /// </summary>
    private void EnsureSwipeHandler()
    {
        if (centerImage == null || centerImage.transform.parent == null)
            return;

        GameObject panel = centerImage.transform.parent.gameObject;

        LobbyCarouselSwipe swipe = panel.GetComponent<LobbyCarouselSwipe>();
        if (swipe == null)
            swipe = panel.AddComponent<LobbyCarouselSwipe>();
        swipe.Bind(this);

        Image surface = panel.GetComponent<Image>();
        if (surface != null)
        {
            surface.raycastTarget = true;
            // (left, bottom, right, top) — 하단 정보/버튼 패널은 스와이프 히트에서 제외해 버튼 클릭을 살린다.
            surface.raycastPadding = new Vector4(0f, GameConfig.LobbySwipeBottomExclusionPx, 0f, 0f);
        }
    }

    private static int Mod(int a, int m)
    {
        return ((a % m) + m) % m;
    }

    private void BuildRing()
    {
        bool wired = centerImage != null && leftImage != null && rightImage != null
                     && bufferImages != null && bufferImages.Length >= 4
                     && bufferImages[0] != null && bufferImages[1] != null
                     && bufferImages[2] != null && bufferImages[3] != null;
        if (!wired)
            return;

        FitRing();
        if (!_ringValid)
            return;

        _circles = new[]
        {
            bufferImages[0], bufferImages[1], leftImage, centerImage, rightImage, bufferImages[2], bufferImages[3],
        };
        _ang = new float[CircleCount];
        _circleSong = new int[CircleCount];
        _circleBaseScale = new Vector3[CircleCount];
        for (int i = 0; i < CircleCount; i++)
        {
            _ang[i] = _frontAngle + SlotOffset[i] * _stepAngle;
            _circleBaseScale[i] = _circles[i].transform.localScale;
        }
    }

    /// <summary>3 슬롯 좌표에서 외접원(중심/반지름/앞각/스텝각)을 구한다. 일직선이면 _ringValid=false.</summary>
    private void FitRing()
    {
        Vector2 a = _leftPos, b = _centerPos, c = _rightPos;
        float d = 2f * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
        if (Mathf.Abs(d) < 1e-3f)
        {
            _ringValid = false;
            return;
        }

        float a2 = a.sqrMagnitude, b2 = b.sqrMagnitude, c2 = c.sqrMagnitude;
        float ux = (a2 * (b.y - c.y) + b2 * (c.y - a.y) + c2 * (a.y - b.y)) / d;
        float uy = (a2 * (c.x - b.x) + b2 * (a.x - c.x) + c2 * (b.x - a.x)) / d;

        _ringCenter = new Vector2(ux, uy);
        _ringRadius = Vector2.Distance(_ringCenter, _centerPos);
        _frontAngle = AngleOf(_centerPos);
        _stepAngle = Mathf.DeltaAngle(_frontAngle, AngleOf(_leftPos));
        _ringValid = Mathf.Abs(_stepAngle) > 1f;
    }

    private float AngleOf(Vector2 pos)
    {
        Vector2 v = pos - _ringCenter;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }

    private void Update()
    {
        if (_songs.Count == 0 || Keyboard.current == null)
            return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            Step(-1);
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            Step(1);
    }

    /// <summary>
    /// 방향키·스와이프용. 누른 방향으로만 진행하도록 목표를 가상 인덱스 공간에서 ±1 예약한다.
    /// (절대 인덱스 + wrap 최단경로로 계산하면 곡 수가 적을 때 한 바퀴 돌아 방향이 뒤집힌다.)
    /// </summary>
    private void Step(int dir)
    {
        if (_songs.Count < 2)
            return;

        _targetVirtual = Mathf.Clamp(_targetVirtual + dir, _virtualIndex - 1, _virtualIndex + 1);
        if (!_sliding)
            StepSlide();
    }

    /// <summary>스크롤바용. 요청 시점에 wrap 최단 경로를 부호 있는 델타로 확정한다.</summary>
    private void GoToIndex(int target)
    {
        int n = _songs.Count;
        if (n < 2)
            return;

        target = Mathf.Clamp(target, 0, n - 1);
        int forward = Mod(target - Mod(_virtualIndex, n), n);
        int delta = forward * 2 <= n ? forward : forward - n;

        _targetVirtual = _virtualIndex + delta;
        if (!_sliding)
            StepSlide();
    }

    /// <summary>마우스 스와이프에서 호출. 한 칸 이동.</summary>
    public void SwipeStep(int dir)
    {
        Step(dir);
    }

    private void StepSlide()
    {
        int n = _songs.Count;
        if (_virtualIndex == _targetVirtual)
        {
            // 값이 무한정 커지지 않게 정착 시 정규화.
            _virtualIndex = _index;
            _targetVirtual = _virtualIndex;
            PlayPreview();
            return;
        }

        int d = _targetVirtual > _virtualIndex ? 1 : -1;
        _virtualIndex += d;
        _index = Mod(_virtualIndex, n);

        if (!_ringValid || _circles == null || n < 2)
        {
            ApplySelection();
            StepSlide();
            return;
        }

        _sliding = true;
        _slideSeq?.Kill();

        // 리사이클: 나가는 끝(offset ±3) 원을 화면 밖(offset ∓4)으로 옮겨, 트윈 후 offset ∓3 슬롯에 안착.
        int recycle = ExitingCircle(d);
        _ang[recycle] = _frontAngle - 4f * d * _stepAngle;
        _circleSong[recycle] = ((_index + 3 * d) % n + n) % n;
        RenderCircle(recycle, true);

        float[] start = (float[])_ang.Clone();
        float[] target = new float[CircleCount];
        for (int i = 0; i < CircleCount; i++)
            target[i] = start[i] + d * _stepAngle;

        float t = 0f;
        _slideSeq = DOTween.Sequence();
        _slideSeq.Append(DOTween.To(() => t, v =>
        {
            t = v;
            for (int i = 0; i < CircleCount; i++)
            {
                _ang[i] = Mathf.Lerp(start[i], target[i], v);
                RenderCircle(i, false);
            }
            ReorderCirclesByDepth();
        }, 1f, GameConfig.LobbySlideSeconds).SetEase(Ease.OutCubic));

        _slideSeq.OnComplete(() =>
        {
            for (int i = 0; i < CircleCount; i++)
                _ang[i] = _frontAngle + Mathf.Round(Mathf.DeltaAngle(_frontAngle, _ang[i]) / _stepAngle) * _stepAngle;

            ApplySelection();
            FrontPunch();
            _sliding = false;
            StepSlide();
        });
    }

    private int ExitingCircle(int d)
    {
        int best = 0;
        for (int i = 1; i < CircleCount; i++)
        {
            bool take = d > 0 ? _ang[i] > _ang[best] : _ang[i] < _ang[best];
            if (take) best = i;
        }
        return best;
    }

    private int FrontCircle()
    {
        int best = 0;
        for (int i = 1; i < CircleCount; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(_ang[i], _frontAngle)) < Mathf.Abs(Mathf.DeltaAngle(_ang[best], _frontAngle)))
                best = i;
        }
        return best;
    }

    private void FrontPunch()
    {
        int f = FrontCircle();
        Transform tr = _circles[f].transform;
        tr.DOKill(true);
        tr.localScale = _circleBaseScale[f];
        tr.DOPunchScale(_circleBaseScale[f] * 0.1f, 0.22f, 6, 0.6f);
    }

    /// <summary>앞에 가까운 원일수록 뒤 sibling(위에 그려짐)로 정렬.</summary>
    private void ReorderCirclesByDepth()
    {
        int[] order = new int[CircleCount];
        for (int i = 0; i < CircleCount; i++)
            order[i] = i;

        for (int p = 0; p < CircleCount - 1; p++)
        {
            for (int q = 0; q < CircleCount - 1 - p; q++)
            {
                if (Prominence(order[q]) > Prominence(order[q + 1]))
                    (order[q], order[q + 1]) = (order[q + 1], order[q]);
            }
        }

        for (int s = 0; s < CircleCount; s++)
            _circles[order[s]].transform.SetSiblingIndex(s);
    }

    private float Prominence(int i)
    {
        return Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(_ang[i], _frontAngle)) / (Mathf.Abs(_stepAngle) * 2.7f));
    }

    /// <summary>원 i 를 현재 각도에 맞춰 배치·스케일·색 갱신. setSprite 면 스프라이트도 교체.</summary>
    private void RenderCircle(int i, bool setSprite)
    {
        SongDataConfig song = _songs[_circleSong[i]];

        float rad = _ang[i] * Mathf.Deg2Rad;
        _circles[i].rectTransform.anchoredPosition =
            _ringCenter + _ringRadius * new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        float k = Prominence(i);
        _circles[i].transform.localScale = _circleBaseScale[i] * Mathf.Lerp(0.65f, 1f, k);

        // AlbumArt 가 있을 때만 스프라이트를 교체한다. 없으면 씬/인스펙터에 배치된 스프라이트를 그대로 둔다.
        if (setSprite)
        {
            if (song.AlbumArt != null)
                _circles[i].sprite = song.AlbumArt;
            else if (_circles[i].sprite == null)
                _circles[i].sprite = circleSprite;
        }

        // 곡 수가 적으면 먼 슬롯이 같은 곡을 중복 표시하므로, _maxVisibleOffset 을 넘는 원은 사라진다.
        float offsetAbs = Mathf.Abs(Mathf.DeltaAngle(_ang[i], _frontAngle)) / Mathf.Abs(_stepAngle);
        float alpha = Mathf.Clamp01(_maxVisibleOffset + 1f - offsetAbs);

        float dim = Mathf.Lerp(0.35f, 1f, k);
        _circles[i].color = new Color(dim, dim, dim, alpha);
    }

    private void OnDestroy()
    {
        if (positionBar != null)
            positionBar.onValueChanged.RemoveListener(OnPositionBar);

        _slideSeq?.Kill();
        _previewFade?.Kill();
        if (_circles != null)
        {
            foreach (Image c in _circles)
                if (c != null) c.transform.DOKill();
        }
    }

    private void ApplySelection()
    {
        int n = _songs.Count;

        if (_ringValid && _circles != null && n > 0)
        {
            for (int i = 0; i < CircleCount; i++)
            {
                _ang[i] = _frontAngle + Mathf.Round(Mathf.DeltaAngle(_frontAngle, _ang[i]) / _stepAngle) * _stepAngle;
                int offset = Mathf.RoundToInt(Mathf.DeltaAngle(_frontAngle, _ang[i]) / _stepAngle);
                _circleSong[i] = ((_index - offset) % n + n) % n;
                RenderCircle(i, true);
            }
            ReorderCirclesByDepth();
        }
        else if (n > 0)
        {
            ApplyCircle(centerImage, _songs[_index], 1f);
            ApplyCircle(leftImage, _songs[(_index - 1 + n) % n], 0.45f);
            ApplyCircle(rightImage, _songs[(_index + 1) % n], 0.45f);
        }

        if (n == 0)
            return;

        SongDataConfig current = _songs[_index];
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(current.SongName) ? "(제목 없음)" : current.SongName;
        if (highScoreText != null)
        {
            SongScoreRecord record = ScoreRecordManager.Instance != null
                ? ScoreRecordManager.Instance.Get(current.SongID)
                : null;
            highScoreText.text = record != null
                ? $"HIGH SCORE : {record.bestScore:N0}"
                : "HIGH SCORE : ---";
        }

        if (positionBar != null)
        {
            // 프로그램적 갱신이 OnPositionBar 로 되먹임되지 않도록 차단.
            _suppressBarCallback = true;
            positionBar.size = 1f / n;
            positionBar.value = n > 1 ? _index / (float)(n - 1) : 0f;
            _suppressBarCallback = false;
        }
    }

    /// <summary>스크롤바 조작 → 해당 위치의 곡으로 이동.</summary>
    private void OnPositionBar(float value)
    {
        if (_suppressBarCallback || _songs.Count < 2)
            return;

        GoToIndex(Mathf.RoundToInt(value * (_songs.Count - 1)));
    }

    private void ApplyCircle(Image image, SongDataConfig song, float dim)
    {
        if (image == null)
            return;

        // AlbumArt 가 있을 때만 스프라이트를 교체. 없으면 씬에 배치된 스프라이트 유지.
        if (song.AlbumArt != null)
            image.sprite = song.AlbumArt;
        else if (image.sprite == null)
            image.sprite = circleSprite;

        image.color = new Color(dim, dim, dim, 1f);
    }

    private void PlayPreview()
    {
        if (previewSource == null)
            return;

        AudioClip clip = _songs[_index].SongAudioClip;
        _previewFade?.Kill();

        if (clip == null)
        {
            previewSource.Stop();
            return;
        }

        bool playing = previewSource.isPlaying;
        _previewFade = DOTween.Sequence();

        if (playing)
            _previewFade.Append(previewSource.DOFade(0f, GameConfig.LobbyPreviewFadeSeconds));

        _previewFade.AppendCallback(() =>
        {
            previewSource.clip = clip;
            previewSource.loop = true;
            previewSource.time = 0f;
            previewSource.volume = 0f;
            previewSource.Play();
        });

        _previewFade.Append(previewSource.DOFade(previewVolume, GameConfig.LobbyPreviewFadeSeconds));
    }

    private void StartGame()
    {
        if (_songs.Count == 0)
            return;

        _slideSeq?.Kill();
        _previewFade?.Kill();
        if (previewSource != null)
            previewSource.Stop();

        SongSelection.Selected = _songs[_index];
        SongSelection.LastIndex = _index;
        SceneFader.Load("GameScene");
    }

    /// <summary>게임 종료. 에디터에서는 플레이 모드를 정지한다.</summary>
    private void QuitGame()
    {
        _slideSeq?.Kill();
        _previewFade?.Kill();
        if (previewSource != null)
            previewSource.Stop();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
