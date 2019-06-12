using System;
using System.Collections;
using UnityEngine;

namespace TO5.Combat
{
    /// <summary>
    /// The anchor for a target to attach to. This should be placed behind cover
    /// </summary>
    public class TargetAnchor : MonoBehaviour
    {
        /// <summary>
        /// Delegate for when the transition to the vantage point has finished
        /// </summary>
        public delegate void VantagePointTransitionComplete();

        // If a target is anchored
        public bool hasTarget { get { return m_Target != null; } }

        // If anchor has a vantage point
        public bool hasVantagePoint { get { return m_VantagePoint != null; } }

        public VantagePointTransitionComplete OnTransitionComplete;     // Event for when transition has finished
        public Transform m_VantagePoint;                                // Transform for target to move to when exiting cover
        private Target m_Target;                                        // The target anchored

        private bool m_IsTransitioning = false;     // If target is being moved out of cover

        public void AttachTarget(Target target)
        {
            if (m_IsTransitioning)
                StopCoroutine(TransitionToVantagePoint(0f));

            m_Target = target;
            m_IsTransitioning = false;

            if (m_Target)
                m_Target.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }

        public void DetachTarget()
        {
            if (m_IsTransitioning)
                StopCoroutine(TransitionToVantagePoint(0f));

            m_IsTransitioning = false;
            m_Target = null;
        }

        public void MoveToVantagePoint(float duration)
        {
            if (hasTarget && hasVantagePoint)
            {
                if (!m_IsTransitioning)
                    StartCoroutine(TransitionToVantagePoint(duration));
            }
        }

        private IEnumerator TransitionToVantagePoint(float duration)
        {
            float end = Time.time + duration;

            m_IsTransitioning = true; 
            while (m_IsTransitioning)
            {
                if (!hasTarget || !hasVantagePoint)
                    break;

                // Move to vantage point (also rotate to vantage point rotation)
                float alpha = Mathf.Clamp01((Time.time - end) / duration);
                Vector3 position = Vector3.Lerp(transform.position, m_VantagePoint.position, alpha);
                Quaternion rotation = Quaternion.Slerp(transform.rotation, m_VantagePoint.rotation, alpha);
                m_Target.transform.SetPositionAndRotation(position, rotation);

                m_IsTransitioning = alpha < 1f;
                yield return null;               
            }

            m_IsTransitioning = false;

            if (OnTransitionComplete != null)
                OnTransitionComplete.Invoke();
        }
    }
}
