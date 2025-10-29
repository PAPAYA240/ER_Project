using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Player_SkillState : IPlayerState, IReceivesMoveCommand
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
        _tEnd = _tStart.AddSeconds(_handler.GetDuration());

        _handler.OnEnter(player, _ctx);
    }

    public void RequestFinish() => _forceEnd = true;

    public void Execute(Player player)
    {
        var now = DateTime.UtcNow;

        if (_forceEnd || now >= _tEnd)
        {
            ChangeState(player);
        }

        if (!_didHit && now >= _tHit)
        {
            _handler.OnHit(player, _ctx);
            _didHit = true;
        }

        _handler.OnTick(player, _ctx);
    }

    public void Exit(Player player)
    {
        _handler.OnExit(player, _ctx);
    }

    private void ChangeState(Player player)
    {
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

            player.ChangeState(new Player_MovingState(cmd));
            player.SendMoveSyncPacket(dest.TargetPos);
        }
        else
        {
            player.ChangeState(new Player_IdleState());
        }
    }

    public void OnMoveCommand(Player player, C_Move move)
    {
        if (_handler.CanMoveDuringCast)
        {
            // (A) 이 스킬은 시전 중 이동 허용

            player.SendMoveSyncPacket(
                move.TargetPosition,
                _handler.MoveSpeedMultiplier
            );
        }
        else
        {
            // (B) 시전 중 이동 불가 스킬
            // 지금은 못 움직이니까 예약
            player.SendStopPacket(StopReason.StopMoveOnly);

            // 2) 나중에 스킬이 끝나면 바로 이동시키기 위해 의도를 큐에 넣는다.
            C_SetMoveTarget deferred = new C_SetMoveTarget()
            {
                IsGround = !move.IsTargetOn,
                TargetId = move.TargetId,
                TargetPos = move.TargetPosition
            };
            player.EnqueueMove(deferred);
        }
    }
}

