using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentUT
{
    public readonly float CellSize;
    public readonly float _InvCellSize;

    private Dictionary<Vector3Int, List<Agent>> Cells;
    private Dictionary<Agent, Entry> Entries;

    int _queryId = 1;

    private struct Entry
    {
        public Vector3Int cellkey;
        public int CellIndex;
    }
    public AgentUT(float CellSize,int initialCellCapacity = 1024, int initialEntriesCapacity = 16384)
    {
        CellSize = Mathf.Max(0.00001f, CellSize);
        _InvCellSize = 1/CellSize;

        Cells = new Dictionary<Vector3Int, List<Agent>>(initialEntriesCapacity);
        Entries = new Dictionary<Agent, Entry>(initialEntriesCapacity);
    }

    public void Register(Agent agent)
    {
        if (agent == null) return;
        if (Entries.ContainsKey(agent)) return;

        var key = WorldToKey(agent.Position);
        if(Cells.TryGetValue(key,out List<Agent> ag))
        {
            ag.Add(agent);
            Entries[agent] = new Entry() { cellkey = key, CellIndex = ag.Count - 1 };
        }
        else
        {
            ag = new List<Agent>();
            ag.Add(agent);
            Cells.Add(key, ag);
            Entries[agent] = new Entry() {cellkey = key, CellIndex = 0 };
        }
        
    }
    public void Remove(Agent agent,Vector3Int pos,int cellidx)
    {
        if(agent==null) return;
        if(!Cells.TryGetValue(pos, out List<Agent> agents)) return;
        int last =  agents.Count-1;
        if(last<0) return;
        if(cellidx == last)
        {
           agents.RemoveAt(last);
        }
        else
        {
            Agent temp = agents[last];
            // 更新被移动的 agent 的索引（Entry 是结构体，需要重新赋值回字典）
            if (Entries.TryGetValue(temp, out Entry entry))
            {
                entry.CellIndex = cellidx;
                Entries[temp] = entry; // 关键：将修改后的结构体重新赋值回字典
            }
            // 将最后一个元素移动到要删除的位置
            agents[cellidx] = temp;
            agents.RemoveAt(last);
        }
        // 从 Entries 字典中移除被删除的 agent
        Entries.Remove(agent);
    }
    public void UpdateAgentCell(Agent agent)
    {
        if (agent == null) return;
        if(!Entries.TryGetValue(agent,out Entry ent))
        {
            Register(agent);
            return;
        }
        Vector3Int newpos = WorldToKey(agent.Position);
       // Debug.Log(newpos);
        if(newpos==ent.cellkey) return;

        Remove(agent, ent.cellkey, ent.CellIndex);
        Register(agent);


    }
    Vector3Int WorldToKey(Vector3 pos)
    {
        return Vector3Int.FloorToInt(pos);
    }
}
