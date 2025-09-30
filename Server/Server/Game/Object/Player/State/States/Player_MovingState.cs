using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

public class Player_MovingState : IPlayerState, IReceivesMoveCommand
{
    private bool _isTargetOn = false;
    private int _targetId;
    private Vector3 _targetPos;

    private const int HUNDREDS_MS = 100;
    private const int THOUSANDS_MS = 1000;

    // 목적지 미세 변경 무시 임계값(너무 잦은 경로 재계산 방지)
    private const float DEST_CHANGE_EPS = 0.05f;

    private long _nextCalcPathTick = 0;
    private long _nextMoveTick = 0;
    private long _nextWaitTick = Environment.TickCount64;

    public Player_MovingState(C_Move packet)
    {
        _isTargetOn = packet.IsTargetOn;
        _targetId = packet.TargetId;
        _targetPos = new Vector3
        {
            X = packet.TargetPosition.PosX,
            Y = packet.TargetPosition.PosY,
            Z = packet.TargetPosition.PosZ
        };

        _nextMoveTick = 0;
        _nextWaitTick = Environment.TickCount64;
        _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Moving;
        player.SendAnimPacket("RUN", 0.1f);
        // 최초 경로 계산
        player.Get_CalculatePath(_targetPos);
    }

    public void Execute(Player player)
    {
        // 타겟팅 이동이면, 매 틱 타겟 현재 좌표로 갱신
        if (_isTargetOn)
        {
            GameObject target = player.FindTarget(_targetId); 
            if (target == null || target.State == CreatureState.Dead)
            {
                // 타겟을 잃었으면 일반 목적지 이동으로 강등
                _isTargetOn = false;
                _targetId = 0;
            }
            else
            {
                _targetPos = new Vector3
                {
                    X = target.PosInfo.PosX,
                    Y = target.PosInfo.PosY,
                    Z = target.PosInfo.PosZ
                };
            }
        }

        // 주기 경로 갱신
        if (_nextCalcPathTick < Environment.TickCount64)
        {
            player.Get_CalculatePath(_targetPos);
            _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
        }

        // 이동 및 브로드캐스트
        player.Get_MoveAlongPath();
        player.SendMovePacket(new PositionInfo(player.PosInfo), new RotationInfo(player.RotInfo));

        // 클라(보간) 위치 기반 도착/사거리 판정
        Vector3 clientPos = new Vector3
        {
            X = player.ClientPos.PosX,
            Y = player.ClientPos.PosY,
            Z = player.ClientPos.PosZ
        };

        if (_isTargetOn)
        {
            // 사거리 안에 들어가면 Attack으로 전환
            float attackRange = 3.0f;   // TODO: 무기/스킬별 실제 사거리로 교체
            if (Vector3.Distance(clientPos, _targetPos) <= attackRange)
            { 
                player.ChangeState(new Player_AttackState(_targetId, attackRange));
                return;
            }
        }
        else
        {
            // << 고침: 이전 코드에선 //else 주석 때문에 항상 실행됨 >>
            float stopRange = 0.1f;
            if (Vector3.Distance(clientPos, _targetPos) <= stopRange)
            {
                player.ChangeState(new Player_IdleState());
                return;
            }
        }
    }

    public void Exit(Player player)
    {
        _nextMoveTick = 0;
        _nextCalcPathTick = Environment.TickCount64 + THOUSANDS_MS;
    }

    // ===========================
    // C_Move가 연속으로 들어올 때 "상태 재진입 없이" 목표지/타겟만 갱신
    // ===========================
    public void OnMoveCommand(Player player, C_Move packet)
    {
        bool newIsTargetOn = packet.IsTargetOn;
        int newTargetId = packet.TargetId;
        Vector3 newTargetPos = new Vector3
        {
            X = packet.TargetPosition.PosX,
            Y = packet.TargetPosition.PosY,
            Z = packet.TargetPosition.PosZ
        };

        // 목적지/타겟이 유의미하게 바뀐 경우에만 갱신
        bool destChanged = (newTargetPos - _targetPos).LengthSquared() > (DEST_CHANGE_EPS * DEST_CHANGE_EPS);
        bool targetChanged = (newIsTargetOn != _isTargetOn) || (newTargetId != _targetId);

        if (!destChanged && !targetChanged)
            return;

        _isTargetOn = newIsTargetOn;
        _targetId = newTargetId;
        _targetPos = newTargetPos;

        // 타겟팅 이동이면 즉시 현재 타겟 좌표로 한 번 맞춰줌(후속 틱에서 계속 따라감)
        if (_isTargetOn)
        {
            GameObject target = player.FindTarget(_targetId);
            if (target != null)
            {
                _targetPos = new Vector3
                {
                    X = target.PosInfo.PosX,
                    Y = target.PosInfo.PosY,
                    Z = target.PosInfo.PosZ
                };
            }
        }

        // 다음 틱에 바로 경로 재계산되도록 트리거
        _nextCalcPathTick = 0;

        // 서버가 경로 기반으로 움직이는 경우, 목적지 갱신
        player.Get_CalculatePath(_targetPos);

        // 원격들에만 목적지 통보(본인 에코 X) — 네 유틸에 맞춰 필요 시 유지
        player.SendMovePacket(new PositionInfo(player.PosInfo), new RotationInfo(player.RotInfo));
    }
}

