using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static ISkillHandler;

public sealed class Rozzi_W : ISkillHandler
{
    // 인터페이스 요구 프로퍼티의 백킹필드
    public int LastSeq { get; set; }
    public SkillCollisionProposal Latest { get; set; }

    private bool _committed;

    private Vector3 _finalEnd; private float _duration;

    SkillSpec _spec;

    public void OnEnter(Player p, SkillSpec spec, SkillContext ctx)
    {
        p.SendStopPacket(StopReason.StopMoveOnly);  // 이동 잠금
        p.SendAnimPacket("ROZZI_W", 0.05f);         // 애니 브로드캐스트
                                                    // TODO: 코스트/쿨타임 차감

        _committed = false;
        LastSeq = 0;
        Latest = default;
        _spec = spec;
    }

    public void OnHit(Player p, SkillSpec spec, SkillContext ctx)
    {
        return;
    }

    public void OnTick(Player p, SkillSpec spec, SkillContext ctx)
    {
        return;
    }

    public void OnExit(Player p, SkillSpec spec, SkillContext ctx)
    {
        // 종료 시 최종 보정
        p.PosInfo.PosX = _finalEnd.X;
        p.PosInfo.PosY = _finalEnd.Y;
        p.PosInfo.PosZ = _finalEnd.Z;
        p.SendMovePacket(new PositionInfo(p.PosInfo), new RotationInfo(p.RotInfo));

        p.Flags.IsInSkillMotion = false;
    }

    private void CommitMotionOnce(Player p, SkillSpec spec, Vector3 from, Vector3 end)
    {
        _finalEnd = end;

        float dist = Vector3.Distance(from, end);
        float speed = spec.limits.speed;
        float duration = MathF.Max(0.05f, dist / speed);

        p.SendSkillMotion(
             type: SkillMotionType.Dash,
             start: from,
             end: _finalEnd,
             duration: _duration,
             anim: ""/*spec.AnimName*/,
             curveId: "EaseOutCubic",
             serverCollision: true,
             authoritativeEnd: true);

        p.Flags.IsInSkillMotion = true;
    }

    public void OnPropose(Player p, in SkillCollisionProposal proposal)
    {
        if (_committed)
            return;
        if (proposal.Seq <= LastSeq)
            return;

        LastSeq = proposal.Seq;
        Latest = proposal;

        var from = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
        var end = Latest.EndPass;
        _duration = MathF.Max(0.01f, 1.0f);

        CommitMotionOnce(p, _spec, from, end);

        _committed = true;
    }
}

