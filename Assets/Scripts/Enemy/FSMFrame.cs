using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void OnEnter();  // ����״̬ʱ���������Ŷ��������ü�ʱ����
    void Tick();     // ÿ֡�߼����൱��Update��
    void OnExit();   // �˳�״̬ʱ�������������ݡ�ֹͣ������
}
public class StateMachine
{
    private IState _currentState;

    public void ChangeState(IState newState)
    {
        if(_currentState!=null) _currentState.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
        Debug.Log($"State changed to {newState.GetType().Name}");
    }
    public void Update()
    {
        if(_currentState!=null)_currentState.Tick();
    }
}