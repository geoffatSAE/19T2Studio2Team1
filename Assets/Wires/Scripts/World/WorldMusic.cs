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
        [SerializeField] private AudioSource m_MusicSource1;        // First source for audio
        [SerializeField] private AudioSource m_MusicSource2;        // Second source for audio
        private bool m_Source1Active = true;                        // Which source is active source

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
        }

        /// <summary>
        /// Sets active music, instantly overriding whats actively playing (can be used during Blending)
        /// </summary>
        /// <param name="audioClip">Music to play</param>
        public void SetActiveMusic(AudioClip audioClip)
        {
            if (!audioClip)
            {
                Debug.LogWarning("Audio clip is invalid. Unable to play music", this);
                return;
            }

            AudioSource activeSource = m_Source1Active ? m_MusicSource1 : m_MusicSource2;
            activeSource.enabled = true;

            // Not all clips are expected to be the same length
            float progess = activeSource.clip ? activeSource.time / activeSource.clip.length : 0f;

            activeSource.clip = audioClip;
            activeSource.volume = 1f;

            activeSource.time = audioClip.length * progess;
            activeSource.Play();            
        }

        /// <summary>
        /// Sets active music pending to be blended, allowing BlendMusic to be called
        /// </summary>
        /// <param name="audioClip">Music to fade to</param>
        public void SetPendingMusic(AudioClip audioClip)
        {
            if (!audioClip)
            {
                Debug.LogWarning("Audio clip is invalid. Unable to play music", this);
                return;
            }

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

            // Disable inactive source when finished (as it should be mute)
            if (progress >= 1f)
                fadingSource.enabled = false;
        }
    }
}
