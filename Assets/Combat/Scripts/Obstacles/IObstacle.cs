using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    public interface IObstacle
    {
        /// <summary>
        /// Notify that the obstacle has been damaged
        /// </summary>
        /// <param name="hit"></param>
        void TakeDamage(RaycastHit hit);
    }
}
