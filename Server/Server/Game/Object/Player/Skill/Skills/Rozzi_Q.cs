using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public sealed class Rozzi_Q : ISkillHandler
{
    object _lock = new object();

    public void OnEnter(Player p, SkillSpec spec, SkillContext ctx)
    {
        p.SendStopPacket(StopReason.StopMoveOnly);  // 이동 잠금
        p.SendAnimPacket(spec.AnimName, 0.05f);     // 애니 브로드캐스트
                                                    // TODO: 코스트/쿨타임 차감
    }

    public void OnHit(Player p, SkillSpec spec, SkillContext ctx)
    {
        lock (_lock)
        {
            // from/dir 계산
            var from = new Vector3(p.PosInfo.PosX, p.PosInfo.PosY, p.PosInfo.PosZ);
            var toXZ = new Vector3(ctx.MousePos.X, from.Y, ctx.MousePos.Y);
            var flat = new Vector3(toXZ.X - from.X, 0, toXZ.Z - from.Z);
            var dir = flat.LengthSquared() > 1e-6f ? Vector3.Normalize(flat) : (Vector3.UnitZ);

            var wishEnd = from + dir * spec.Move.Distance;

            // 1) 캡슐 스윕으로 충돌 체크(서버 NavmeshService 사용)
            var nm = NavmeshService.Instance;
            if (nm.SweepCapsule(from, wishEnd, spec.Hitbox.Radius, out var hit, out var normal))
            {
                if (spec.Collision.StopOnWall)
                    wishEnd = hit - dir * spec.Collision.Skin;

                if (spec.Collision.SlideOnWall)
                {
                    var tangent = Vector3.Normalize(dir - Vector3.Dot(dir, normal) * normal);
                    var remain = (from + dir * spec.Move.Distance) - hit;
                    var slideEnd = hit + tangent * remain.Length();
                    // (옵션) 2차 스윕으로 정밀화
                    wishEnd = slideEnd;
                }
            }

            // 2) 워커블 클램프
            wishEnd = nm.ClampPointToNavmesh(wishEnd);

            // 3) 권위 좌표 브로드캐스트 (클라는 Warp 보정)
            p.PosInfo.PosX = wishEnd.X;
            p.PosInfo.PosY = wishEnd.Y;
            p.PosInfo.PosZ = wishEnd.Z;
            //p.SendMovePacket(new PositionInfo(p.PosInfo), new RotationInfo(p.RotInfo));
            PositionInfo final = new PositionInfo { PosX = wishEnd.X, PosY = wishEnd.Y, PosZ = wishEnd.Z };
            p.SendMovePacket(final, new RotationInfo(p.RotInfo));
        }        
    }

    public void OnExit(Player p, SkillSpec spec, SkillContext ctx)
    {
        // i-frame 해제/락 해제 등 필요시
    }
}

