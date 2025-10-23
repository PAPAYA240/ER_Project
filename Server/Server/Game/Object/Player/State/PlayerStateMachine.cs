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
        // 같은 상태로의 중복 전환 방지
        //if (_currentState != null && newState != null &&
        //_currentState.GetType() == newState.GetType())           
        //    return;

        _currentState?.Exit(player);
        _currentState = newState;
        _currentState?.Enter(player);

        player.CurrentState = _currentState;
    }

    public void Update(Player player)
    {
        _currentState?.Execute(player);
    }
}

