using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Numerics;

public class Player_SkillState : IPlayerState, IReceivesMoveCommand, IReceivesStopCommand
{
    private readonly ISkill _handler;
    public ISkill Handler {  get { return _handler; } }
    private readonly SkillContext _ctx;

    private int _tStartTick, _tHitTick, _tEndTick;
    private bool _didHit, _forceEnd;

    private Vector3? _currentDestination = null;

    public Player_SkillState(ISkill handler, SkillContext ctx)
    {
        _handler = handler;
        _ctx = ctx;
        _ctx.AttachFinishHandler(RequestFinish);
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Skill;
        player.SendStatePacket();

        int nowTick = TimeUtil.LastTick;
        _tStartTick = nowTick;

        float durSec = _handler.GetDuration();
        _tEndTick = unchecked(_tStartTick + (int)MathF.Round(durSec * 1000f));

        //float hitSec;
        //_tHitTick = unchecked(_tStartTick + (int)MathF.Round(hitSec * 1000f));

        _handler.OnEnter(player, _ctx);
    }

    public void Execute(Player player)
    {
        int now = TimeUtil.LastTick;

        if (_forceEnd || TimeUtil.IsPastOrNow(now, _tEndTick))
        {
            ChangeState(player);
            return;
        }

        if (!_didHit && TimeUtil.IsPastOrNow(now, _tHitTick))
        {
            _handler.OnHit(player, _ctx);
            _didHit = true;
        }

        if(HandleMovementCompletion(player))
            OnStopCommand(player, null);

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

    // (스킬 중에) 이동 시 목적지까지 도착했는가?
    private bool HandleMovementCompletion(Player player)
    {
        if (_currentDestination.HasValue)
        {
            Vector3 playerPos = player.PosInfo.ToVector();

            float distanceSq = Vector3.DistanceSquared(playerPos, _currentDestination.Value);

            if (distanceSq < 0.05)
                return true;
        }
        return false;
    }
    public void OnMoveCommand(Player player, C_Move move)
    {
        Console.WriteLine("@ OnMoveCommand : Skill");

        if (_handler.CanMoveDuringCast)
        {
            // (A) 이 스킬은 시전 중 이동 허용
            player.SendMoveSyncPacket(
                move.TargetPosition,
                _handler.MoveSpeedMultiplier
            );

            _currentDestination = move.TargetPosition.ToVector();
            _handler.OnMove(player);
        }
        else
        {
            if (_handler.CanStopSkill)
            {
                player.ChangeState(new Player_MovingState(move));
                player.SendMoveSyncPacket(move.TargetPosition);
            }
            else
            {
                // (B) 시전 중 이동 불가 스킬
                // 지금은 못 움직이니까 예약
                //player.SendStopPacket(StopReason.StopMoveOnly);

                // 2) 나중에 스킬이 끝나면 바로 이동시키기 위해 의도를 큐에 넣음
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
    public void OnStopCommand(Player player, C_Stop stopPacket)
    {
        _currentDestination = null;
        _handler.OnStop(player);
    }
    public void RequestFinish(SkillFinishReason reason = SkillFinishReason.EarlyEnd)
    {
        _forceEnd = true;
    }
}

