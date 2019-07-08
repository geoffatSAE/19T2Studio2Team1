using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Factory for the types of wires to spawn into the world
    /// </summary>
    [CreateAssetMenu]
    public class WireFactory : ScriptableObject
    {
        // The color for this factory
        public Color color { get { return m_Color; } }

        // The color for the skybox
        public Color skyboxColor { get { return m_SkyboxColor; } }

        // The texture for the outer wire
        public Texture2D borderTexture { get { return m_BorderTexture; } }

        [SerializeField] private Color m_Color;                     // Wires color
        [SerializeField] private Color m_SkyboxColor;               // Skyboxes color
        [SerializeField] private Texture2D m_BorderTexture;         // Texture for outer border
        [SerializeField] private AudioClip[] m_Music;               // Wires music tracks (for each intensity)

        /// <summary>
        /// Get music at index
        /// </summary>
        /// <param name="index">Index of music</param>
        /// <returns>Audio clip or null</returns>
        public AudioClip GetMusic(int index)
        {
            if (index < m_Music.Length)
                return m_Music[index];

            return null;
        }
            
        //private MaterialPropertyBlock m_WireMaterialProperties;
    }
}
