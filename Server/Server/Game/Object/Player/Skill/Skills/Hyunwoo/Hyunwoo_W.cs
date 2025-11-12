using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;
using static Server.Game.GameObject;


public sealed class Hyunwoo_W : InstantHandlerBase
{
    public override void ExecuteInstant(Player p)
    {
        _characterType = CharacterType.Hyunwoo;
        _keyCode = KeyCode.W;

        Skill skill = p.GetSkill(Server.Data.DataUtils.KeyCode.W);
        
        StatusEffect statusEffect = new StatusEffect();
        statusEffect.type = skill.SkillData.levels[skill.CurLevel].effects[0].type;
        statusEffect.stat = skill.SkillData.levels[skill.CurLevel].effects[0].stat;
        statusEffect.value = skill.SkillData.levels[skill.CurLevel].effects[0].value;
        statusEffect.duration = skill.SkillData.levels[skill.CurLevel].effects[0].duration;
        statusEffect.attacker = p;
        if (!Enum.TryParse<Subject>(skill.SkillData.levels[skill.CurLevel].effects[0].subject, out statusEffect.subject))
            return;

        p.AddStatusEffect(statusEffect);

        SendSkillConfirmPacket(p);
    }
}
