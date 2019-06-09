using System;
using UnityEngine;

namespace TO5.Combat
{
    /// <summary>
    /// The anchor for a target to attach to
    /// </summary>
    public class TargetAnchor : MonoBehaviour
    {
        // If a target is anchored
        public bool hasTarget { get { return m_Target != null; } }

        private Target m_Target;     // The target anchored

        public void AttachTarget(Target target)
        {
            m_Target = target;
            if (m_Target)
                m_Target.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }

        public void DetachTarget()
        {
            m_Target = null;
        }
    }
}
