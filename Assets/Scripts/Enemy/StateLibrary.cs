using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolState : IState
{
    private EnemyController EnemyController;
    private int _currentPointIndex=0;
    public PatrolState(EnemyController enemyController)
    {
        EnemyController = enemyController;
    }
    public void OnEnter()
    {
        EnemyController.Agent.isStopped = false;
        EnemyController.Agent.speed = 2.0f;
        SetnextDestination();

    }

    public void OnExit()
    {
        throw new System.NotImplementedException();
    }

    public void Tick()
    {
        if (!EnemyController.Agent.pathPending && EnemyController.Agent.remainingDistance < 0.5f)
        {
            //比如3+1%3 = 1；3+2%3 = 2 3+3%3 = 0；以此类推
            _currentPointIndex = (_currentPointIndex+1)%EnemyController.PatrolPoints.Length;
            SetnextDestination();
        }
    }
    void SetnextDestination()
    {
        if(EnemyController.PatrolPoints.Length==0) return;
        EnemyController.Agent.SetDestination(EnemyController.PatrolPoints[_currentPointIndex].position);
    }
}
