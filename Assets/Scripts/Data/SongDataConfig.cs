using UnityEngine;
using System.Collections.Generic;

//json이나 csv파일에서 가져와서 동기화 하기
[CreateAssetMenu(fileName = "SongDataConfig", menuName = "BeatPop/SongDataConfig")]
public class SongDataConfig : ScriptableObject
{
    // 노래 정보
    [Header("Song")]
    public int SongID;
    public string SongName;
    public Sprite AlbumArt;
    /// <summary>앨범 아트가 없을 때 로비 캐러셀 원에 쓰는 대표 색.</summary>
    public Color ThemeColor = Color.white;
    public AudioClip SongAudioClip;

    [Range(0f, 1f)]
    [Tooltip("곡별 재생 볼륨 배율. 곡마다 마스터링 라우드니스가 달라 보정용. 1 = 원음.")]
    public float SongVolume = 1f;


    // 해당 노래의 노트 배치 데이터
    [Header("Note")]
    public List<NoteData> NoteDatas = new List<NoteData>();


}
