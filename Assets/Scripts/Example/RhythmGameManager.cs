// RhythmGameManager.cs
// 게임 전체 흐름(상태머신), 노트 스폰/이동/판정, 입력 처리 통합.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TMP_Text = TMPro.TMP_Text;

namespace PixelBeat
{
    public enum GameState { Ready, Playing, Ended }

    public class RhythmGameManager : MonoBehaviour
    {
        [Header("Config")]
        public RhythmGameConfig config;
        public RhythmChart chart;

        [Header("UI References")]
        public RectTransform notesLayer;            // 노트들이 생성되는 부모 (판정선 포함 영역)
        public RectTransform judgeLine;
        public NoteController notePrefab;
        public LaneController[] lanes;              // laneCount 개수만큼
        public Image judgeLineImage;

        [Header("Sub-Controllers")]
        public HUDController hud;
        public HealthBarController hpBar;
        public SongProgressController songProgress;
        public JudgmentTextController judgmentText;
        public OverlayController overlay;
        public BlipPlayer blipPlayer;

        private GameState _state = GameState.Ready;
        private float _elapsed;
        private float _nextBeatTime;
        private float _beatInterval;

        // 통계
        private int _score, _combo, _maxCombo, _perfect, _great, _miss;
        private readonly List<NoteController> _activeNotes = new List<NoteController>();

        // 입력
        private bool[] _laneKeyDown;

        private void Awake()
        {
            _laneKeyDown = new bool[config.laneCount];
            _beatInterval = 60f / config.bpm;
            _nextBeatTime = 0f;
        }

        private void Start()
        {
            // 챠트가 없으면 절차적 생성
            if (chart == null)
                chart = RhythmChart.Generate(config.songLength, config.bpm, config.laneCount);

            // 판정선 위치 동기화
            if (judgeLine != null)
                judgeLine.anchoredPosition = new Vector2(judgeLine.anchoredPosition.x, config.judgeLineY);

            // 레인 시각 설정
            int laneCount = Mathf.Min(config.laneCount, lanes != null ? lanes.Length : 0);
            for (int i = 0; i < laneCount; i++)
                if (lanes[i] != null) lanes[i].Configure(i, config);

            hpBar.Init(config.maxHealth);
            hud.Init(chart.TotalCount);
            songProgress.SetSongName(config.songName);
            songProgress.Refresh(0f, config.songLength);

            ShowStart();
        }

        private void Update()
        {
            if (_state != GameState.Playing) return;

            HandleInput();

            _elapsed += Time.deltaTime;

            SpawnNotes();
            UpdateNotes();
            UpdateBeats();

            songProgress.Refresh(_elapsed, config.songLength);

            if (_elapsed >= config.songLength)
                EndGame(true);

            if (hpBar.IsDead)
                EndGame(false);
        }

        // ===== State =====

        private void ShowStart()
        {
            _state = GameState.Ready;
            overlay.ShowStart(StartGame);
        }

        private void StartGame()
        {
            ResetState();
            _state = GameState.Playing;
            overlay.HideAll();
            _elapsed = 0f;
        }

        private void EndGame(bool win)
        {
            _state = GameState.Ended;
            float acc = (_perfect + _great) == 0 ? 0f : (_perfect + _great * 0.5f) / Mathf.Max(1, chart.TotalCount) * 100f;
            overlay.ShowResult(win, _score, _perfect, _great, _miss, _maxCombo, acc, StartGame);
        }

        private void ResetState()
        {
            foreach (var n in _activeNotes)
                if (n != null) Destroy(n.gameObject);
            _activeNotes.Clear();

            chart.ResetRuntimeFlags();

            _score = _combo = _maxCombo = _perfect = _great = _miss = 0;
            _elapsed = 0f;
            _nextBeatTime = 0f;
            hpBar.Init(config.maxHealth);
            hud.Init(chart.TotalCount);
            songProgress.Refresh(0f, config.songLength);
        }

        // ===== Notes =====

