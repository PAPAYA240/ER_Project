using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

public class Player_MovingState : IPlayerState, IReceivesMoveCommand
{
    // ==== 클라 주도 이동에 맞춘 최소 상태 ====
    private bool _isTargetOn = false;
    private int _targetId;
    private Vector3 _targetPos; // 지형 목적지 or 최근 타겟 위치(참조용)

    // 사거리/도착 판정 파라미터 (필요시 테이블로 이관)
    private const float ATTACK_RANGE = 3.0f; // 기본 평타 사거리
    private const float STOP_RANGE = 0.10f; // 지형 이동 도착 허용 반경
    private const float DEST_CHANGE_EPS = 0.05f; // 목적지 미세변경 무시

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
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Moving;
        player.SendAnimPacket("RUN", 0.1f);
    }

    public void Execute(Player player)
    {
        if (player == null || player.Room == null)
            return;

        var serverPos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);

        // 타겟팅 이동이면, 매 틱 타겟 현재 좌표로 갱신
        if (_isTargetOn)
        {
            GameObject target = player.FindTarget(_targetId); 
            if (target == null || target.State == CreatureState.Dead)
            {
                // 타겟을 잃었으면 지형 이동 모드로 전환
                _isTargetOn = false;
                _targetId = 0;
            }
            else
            {
                _targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);

                // 사거리 들어오면 Attack 으로 전환
                if (Vector3.Distance(serverPos, _targetPos) <= ATTACK_RANGE)
                {
                    player.ChangeState(new Player_AttackState(_targetId, ATTACK_RANGE));
                    return;
                }
            }

            return;
        }

        if (Vector3.Distance(serverPos, _targetPos) <= STOP_RANGE)
        {
            player.ChangeState(new Player_IdleState());
            return;
        }
    }

    public void Exit(Player player)
    {

    }

    // C_Move가 연속으로 들어올 때 "상태 재진입 없이" 목표지/타겟만 갱신
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

        // 바뀐 경우에만 갱신
        bool destChanged = (newTargetPos - _targetPos).LengthSquared() > (DEST_CHANGE_EPS * DEST_CHANGE_EPS);
        bool targetChanged = (newIsTargetOn != _isTargetOn) || (newTargetId != _targetId);

        if (!destChanged && !targetChanged)
            return;

        _isTargetOn = newIsTargetOn;
        _targetId = newTargetId;
        _targetPos = newTargetPos;

        if (_isTargetOn)
        {
            // 타겟팅 이동이면 최신 타겟 좌표로 보정
            GameObject target = player.FindTarget(_targetId);
            if (target != null)
            {
                _targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);
            }
        }

        // 위치 정보 목적지 통보
        player.SendMovePacket(new PositionInfo(player.PosInfo), new RotationInfo(player.RotInfo));
    }
}

