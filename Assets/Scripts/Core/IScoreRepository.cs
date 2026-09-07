using System.Collections.Generic;

namespace RhythmTherapy.Core
{
    /// <summary>
    /// 곡별 점수 기록의 영속 저장소. 저장 매체(JSON 파일, 메모리 등)를 이 인터페이스 뒤에 숨겨
    /// 도메인 로직(<see cref="HighScoreStore"/>)이 매체를 모르게 한다.
    ///
    /// 구현은 "저장소에 있는 그대로"의 CRUD만 담당한다. 신기록 판단이나 레코드 병합은 하지 않는다.
    /// 파일 경로·직렬화 형식 등 구현 세부는 밖으로 노출하지 않는다.
    /// </summary>
    public interface IScoreRepository
    {
        /// <summary>저장소에서 전체 기록을 메모리로 로드한다. 저장소가 비어 있으면 빈 상태로 시작한다.</summary>
        void Load();

        /// <summary>메모리에 있는 현재 상태를 저장소에 영속화한다.</summary>
        void Save();

        /// <summary>해당 곡의 기록을 반환한다. 없으면 null.</summary>
        SongScoreRecord Get(int songId);

        /// <summary>곡 기록을 통째로 넣거나 교체한다. 메모리만 갱신하며, 영속화는 <see cref="Save"/>로 따로 한다.</summary>
        void Put(SongScoreRecord record);

        /// <summary>보관 중인 모든 곡 기록. 로비 목록 등 전체 순회용.</summary>
        IReadOnlyList<SongScoreRecord> GetAll();
    }
}
