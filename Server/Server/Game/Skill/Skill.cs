using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

public class Skill
{
    SkillData _skillData = new SkillData();
    public virtual SkillData SkillData
    {
        get { return _skillData; }
        set
        {
            if (_skillData.Equals(value))
                return;

            _skillData = value;
        }
    }
}

