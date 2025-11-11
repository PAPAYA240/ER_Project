using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Numerics;

public class Player_StunState : IPlayerState
{
    private double _startTime;      // 기절 시작시간
    private Vector3 _startPos;      // 시작 위치
    private bool _isMoving;         // 움직이고 있는지

    public class StunStateDesc
    {
        public float Duration;      // 기절 지속 시간
        public float Speed;         // 밀리는 속도
        public Vector3 EndPos;      // 어디로. 여기 들어오는 위치는 계산이 끝났다고 가정.
    }

    StunStateDesc _desc;

    public Player_StunState(StunStateDesc desc)
    {
        _desc = desc;
    }

    public void Enter(Player player)
    {
        _startTime = TimeUtil.UtcSec();
        _startPos = player.Position;

        // 이동이 필요한 경우
        if ((_startPos - _desc.EndPos).Length() > float.Epsilon)
        {
            _isMoving = true;
        }
        else // 제자리 기절 (이동 없이 바로 기절)
        {
            _isMoving = false; // 이동 없음
        }

        player.State = CreatureState.Stun;
        player.SendStatePacket();
        player.SendAnimPacket("WAIT", 0.1f);
    }

    public void Execute(Player player)
    {
        // 전체 기절 지속 시간 확인
        double elapsedTime = TimeUtil.UtcSec() - _startTime;
        if (elapsedTime >= _desc.Duration)
        {
            // 기절 지속 시간 종료, IDLE 상태로 전환
            player.ChangeState(new Player_IdleState()); 
            return;
        }
        // 이동 처리 
        if (_isMoving)
        {
            Vector3 currentPos = player.Position;
            float distanceToTravel = Vector3.Distance(_startPos, _desc.EndPos);

            // 이동에 필요한 총 시간 계산
            float knockbackMovementTotalTime = (distanceToTravel > float.Epsilon) ? (distanceToTravel / _desc.Speed) : 0f;

            double knockbackElapsedTime = TimeUtil.UtcSec() - _startTime;

            if (knockbackElapsedTime < knockbackMovementTotalTime)
            {
                // 아직 이동 중
                float t = Math.Clamp((float)(knockbackElapsedTime / knockbackMovementTotalTime), 0, 1);
                Vector3 nextPos = Vector3.Lerp(_startPos, _desc.EndPos, t);

                // 플레이어의 위치 업데이트 요청 (클라이언트에게 전송)
                if (nextPos != currentPos) // 위치가 실제로 변경되었을 때만 전송
                {
                    player.Position = nextPos;
                    player.SendSkillMotion(
                        type: SkillMotionType.Transform,
                        start: player.Position,
                        end: nextPos
                    );
                    //player.SendMoveSyncPacket(player.PosInfo);
                    //player.SendMovePacket(); // 또는 특정 스킬 모션 패킷으로
                }
            }
            else
            {
                // 이동 시간 종료 (목표 위치 도달 또는 계산된 시간 경과)
                _isMoving = false; // 이동 종료
                // 플레이어를 최종 목표 위치에 정확히 안착
                if (player.Position != _desc.EndPos)
                {
                    player.Position = _desc.EndPos;
                    player.SendMoveSyncPacket(player.PosInfo);
                }
            }
        }
    }

    public void Exit(Player player)
    {
        
    }
}

