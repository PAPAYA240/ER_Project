using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;
using static Lucene.Net.Util.AttributeSource;
using static Server.Data.DataUtils;

public class Skill
{
    SkillData _skillData = new SkillData();
    public SkillData SkillData
    {
        get { return _skillData; }
        set
        {
            if (_skillData.Equals(value))
                return;

            _skillData = value;
        }
    }

    public int CurLevel { get; set; }
    public int MaxLevel { get { return SkillData.maxLevel; } }

    public float MaxCooldown { get; set; }
    public float CurLevelCooldown
    {
        get
        {
            if (CurLevel > 0 && CurLevel <= MaxLevel)
                return SkillData.levels[CurLevel].cooldown;
            return SkillData.levels[1].cooldown;
        }
    }

    public float CurLevelStamina
    {
        get
        {
            if (CurLevel > 0 && CurLevel <= MaxLevel)
                return SkillData.levels[CurLevel].staminaCost;
            return SkillData.levels[1].staminaCost;
        }
    }

    public float GetSkillDamage()
    {
        return _skillData.levels[CurLevel].damage;
    }

    // 증폭 뎀
    public float GetSkillBonusDamage()
    {
        string bonusDam = _skillData.descriptionInfo["damage"][CurLevel];
        if(!int.TryParse(bonusDam, out int percentage))
            return 0f;
        return percentage;
    }
}

