using UnityEngine;

/// <summary>
/// 곡 재생 + "노래 재생 시간" 단일 공급원. 스폰/노트이동/입력 판정이 전부 이 시계를 기준으로 동작한다.
///
/// 1차 구현: AudioSource + AudioSettings.dspTime (PlayScheduled 로 시작 시점 고정).
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
    public double SongTimeMs =>
        IsPlaying ? (AudioSettings.dspTime - dspStartTime) * 1000.0 - startOffsetMs : 0.0;

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

    private double dspStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 재생 전 노트 삽입이 끝나야 하므로(architecture.md §6), NoteSpawn 이 준비를 마친 뒤
        // PlayConfigured() 를 호출해 재생을 시작한다. playOnStart 는 NoteSpawn 이 없는
        // 상황(단독 테스트 등)을 대비한 폴백으로만 남겨둔다.
        if (playOnStart && FindAnyObjectByType<NoteSpawn>() == null)
            PlayConfigured();
    }

    /// <summary>인스펙터에 지정된 songDataConfig 클립으로 재생 시작. NoteSpawn 이 노트 삽입 후 호출.</summary>
    public void PlayConfigured()
    {
        if (IsPlaying)
            return;

        if (songDataConfig != null && songDataConfig.SongAudioClip != null)
            Play(songDataConfig.SongAudioClip, startOffsetMs);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play(AudioClip clip, double offsetMs)
    {
        audioSource.clip = clip;
        dspStartTime = AudioSettings.dspTime + ScheduleLeadSeconds;
        audioSource.PlayScheduled(dspStartTime);

        startOffsetMs = offsetMs;
        IsPlaying = true;
    }

    public void Stop()
    {
        audioSource.Stop();
        IsPlaying = false;
    }
}
