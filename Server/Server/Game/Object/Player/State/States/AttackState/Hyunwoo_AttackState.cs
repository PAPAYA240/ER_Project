using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class Hyunwoo_AttackState : Player_AttackState
    {
        KeyCode _keyCode = KeyCode.T;
        bool IsPassiveAttack = false;

        public Hyunwoo_AttackState(int targetId, bool chaseAllowed = true) : base(targetId, chaseAllowed)
        {
        }

        protected override void ApplyHit(Player p, GameObject target)
        {
            if (target == null || target.State == CreatureState.Dead || target.IsUntargetable())
                return;

            Hyunwoo hyunwoo = p as Hyunwoo;
            GameRoom room = hyunwoo.Room;

            IsPassiveAttack = hyunwoo.CheckTActivate(); // check activate

            // t damage
            if (IsPassiveAttack)
            {
                room.Push(room.AttackSkillTarget, p, target, _keyCode);
                // t count to zero, hp recovery
                if (hyunwoo != null)
                {
                    room.Push(hyunwoo.ActivateTSKill);
                }
            }
            
            if (hyunwoo != null)
            {
                room.Push(hyunwoo.AddTSkillCount, 1); // stack up T
            }

            // basic damage
            room.Push(target.OnDamaged, p, p.Attack, false, true);
        }
    }
}
