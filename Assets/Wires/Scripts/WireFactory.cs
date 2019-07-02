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

        // The color for for the skybox
        public Color skyboxColor { get { return m_Color; } }

        // The music for this factory
        public AudioClip music { get { return m_Music; } }

        [SerializeField] private Color m_Color;         // Wires color
        [SerializeField] private Color m_SkyboxColor;   // Skyboxes color
        [SerializeField] private AudioClip m_Music;     // Wires music

        private MaterialPropertyBlock m_WireMaterialProperties;
    }
}
