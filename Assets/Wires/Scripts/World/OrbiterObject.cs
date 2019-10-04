using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// An object that can be instanced by the orbiter script
    /// </summary>
    public class OrbiterObject : MonoBehaviour
    {
        [SerializeField] private Renderer m_Mesh;           // Orbiters mesh
        [SerializeField] private TrailRenderer m_Trail;     // Orbiters trail

        // This objects mesh renderer
        public Renderer mesh { get { return m_Mesh; } }

        // This objects trail renderer
        public TrailRenderer trails { get { return m_Trail; } }

        // This objects mesh material
        public Material meshMaterial { get { return m_Mesh ? m_Mesh.material : null; } }

        // This objects trail material
        public Material trailsMaterial { get { return m_Trail ? m_Trail.material : null; } }
    }
}
