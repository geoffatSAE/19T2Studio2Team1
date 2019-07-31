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

        [SerializeField] private AudioSource m_MusicSource1;        // First source for audio
        [SerializeField] private AudioSource m_MusicSource2;        // Second source for audio
        private bool m_Source1Active = true;                        // Which source is active source

        public AnimationCurve m_BeatCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);      // Animation curve of beat (value provided to shaders)
        private float m_ActiveBeatRate = 1f;                                            // Beat rate of active music
        private float m_ActiveBeatDelay = 0f;                                           // Beat delay of active music
        private float m_PendingBeatRate = 1f;                                           // Beat rate of pending music
        private float m_PendingBeatDelay = 0f;                                          // Beat delay of pending music

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
            // Current time of beat (accounted for initial delay)
            float beatRate = m_ActiveBeatRate > 0f ? m_ActiveBeatRate : 1f;
            float beatTime = Mathf.Max(Time.time - m_ActiveBeatDelay, 0f) / beatRate;
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
    }
}
