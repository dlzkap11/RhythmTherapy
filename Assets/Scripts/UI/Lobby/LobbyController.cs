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
    [SerializeField] private Scrollbar positionBar;

    [Header("미리듣기")]
    [SerializeField] private AudioSource previewSource;
    [SerializeField] private float previewVolume = 0.5f;

    private readonly List<SongDataConfig> _songs = new List<SongDataConfig>();
    private int _index;
    private int _pending;
    private bool _sliding;
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

        BuildRing();

        if (playButton != null)
            playButton.onClick.AddListener(StartGame);

        if (positionBar != null)
        {
            positionBar.interactable = false;
            positionBar.transform.SetAsLastSibling();
        }

        if (_songs.Count == 0)
        {
            if (titleText != null) titleText.text = "(곡 없음)";
            return;
        }

        _index = Mathf.Clamp(SongSelection.LastIndex, 0, _songs.Count - 1);
        ApplySelection();
        PlayPreview();
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
            Move(-1);
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            Move(1);
    }

    private void Move(int dir)
    {
        // 연타로 입력이 누적돼 키를 떼도 계속 넘어가지 않도록 ±1로 제한 (슬라이드 중 1칸까지만 예약).
        _pending = Mathf.Clamp(_pending + dir, -1, 1);
        if (!_sliding)
            StepSlide();
    }

    private void StepSlide()
    {
        if (_pending == 0)
        {
            PlayPreview();
            return;
        }

        int n = _songs.Count;
        int d = _pending > 0 ? 1 : -1;
        _pending -= d;
        _index = (_index + d + n) % n;

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

        float dim = Mathf.Lerp(0.35f, 1f, k);
        _circles[i].color = new Color(dim, dim, dim, 1f);
    }

    private void OnDestroy()
    {
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
            positionBar.size = 1f / n;
            positionBar.value = n > 1 ? _index / (float)(n - 1) : 0f;
        }
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
}
