using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Server.Game
{
    public class KnockbackSkill : ISkillBehavior
    {
        public void OnStart(Monster caster, MonsterSkillData skillData)
        {

        }
        public void OnUpdate(Monster caster)
        {
            // 몬스터한테 콜리전 먼저 붙이기
        }
        public void OnEnd(Monster caster)
        {
        }

        public void OnHit(Monster caster, Creature target)
        {

        }
    }

}
