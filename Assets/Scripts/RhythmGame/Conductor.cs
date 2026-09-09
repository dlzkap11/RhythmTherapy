using System;

using UnityEngine;

/// <summary>
/// 곡 재생 + "노래 재생 시간" 단일 공급원. 스폰/노트이동/입력 판정이 전부 이 시계를 기준으로 동작한다.
///
/// 1차 구현: AudioSource + AudioSettings.dspTime (PlayScheduled 로 시작 시점 고정).
/// dspTime 은 오디오 버퍼 단위로만 갱신되므로 그 사이는 실시간으로 보간한다(CurrentDspTime).
/// 추후 FMOD 로 교체 시 이 클래스 뒤만 바꾸면 되도록 캡슐화.
/// </summary>
public class Conductor : MonoBehaviour
{
    public static Conductor Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SongDataConfig songDataConfig;
    [SerializeField] private bool playOnStart = true;

    [Tooltip("오디오 출력 지연 등 사용자 오프셋 보정(ms). 설정창에서 조정 예정.")]
    [SerializeField] private double startOffsetMs = 0.0;

    private const double ScheduleLeadSeconds = 0.1;

    /// <summary>곡 시작 기준 현재 재생 시간(ms), 오프셋 보정 포함. 재생 전/예약시각 이전이면 음수/0.</summary>
    public double SongTimeMs
    {
        get
        {
            if (!IsPlaying) return 0.0;
            // 일시정지 중에는 시계가 멈춰 있으므로 보간 없이 얼어붙은 원시값을 그대로 쓴다.
            double now = IsPaused ? _pausedAtDspTime : CurrentDspTime();
            return (now - dspStartTime) * 1000.0 - startOffsetMs;
        }
    }

    /// <summary>초 단위 재생 시간 (기존 코드 호환).</summary>
    public double SongTime => SongTimeMs / 1000.0;

    

