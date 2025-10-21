using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public static class SkillRegistry
{
    private static readonly Dictionary<string, Func<ISkillHandler>> _map = new Dictionary<string, Func<ISkillHandler>>();

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

    public static void InitRegister()
    {
        SkillRegistry.Register<Rozzi_Q_Dash>("Rozzi_Q_Dash");
    }

    public static void Register<T>(string key) where T : ISkillHandler, new()
        => _map[key] = () => new T();

    public static ISkillHandler Create(string key)
        => _map.TryGetValue(key, out var f) ? f() : null;
}