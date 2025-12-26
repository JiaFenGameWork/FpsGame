using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    AgentUT agentUT;
    List<Agent> agentList;
    // Start is called before the first frame update
    void Start()
    {
        agentList = new List<Agent>();
        agentUT = new AgentUT(1);
        Agent[] agents = GameObject.FindObjectsOfType<Agent>();
        foreach (Agent agent in agents)
        {
            agentUT.Register(agent);
            agentList.Add(agent);
        }
            
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var b = 0;
        }
        foreach (Agent agent in agentList)
        {
            agentUT.UpdateAgentCell(agent);
        }
    }
}