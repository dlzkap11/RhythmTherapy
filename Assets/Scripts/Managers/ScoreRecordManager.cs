using UnityEngine;

using RhythmTherapy.Core;

namespace RhythmTherapy.Managers
{
    /// <summary>
    /// 곡별 점수 기록의 런타임 진입점. JSON 파일 저장소(<see cref="JsonFileScoreRepository"/>)를
    /// <see cref="HighScoreStore"/>에 물려 두고, 게임 시작 시 1회 로드해 메모리에 유지한다.
    ///
    /// GameManager 는 곡 종료 시 <see cref="Submit"/>, 로비는 <see cref="Get"/>로 조회한다.
    /// 싱글턴 부트스트랩은 다른 매니저(@Managers GameObject)와 동일한 방식.
    /// </summary>
    public sealed class ScoreRecordManager : MonoBehaviour
    {
        public static ScoreRecordManager Instance { get; private set; }

        private HighScoreStore _store;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            GameObject go = GameObject.Find("@Managers");
            if (go == null)
                go = new GameObject("@Managers");

            if (go.GetComponent<ScoreRecordManager>() == null)
                go.AddComponent<ScoreRecordManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            JsonFileScoreRepository repository = new JsonFileScoreRepository();
            repository.Load();
            _store = new HighScoreStore(repository);
            Debug.Log($"[ScoreRecordManager] 기록 로드 완료: {repository.FilePath}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>한 판의 결과를 제출한다. 병합·저장 후 최고 점수 갱신 여부를 반환한다.</summary>
        public bool Submit(in GameResult result)
        {
            return _store != null && _store.Submit(result);
        }

        /// <summary>해당 곡의 현재 기록. 없으면 null.</summary>
        public SongScoreRecord Get(int songId)
        {
            return _store?.Get(songId);
        }
    }
}
