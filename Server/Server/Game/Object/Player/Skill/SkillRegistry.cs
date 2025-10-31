using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public static class SkillRegistry
{
    private static readonly Dictionary<string, Func<ISkill>> _map = new Dictionary<string, Func<ISkill>>();

    public static ISkill Resolve(CharacterType character, KeyCode key/*, SkillSpec spec*/)
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
        else if (character == CharacterType.Yuki)
        {
            //if (key == KeyCode.Q) return new Yuki_Q();
            //if (key == KeyCode.W) return new Yuki_W();
            //if (key == KeyCode.E) return new Yuki_E();
            //if (key == KeyCode.R) return new Yuki_R();
            //if (key == KeyCode.D) return new Yuki_D();
            //if (key == KeyCode.F) return new Skill_();
        }
        else if (character == CharacterType.Abigail)
        {
            if (key == KeyCode.Q) return new Abigail_Q();
            if (key == KeyCode.W) return new Abigail_W();
            if (key == KeyCode.E) return new Abigail_E();
            if (key == KeyCode.R) return new Abigail_R();
            //if (key == KeyCode.D) return new Abigail_D();
            //if (key == KeyCode.F) return new Skill_();
        }
        else if (character == CharacterType.Theodore)
        {
            if (key == KeyCode.Q) return new Theodore_Q();
            if (key == KeyCode.W) return new Theodore_W();
            if (key == KeyCode.E) return new Theodore_E();
            if (key == KeyCode.R) return new Theodore_R();
            if (key == KeyCode.D) return new Theodore_D();
            if (key == KeyCode.F) return new Skill_Blink();
        }
        else if (character == CharacterType.Hyunwoo)
        {
            //if (key == KeyCode.Q) return new Hyunwoo_Q();
            //if (key == KeyCode.W) return new Hyunwoo_W();
            //if (key == KeyCode.E) return new Hyunwoo_E();
            //if (key == KeyCode.R) return new Hyunwoo_R();
            //if (key == KeyCode.D) return new Hyunwoo_D();
            //if (key == KeyCode.F) return new Skill_();
        }

        return null;
    }

    // 다른 명령을 대체할 스킬 등록
    public static void InitRegister()
    {
        SkillRegistry.Register<Rozzi_Q_Dash>("Rozzi_Q_Dash");
    }

    public static void Register<T>(string key) where T : ISkill, new()
        => _map[key] = () => new T();

    public static ISkill Create(string key)
        => _map.TryGetValue(key, out var f) ? f() : null;
}