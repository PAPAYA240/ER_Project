using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;


public class Abigail_T : Player_AttackState
{
    static readonly float _tAttackRange = 2.15f;
    private const string AnimAttackT = "SKILL_T";

    public Abigail_T(int targetId, bool chaseAllowed = true, float attackRange = DefaultAttackRange) : base(targetId, chaseAllowed, _tAttackRange)
    {
    }

    protected override void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageApplied = false;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds);

        // 애니 송출(서버 권한)
        p.SendAnimPacket(AnimAttackT, 0.05f);

        //p.FaceToTarget(_targetId);
    }

    // 데미지 적용 훅(프로젝트 룰에 맞게 연결)
    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead)
            return;

        // TODO: 실제 데미지 계산/적용 로직에 연결
        // 예) target.OnDamaged(p, 10f);
    }
}
