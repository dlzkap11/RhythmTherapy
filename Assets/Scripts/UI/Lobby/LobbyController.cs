using System.Collections.Generic;

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로비 곡 캐러셀. 방향키(←/→)로 곡을 넘기고(wrap-around), 가운데 원이 현재 선택곡.
/// 선택곡을 미리듣기로 재생하고, Play 버튼으로 게임씬에 넘긴다.
/// </summary>
public sealed class LobbyController : MonoBehaviour
{
    [Header("캐러셀 원 (Image)")]
    [SerializeField] private Image centerImage;
    [SerializeField] private Image leftImage;
    [SerializeField] private Image rightImage;
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

    private void Start()
    {
        _songs.Clear();
        _songs.AddRange(SongCatalog.Songs);

        if (playButton != null)
            playButton.onClick.AddListener(StartGame);

        if (positionBar != null)
            positionBar.interactable = false;

        if (_songs.Count == 0)
        {
            if (titleText != null) titleText.text = "(곡 없음)";
            return;
        }

        _index = Mathf.Clamp(SongSelection.LastIndex, 0, _songs.Count - 1);
        ApplySelection();
        PlayPreview();
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
        int n = _songs.Count;
        _index = (_index + dir + n) % n;

        ApplySelection();
        PlayPreview();

        if (centerImage != null)
        {
            centerImage.transform.DOKill();
            centerImage.transform.localScale = Vector3.one;
            centerImage.transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f);
        }
    }

    private void ApplySelection()
    {
        int n = _songs.Count;
        ApplyCircle(centerImage, _songs[_index], 1f);
        ApplyCircle(leftImage, _songs[(_index - 1 + n) % n], 0.45f);
        ApplyCircle(rightImage, _songs[(_index + 1) % n], 0.45f);

        SongDataConfig current = _songs[_index];
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(current.SongName) ? "(제목 없음)" : current.SongName;
        if (highScoreText != null)
            highScoreText.text = "HIGH SCORE\n---";

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

        if (song.AlbumArt != null)
        {
            image.sprite = song.AlbumArt;
            image.color = new Color(dim, dim, dim, 1f);
        }
        else
        {
            image.sprite = circleSprite;
            Color c = song.ThemeColor * dim;
            c.a = 1f;
            image.color = c;
        }
    }

    private void PlayPreview()
    {
        if (previewSource == null)
            return;

        AudioClip clip = _songs[_index].SongAudioClip;
        if (clip == null)
        {
            previewSource.Stop();
            return;
        }

        previewSource.clip = clip;
        previewSource.loop = true;
        previewSource.volume = previewVolume;
        previewSource.time = 0f;
        previewSource.Play();
    }

    private void StartGame()
    {
        if (_songs.Count == 0)
            return;

        if (previewSource != null)
            previewSource.Stop();

        SongSelection.Selected = _songs[_index];
        SongSelection.LastIndex = _index;
        SceneFader.Load("GameScene");
    }
}
