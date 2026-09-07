using System;
using System.IO;

using NUnit.Framework;

using RhythmTherapy.Core;

namespace RhythmTherapy.Core.Tests
{
    /// <summary>
    /// 점수 기록 영속화 계층 검증.
    /// - HighScoreStore: 병합·신기록 판단 로직 (InMemoryScoreRepository 사용, 디스크 무관)
    /// - JsonFileScoreRepository: 임시 파일로 저장/로드 라운드트립과 손상 파일 폴백
    /// </summary>
    public class HighScoreStoreTests
    {
        private static GameResult MakeResult(int songId, int score, float accuracy = 90f, string grade = "A",
            int maxCombo = 10, bool cleared = true, bool fullCombo = false, bool allPerfect = false)
        {
            return new GameResult
            {
                songName = "Test Song",
                songID = songId,
                score = score,
                accuracy = accuracy,
                grade = grade,
                maxCombo = maxCombo,
                cleared = cleared,
                fullCombo = fullCombo,
                allPerfect = allPerfect,
            };
        }

        // --- HighScoreStore 로직 ---------------------------------------------------

        [Test]
        public void Submit_FirstTime_IsNewRecordAndStored()
        {
            HighScoreStore store = new HighScoreStore(new InMemoryScoreRepository());

            bool isNewRecord = store.Submit(MakeResult(songId: 1, score: 5000));

            Assert.IsTrue(isNewRecord);
            Assert.AreEqual(5000, store.Get(1).bestScore);
        }

        [Test]
        public void Submit_LowerScore_NotNewRecordAndBestUnchanged()
        {
            HighScoreStore store = new HighScoreStore(new InMemoryScoreRepository());
            store.Submit(MakeResult(songId: 1, score: 5000, grade: "A", maxCombo: 40));

            bool isNewRecord = store.Submit(MakeResult(songId: 1, score: 3000, grade: "C", maxCombo: 5));

            Assert.IsFalse(isNewRecord);
            SongScoreRecord record = store.Get(1);
            Assert.AreEqual(5000, record.bestScore);
            Assert.AreEqual("A", record.bestGrade);
            Assert.AreEqual(40, record.maxCombo);
        }

        [Test]
        public void Submit_HigherScore_ReplacesBestStats()
        {
            HighScoreStore store = new HighScoreStore(new InMemoryScoreRepository());
            store.Submit(MakeResult(songId: 1, score: 5000, accuracy: 88f, grade: "B", maxCombo: 30));

            bool isNewRecord = store.Submit(MakeResult(songId: 1, score: 9000, accuracy: 97f, grade: "S", maxCombo: 120));

            Assert.IsTrue(isNewRecord);
            SongScoreRecord record = store.Get(1);
            Assert.AreEqual(9000, record.bestScore);
            Assert.AreEqual(97f, record.bestAccuracy);
            Assert.AreEqual("S", record.bestGrade);
            Assert.AreEqual(120, record.maxCombo);
        }

        [Test]
        public void Submit_FullComboFlag_IsStickyAcrossPlays()
        {
            HighScoreStore store = new HighScoreStore(new InMemoryScoreRepository());
            store.Submit(MakeResult(songId: 1, score: 8000, fullCombo: true, allPerfect: true));

            store.Submit(MakeResult(songId: 1, score: 9000, fullCombo: false, allPerfect: false));

            SongScoreRecord record = store.Get(1);
            Assert.IsTrue(record.fullCombo);
            Assert.IsTrue(record.allPerfect);
        }

        [Test]
        public void Submit_CountsPlaysAlwaysAndClearsOnlyWhenCleared()
        {
            HighScoreStore store = new HighScoreStore(new InMemoryScoreRepository());

            store.Submit(MakeResult(songId: 1, score: 100, cleared: true));
            store.Submit(MakeResult(songId: 1, score: 200, cleared: false));
            store.Submit(MakeResult(songId: 1, score: 300, cleared: true));

            SongScoreRecord record = store.Get(1);
            Assert.AreEqual(3, record.playCount);
            Assert.AreEqual(2, record.clearCount);
        }

        [Test]
        public void Submit_DifferentSongIds_TrackedIndependently()
        {
            HighScoreStore store = new HighScoreStore(new InMemoryScoreRepository());

            store.Submit(MakeResult(songId: 1, score: 5000));
            store.Submit(MakeResult(songId: 2, score: 7000));

            Assert.AreEqual(5000, store.Get(1).bestScore);
            Assert.AreEqual(7000, store.Get(2).bestScore);
        }

        [Test]
        public void Submit_PersistsThroughRepositorySave()
        {
            InMemoryScoreRepository repo = new InMemoryScoreRepository();
            HighScoreStore store = new HighScoreStore(repo);

            store.Submit(MakeResult(songId: 1, score: 5000));

            Assert.AreEqual(1, repo.SaveCallCount);
        }

        // --- JsonFileScoreRepository 라운드트립 ----------------------------------

        private string _tempPath;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "rt_scores_" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string path in new[] { _tempPath, _tempPath + ".tmp" })
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void JsonFile_SaveThenLoadInNewInstance_RoundTrips()
        {
            JsonFileScoreRepository writer = new JsonFileScoreRepository(_tempPath);
            writer.Load();
            writer.Put(new SongScoreRecord
            {
                songId = 42,
                bestScore = 12345,
                bestAccuracy = 96.5f,
                bestGrade = "S",
                maxCombo = 210,
                fullCombo = true,
                playCount = 3,
                clearCount = 2,
            });
            writer.Save();

            Assert.IsTrue(File.Exists(_tempPath));

            JsonFileScoreRepository reader = new JsonFileScoreRepository(_tempPath);
            reader.Load();

            SongScoreRecord record = reader.Get(42);
            Assert.IsNotNull(record);
            Assert.AreEqual(12345, record.bestScore);
            Assert.AreEqual("S", record.bestGrade);
            Assert.AreEqual(210, record.maxCombo);
            Assert.IsTrue(record.fullCombo);
            Assert.AreEqual(3, record.playCount);
        }

        [Test]
        public void JsonFile_LoadWhenFileMissing_IsEmptyAndDoesNotThrow()
        {
            JsonFileScoreRepository repo = new JsonFileScoreRepository(_tempPath);

            Assert.DoesNotThrow(() => repo.Load());
            Assert.IsNull(repo.Get(1));
            Assert.AreEqual(0, repo.GetAll().Count);
        }

        [Test]
        public void JsonFile_LoadWhenFileCorrupt_FallsBackToEmpty()
        {
            File.WriteAllText(_tempPath, "{{{ not json");

            JsonFileScoreRepository repo = new JsonFileScoreRepository(_tempPath);

            Assert.DoesNotThrow(() => repo.Load());
            Assert.AreEqual(0, repo.GetAll().Count);
        }

        [Test]
        public void JsonFile_EndToEndWithStore_RecordSurvivesReload()
        {
            JsonFileScoreRepository repo = new JsonFileScoreRepository(_tempPath);
            repo.Load();
            HighScoreStore store = new HighScoreStore(repo);
            store.Submit(MakeResult(songId: 7, score: 8800, grade: "A", maxCombo: 90));

            JsonFileScoreRepository reloaded = new JsonFileScoreRepository(_tempPath);
            reloaded.Load();

            Assert.AreEqual(8800, reloaded.Get(7).bestScore);
        }
    }
}
