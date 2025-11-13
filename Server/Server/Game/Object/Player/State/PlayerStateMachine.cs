using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class PlayerStateMachine
{
    private IPlayerState _currentState;
    public IPlayerState Current => _currentState;

    public void ChangeState(IPlayerState newState, Player player)
    {
        _currentState?.Exit(player);
        _currentState = newState;
        _currentState?.Enter(player);

        player.CurrentState = _currentState;

        UpdateState(newState, player);
    }

    public void Update(Player player)
    {
        _currentState?.Execute(player);
    }

    private void UpdateState(IPlayerState newState, Player player)
    {
        // 상태 동기화
        if (newState is Player_IdleState)           player.State = CreatureState.Idle;
        else if (newState is Player_MovingState)    player.State = CreatureState.Moving;
        else if (newState is Player_SkillState)     player.State = CreatureState.Skill;
        else if (newState is Player_AttackState)    player.State = CreatureState.Attack;
        else if (newState is Player_OperateState)   player.State = CreatureState.Operate;
        else if (newState is Player_RestState)      player.State = CreatureState.Rest;
        else if (newState is Player_StunState)      player.State = CreatureState.Stun;
        else if (newState is Player_DeadState)      player.State = CreatureState.Dead;

        player.SendStatePacket();
    }
}

