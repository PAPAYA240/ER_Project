using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public struct SkillCollisionProposal
{
    public int requestId;
    public int Seq;                         // 최신만

    public Vector3 collisionPos;

    public int CandidateTargetId;           // BehindBlocked 에서 사용된 타겟 ID
    public float Speed;                     // TEMP: 대쉬 속도
                                            //public float Radius, MaxDistance;     // 힌트(서버 스펙으로 재검증)
}

public interface ISkill
{
    // 상태 진입
    void OnEnter(Player p, SkillContext ctx);

    // 타격 타이밍(예: tHit)
    void OnHit(Player p, SkillContext ctx);

    void OnCollision(Player p);

    // 매 틱(Streaming 필요 시만 Player_SkillState에서 호출)
    void OnTick(Player p, SkillContext ctx);

    // 클라에서 제안 패킷 도착 시 호출
    void OnPropose(Player p, in SkillCollisionProposal prop);

    // 상태 종료
    void OnExit(Player p, SkillContext ctx);

    bool CanCast(Player p, SkillContext ctx);

    // 스킬 중에 움직이는가?
    void OnMove(Player p);
    void OnStop(Player p);

    bool CanMoveDuringCast { get; }
    float MoveSpeedMultiplier { get; }

    bool CanStopSkill { get; }
    #region Utils
    float GetDuration();

    KeyCode GetKeyCode();
    #endregion
}

