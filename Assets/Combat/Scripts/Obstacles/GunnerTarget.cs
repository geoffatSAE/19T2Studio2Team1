using System.Collections;
using UnityEngine;

namespace TO5.Combat
{
    public class GunnerTarget : Target
    {
        public float m_Accuracy = 0.8f;         // Accuracy of the gunner, 0 means complete accuracy
        public float m_AccuracyOffset = 1f;     // Offset of inaccurate shots, so we don't wildly shoot everywhere
        public float m_Damage = 5f;             // The damage this gunner does

        [SerializeField] private float m_MinShootDelay = 1f;    // Min time before shooting again
        [SerializeField] private float m_MaxShootDelay = 2f;    // Max time before shooting again

        /// <summary>
        /// Repeatedly fires using min and max shoot delay
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator FireRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(Random.Range(m_MinShootDelay, m_MaxShootDelay));
                Fire();
            }
        }

        /// <summary>
        /// Fires at the target, damaging it if able to
        /// </summary>
        private void Fire()
        {
            if (m_Target)
            {
                // TODO: Replace with a cone (random direction in cone)
                Vector3 shootAt = m_Target.transform.position;
                if (m_Accuracy > 0)
                {
                    float offset = Random.Range(0f, m_Accuracy) * m_AccuracyOffset;
                    shootAt += Random.insideUnitSphere.normalized * offset;
                }

                Vector3 direction = (shootAt - transform.position).normalized;

                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide))
                {
                    GameObject gameObject = hit.collider.gameObject;

                    // Handle damaging player
                    {
                        CombatController controller = gameObject.GetComponent<CombatController>();
                        if (controller)
                            controller.TakeDamage(m_Damage);
                    }
                }
            
                //Debug.DrawLine(transform.position, shootAt, Color.magenta, 2f);
            }
        }

        // Target Interface
        public override void ActivateTarget(Transform target, TargetAnchor anchor)
        {
            base.ActivateTarget(target, anchor);
            StartCoroutine(FireRoutine());
        }

        // Target interface
        public override void DeacativateTarget()
        {
            StopCoroutine(FireRoutine());
            base.DeacativateTarget();
        }
    }
}
