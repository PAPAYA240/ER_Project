using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public static class SkillRegistry
{
    public static ISkillHandler Resolve(CharacterType character, KeyCode key/*, SkillSpec spec*/)
    {
        // 캐릭터/키별 매핑
        if (character == CharacterType.Rozzi)
        {
            if (key == KeyCode.Q) return new Rozzi_Q();
            if (key == KeyCode.W) return new Rozzi_W();
            if (key == KeyCode.E) return new Rozzi_E();
            if (key == KeyCode.R) return new Rozzi_R();
            if (key == KeyCode.D) return new Rozzi_D();
            if (key == KeyCode.F) return new Skill_Blink();
        }
        

        return null;
    }
}