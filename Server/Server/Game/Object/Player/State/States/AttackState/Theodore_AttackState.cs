using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class Theodore_AttackState : Player_AttackState
{
    private float _projectileSpeed = 15f;
    public Theodore_AttackState(int targetId, bool chaseAllowed = true) 
        : base(targetId, chaseAllowed)
    {
       
    }
    public override void Enter(Player player)
    {
        base.Enter(player);
    }
    private void CreateProjectile(Player p)
    {
        Projectile_Theodore_Attack projectile = ObjectManager.Instance.Add<Projectile_Theodore_Attack>();
        if (projectile != null)
        {
            projectile.ProjectileType = ProjectileType.ProjectileTheodoreNormalAttack;
            projectile.Owner = p;
            projectile.Init();
            p.Room.Push(p.Room.EnterGame, projectile, 0);
            projectile.SendTheodoreNormalAttackPacket(p, _targetId, _projectileSpeed);
        }
    }
    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead || target.IsUntargetable())
            return;

        CreateProjectile(p);
        target.OnDamaged(p, p.Attack, false, true);
    }
}
