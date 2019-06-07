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

        protected Transform m_Target;                   // The target of this obstacle

        public virtual void InitializeTarget(Transform target)
        {

        }

        // IObstacle Interface
        public void TakeDamage(RaycastHit hit)
        {
            OnTargetDestroyed(this);
        }
    }
}
