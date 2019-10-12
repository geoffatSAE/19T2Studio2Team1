using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Handler for the tunnel that appears during the finale
    /// </summary>
    public class TunnelVision : MonoBehaviour
    {
        [SerializeField] private Material m_VisionMaterial;     // Vision to apply to planes
        [SerializeField] private MeshRenderer m_Mesh;           // Mesh to manipulate

        [SerializeField, Range(0f, 1f)] private float m_StartAlpha = 0f;            // Alpha of material when finale starts
        [SerializeField, Range(0f, 1f)] private float m_EndAlpha = 0.6f;            // Alpha of material when finale ends

        void Awake()
        {
            // Make a copy to avoid changing original material
            if (m_VisionMaterial)
                m_VisionMaterial = new Material(m_VisionMaterial);

            if (m_VisionMaterial && m_Mesh != null)
                m_Mesh.material = m_VisionMaterial;
        }

        /// <summary>
        /// Activates the mesh renderers for the tunnel
        /// </summary>
        public void ActivatetTunnel()
        {
            if (m_VisionMaterial)
                m_VisionMaterial.SetFloat("_Alpha", m_StartAlpha);
        }

        /// <summary>
        /// Interpolates the alpha of the tunnel material
        /// </summary>
        /// <param name="alpha">Alpha of interpolation</param>
        public void InterpolateAlpha(float alpha)
        {
            if (m_VisionMaterial)
                m_VisionMaterial.SetFloat("_Alpha", Mathf.Lerp(m_StartAlpha, m_EndAlpha, alpha));
        }

        /// <summary>
        /// Sets the color of the tunnels material. The Alpha property
        /// should be the blend between the first and second texture
        /// </summary>
        /// <param name="color">RGB of color, interpolation of textures</param>
        public void SetColor(Color color)
        {
            color.a = Mathf.Clamp01(color.a);

            if (m_VisionMaterial)
                m_VisionMaterial.SetColor("_Color", color);
        }

        /// <summary>
        /// Updates the textures used by the vision material
        /// </summary>
        /// <param name="from">Texture that has focus when interp is zero</param>
        /// <param name="to">Texture that has focus when interp is one</param>
        public void SetTextures(Texture from, Texture to)
        {
            if (m_VisionMaterial)
            {
                m_VisionMaterial.SetTexture("_Tex1", from);
                m_VisionMaterial.SetTexture("_Tex2", to);
            }
        }
    }
}
