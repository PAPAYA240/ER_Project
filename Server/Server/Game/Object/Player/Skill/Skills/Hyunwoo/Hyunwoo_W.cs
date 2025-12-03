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
        
        StatusEffect statusEffectDefense = new StatusEffect();
        statusEffectDefense.type = skill.SkillData.levels[skill.CurLevel].effects[0].type;
        statusEffectDefense.stat = skill.SkillData.levels[skill.CurLevel].effects[0].stat;
        statusEffectDefense.value = skill.SkillData.levels[skill.CurLevel].effects[0].value + p.Defense * 0.1f;
        if (!Enum.TryParse(skill.SkillData.levels[skill.CurLevel].effects[0].valueType, out statusEffectDefense.valueType))
            return;
        statusEffectDefense.duration = skill.SkillData.levels[skill.CurLevel].effects[0].duration;
        statusEffectDefense.attacker = p;
        if (!Enum.TryParse<Subject>(skill.SkillData.levels[skill.CurLevel].effects[0].subject, out statusEffectDefense.subject))
            return;

        p.AddStatusEffect(statusEffectDefense);

        StatusEffect statusEffectUnstoppable = new StatusEffect();
        statusEffectUnstoppable.type = skill.SkillData.levels[skill.CurLevel].effects[1].type;
        statusEffectUnstoppable.duration = skill.SkillData.levels[skill.CurLevel].effects[1].duration;
        if (!Enum.TryParse<Subject>(skill.SkillData.levels[skill.CurLevel].effects[1].subject, out statusEffectUnstoppable.subject))
            return;

        p.AddStatusEffect(statusEffectUnstoppable);

        if(p is Hyunwoo hyunwoo)
        {
            hyunwoo.AddTSkillCount(10);
        }

        SendSkillConfirmPacket(p);
        p.SendSoundPacket("SKILL_W");
        p.SendSoundPacket("SKILL_W", "Voice");
        p.SendSkillEffect(new System.Numerics.Vector2(), keyCode: _keyCode, false);
    }
}
