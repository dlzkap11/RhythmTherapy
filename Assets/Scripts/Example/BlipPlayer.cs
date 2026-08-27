// BlipPlayer.cs
// 절차적 사운드 효과 (AudioSource 없이 Oscillator 느낌은 OnAudioFilterRead로 흉내).
// 실제 환경에서는 AudioSource + AudioClip 풀로 교체 권장.
using UnityEngine;

namespace PixelBeat
{
    [RequireComponent(typeof(AudioSource))]
    public class BlipPlayer : MonoBehaviour
    {
        public static BlipPlayer Instance { get; private set; }

        [SerializeField] private float baseVolume = 0.12f;
        private AudioSource _src;

        private void Awake()
        {
            Instance = this;
            _src = GetComponent<AudioSource>();
            _src.loop = false;
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
        }

        // AudioClip 풀이 없는 데모용: 매우 짧은 톤을 즉석 생성.
        public void PlayTone(float freq, float vol = 1f, float dur = 0.12f)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int samples = Mathf.CeilToInt(sampleRate * dur);
            var clip = AudioClip.Create("blip", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float env = Mathf.Exp(-(float)i / samples * 6f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * env * baseVolume * vol;
            }
            clip.SetData(data, 0);
            _src.clip = clip;
            _src.Play();
        }

        public void PlayBeat(float bpm)
        {
            PlayTone(80f, 0.35f, 0.06f);
        }
    }
}
