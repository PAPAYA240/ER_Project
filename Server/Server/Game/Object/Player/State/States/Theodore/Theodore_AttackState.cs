using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public class Theodore_AttackState : Player_AttackState
{
    static readonly float _tAttackRange = 6.0f;

    public Theodore_AttackState(int targetId, bool chaseAllowed = true, float attackRange = DefaultAttackRange) 
        : base(targetId, chaseAllowed, _tAttackRange)
    {
        attackRange = 6.0f;
    }
    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead)
            return;

        Projectile projectile = ObjectManager.Instance.Add<Projectile>();
        if (projectile != null)
        {
            projectile.ProjectileType = ProjectileType.ProjectileBullet;
            projectile.Owner = p;
            projectile.Init();
            p.Room.EnterGame(projectile);
        }
    }
}
