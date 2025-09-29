using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_AttackState : IPlayerState
{
    private GameObject _target;
    private GameObject _nextTarget;

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

