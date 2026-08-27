using UnityEngine;

public class Conductor : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SongDataConfig songDataConfig;

    public double SongTime { get; private set; }
    public bool IsPlaying { get; private set; }

    private double dspStartTime;
    private double startOffset;

    public void Play(AudioClip clip, double offset)
    {
        audioSource.clip = songDataConfig.SongAudioClip;
        audioSource.clip = clip;
        audioSource.PlayScheduled(AudioSettings.dspTime + 0.1);

        dspStartTime = AudioSettings.dspTime + 0.1;
        startOffset = offset;
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        SongTime = AudioSettings.dspTime - dspStartTime - startOffset;
    }

    public void Stop()
    {
        audioSource.Stop();
        IsPlaying = false;
    }
}