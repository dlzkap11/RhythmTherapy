using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SongData
{
    // 노래 정보
    [Header("Song")]
    public int SongID;
    public string SongName;
    public string SongFileName;
    public string SongAlbumName;

    // 해당 노래의 노트 배치 데이터
    [Header("Note")]
    public List<NoteData> NoteDatas = new();
}
