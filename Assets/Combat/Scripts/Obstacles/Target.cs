using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    public class Target : MonoBehaviour, IObstacle
    {
        /// <summary>
        /// Delegate for a target being destroyed
        /// </summary>
        /// <param name="target">Destroyed target</param>
        public delegate void TargetDestroyed(Target target);

        public TargetDestroyed OnTargetDestroyed;       // Event for when destroyed
        protected Transform m_Target;                   // Target of this obstacle
        protected TargetAnchor m_Anchor;                // Anchor we are attached to

        /// <summary>
        /// Activates this target
        /// </summary>
        /// <param name="transform"></param>
        public virtual void ActivateTarget(Transform target, TargetAnchor anchor)
        {
            m_Target = target.transform;
            m_Anchor = anchor;

            if (m_Anchor)
                m_Anchor.AttachTarget(this);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Deactivates this target
        /// </summary>
        public virtual void DeacativateTarget()
        {
            gameObject.SetActive(false);

            if (m_Anchor)
                m_Anchor.DetachTarget();

            m_Anchor = null;
            m_Target = null;
        }

        // IObstacle Interface
        public void TakeDamage(RaycastHit hit)
        {
            OnTargetDestroyed(this);
        }
    }
}
