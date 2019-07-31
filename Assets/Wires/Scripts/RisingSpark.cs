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

        [Header("Game")]
        [SerializeField] private State m_StartingState;                 // State we start at
        [SerializeField] private State m_EndingState;                   // State we end at
        [SerializeField] private Transform m_OverrideAnchor;            // The transform we move locally too (will use parent by default)
        [SerializeField] private WiresGameMode m_GameMode;              // Game mode to sync with
        [SerializeField] private float m_FallbackDuration = 600f;       // Fallback duration to use if game mode has no set game length

        [Header("Post Game")]
        [SerializeField] private State m_EndGameState;                  // State to transition to after game has finished
        [SerializeField] private float m_EndGameFallbackDuration = 10f; // Fallback duration to use if end game state has no same length

        [Min(0.1f)] public float m_StartRotationInterval = 1.5f;        // Initial rotation interval
        [Min(0.1f)] public float m_EndRotationInterval = 0.5f;          // Rotation interval to interpolate to during rise   
        public Transform m_RotationOverride;                            // Override transform to rotate

        private State m_FinishingState;             // State we were at when game finished
        private float m_RotationInterval = 1.5f;    // Time for a full rotation (lerped between start and end based on rise)
        private float m_RotationTime = 0f;          // Time passed of current rotation
        private Quaternion m_FromOrientation;       // Orientation to rotate from
        private Quaternion m_ToOrientation;         // Orientation to rotate to

        void Awake()
        {
            if (!m_OverrideAnchor)
                m_OverrideAnchor = transform.parent;

            // Default spot
            InterpolateGameState(0f);

            if (m_GameMode)
                m_GameMode.OnGameStarted += OnGameStart;
            else
                Debug.LogWarning("Rising spark will not play as no game mode has been provided");

            Transform rotationTransform = m_RotationOverride ? m_RotationOverride : transform;
            m_FromOrientation = rotationTransform.rotation;
            m_ToOrientation = Random.rotation;
            m_RotationInterval = m_StartRotationInterval;
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
        /// Interpolates transform based on game start and end states
        /// </summary>
        /// <param name="alpha">Alpha of lerp</param>
        private void InterpolateGameState(float alpha)
        {
            transform.localPosition = Vector3.Lerp(m_StartingState.m_Position, m_EndingState.m_Position, alpha);
            transform.localScale = Vector3.Lerp(m_StartingState.m_Scale, m_EndingState.m_Scale, alpha);
            m_RotationInterval = Mathf.Lerp(m_StartRotationInterval, m_EndRotationInterval, alpha);
        }

        /// <summary>
        /// Interpolates transform based on finishing and end game states
        /// </summary>
        /// <param name="alpha">Alpha of lerp</param>
        private void InterpolatePostGameState(float alpha)
        {
            transform.localPosition = Vector3.Lerp(m_FinishingState.m_Position, m_EndGameState.m_Position, alpha);
            transform.localScale = Vector3.Lerp(m_FinishingState.m_Scale, m_EndGameState.m_Scale, alpha);
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
                    InterpolateGameState(alpha);

                    yield return null;
                }
            }

            InterpolateGameState(1f);
        }

        /// <summary>
        /// Routine for handling the interpolation of transform after the game
        /// </summary>
        /// <param name="duration">Duration of approach</param>
        private IEnumerator ApproachRoutine(float duration)
        {
            if (duration > 0f)
            {
                m_FinishingState.m_Position = transform.localPosition;
                m_FinishingState.m_Scale = transform.localScale;

                float end = Time.time + duration;
                while (Time.time < end)
                {
                    float alpha = 1f - Mathf.Clamp01((end - Time.time) / duration);
                    InterpolatePostGameState(alpha);

                    yield return null;
                }
            }

            InterpolatePostGameState(1f);
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
                m_GameMode.OnGameFinished += OnGameFinished;

                // Fix rise duration to match length of game modes with fixed time
                WiresArcade arcade = m_GameMode as WiresArcade;
                if (arcade)
                    duration = arcade.arcadeLength;                 
            }

            StartCoroutine(RiseRoutine(duration));
        }

        /// <summary>
        /// Notify that the game has finished
        /// </summary>
        private void OnGameFinished()
        {
            StopCoroutine("RiseRoutine");

            float duration = m_EndGameFallbackDuration;
            if (m_GameMode)
            {
                m_GameMode.OnGameFinished -= OnGameFinished;

                // Fix approach duration to length of post game time
                WiresArcade arcade = m_GameMode as WiresArcade;
                if (arcade)
                    duration = arcade.postArcadeLength;
            }

            StartCoroutine(ApproachRoutine(duration));
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
            Vector3 start = Vector3.zero, end = Vector3.zero, postEnd = Vector3.zero, origin = Vector3.zero; 

            if (Application.isPlaying || m_OverrideAnchor)
            {
                start = TransformStatePosition(m_StartingState.m_Position);
                end = TransformStatePosition(m_EndingState.m_Position);
                postEnd = TransformStatePosition(m_EndGameState.m_Position);
            }
            else
            {
                // We use parent as default (only gets set in awake, so it's not avaliable in scene view)
                m_OverrideAnchor = transform.parent;

                start = TransformStatePosition(m_StartingState.m_Position);
                end = TransformStatePosition(m_EndingState.m_Position);
                postEnd = TransformStatePosition(m_EndGameState.m_Position);

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

            // Post Path
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(end, postEnd);

            // Post Size
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(postEnd, Vector3.Scale(transform.lossyScale, m_EndGameState.m_Scale));
        }
        #endif
    }
}
