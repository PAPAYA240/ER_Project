using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class Rozzi_Q : ISkillHandler
{
    private Vector3 _finalEnd; private float _duration;

    public void OnEnter(Player p, SkillSpec spec, SkillContext ctx)
    {
        p.SendStopPacket(StopReason.StopMoveOnly);  // 이동 잠금
        p.SendAnimPacket(spec.AnimName, 0.05f);     // 애니 브로드캐스트
                                                    // TODO: 코스트/쿨타임 차감
    }

    public void OnHit(Player p, SkillSpec spec, SkillContext ctx)
    {
        var from = new System.Numerics.Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
        var aim = new System.Numerics.Vector3(ctx.MousePos.X, from.Y, ctx.MousePos.Y);
        var flat = new System.Numerics.Vector3(aim.X - from.X, 0, aim.Z - from.Z);
        var dir = flat.LengthSquared() > 1e-6f ? System.Numerics.Vector3.Normalize(flat) : System.Numerics.Vector3.UnitZ;

        // 0) 의도한 목적지
        var wish = from + dir * spec.Move.Distance;

        // 1) 스윕(진짜 충돌) 먼저. 반경에 작은 여유(skin) 더하기
        var nm = NavmeshService.Instance;
        float sweepRadius = spec.Hitbox.Radius + 0.05f;   // 안전 여유
        if (nm.SweepCapsule(from, wish, sweepRadius, out var hit, out var nrm))
        {
            // 벽 앞에서 멈추기
            wish = hit - dir * spec.Collision.Skin;
        }

        // 2) 최종점 워커블로 클램프 (끝점만)
        wish = nm.ClampPointToNavmesh(wish);

        // 3) 재생 시간은 "실제 이동 거리"로 산정
        float dist = System.Numerics.Vector3.Distance(from, wish);
        float speed = (spec.Move.Speed > 0 ? spec.Move.Speed : 20f);
        _duration = MathF.Max(0.01f, dist / speed);
        _finalEnd = wish;

        // 범용 스킬 모션 패킷
        p.SendSkillMotion(
            type: SkillMotionType.Dash,
            start: from,
            end: _finalEnd,
            duration: _duration,
            anim: ""/*spec.AnimName*/,
            curveId: "EaseOutCubic",
            serverCollision: true,
            authoritativeEnd: true);

        // 스킬 중 MoveSync 감시 모드
        p.Flags.IsInSkillMotion = true;
        p.Flags.SkillMotionStart = from;
        p.Flags.SkillMotionEnd = _finalEnd;
        p.Flags.SkillMotionEndTimeUtc = (float)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000f + _duration;
    }

    public void OnExit(Player p, SkillSpec spec, SkillContext ctx)
    {
        // 종료 시 최종 보정
        p.PosInfo.PosX = _finalEnd.X;
        p.PosInfo.PosY = _finalEnd.Y;
        p.PosInfo.PosZ = _finalEnd.Z;
        p.SendMovePacket(new PositionInfo(p.PosInfo), new RotationInfo(p.RotInfo));

        p.Flags.IsInSkillMotion = false;

        //// 스킬 종료 시 최종 보정 1회
        //p.PosInfo.PosX = _finalEnd.X;
        //p.PosInfo.PosY = _finalEnd.Y;
        //p.PosInfo.PosZ = _finalEnd.Z;
        //p.SendMovePacket(new PositionInfo(p.PosInfo), new RotationInfo(p.RotInfo));

        //p.EndSkillMotion();                  // ← 스킬 끝: MoveSync 평상시 복귀
    }
}

