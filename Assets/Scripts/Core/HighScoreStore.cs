using System;

namespace RhythmTherapy.Core
{
    /// <summary>
    /// 곡 종료 결과(<see cref="GameResult"/>)를 받아 기존 기록과 병합하고 저장소에 반영한다.
    /// 저장 매체는 <see cref="IScoreRepository"/> 뒤에 있어 이 클래스는 파일 존재 자체를 모른다.
    ///
    /// 병합 규칙: bestScore는 최댓값 유지, bestScore가 갱신될 때만 그 판의 정확도·등급·최대콤보로 교체.
    /// fullCombo/allPerfect는 한 번 달성하면 유지(OR). playCount는 매번 +1, clearCount는 완주 시 +1.
    /// </summary>
    public sealed class HighScoreStore
    {
        private readonly IScoreRepository _repo;

        public HighScoreStore(IScoreRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        /// <summary>
        /// 한 판의 결과를 제출한다. <see cref="GameResult.songID"/> 기준으로 기존 기록과 병합해
        /// 저장소에 넣고 즉시 <see cref="IScoreRepository.Save"/>한다.
        /// </summary>
        /// <returns>이번 제출로 최고 점수가 갱신됐으면(첫 기록 포함) true.</returns>
        public bool Submit(in GameResult result)
        {
            SongScoreRecord previous = _repo.Get(result.songID);
            bool isNewRecord = previous == null || result.score > previous.bestScore;

            _repo.Put(Merge(result.songID, previous, result));
            _repo.Save();

            return isNewRecord;
        }

        /// <summary>해당 곡의 현재 기록. 없으면 null.</summary>
        public SongScoreRecord Get(int songId)
        {
            return _repo.Get(songId);
        }

        private static SongScoreRecord Merge(int songId, SongScoreRecord previous, in GameResult result)
        {
            SongScoreRecord record = previous ?? new SongScoreRecord { songId = songId };

            if (previous == null || result.score > record.bestScore)
            {
                record.bestScore = result.score;
                record.bestAccuracy = result.accuracy;
                record.bestGrade = result.grade;
                record.maxCombo = result.maxCombo;
            }
            else if (result.maxCombo > record.maxCombo)
            {
                // 점수 갱신은 아니어도 콤보만 더 높았으면 그건 반영해 준다.
                record.maxCombo = result.maxCombo;
            }

            record.fullCombo |= result.fullCombo;
            record.allPerfect |= result.allPerfect;
            record.playCount++;
            if (result.cleared)
                record.clearCount++;
            record.lastPlayedIso = DateTime.UtcNow.ToString("o");

            return record;
        }
    }
}
