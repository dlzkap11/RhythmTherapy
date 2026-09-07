using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace RhythmTherapy.Core
{
    /// <summary>
    /// 곡별 점수 기록을 JSON 파일 하나로 영속화하는 저장소.
    /// 기본 경로는 <c>Application.persistentDataPath/scores.json</c>이며, 테스트를 위해 경로를 주입할 수 있다.
    ///
    /// JsonUtility는 최상위 배열/Dictionary를 직렬화하지 못하므로 <see cref="ScoreDatabase"/> 래퍼로 감싼다.
    /// 조회 성능을 위해 로드 시 곡 id 기준 딕셔너리로 캐시한다.
    /// </summary>
    public sealed class JsonFileScoreRepository : IScoreRepository
    {
        /// <summary>스키마가 바뀌면 올린다. Load 시 값이 다르면 마이그레이션 분기 지점.</summary>
        private const int CurrentSchemaVersion = 1;

        private readonly string _filePath;
        private readonly Dictionary<int, SongScoreRecord> _cache = new Dictionary<int, SongScoreRecord>();

        /// <param name="filePath">
        /// 저장 파일 경로. null이면 <c>Application.persistentDataPath/scores.json</c>를 쓴다.
        /// </param>
        public JsonFileScoreRepository(string filePath = null)
        {
            _filePath = string.IsNullOrEmpty(filePath)
                ? Path.Combine(Application.persistentDataPath, "scores.json")
                : filePath;
        }

        /// <summary>실제 사용 중인 파일 경로. 진단·로그용이며 저장 로직은 이 값에만 의존한다.</summary>
        public string FilePath => _filePath;

        public void Load()
        {
            _cache.Clear();

            if (!File.Exists(_filePath))
                return;

            ScoreDatabase db = null;
            try
            {
                string json = File.ReadAllText(_filePath);
                db = JsonUtility.FromJson<ScoreDatabase>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonFileScoreRepository] 기록 파일을 읽지 못해 빈 상태로 시작합니다: {e.Message}");
                return;
            }

            if (db == null || db.records == null)
            {
                Debug.LogWarning("[JsonFileScoreRepository] 기록 파일 형식이 올바르지 않아 빈 상태로 시작합니다.");
                return;
            }

            if (db.schemaVersion != CurrentSchemaVersion)
                Debug.LogWarning($"[JsonFileScoreRepository] 스키마 버전 불일치(file={db.schemaVersion}, app={CurrentSchemaVersion}).");

            foreach (SongScoreRecord record in db.records)
            {
                if (record != null)
                    _cache[record.songId] = record;
            }
        }

        public void Save()
        {
            ScoreDatabase db = new ScoreDatabase
            {
                schemaVersion = CurrentSchemaVersion,
                records = new List<SongScoreRecord>(_cache.Values),
            };

            string json = JsonUtility.ToJson(db, prettyPrint: true);
            string tempPath = _filePath + ".tmp";

            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(tempPath, json);

                // 쓰기 도중 크래시나도 원본이 남도록 임시 파일에 먼저 쓰고 원자적으로 교체한다.
                if (File.Exists(_filePath))
                    File.Replace(tempPath, _filePath, null);
                else
                    File.Move(tempPath, _filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonFileScoreRepository] 기록 저장 실패: {e.Message}");
                TryDeleteTemp(tempPath);
            }
        }

        public SongScoreRecord Get(int songId)
        {
            return _cache.TryGetValue(songId, out SongScoreRecord record) ? record : null;
        }

        public void Put(SongScoreRecord record)
        {
            _cache[record.songId] = record;
        }

        public IReadOnlyList<SongScoreRecord> GetAll()
        {
            return new List<SongScoreRecord>(_cache.Values);
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[JsonFileScoreRepository] 임시 파일 정리 실패: {e.Message}");
            }
        }

        /// <summary>파일에 실제로 기록되는 최상위 구조. JsonUtility 제약 때문에 List를 래핑한다.</summary>
        [Serializable]
        private sealed class ScoreDatabase
        {
            public int schemaVersion = CurrentSchemaVersion;
            public List<SongScoreRecord> records = new List<SongScoreRecord>();
        }
    }
}
