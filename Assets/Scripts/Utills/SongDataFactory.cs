using System.Collections.Generic;
using UnityEngine;

public static class SongDataFactory
{
    /// <summary>
    /// 간단한 테스트용 SongData 생성
    /// </summary>
    public static SongData CreateTestSong(
        int songId = 1,
        string songName = "Test Song",
        string fileName = "test_song.ogg",
        string albumName = "Test Album",
        int laneCount = 2,
        float bpm = 120f,
        int measureCount = 8)
    {
        SongData song = new SongData
        {
            SongID = songId,
            SongName = songName,
            SongFileName = fileName,
            SongAlbumName = albumName,
            NoteDatas = new List<NoteData>()
        };

        // BPM 기반 노트 생성
        double beatMs = 60000.0 / bpm;  // 1박의 밀리초

        int noteIndex = 0;
        for (int measure = 0; measure < measureCount; measure++)
        {
            for (int beat = 0; beat < 4; beat++)
            {
                double beatTimeMs =
                    (measure * 4 + beat) * beatMs;

                // 1박마다 단타 생성 (교대로)
                if (noteIndex % 2 == 0)
                {
                    song.NoteDatas.Add(CreateTapNote(
                        lane: 0,
                        hitTimeMs: (int)beatTimeMs));
                }
                else
                {
                    song.NoteDatas.Add(CreateTapNote(
                        lane: 1,
                        hitTimeMs: (int)beatTimeMs));
                }

                noteIndex++;

                // 2박마다 롱노트 생성
                if (beat % 2 == 0 && measure > 0)
                {
                    song.NoteDatas.Add(CreateHoldNote(
                        lane: (measure % 2),
                        hitTimeMs: (int)beatTimeMs,
                        durationMs: (int)(beatMs * 2)));  // 2박 길이
                }
            }
        }

        // 노트 정렬
        song.NoteDatas.Sort(
            (a, b) => a.HitTimeMS.CompareTo(b.HitTimeMS));

        return song;
    }

    /// <summary>
    /// 단타 노트 생성
    /// </summary>
    private static NoteData CreateTapNote(int lane, int hitTimeMs)
    {
        return new NoteData
        {
            type = NoteType.Tap,
            lane = lane,
            HitTimeMS = hitTimeMs,
            Duration = 0
        };
    }

    /// <summary>
    /// 롱노트 생성
    /// </summary>
    private static NoteData CreateHoldNote(
        int lane,
        int hitTimeMs,
        int durationMs)
    {
        return new NoteData
        {
            type = NoteType.Hold,
            lane = lane,
            HitTimeMS = hitTimeMs,
            Duration = durationMs
        };
    }

    /// <summary>
    /// 패턴 테스트용 (간단한 4비트)
    /// </summary>
    public static SongData CreateSimplePattern(
        string songName = "Simple Pattern")
    {
        SongData song = new SongData
        {
            SongID = 999,
            SongName = songName,
            SongFileName = "test.ogg",
            SongAlbumName = "Test",
            NoteDatas = new List<NoteData>()
        };

        // 1초마다 교대로 단타
        song.NoteDatas.Add(CreateTapNote(0, 1000));
        song.NoteDatas.Add(CreateTapNote(1, 2000));
        song.NoteDatas.Add(CreateTapNote(0, 3000));
        song.NoteDatas.Add(CreateTapNote(1, 4000));

        // 5초에 롱노트
        song.NoteDatas.Add(CreateHoldNote(0, 5000, 2000));

        // 8초에 연타
        song.NoteDatas.Add(CreateTapNote(0, 8000));
        song.NoteDatas.Add(CreateTapNote(1, 8250));
        song.NoteDatas.Add(CreateTapNote(0, 8500));
        song.NoteDatas.Add(CreateTapNote(1, 8750));

        return song;
    }

    /// <summary>
    /// BPM 변화 테스트용
    /// </summary>
    public static SongData CreateBpmTestSong(float bpm = 120f)
    {
        SongData song = new SongData
        {
            SongID = 888,
            SongName = $"BPM Test {bpm}",
            SongFileName = "test.ogg",
            SongAlbumName = "Test",
            NoteDatas = new List<NoteData>()
        };

        double beatMs = 60000.0 / bpm;

        // 16박 생성
        for (int i = 0; i < 16; i++)
        {
            int timeMs = (int)(i * beatMs);
            int lane = i % 2;

            song.NoteDatas.Add(CreateTapNote(lane, timeMs));
        }

        return song;
    }

    /// <summary>
    /// 랜덤 노트 생성 (테스트용)
    /// </summary>
    public static SongData CreateRandomSong(
        int songId,
        string songName,
        int noteCount = 100,
        int laneCount = 2,
        float bpm = 120f,
        int startDelayMs = 2000)
    {
        SongData song = new SongData
        {
            SongID = songId,
            SongName = songName,
            SongFileName = "test.ogg",
            SongAlbumName = "Test",
            NoteDatas = new List<NoteData>()
        };

        double beatMs = 60000.0 / bpm;
        System.Random random = new System.Random();

        for (int i = 0; i < noteCount; i++)
        {
            int measure = i / 4;
            int beat = i % 4;

            // 약간의 랜덤 오프셋 (±50ms)
            int offset = random.Next(-50, 51);
            int timeMs = (int)(
                (measure * 4 + beat) * beatMs) + startDelayMs + offset;

            int lane = random.Next(0, laneCount);
            song.NoteDatas.Add(
                    CreateTapNote(lane, timeMs));

            /*
            // 20% 확률로 롱노트
            if (random.Next(0, 100) < 20)
            {
                int duration = (int)(beatMs * random.Next(1, 4));
                song.NoteDatas.Add(
                    CreateHoldNote(lane, timeMs, duration));
            }
            else
            {
                song.NoteDatas.Add(
                    CreateTapNote(lane, timeMs));
            }
            */
        }

        // 정렬
        song.NoteDatas.Sort(
            (a, b) => a.HitTimeMS.CompareTo(b.HitTimeMS));

        return song;
    }
}