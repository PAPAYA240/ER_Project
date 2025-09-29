using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_AttackState : IPlayerState
{
    private GameObject _target;
    private GameObject _nextTarget;
    private float _attackSpeed = 1.0f;  // TEMP : 1초 기본 공격 속도
    private float _attackTimer;
    private bool _pendingTargetChange;

    public Player_AttackState(GameObject initialTarget = null)
    {
        _target = initialTarget;
    }

    public void Enter(Player player)
    {

    }

    public void Execute(Player player)
    {

    }

    public void Exit(Player player)
    {

    }
}

