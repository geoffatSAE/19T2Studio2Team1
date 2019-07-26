using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Script for handling the giant spark in the background. Will interpolate based on game length
    /// </summary>
    public class RisingSpark : MonoBehaviour
    {
        /// <summary>
        /// State of the rising spark
        /// </summary>
        [System.Serializable]
        public struct State
        {
            public Vector3 m_Position;       // Local position
            public Vector3 m_Scale;          // Local scale
        }

        [SerializeField] private State m_StartingState;                 // State we start at
        [SerializeField] private State m_EndingState;                   // State we end at
        [SerializeField] private Transform m_OverrideAnchor;            // The transform we move locally too (will use parent by default)
        [SerializeField] private WiresGameMode m_GameMode;              // Game mode to sync with
        [SerializeField] private float m_FallbackDuration = 600f;       // Fallback duration to use if game mode has no set game length

        [Min(0.1f)] public float m_RotationInterval = 1.5f;             // Time for a full rotation
        public Transform m_RotationOverride;                            // Override transform to rotate

        private float m_RotationTime = 0f;      // Time passed of current rotation
        private Quaternion m_FromOrientation;   // Orientation to rotate from
        private Quaternion m_ToOrientation;     // Orientation to rotate to

        void Awake()
        {
            if (!m_OverrideAnchor)
                m_OverrideAnchor = transform.parent;

            // Default spot
            Interpolate(0f);

            if (m_GameMode)
                m_GameMode.OnGameStarted += OnGameStart;
            else
                Debug.LogWarning("Rising spark will not play as no game mode has been provided");

            Transform rotationTransform = m_RotationOverride ? m_RotationOverride : transform;
            m_FromOrientation = rotationTransform.rotation;
            m_ToOrientation = Random.rotation;
        }

        void Update()
        {
            Transform rotationTransform = m_RotationOverride ? m_RotationOverride : transform;

            m_RotationTime += Time.deltaTime;
            if (m_RotationTime > m_RotationInterval)
            {
                m_FromOrientation = rotationTransform.rotation;
                m_ToOrientation = Random.rotation;

                m_RotationTime = Mathf.Repeat(m_RotationTime, m_RotationInterval);
            }

            float alpha = m_RotationTime / m_RotationInterval;

            // InOutSine
            // See: https://easings.net/en
            float ease = -(Mathf.Cos(Mathf.PI * alpha) - 1f) / 2f;

            rotationTransform.rotation = Quaternion.Slerp(m_FromOrientation, m_ToOrientation, ease);
        }

        /// <summary>
        /// Interpolates transform
        /// </summary>
        /// <param name="alpha">Alpha of lerp</param>
        private void Interpolate(float alpha)
        {
            transform.localPosition = Vector3.Lerp(m_StartingState.m_Position, m_EndingState.m_Position, alpha);
            transform.localScale = Vector3.Lerp(m_StartingState.m_Scale, m_EndingState.m_Scale, alpha);
        }

        /// <summary>
        /// Routine for handling the interpolation of transform
        /// </summary>
        /// <param name="duration">Duration of rise</param>
        private IEnumerator RiseRoutine(float duration)
        {
            if (duration > 0f)
            {
                float end = Time.time + duration;
                while (Time.time < end)
                {
                    float alpha = 1f - Mathf.Clamp01((end - Time.time) / duration);
                    Interpolate(alpha);

                    yield return null;
                }
            }

            Interpolate(1f);
        }

        /// <summary>
        /// Notify that the game has started
        /// </summary>
        private void OnGameStart()
        {
            float duration = m_FallbackDuration;
            if (m_GameMode)
            {
                m_GameMode.OnGameStarted -= OnGameStart;

                // If we know the games expected length, we can let the routine finish
                // as is instead of relying on when the game actually ends
                WiresArcade arcade = m_GameMode as WiresArcade;
                if (arcade)
                    duration = arcade.arcadeLength;
                else
                    m_GameMode.OnGameFinished += OnGameFinished;
            }

            StartCoroutine(RiseRoutine(duration));
        }

        /// <summary>
        /// Notify that the game has finished
        /// </summary>
        private void OnGameFinished()
        {
            StopCoroutine("RiseRoutine");
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Helper function for drawing visualization of rising sparks path
        /// </summary>
        /// <param name="localPosition">Local position of spark</param>
        /// <returns>World space position</returns>
        private Vector3 TransformStatePosition(Vector3 localPosition)
        {
            if (m_OverrideAnchor)
                return m_OverrideAnchor.TransformPoint(localPosition);
            else
                return localPosition;
        }
        
        void OnDrawGizmos()
        {
            Vector3 start = Vector3.zero, end = Vector3.zero, origin = Vector3.zero;

            if (Application.isPlaying || m_OverrideAnchor)
            {
                start = TransformStatePosition(m_StartingState.m_Position);
                end = TransformStatePosition(m_EndingState.m_Position);
            }
            else
            {
                // We use parent as default (only gets set in awake, so it's not avaliable in scene view)
                m_OverrideAnchor = transform.parent;

                start = TransformStatePosition(m_StartingState.m_Position);
                end = TransformStatePosition(m_EndingState.m_Position);

                m_OverrideAnchor = null;
            }

            // Line from us
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, start);

            // Line from anchor
            Gizmos.color = Color.blue;
            if (m_OverrideAnchor)
                Gizmos.DrawLine(m_OverrideAnchor.position, start);
            else if (transform.parent)
                Gizmos.DrawLine(transform.parent.position, start);

            // Path
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, end);

            // Size
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(start, Vector3.Scale(transform.lossyScale, m_StartingState.m_Scale));
            Gizmos.DrawWireCube(end, Vector3.Scale(transform.lossyScale, m_EndingState.m_Scale));
        }
        #endif
    }
}
