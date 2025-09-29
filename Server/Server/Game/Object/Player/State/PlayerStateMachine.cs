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

        player.CurState = _currentState;
    }

    public void Update(Player player)
    {
        _currentState?.Execute(player);
    }
}