        private void SpawnNotes()
        {
            // 스폰 시점 = 판정시각 - (레이어 높이 / 속도)
            float layerHeight = notesLayer.rect.height;
            float spawnLead = layerHeight / config.noteSpeed;

            for (int i = 0; i < chart.notes.Count; i++)
            {
                var data = chart.notes[i];
                if (data.hit || data.missed) continue;
                if (data.spawned) continue;

                if (_elapsed >= data.time - spawnLead && _elapsed < data.time + 0.4f)
                {
                    var note = Instantiate(notePrefab, notesLayer);
                    Color c = config.laneColors[data.lane];
                    note.Setup(data, config, layerHeight, config.laneWidth, c);
                    _activeNotes.Add(note);
                    data.spawned = true;
                }
            }
        }

        private void UpdateNotes()
        {
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                if (note == null) { _activeNotes.RemoveAt(i); continue; }

                note.UpdatePosition(_elapsed);

                // 미스 처리: 판정선을 넘어 greatWindow 초과
                if (!note.IsReleased && note.IsPastJudgeLine(_elapsed))
                {
                    note.MarkMissed();
                    OnMiss(note.Data);
                    note.Release();
                    _activeNotes.RemoveAt(i);
                }
            }
        }

        private void UpdateBeats()
        {
            if (blipPlayer == null) return;
            while (_elapsed >= _nextBeatTime)
            {
                blipPlayer.PlayBeat(config.bpm);
                _nextBeatTime += _beatInterval;
            }
        }

        // ===== Input & Judgment =====

        private void HandleInput()
        {
            int keyCount = Mathf.Min(config.laneCount, config.laneKeys != null ? config.laneKeys.Length : 0);
            for (int i = 0; i < keyCount; i++)
            {
                bool down = Input.GetKeyDown(config.laneKeys[i]);
                if (down)
                {
                    FlashLane(i);
                    TryHitLane(i);
                }
            }
        }

        private void TryHitLane(int lane)
        {
            // 해당 레인에서 판정 윈도우 내 가장 가까운 미처리 노트 탐색
            NoteController best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                if (note == null || note.IsReleased) continue;
                if (note.Data.lane != lane) continue;

                float dist = Mathf.Abs(note.Data.time - _elapsed);
                if (dist < bestDist && dist <= config.greatWindow)
                {
                    best = note;
                    bestDist = dist;
                }
            }

            if (best == null)
            {
                if (blipPlayer != null) blipPlayer.PlayTone(180f, 0.4f, 0.08f);
                return;
            }

            JudgeRank rank = bestDist <= config.perfectWindow ? JudgeRank.Perfect : JudgeRank.Great;
            best.MarkHit();
            OnJudge(rank, best.Data);
            best.Release();
            _activeNotes.Remove(best);
        }

        private void FlashLane(int lane)
        {
            if (lanes == null || lane >= lanes.Length || lanes[lane] == null) return;
            lanes[lane].Flash(config.laneColors[lane]);
        }

        // ===== Result handlers =====

        private void OnJudge(JudgeRank rank, NoteData note)
        {
            switch (rank)
            {
                case JudgeRank.Perfect:
                    _perfect++;
                    _score += config.perfectScore + Mathf.FloorToInt(_combo * config.comboMultiplier);
                    if (blipPlayer != null) blipPlayer.PlayTone(660f, 1f, 0.12f);
                    break;
                case JudgeRank.Great:
                    _great++;
                    _score += config.greatScore + Mathf.FloorToInt(_combo * (config.comboMultiplier * 0.6f));
                    if (blipPlayer != null) blipPlayer.PlayTone(440f, 0.8f, 0.1f);
                    break;
            }
            _combo++;
            if (_combo > _maxCombo) _maxCombo = _combo;
            judgmentText.Show(rank);
            RefreshHUD();
        }

        private void OnMiss(NoteData note)
        {
            _miss++;
            _combo = 0;
            hpBar.ApplyDelta(-config.missHealthLoss);
            judgmentText.Show(JudgeRank.Miss);
            if (blipPlayer != null) blipPlayer.PlayTone(120f, 0.5f, 0.06f);
            RefreshHUD();
        }

        private void RefreshHUD()
        {
            hud.Refresh(_score, _combo, _perfect, _great);
        }
    }
}
