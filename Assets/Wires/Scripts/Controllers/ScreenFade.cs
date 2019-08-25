using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Specialized class designed to handle screen fades. 
    /// Should ideally be attached to the camera of the player (to avoid culling)
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when a screen fade has finished
        /// </summary>
        /// <param name="endAlpha">The alpha the fade finished at</param>
        public delegate void FadeFinished(float endAlpha);

        public FadeFinished OnFadeFinished;                 // Event for when fading has finished

        [Min(0.1f)] public float m_FadeTime = 1f;           // Time between fading
        public Color m_FadeColor = Color.black;             // Color of fade
        public bool m_FadeAudio = true;                     // If game audio fades with screen

        private Material m_Material;                        // Fade material
        private MeshRenderer m_Renderer;                    // Plane renderer (created by us)
        private MeshFilter m_Filter;                        // Plane filter (created by us)

        private float m_FadeStart = -1f;                    // Time we started fading
        private float m_FadeFrom = 0f;                      // Alpha to fade from
        private float m_FadeTo = 1f;                        // Alpha to fade to

        void Awake()
        {
            ConstructMesh();
            SetIsFading(false);
        }

        void OnDestroy()
        {
            if (m_Renderer)
                Destroy(m_Renderer);

            if (m_Material)
                Destroy(m_Material);

            if (m_Filter)
            {
                if (m_Filter.mesh)
                    Destroy(m_Filter.mesh);

                Destroy(m_Filter);
            }

            // Restore faded audio
            if (m_FadeAudio)
                AudioListener.volume = 1f;
        }

        void Update()
        {
            if (m_FadeTime >= 0f)
            {
                float end = m_FadeStart + m_FadeTime;
                float alpha = Mathf.Clamp01((end - Time.time) / m_FadeTime);

                // Inversed (1 - alpha)
                float fade = Mathf.Lerp(m_FadeTo, m_FadeFrom, alpha);

                SetFadeAlpha(fade);

                if (Time.time > end)
                {
                    // We can disable renderer is fully transparent
                    if (m_FadeTo <= 0f)
                        SetIsFading(false);

                    if (OnFadeFinished != null)
                        OnFadeFinished.Invoke(m_FadeTo);

                    enabled = false;
                }
            }
        }

        void OnDisable()
        {
            m_FadeStart = -1f;
        }

        /// <summary>
        /// Fades in from blocked screen to transparent screen
        /// </summary>
        public void FadeIn()
        {
            StartFading(1f, 0f);
        }

        /// <summary>
        /// Fades out to blocked screen from transparent screen
        /// </summary>
        public void FadeOut()
        {
            StartFading(0f, 1f);
        }

        /// <summary>
        /// Clears the fading screen, stopping any fade in progress
        /// </summary>
        public void ClearFade()
        {
            SetIsFading(false);
            SetFadeAlpha(0f);
        }

        /// <summary>
        /// Starts a new fade sequence
        /// </summary>
        /// <param name="startAlpha">Alpha to fade from</param>
        /// <param name="endAlpha">Alpha to fade to</param>
        public void StartFading(float startAlpha, float endAlpha)
        {
            m_FadeFrom = startAlpha;
            m_FadeTo = endAlpha;

            m_FadeStart = Time.time;
            SetIsFading(true);
        }

        /// <summary>
        /// Sets the alpha of the fade in the material
        /// </summary>
        /// <param name="alpha">Alpha of fade</param>
        private void SetFadeAlpha(float alpha)
        {
            if (m_Material)
            {
                Color color = m_FadeColor;
                color.a = alpha;

                m_Material.color = color;
            }

            // We inverse this as volume should be full when alpha is 0
            if (m_FadeAudio)
                AudioListener.volume = 1f - alpha;
        }

        /// <summary>
        /// Set if a fade is in progress
        /// </summary>
        /// <param name="isFading"></param>
        private void SetIsFading(bool isFading)
        {
            if (m_Renderer)
                m_Renderer.enabled = isFading;

            enabled = isFading;
        }

        /// <summary>
        /// Constructs the mesh and material used for fading screen
        /// </summary>
        private void ConstructMesh()
        {
            Shader fadeShader = Shader.Find("Wires/ScreenFade");
            if (!fadeShader)
            {
                Debug.LogError("Failed to fade screen fade shader", this);
                return;
            }

            m_Material = new Material(fadeShader);
            m_Material.renderQueue = 4000;

            m_Renderer = gameObject.AddComponent<MeshRenderer>();
            m_Renderer.material = m_Material;
            m_Renderer.allowOcclusionWhenDynamic = false;

            m_Filter = gameObject.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();
            m_Filter.mesh = mesh;

            // Plane glued to the screen
            {
                Vector3[] vertices = new Vector3[4] 
                {
                    new Vector3(-1, -1, 1), 
                    new Vector3( 1, -1, 1), 
                    new Vector3( 1, 1, 1), 
                    new Vector3( -1, 1, 1)
                };

                int[] indices = new int[6]
                {
                    0, 3, 1,
                    1, 3, 2
                };

                mesh.vertices = vertices;
                mesh.triangles = indices;
            }
        }
    }
}