    /// <summary>
    /// 설정된 클립 길이(ms). 클립이 없으면 0. 곡 종료 시각 산출에 사용.
    /// songDataConfig 기준으로 조회 — PlayConfigured() 로 실제 재생이 시작되기 전
    /// (NoteSpawn.Start() 가 songEndMs 를 계산하는 시점)에도 값을 알 수 있어야 하므로,
    /// audioSource.clip(재생 시작 후에만 채워짐) 이 아니라 songDataConfig 를 우선 조회한다.
    /// </summary>
    public double ClipLengthMs
    {
        get
        {
            if (songDataConfig != null && songDataConfig.SongAudioClip != null)
                return songDataConfig.SongAudioClip.length * 1000.0;

            return audioSource != null && audioSource.clip != null ? audioSource.clip.length * 1000.0 : 0.0;
        }
    }

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }

    private double dspStartTime;
    // 일시정지 순간 값 두 벌. 표시용은 보간값(얼려도 화면이 튀지 않게), 재개 계산용은
    // 원시값(정지 시간을 원시끼리 빼야 정확히 상쇄된다).
    private double _pausedAtDspTime;
    private double _pausedAtRawDspTime;

    // dspTime 보간용. 마지막으로 값이 바뀐 순간의 dspTime 과 그때의 실시간을 짝지어 둔다.
    private double _lastRawDspTime;
    private double _lastDspSampleRealtime;
    private double _dspBufferSeconds;

    /// <summary>
    /// 보간된 dspTime(초).
    ///
    /// AudioSettings.dspTime 은 오디오 버퍼가 넘어갈 때만 갱신된다. 현재 설정(1024 샘플)에서는
    /// 약 21ms 계단이라, 120fps 기준 2~3 프레임 동안 같은 값이 나온다. 판정창(Perfect ±25ms)과
    /// 맞먹는 크기라 그대로 두면 입력 시각이 최대 한 버퍼만큼 낡은 채로 찍힌다.
    ///
    /// 그래서 dspTime 이 멈춰 있는 구간은 실시간 경과분으로 메운다. 다만 다음 버퍼 경계 너머로는
    /// 예측하지 않는다 — 그래야 실제 값이 도착했을 때 시간이 뒤로 가지 않는다(단조 증가 보장).
    /// </summary>
    private double CurrentDspTime()
    {
        double raw = AudioSettings.dspTime;
        double realtime = Time.realtimeSinceStartupAsDouble;

        if (raw != _lastRawDspTime)
        {
            _lastRawDspTime = raw;
            _lastDspSampleRealtime = realtime;
            return raw;
        }

        double extrapolated = _lastRawDspTime + (realtime - _lastDspSampleRealtime);
        if (_dspBufferSeconds <= 0.0)
            return extrapolated;

        return Math.Min(extrapolated, _lastRawDspTime + _dspBufferSeconds);
    }

    /// <summary>보간 기준점을 현재 시각으로 다시 맞춘다. 재생 시작·일시정지 해제 직후에 호출.</summary>
    private void ResyncDspClock()
    {
        _lastRawDspTime = AudioSettings.dspTime;
        _lastDspSampleRealtime = Time.realtimeSinceStartupAsDouble;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 보간 상한으로 쓸 버퍼 길이. 장치마다 다르므로 실제 설정에서 읽는다.
        AudioConfiguration audioConfig = AudioSettings.GetConfiguration();
        _dspBufferSeconds = audioConfig.sampleRate > 0
            ? (double)audioConfig.dspBufferSize / audioConfig.sampleRate
            : 0.0;
        ResyncDspClock();

        QualitySettings.vSyncCount = 0;          // vSync 끄고
        Application.targetFrameRate = 120;       // 명시적 타겟 (모니터 주사율 이상 권장)

        // 로비에서 곡을 골라 넘어왔으면 그 곡을 재생 대상으로 사용.
        if (SongSelection.Selected != null)
            songDataConfig = SongSelection.Selected;
    }

    private void Start()
    {
        // 재생 전 노트 삽입이 끝나야 하므로(architecture.md §6), NoteSpawn 이 준비를 마친 뒤
        // PlayConfigured() 를 호출해 재생을 시작한다. playOnStart 는 NoteSpawn 이 없는
        // 상황(단독 테스트 등)을 대비한 폴백으로만 남겨둔다.
        if (playOnStart && FindAnyObjectByType<NoteSpawn>() == null)
            PlayConfigured();
    }

    /// <summary>재생 시작 전 곡 지정. 로비를 안 거치고 GameScene 을 직접 실행하는 테스트에서 NoteSpawn 이 호출.</summary>
    public void SetSong(SongDataConfig config)
    {
        if (!IsPlaying && config != null)
            songDataConfig = config;
    }

    /// <summary>인스펙터에 지정된 songDataConfig 클립으로 재생 시작. NoteSpawn 이 노트 삽입 후 호출.</summary>
    public void PlayConfigured()
    {
        if (IsPlaying)
            return;

        if (songDataConfig != null && songDataConfig.SongAudioClip != null)
            Play(songDataConfig.SongAudioClip, startOffsetMs, songDataConfig.SongVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play(AudioClip clip, double offsetMs, float volume)
    {
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);
        dspStartTime = AudioSettings.dspTime + ScheduleLeadSeconds;
        audioSource.PlayScheduled(dspStartTime);

        startOffsetMs = offsetMs;
        IsPlaying = true;
        ResyncDspClock();
    }

    public void Pause()
    {
        if(IsPlaying && !IsPaused && SongTimeMs > 0)
        {
            audioSource.Pause();
            _pausedAtDspTime = CurrentDspTime();          // 화면에 보일 시각 — 보간값 그대로 얼린다
            _pausedAtRawDspTime = AudioSettings.dspTime;  // 재개 보정용 — 원시값
            IsPaused = true;
        }
    }

    public void Resume()
    {
        if(IsPlaying && IsPaused)
        {
            dspStartTime += AudioSettings.dspTime - _pausedAtRawDspTime;
            audioSource.UnPause();
            IsPaused = false;
            ResyncDspClock();
        }
    }

    public void Stop()
    {
        audioSource.Stop();
        IsPlaying = false;
    }
}
