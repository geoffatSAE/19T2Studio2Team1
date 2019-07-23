using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TO5.Wires
{
    /// <summary>
    /// Specialized class designed to handle screen fades
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

        private Coroutine m_FadeRoutine;                    // Coroutine handling current fade

        void Awake()
        {
            ConstructMesh();
            SetFadeAlpha(0f);
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
            if (m_FadeRoutine != null)
            {
                StopCoroutine(m_FadeRoutine);
                m_FadeRoutine = null;
            }

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
            if (m_FadeRoutine != null)
                StopCoroutine(m_FadeRoutine);

            m_FadeRoutine = StartCoroutine(FadeRoutine(startAlpha, endAlpha));
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
        }

        /// <summary>
        /// Routine that handles fading between two alphas
        /// </summary>
        /// <param name="startFade">Alpha to fade from</param>
        /// <param name="endFade">Alpha to fade to</param>
        private IEnumerator FadeRoutine(float startFade, float endFade)
        {
            SetIsFading(true);

            float end = Time.time + m_FadeTime;
            while (Time.time <= end)
            {
                float alpha = Mathf.Clamp01((end - Time.time) / m_FadeTime);
                float fade = Mathf.Lerp(endFade, startFade, alpha);
                SetFadeAlpha(fade);

                yield return new WaitForEndOfFrame();
            }

            SetFadeAlpha(endFade);

            // We can disable renderer is fully transparent
            if (endFade <= 0f)
                SetIsFading(false);

            if (OnFadeFinished != null)
                OnFadeFinished.Invoke(endFade);

            m_FadeRoutine = null;
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
                    new Vector3(-1, -1, 0), 
                    new Vector3( 1, -1, 0), 
                    new Vector3( 1, 1, 0), 
                    new Vector3( -1, 1, 0)
                };

                int[] indices = new int[6]
                {
                    0, 3, 1,
                    1, 3, 2
                };

                mesh.vertices = vertices;
                mesh.triangles = indices;

                // We increase the bounds so mesh is already rendered (tends to be frustumed culled when looking away)
                mesh.bounds = new Bounds(transform.position, new Vector3(1000f, 1000f, 1000f));
            }
        }
    }
}
