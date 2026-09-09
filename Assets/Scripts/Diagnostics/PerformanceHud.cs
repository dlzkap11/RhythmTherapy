using System;
using System.Text;

using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RhythmTherapy.Diagnostics
{
    /// <summary>
    /// 화면 좌상단에 프레임타임 / FPS / 1% low / 최대 스파이크 / 프레임당 GC 할당을 표시하는 성능 오버레이.
    /// <c>@Diagnostics</c> 싱글턴으로 부트스트랩되고 빌드에서도 동작한다.
    ///
    /// - <b>F3</b> : HUD 표시 토글
    /// - <b>F4</b> : 콘솔 요약 로그 토글 (2초 간격, Player.log 로 남음)
    /// - 실행 인자 <c>-perftest</c> : vSync/프레임캡 해제 + GameScene 자동 로드 + 로그 ON +
    ///   <see cref="PerfTestQuitSeconds"/> 후 자동 종료. 헤드리스 성능 측정용.
    ///
    /// GC 할당량은 development build / 에디터에서만 유효(프로파일러 카운터). 릴리스 빌드에선 0으로 표시된다.
    /// </summary>
    public sealed class PerformanceHud : MonoBehaviour
    {
        private const int SampleCount = 300;              // 통계 창(최근 N 프레임)
        private const float StatsIntervalSeconds = 0.25f; // 통계 재계산 주기
        private const float LogIntervalSeconds = 2f;
        private const float PerfTestQuitSeconds = 75f;
        private const int GraphWidth = 160;               // 프레임타임 그래프 폭(px = 표본 수)

        private static bool _perfTestMode;

        /// <summary>-perftest 로 실행 중인지. GameManager 가 HP 즉사(입력 없음)를 막는 데 참조.</summary>
        public static bool PerfTestActive => _perfTestMode;

        private readonly float[] _frameMs = new float[SampleCount];
        private int _cursor;
        private int _filled;

        private ProfilerRecorder _gcRecorder;
        private long _gcLastFrameBytes;

        private bool _show = true;
        private bool _log;
        private float _statsTimer;
        private float _logTimer;
        private float _elapsed;
        private bool _perfTestSceneRequested;

        private float _avgMs, _maxMs, _p99Ms;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private Texture2D _pixel;
        private readonly StringBuilder _sb = new StringBuilder(256);

        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-perftest", StringComparison.OrdinalIgnoreCase))
                    _perfTestMode = true;

                // -song Song_BeltConveyor : GameScene 자동 로드 전에 그 곡을 선택곡으로 지정.
                if (string.Equals(args[i], "-song", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    SongDataConfig cfg = Resources.Load<SongDataConfig>("SongData/" + args[i + 1]);
                    if (cfg != null)
                    {
                        SongSelection.Selected = cfg;
                        Debug.Log($"[Perf] -song {cfg.SongName} (notes {cfg.NoteDatas.Count})");
                    }
                    else
                    {
                        Debug.LogWarning($"[Perf] -song: Resources/SongData/{args[i + 1]} 없음");
                    }
                }
            }

            GameObject go = new GameObject("@Diagnostics");
            go.AddComponent<PerformanceHud>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            if (_perfTestMode)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;   // 캡 해제 → CPU 헤드룸 측정
                _log = true;
            }
        }

        private void OnDestroy()
        {
            if (_gcRecorder.Valid)
                _gcRecorder.Dispose();

            if (_pixel != null)
                Destroy(_pixel);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float ms = dt * 1000f;

            _frameMs[_cursor] = ms;
            _cursor = (_cursor + 1) % SampleCount;
            if (_filled < SampleCount)
                _filled++;

            if (_gcRecorder.Valid)
                _gcLastFrameBytes = _gcRecorder.LastValue;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f3Key.wasPressedThisFrame) _show = !_show;
                if (kb.f4Key.wasPressedThisFrame) _log = !_log;
            }

            _statsTimer += dt;
            if (_statsTimer >= StatsIntervalSeconds)
            {
                _statsTimer = 0f;
                RecomputeStats();
            }

            _elapsed += dt;

            if (_perfTestMode && !_perfTestSceneRequested && _elapsed >= 0.5f)
            {
                _perfTestSceneRequested = true;
                SceneManager.LoadScene("GameScene");
            }

            if (_log)
            {
                _logTimer += dt;
                if (_logTimer >= LogIntervalSeconds)
                {
                    _logTimer = 0f;
                    Debug.Log(BuildSummary());
                }
            }

            if (_perfTestMode && _elapsed >= PerfTestQuitSeconds)
            {
                Debug.Log("[Perf] perftest done — " + BuildSummary());
                Quit();
            }
        }

        private void RecomputeStats()
        {
            int n = _filled;
            if (n == 0)
                return;

            float sum = 0f;
            float max = 0f;
            float[] sorted = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = _frameMs[i];
                sorted[i] = v;
                sum += v;
                if (v > max)
                    max = v;
            }

            Array.Sort(sorted);
            int p99 = Mathf.Clamp(Mathf.CeilToInt(n * 0.99f) - 1, 0, n - 1);

            _avgMs = sum / n;
            _maxMs = max;
            _p99Ms = sorted[p99];   // 1% low: 가장 느린 1% 프레임의 프레임타임
        }

        private string BuildSummary()
        {
            _sb.Clear();
            _sb.Append("[Perf] t=").Append(_elapsed.ToString("F0")).Append("s  ")
               .Append("avg=").Append(_avgMs.ToString("F2")).Append("ms (")
               .Append((1000f / Mathf.Max(_avgMs, 0.001f)).ToString("F0")).Append("fps)  ")
               .Append("1%low=").Append(_p99Ms.ToString("F2")).Append("ms (")
               .Append((1000f / Mathf.Max(_p99Ms, 0.001f)).ToString("F0")).Append("fps)  ")
               .Append("max=").Append(_maxMs.ToString("F2")).Append("ms  ")
               .Append("gc/frame=").Append((_gcLastFrameBytes / 1024f).ToString("F1")).Append("KB  ")
               .Append("vSync=").Append(QualitySettings.vSyncCount)
               .Append(" target=").Append(Application.targetFrameRate);
            return _sb.ToString();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnGUI()
        {
            if (!_show)
                return;

            EnsureStyles();

            float cur = _filled > 0 ? _frameMs[(_cursor - 1 + SampleCount) % SampleCount] : 0f;

            _sb.Clear();
            _sb.Append("cur   ").Append(cur.ToString("F2")).Append(" ms  (").Append((1000f / Mathf.Max(cur, 0.001f)).ToString("F0")).Append(" fps)\n");
            _sb.Append("avg   ").Append(_avgMs.ToString("F2")).Append(" ms  (").Append((1000f / Mathf.Max(_avgMs, 0.001f)).ToString("F0")).Append(" fps)\n");
            _sb.Append("1% low ").Append(_p99Ms.ToString("F2")).Append(" ms  (").Append((1000f / Mathf.Max(_p99Ms, 0.001f)).ToString("F0")).Append(" fps)\n");
            _sb.Append("max   ").Append(_maxMs.ToString("F2")).Append(" ms\n");
            _sb.Append("GC/frame ").Append((_gcLastFrameBytes / 1024f).ToString("F1")).Append(" KB\n");
            _sb.Append("vSync ").Append(QualitySettings.vSyncCount).Append("  target ").Append(Application.targetFrameRate).Append("\n");
            _sb.Append("[F3] hide   [F4] log ").Append(_log ? "ON" : "OFF");

            const float w = 260f;
            const float h = 150f;
            GUI.Box(new Rect(8f, 8f, w, h), GUIContent.none, _boxStyle);
            GUI.Label(new Rect(16f, 12f, w - 16f, h - 40f), _sb.ToString(), _labelStyle);

            DrawGraph(new Rect(16f, 12f + h - 34f, GraphWidth, 24f));
        }

        private void DrawGraph(Rect area)
        {
            int n = Mathf.Min(_filled, GraphWidth);
            if (n < 2)
                return;

            // 16.7ms(60fps) 기준선 대비 상대 높이. 33ms 이상은 클램프.
            const float refMs = 16.7f;
            for (int i = 0; i < n; i++)
            {
                int idx = (_cursor - n + i + SampleCount) % SampleCount;
                float frac = Mathf.Clamp01(_frameMs[idx] / (refMs * 2f));
                float barH = frac * area.height;
                Color c = _frameMs[idx] > refMs * 1.5f ? new Color(1f, 0.4f, 0.3f)
                        : _frameMs[idx] > refMs ? new Color(1f, 0.85f, 0.3f)
                        : new Color(0.4f, 0.9f, 0.5f);
                GUI.color = c;
                GUI.DrawTexture(new Rect(area.x + i, area.yMax - barH, 1f, barH), _pixel);
            }
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _pixel.SetPixel(0, 0, Color.white);
                _pixel.Apply();
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                Texture2D bg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
                bg.Apply();
                _boxStyle.normal.background = bg;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.UpperLeft,
                    richText = false,
                };
                _labelStyle.normal.textColor = Color.white;
            }
        }
    }
}
