using System.Collections.Generic;

namespace RhythmTherapy.Core
{
    /// <summary>
    /// 파일 IO 없이 딕셔너리 하나만 쓰는 저장소. 테스트에서 <see cref="HighScoreStore"/>의
    /// 병합·신기록 로직을 디스크 없이 검증하는 fake이며, 필요하면 런타임 임시 캐시로도 쓸 수 있다.
    ///
    /// <see cref="Load"/>/<see cref="Save"/>는 실제로 하는 일이 없고 호출 횟수만 센다(테스트 확인용).
    /// </summary>
    public sealed class InMemoryScoreRepository : IScoreRepository
    {
        private readonly Dictionary<int, SongScoreRecord> _records = new Dictionary<int, SongScoreRecord>();

        public int LoadCallCount { get; private set; }
        public int SaveCallCount { get; private set; }

        public void Load()
        {
            LoadCallCount++;
        }

        public void Save()
        {
            SaveCallCount++;
        }

        public SongScoreRecord Get(int songId)
        {
            return _records.TryGetValue(songId, out SongScoreRecord record) ? record : null;
        }

        public void Put(SongScoreRecord record)
        {
            _records[record.songId] = record;
        }

        public IReadOnlyList<SongScoreRecord> GetAll()
        {
            return new List<SongScoreRecord>(_records.Values);
        }
    }
}
