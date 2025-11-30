using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public StateMachine StateMachine;
    Transform Target;
    [Header("设置，要挂一个nav agent在本体上")]
    //视野范围
    public float SightRange = 10f;
    //攻击距离
    public float AttackRange = 2.0f;
    //攻击间隔
    public float AttackDuation = 1.5f;
    //巡逻点
    public Transform[] PatrolPoints;
    public PatrolState PatrolState;
    // Start is called before the first frame update
    private void Awake()
    {
        Target = GameObject.FindGameObjectWithTag("Player").transform;

        StateMachine = new StateMachine();
        PatrolState = new PatrolState(this);
    }
    void Start()
    {
        StateMachine.ChangeState(PatrolState);
    }

    // Update is called once per frame
    void Update()
    {
        StateMachine.Update();
    }
}
