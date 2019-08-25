using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Instance of a firework spawned by the fireworks handler. Both
    /// particle systems need to be attached to the game object with this script
    /// </summary>
    [RequireComponent(typeof(ParticleSystem), typeof(AudioSource))]
    public class FireworksObject : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when a fireworks object has finished simulation (explosion particles has stopped)
        /// </summary>
        /// <param name="fireworks">Fireworks that has completed</param>
        public delegate void FireworksCompleted(FireworksObject fireworks);

        public FireworksCompleted OnFireworksCompleted;         // Event for when fireworks has finished

        [SerializeField] private AudioClip[] m_BurstSounds;     // Collection of sounds that could play when ignited

        private ParticleSystem m_ParticleSystem;    // Particle system for emitting particles
        private AudioSource m_AudioSource;          // Audio source for playing sounds

        void Awake()
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            m_AudioSource = GetComponent<AudioSource>();

            ParticleSystem.MainModule main = m_ParticleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        /// <summary>
        /// Plays this firework at local position
        /// </summary>
        /// <param name="localPosition">Position to set self</param>
        public void Play(Vector3 localPosition)
        {
            transform.localPosition = localPosition;

            m_ParticleSystem.Play(true);

            if (m_BurstSounds != null && m_BurstSounds.Length > 0)
                m_AudioSource.clip = m_BurstSounds[Random.Range(0, m_BurstSounds.Length)];

            m_AudioSource.time = 0f;
            m_AudioSource.pitch = Random.Range(1f, 1.4f);
            m_AudioSource.Play();
        }

        void OnParticleSystemStopped()
        {
            if (OnFireworksCompleted != null)
                OnFireworksCompleted.Invoke(this);
        }
    }
}
