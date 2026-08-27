// RhythmChart.cs
// 챠트 데이터 구조 + 절차적 챠트 생성기.
using System.Collections.Generic;
using UnityEngine;

namespace PixelBeat
{
    [System.Serializable]
    public class NoteData
    {
        public float time;     // 판정 시점 (초, 곡 시작 기준)
        public int lane;       // 0 ~ laneCount-1
        public bool hit;
        public bool missed;
        public bool spawned;   // 화면에 스폰되었는지

        public NoteData(float time, int lane)
        {
            this.time = time;
            this.lane = lane;
        }
    }

    [CreateAssetMenu(fileName = "RhythmChart", menuName = "PixelBeat/RhythmChart")]
    public class RhythmChart : ScriptableObject
    {
        public List<NoteData> notes = new List<NoteData>();

        /// <summary> 곡 길이와 BPM 기반으로 절차적 챠트 생성 (데모용) </summary>
        public static RhythmChart Generate(float songLength, float bpm, int laneCount)
        {
            var chart = ScriptableObject.CreateInstance<RhythmChart>();
            float beat = 60f / bpm;
            float t = 2f; // lead-in
            float density = 0.55f;

            while (t < songLength - 1f)
            {
                if (Random.value < density)
                {
                    var lanes = new List<int>();
                    for (int i = 0; i < laneCount; i++) lanes.Add(i);
                    int count = (t > 30f && Random.value < 0.22f) ? 2 : 1;
                    for (int i = 0; i < count; i++)
                    {
                        int idx = Random.Range(0, lanes.Count);
                        chart.notes.Add(new NoteData(t, lanes[idx]));
                        lanes.RemoveAt(idx);
                    }
                }

                if (Random.value < 0.3f)
                {
                    int l = Random.Range(0, laneCount);
                    chart.notes.Add(new NoteData(t + beat * 0.5f, l));
                }

                t += beat;
                density = Mathf.Min(0.85f, density + 0.004f);
            }

            chart.notes.Sort((a, b) => a.time.CompareTo(b.time));
            return chart;
        }

        public int TotalCount => notes.Count;

        public void ResetRuntimeFlags()
        {
            foreach (var n in notes)
            {
                n.hit = false;
                n.missed = false;
                n.spawned = false;
            }
        }
    }
}
