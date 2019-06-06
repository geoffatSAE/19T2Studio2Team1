using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{
    [CreateAssetMenu]
    public class Scenario : ScriptableObject
    {
        [SerializeField]
        ScenarioManager m_Manager;      // Manager of the scenario
    }
}
