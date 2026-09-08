using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace RhythmTherapy.EditorTools
{
    /// <summary>
    /// Song_BeltConveyor 의 임시 채보를 코드로 생성해 에셋에 써 넣는다. 채보 에디터가 나오기 전까지
    /// 실제 게임에서 쓸 손채보 대용 + 성능 테스트용 고밀도 구간 포함.
    ///
    /// 곡 길이 133.44s → 마지막 노트는 ~129s 안쪽(songEndMs = last + SongEndTailMs 1.5s < 133s).
    /// 레인은 씬 구성상 2개 고정. 밀도는 간격으로만 조절한다.
    /// </summary>
    public static class BeltConveyorChartGen
    {
        private const string AssetPath = "Assets/Resources/SongData/Song_BeltConveyor.asset";

        [MenuItem("RhythmTherapy/Charts/Belt Conveyor — Normal (playable + bursts)")]
        public static void GenerateNormal() => Write(BuildNormal());

        [MenuItem("RhythmTherapy/Charts/Belt Conveyor — Heavy (perf stress)")]
        public static void GenerateHeavy() => Write(BuildHeavy());

        [MenuItem("RhythmTherapy/Charts/Belt Conveyor — Clear notes")]
        public static void ClearNotes() => Write(new List<NoteData>());

        private static void Write(List<NoteData> notes)
        {
            SongDataConfig cfg = AssetDatabase.LoadAssetAtPath<SongDataConfig>(AssetPath);
            if (cfg == null)
            {
                Debug.LogError($"[BeltConveyorChartGen] {AssetPath} 를 찾을 수 없습니다.");
                return;
            }

            notes.Sort((a, b) => a.HitTimeMS.CompareTo(b.HitTimeMS));
            cfg.NoteDatas = notes;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();

            int last = notes.Count > 0 ? notes[notes.Count - 1].HitTimeMS : 0;
            Debug.Log($"[BeltConveyorChartGen] {notes.Count} notes → {AssetPath} " +
                      $"(마지막 판정시간 {last}ms, 최대 동시노트 {PeakConcurrency(notes, 1900)})");
        }

        private static NoteData Tap(int ms, int lane) =>
            new NoteData { type = NoteType.Tap, lane = lane & 1, HitTimeMS = ms, Duration = 0 };

        /// <summary>startMs~endMs 를 stepMs 간격으로 채우고 레인을 번갈아 준다.</summary>
        private static void Run(List<NoteData> o, int startMs, int endMs, int stepMs, int laneStart = 0)
        {
            int i = laneStart;
            for (int t = startMs; t < endMs; t += stepMs)
                o.Add(Tap(t, i++));
        }

        /// <summary>startMs 부터 count 개를 stepMs 간격으로. dbl 이면 매 타이밍마다 양 레인 동시.</summary>
        private static void Wall(List<NoteData> o, int startMs, int count, int stepMs, bool dbl)
        {
            for (int k = 0; k < count; k++)
            {
                int t = startMs + k * stepMs;
                if (dbl)
                {
                    o.Add(Tap(t, 0));
                    o.Add(Tap(t, 1));
                }
                else
                {
                    o.Add(Tap(t, k));
                }
            }
        }

        /// <summary>실제로 칠 수 있는 채보. 3곳에 버스트 구간(벽)을 심어 성능도 같이 본다.</summary>
        private static List<NoteData> BuildNormal()
        {
            List<NoteData> o = new List<NoteData>();

            Run(o, 3000, 20000, 400);            // 인트로 4분음
            Run(o, 20000, 35000, 200);           // 벌스 8분음

            Wall(o, 35000, 22, 180, false);      // 빌드업
            Wall(o, 35000, 8, 420, true);        // + 4분 더블 (BURST WALL 1)

            Run(o, 40000, 58000, 200);
            Run(o, 58000, 65000, 100);           // 스트림 (지속 최고밀도)

            Wall(o, 65000, 26, 110, false);      // BURST WALL 2
            Wall(o, 65500, 6, 420, true);

            Run(o, 68000, 90000, 300);           // 브릿지 (숨 고르기)
            Run(o, 90000, 110000, 150);          // 빌드

            Wall(o, 110000, 55, 110, false);     // 클라이맥스 (BURST WALL 3)
            Wall(o, 110000, 12, 500, true);

            Run(o, 120000, 126000, 200);         // 아웃트로 감소
            Run(o, 126000, 129000, 400);

            return o;
        }

        /// <summary>순수 성능 스트레스용. 곡 전체를 30ms 스트림 + 200ms 4겹 벽으로 도배. 플레이 불가.</summary>
        private static List<NoteData> BuildHeavy()
        {
            List<NoteData> o = new List<NoteData>();

            for (int t = 2000; t < 128000; t += 30)
                o.Add(Tap(t, t / 30));

            for (int t = 2000; t < 128000; t += 200)
            {
                o.Add(Tap(t, 0));
                o.Add(Tap(t, 1));
                o.Add(Tap(t + 45, 0));
                o.Add(Tap(t + 45, 1));
            }

            return o;
        }

        /// <summary>정렬된 노트 목록에서 windowMs 창 안에 동시에 들어오는 노트 최대 수(풀 크기 산정과 동일 계산).</summary>
        private static int PeakConcurrency(List<NoteData> notes, int windowMs)
        {
            notes.Sort((a, b) => a.HitTimeMS.CompareTo(b.HitTimeMS));
            int peak = 0;
            int start = 0;
            for (int end = 0; end < notes.Count; end++)
            {
                while (notes[end].HitTimeMS - notes[start].HitTimeMS > windowMs)
                    start++;
                peak = Mathf.Max(peak, end - start + 1);
            }
            return peak;
        }
    }
}
