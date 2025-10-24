using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_SkillState : IPlayerState
{
    private readonly ISkillHandler _handler;
    public ISkillHandler Handler {  get { return _handler; } }
    private readonly SkillContext _ctx;

    private DateTime _tStart, _tHit, _tEnd;
    private bool _didHit, _forceEnd;

    public Player_SkillState(ISkillHandler handler, SkillContext ctx)
    {
        _handler = handler;
        _ctx = ctx;
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Skill;
        player.SendStatePacket();

        _tStart = DateTime.UtcNow;
        //_tHit = _tStart.AddSeconds(_spec.Windup);
        //_tEnd = _tHit.AddSeconds(_spec.Backswing);
        _tEnd = _tStart.AddSeconds(_handler.GetDuration());

        _handler.OnEnter(player, _ctx);
    }

    public void RequestFinish() => _forceEnd = true;

    public void Execute(Player player)
    {
        var now = DateTime.UtcNow;

        if (!_didHit && now >= _tHit)
        {
            _handler.OnHit(player, _ctx);
            _didHit = true;
        }

        _handler.OnTick(player, _ctx);

        if (_forceEnd || now >= _tEnd)
        {
            IPlayerState next;
            if (player.Intent.TryConsume(out var dest))
            {
                if (player.TryHandleMoveWithTokens(dest))
                    return;

                C_Move cmd = new C_Move()
                {
                    IsTargetOn = !dest.IsGround,
                    TargetId = dest.TargetId,
                    TargetPosition = dest.TargetPos,
                };
                next = new Player_MovingState(cmd);
                Console.WriteLine($"SkillState/ x - {dest.TargetPos.PosX}, z - {dest.TargetPos.PosZ}");
                player.SendMoveSyncPacket(dest.TargetPos);
            }
            else
                next = new Player_IdleState();

            player.ChangeState(next);
        }
    }

    public void Exit(Player player)
    {
        _handler.OnExit(player, _ctx);
    }
}

