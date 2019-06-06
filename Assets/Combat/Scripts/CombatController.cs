using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    /// <summary>
    /// Base controller for the player. Implements shared functionality across multiple platforms
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        /// <summary>
        /// Fires a single laser from origin at given direction
        /// </summary>
        /// <param name="origin">Origin of shot</param>
        /// <param name="direction">Direction of shot</param>
        protected void Fire(Vector3 origin, Quaternion direction)
        {
            Fire(new Ray(origin, direction * Vector3.forward));
        }

        /// <summary>
        /// Fires a single laser using ray
        /// </summary>
        /// <param name="ray">Ray to cast</param>
        protected void Fire(Ray ray)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit);
            }
        }

        /// <summary>
        /// Handles a confirmed hit after firing
        /// </summary>
        /// <param name="hit">Hit result</param>
        protected virtual void HandleHit(RaycastHit hit)
        {

        }
    }
}
