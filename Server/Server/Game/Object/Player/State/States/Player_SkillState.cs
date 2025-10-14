using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_SkillState : IPlayerState
{
    private readonly ISkillHandler _handler;
    private readonly SkillSpec _spec;
    private readonly SkillContext _ctx;

    private DateTime _tStart, _tHit, _tEnd;
    private bool _didHit;

    public Player_SkillState(ISkillHandler handler, SkillSpec spec, SkillContext ctx)
    {
        _handler = handler;
        _spec = spec;
        _ctx = ctx;
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Skill;
        player.SendStatePacket();

        _tStart = DateTime.UtcNow;
        _tHit = _tStart.AddSeconds(_spec.Windup);
        _tEnd = _tHit.AddSeconds(_spec.Backswing);

        _handler.OnEnter(player, _spec, _ctx);
    }

    public void Execute(Player player)
    {
        var now = DateTime.UtcNow;
        if (!_didHit && now >= _tHit)
        { _handler.OnHit(player, _spec, _ctx); _didHit = true; }

        if (now >= _tEnd)
        { player.ChangeState(new Player_IdleState()); }
    }

    public void Exit(Player player)
    {
        _handler.OnExit(player, _spec, _ctx);
    }
}

