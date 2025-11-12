using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public class Theodore_AttackState : Player_AttackState
{
    public Theodore_AttackState(int targetId, bool chaseAllowed = true) 
        : base(targetId, chaseAllowed)
    {

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
