using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void OnEnter();  // 进入状态时触发（播放动画、重置计时器）
    void Tick();     // 每帧逻辑（相当于Update）
    void OnExit();   // 退出状态时触发（清理数据、停止动画）
}
public class StateMachine
{
    private IState _currentState;

    public void ChangeState(IState newState)
    {
        if(_currentState!=null) _currentState.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
    }
    public void Update()
    {
        if(_currentState!=null)_currentState.Tick();
    }
}