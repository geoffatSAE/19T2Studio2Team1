using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Wires
{
    /// <summary>
    /// Interface for any classes that the player can interact with via selection
    /// </summary>
    public interface IInteractive
    {
        /// <summary>
        /// If jumper is allowed to interact with this object
        /// </summary>
        /// <param name="jumper">Interacting jumper</param>
        /// <returns>If jumper can interact</returns>
        bool CanInteract(SparkJumper jumper);

        /// <summary>
        /// Player has interacted with this object. Is called only when CanInteract returns true
        /// </summary>
        /// <param name="jumper">Interacting jumper</param>
        void OnInteract(SparkJumper jumper);
    }
}
