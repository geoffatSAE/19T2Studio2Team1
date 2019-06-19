using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TO5.Combat
{    public class Scenario : ScriptableObject
    {
        [SerializeField] private ScenarioManager m_Manager;      // Manager of the scenario, this is instantiated

        virtual public ScenarioManager CreateInstance(Transform origin)
        {
            ScenarioManager manager = Instantiate(m_Manager, origin.position, origin.rotation);
            if (manager)
                manager.InitializeScenario(this);

            return manager;
        }
    }
}
