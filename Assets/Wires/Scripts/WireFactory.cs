using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Properties to use when constructing a new orbiter
    /// </summary>
    [System.Serializable]
    public struct OrbiterColorSet
    {
        public Color m_LowColor;                // Color for the low shader parameter of shader
        public Color m_HighColor;               // Color for the high shader parameter of shader
        public Color m_TrailStartColor;         // Color at start of trails
        public Color m_TrailEndColor;           // Color at end of trails
    }

    /// <summary>
    /// Properties about the music to play
    /// </summary>
    [System.Serializable]
    public struct MusicTrack
    {
        public AudioClip m_Music;                   // Music to play
        [Min(0.01f)] public float m_BeatRate;       // Rate of musics beat
        [Min(0.01f)] public float m_BeatDelay;      // Initial delay of musics beat
    }

    /// <summary>
    /// Factory for the types of wires to spawn into the world
    /// Over time, this class has become more of a theme in general, rather than wires
    /// </summary>
    [CreateAssetMenu]
    public class WireFactory : ScriptableObject
    {       
        // The color for this factory
        public Color color { get { return m_Color; } }

        // The color for the skybox
        public Color skyboxColor { get { return m_SkyboxColor; } }

        // The color for the outer wires particles
        public Color particleColor { get { return m_ParticleColor; } }

        // The color for sparks
        public Color sparkColor { get { return m_SparkColor; } }

        // The color for the boosts particles
        public ParticleSystem.MinMaxGradient boostColor { get { return m_BoostColor; } }

        // The colors for new orbiters
        public OrbiterColorSet orbiterColors { get { return m_OrbiterColors; } }

        // The texture for the outer wire
        public Texture2D borderTexture { get { return m_BorderTexture; } }

        [SerializeField] private Color m_Color;                                     // Wires color
        [SerializeField] private Color m_SkyboxColor;                               // Skyboxes color
        [SerializeField] private Color m_ParticleColor;                             // Particles color
        [SerializeField] private Color m_SparkColor;                                // Spark color
        [SerializeField] private ParticleSystem.MinMaxGradient m_BoostColor;        // Boost color
        [SerializeField] private OrbiterColorSet m_OrbiterColors;                   // Orbiter colors
        [SerializeField] private Texture2D m_BorderTexture;                         // Texture for outer border
        [SerializeField] private MusicTrack[] m_MusicTracks;                        // Wires music tracks (for each intensity)

        /// <summary>
        /// Get music at track
        /// </summary>
        /// <param name="index">Index of track</param>
        /// <returns>Audio clip or default track</returns>
        public MusicTrack GetMusicTrack(int index)
        {
            if (m_MusicTracks == null || m_MusicTracks.Length == 0)
                return new MusicTrack();

            index = Mathf.Clamp(index, 0, m_MusicTracks.Length - 1); ;
            return m_MusicTracks[index];
        }
    }
}
