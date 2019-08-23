using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TO5.Wires
{
    /// <summary>
    /// Handles playing the music for world theme
    /// </summary>
    public class WorldMusic : MonoBehaviour
    {
        static public readonly string BeatTimeShaderName = "_BeatTime";

        [Header("Music")]
        [SerializeField] private AudioSource m_MusicSource1;                // First source for audio
        [SerializeField] private AudioSource m_MusicSource2;                // Second source for audio
        [SerializeField, Min(0f)] private float m_MusicFadeTime = 1.5f;     // Time at which music fades when drifting
        [SerializeField] private float m_MusicFadeVolume = 0.5f;            // Music volume when faded out
        [SerializeField] private float m_MusicFadePitch = 0.75f;            // Music speed when faded out
        private bool m_Source1Active = true;                                // Which source is active source
        private bool m_FadingMusic = false;                                 // If music is being faded out
        private Coroutine m_FadingMusicRoutine = null;                      // Routine for fading music

        public AnimationCurve m_BeatCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);      // Animation curve of beat (value provided to shaders)
        private float m_BeatTime = 0f;                                                  // Time of beat
        private float m_BeatScale = 1f;                                                 // Amount to scale time by during update (changes based on fade)
        private float m_ActiveBeatRate = 1f;                                            // Beat rate of active music
        private float m_ActiveBeatDelay = 0f;                                           // Beat delay of active music
        private float m_PendingBeatRate = 1f;                                           // Beat rate of pending music
        private float m_PendingBeatDelay = 0f;                                          // Beat delay of pending music

        //[Header("Finale")]

        void Awake()
        {
            if (!m_MusicSource1 || !m_MusicSource2)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                Assert.IsTrue(sources.Length >= 2);

                m_MusicSource1 = sources[0];
                m_MusicSource2 = sources[1];
            }

            m_MusicSource1.enabled = false;
            m_MusicSource2.enabled = false;

            Shader.SetGlobalFloat(BeatTimeShaderName, 0f);
        }

        void Update()
        {
            m_BeatTime += Time.deltaTime * m_BeatScale;

            // Current time of beat (accounted for initial delay)
            float beatRate = m_ActiveBeatRate > 0f ? m_ActiveBeatRate : 1f;
            float beatTime = Mathf.Max(m_BeatTime - m_ActiveBeatDelay, 0f) / beatRate;
            Shader.SetGlobalFloat(BeatTimeShaderName, m_BeatCurve.Evaluate(Mathf.Repeat(beatTime, 1f)));
        }

        /// <summary>
        /// Sets active music, instantly overriding whats actively playing (can be used during Blending)
        /// </summary>
        /// <param name="track">Track to play</param>
        public void SetActiveTrack(MusicTrack track)
        {
            if (!track.m_Music)
            {
                Debug.LogWarning("Audio clip is invalid. Unable to play music", this);
                return;
            }

            AudioClip audioClip = track.m_Music;

            AudioSource activeSource = m_Source1Active ? m_MusicSource1 : m_MusicSource2;
            activeSource.enabled = true;

            // Not all clips are expected to be the same length
            float progess = activeSource.clip ? activeSource.time / activeSource.clip.length : 0f;

            activeSource.clip = audioClip;
            activeSource.volume = 1f;

            activeSource.time = audioClip.length * progess;
            activeSource.Play();

            m_ActiveBeatRate = track.m_BeatRate;
            m_ActiveBeatDelay = track.m_BeatDelay;
        }

        /// <summary>
        /// Sets active music pending to be blended, allowing BlendMusic to be called
        /// </summary>
        /// <param name="track">Track to change to</param>
        public void SetPendingTrack(MusicTrack track)
        {
            if (!track.m_Music)
            {
                Debug.LogWarning("Audio clip is invalid. Unable to play music", this);
                return;
            }

            AudioClip audioClip = track.m_Music;

            AudioSource activeSource = m_Source1Active ? m_MusicSource2 : m_MusicSource1;
            AudioSource fadingSource = m_Source1Active ? m_MusicSource1 : m_MusicSource2;

            activeSource.enabled = true;
            fadingSource.enabled = true;

            activeSource.clip = audioClip;
            activeSource.volume = 0f;
            fadingSource.volume = 1f;

            // Not all clips are expected to be the same length
            float progress = fadingSource.clip ? fadingSource.time / fadingSource.clip.length : 0f;

            activeSource.time = audioClip.length * progress;
            activeSource.Play();                

            m_Source1Active = !m_Source1Active;

            m_PendingBeatRate = track.m_BeatRate;
            m_PendingBeatDelay = track.m_BeatDelay;
        }

        /// <summary>
        /// Cross fades music sources
        /// </summary>
        /// <param name="progress">Progress of cross fade</param>
        public void BlendMusic(float progress)
        {
            progress = Mathf.Clamp01(progress);

            AudioSource activeSource = m_Source1Active ? m_MusicSource1 : m_MusicSource2;
            AudioSource fadingSource = m_Source1Active ? m_MusicSource2 : m_MusicSource1;

            activeSource.volume = progress;
            fadingSource.volume = 1f - progress;

            // Switch to pending beat rate
            if (progress >= 0.5f)
            {
                m_ActiveBeatRate = m_PendingBeatRate;
                m_ActiveBeatDelay = m_PendingBeatDelay;
            }

            // Disable inactive source when finished (as it should be mute)
            if (progress >= 1f)
            {
                fadingSource.enabled = false;
            }         
        }

        /// <summary>
        /// Fades the music in and out (cancels previous fade if active)
        /// </summary>
        /// <param name="fadeMusic">If to fade music out</param>
        public void FadeMusic(bool fadeMusic)
        {
            if (m_FadingMusic != fadeMusic)
            {
                m_FadingMusic = fadeMusic;

                if (m_FadingMusicRoutine != null)
                {
                    StopCoroutine(m_FadingMusicRoutine);
                    m_FadingMusicRoutine = null;
                }

                m_FadingMusicRoutine = StartCoroutine(FadeMusicRoutine(!fadeMusic));
            }
        }

        /// <summary>
        /// Routine for fading music in and out (both active and inactive sources)
        /// </summary>
        /// <param name="fadeIn">If music should fade in or out</param>
        /// <returns></returns>
        private IEnumerator FadeMusicRoutine(bool fadeIn)
        {
            float end = Time.time + m_MusicFadeTime;
            while (Time.time < end)
            {
                float alpha = Mathf.Clamp01((end - Time.time) / m_MusicFadeTime);

                if (!fadeIn)
                    alpha = 1f - alpha;

                InterpolateMusicFade(alpha);

                yield return null;
            }

            InterpolateMusicFade(fadeIn ? 0f : 1f);
        }

        /// <summary>
        /// Interpolates the volume and pitch of music sources
        /// </summary>
        /// <param name="alpha">Alpha of interpolation</param>
        private void InterpolateMusicFade(float alpha)
        {
            float volume = Mathf.Lerp(1f, m_MusicFadeVolume, alpha);
            float pitch = Mathf.Lerp(1f, m_MusicFadePitch, alpha);

            if (m_MusicSource1)
            {
                m_MusicSource1.volume = volume;
                m_MusicSource1.pitch = pitch;
            }

            if (m_MusicSource2)
            {
                m_MusicSource2.volume = volume;
                m_MusicSource2.pitch = pitch;
            }

            m_BeatScale = pitch;
        }
    }
}
