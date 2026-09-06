using System.Collections.Generic;
using System.Linq;

using UnityEngine;

/// <summary>
/// Resources/SongData 안의 SongDataConfig 에셋을 모아 SongID 순으로 제공한다.
/// 로비 캐러셀이 순회하는 곡 목록의 단일 소스. (ISongData 통합은 추후)
/// </summary>
public static class SongCatalog
{
    private static List<SongDataConfig> _songs;

    public static IReadOnlyList<SongDataConfig> Songs
    {
        get
        {
            if (_songs == null)
                Reload();

            return _songs;
        }
    }

    public static void Reload()
    {
        _songs = Resources.LoadAll<SongDataConfig>("SongData")
            .Where(s => s != null && s.SongAudioClip != null)
            .OrderBy(s => s.SongID)
            .ToList();
    }
}
