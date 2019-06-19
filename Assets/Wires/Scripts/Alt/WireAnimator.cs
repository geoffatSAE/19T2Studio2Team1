using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Animates a single mesh used by all wires
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WireAnimator : MonoBehaviour
    {  
        private Animator m_Animator;                                    // Animator for animating avatar
        [SerializeField] private SkinnedMeshRenderer m_Renderer;        // Renderer of animated avatar

        // The mesh that has been animated
        public Mesh wireMesh { get { return m_Filter.mesh; } }

        private MeshFilter m_Filter;                                    // Mesh filter (created at start)

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_Filter = gameObject.AddComponent<MeshFilter>();
        }

        void Update()
        {
            // for now, might want to move it into a co-routine
            if (m_Renderer)
                m_Renderer.BakeMesh(m_Filter.mesh);
        }
    }
}
