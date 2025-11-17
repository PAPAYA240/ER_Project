using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Threading;

public class Player_MovingState : IPlayerState, IReceivesMoveCommand
{
    // ==== 클라 주도 이동에 맞춘 최소 상태 ====
    private bool _isTargetOn = false;
    private int _targetId;
    private Vector3 _targetPos; // 지형 목적지 or 최근 타겟 위치(참조용)
    //private PositionInfo _positionInfo;

    // 사거리/도착 판정 파라미터 (필요시 테이블로 이관)
    private const float STOP_RANGE = 0.20f; // 지형 이동 도착 허용 반경
    private const float DEST_CHANGE_EPS = 0.05f; // 목적지 미세변경 무시

    private long _nextPathTick;
    private long _findTick = 10L;

    public Player_MovingState(C_Move packet)
    {
        _isTargetOn = packet.IsTargetOn;
        _targetId = packet.TargetId;
        _targetPos = new Vector3(packet.TargetPosition.PosX, packet.TargetPosition.PosY, packet.TargetPosition.PosZ);
        //_positionInfo = new PositionInfo(packet.TargetPosition);
        _nextPathTick = Environment.TickCount64;

        //Console.WriteLine($"@ Ctor - TargetPos : {_targetPos}");
        //Console.WriteLine($"@ Ctor - PositionInfo : {_positionInfo}");
    }

    public void Enter(Player player)
    {      
        player.SendAnimPacket("RUN", 0.1f);

        if (_isTargetOn)
        {
            var t = player.FindTarget(_targetId);
            if (t != null)
                _targetPos = t.Position;

            //Console.WriteLine($"@ Enter - TargetOn, TargetPos : {_targetPos}");

            // 클라 네비: 타겟 추격 시작
            player.SendSetMoveTarget(isGround: false, targetId: _targetId);
        }

        //player.SendSetMoveTarget(isGround: !_isTargetOn, targetId: _targetId, posOpt: _positionInfo);
        //Console.WriteLine($"@ Enter - TargetPos : {_targetPos}");
        //Console.WriteLine("@ Moving Enter");
    }

    public void Execute(Player player)
    {
        if (player == null || player.Room == null)
            return;

        long now = Environment.TickCount64;

        // 주기적으로 타겟 추적 갱신
        if (_isTargetOn && now >= _nextPathTick)
        {
            _nextPathTick = now + _findTick;
            var t = player.FindTarget(_targetId);
            if (t == null /*|| !t.IsAttackable()*/)
            {
                player.ChangeState(new Player_IdleState());
                return;
            }
            _targetPos = t.Position;
            //Console.WriteLine($"@ Execute - TargetPos : {_targetPos}");
        }

        // 사거리 진입하면 즉시 공격 전환(타겟 이동의 경우)
        if (_isTargetOn)
        {
            var t = player.FindTarget(_targetId);
            if (t != null)
            {
                float dist = Vector3.Distance(player.Position, t.Position);
                if (dist <= player.AttackRange)
                {
                    //Console.WriteLine($"@ Moving -> Attack : 사거리 안");
                    player.ChangeState(Player_AttackState.CreateAttackState(player, _targetId, chaseAllowed: true));
                    return;
                }
            }
        }
        else if(player.ReservedState is Player_OperateState)
        {
            if(player.Room.BeaconManager.IsInRange(player.Position, player.Beacon))
            {
                if(player.Room.BeaconManager.IsOperatable(player.Info.Player.Team, player.Beacon))
                {
                    player.ChangeState(player.ReservedState);
                    player.ReservedState = new Player_IdleState();
                }
                else
                    player.ChangeState(new Player_IdleState());
                return;
            }
        }

        // 도착 판정
        var serverPos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);
        if (Vector3.Distance(serverPos, _targetPos) <= STOP_RANGE)
        {
            if (_isTargetOn)
            {
                // 타겟 이동: 사정거리 진입 의미로 공격 전환
                player.ChangeState(Player_AttackState.CreateAttackState(player, _targetId, chaseAllowed: true));
            }
            else
            {
                player.ChangeState(new Player_IdleState());
            }
        }
    }

    public void Exit(Player player)
    {
        //Console.WriteLine("@ Moving Exit");
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
        //Console.WriteLine($"@ OnMoveCommand - TargetPos : {_targetPos}");

        if (_isTargetOn)
        {
            // 타겟팅 이동이면 최신 타겟 좌표로 보정
            GameObject target = player.FindTarget(_targetId);
            if (target != null)
            {
                _targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);
                //Console.WriteLine($"@ OnMoveCommand - _isTargetOn, TargetPos : {_targetPos}");
            }
        }

        //PositionInfo positionInfo = _positionInfo;
        //positionInfo.SetPosInfoFromVector3(_targetPos);
        //player.SendSetMoveTarget(isGround: !_isTargetOn, targetId: _targetId, posOpt: positionInfo);
    }   
}

