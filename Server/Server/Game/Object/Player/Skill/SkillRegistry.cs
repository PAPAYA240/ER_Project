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
        if (character == CharacterType.Rozzi && key == KeyCode.Q) return new Rozzi_Q();
        //if (character == "Rozzi" && key == KeyCode.W) return new Rozzi_W();

        return null;
    }
}