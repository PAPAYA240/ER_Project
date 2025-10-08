using Data;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
    //public Dictionary<string, Data.CharacterData> GameData { get; private set; } = new Dictionary<string, Data.CharacterData>();
    //public Dictionary<string, Data.SkillData> SkillDict { get; private set; } = new Dictionary<string, Data.SkillData>();

    public static Dictionary<CharacterType, StatInfo> StatDict { get; private set; } = new Dictionary<CharacterType, StatInfo>();
    public static Dictionary<CharacterType, Dictionary<KeyCode, SkillData>> SkillDict { get; private set; }
        = new Dictionary<CharacterType, Dictionary<KeyCode, SkillData>>();

    public static Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>> SkillHitboxDict { get; private set; } 
        = new Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>>();

    public static Dictionary<MonsterSkill, List<EffectData>> MonsterSkillDict { get; private set; } = new Dictionary<MonsterSkill, List<EffectData>>();
    public static Dictionary<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>> PlayerFxDict { get; private set; }
        = new Dictionary<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>();
    public static Dictionary<int, ItemInfoBase> ItemDict { get; private set; } = new Dictionary<int, ItemInfoBase>();

    public void Init()
    {
        //GameData = LoadJson<Data.GameData, string, Data.CharacterData>("newSkillData").MakeDict();
        //SkillDict = LoadJson<Data.SkillDict, string, Data.SkillData>("SkillData").MakeDict();

        // For PlayerData
        StatDict = LoadJson<Data.StatData, CharacterType, StatInfo>("StatData").MakeDict();
        SkillDict = LoadJson<Data.GameData, CharacterType, Dictionary<KeyCode, SkillData>>("newSkillData").MakeDict();
        SkillHitboxDict = LoadJson<Data.HitboxData, CharacterType, Dictionary<KeyCode, SkillHitbox>>("HitboxData").MakeDict();

        // For Effect
        MonsterSkillDict = LoadJson<Data.MonsterEffectDict, MonsterSkill, List<EffectData>>("MonsterData/EffectData/MonsterEffectData").MakeDict();
        PlayerFxDict = LoadJson<Data.PlayerEffectDict, CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>("PlayerEffectData").MakeDict();

        // For Item
        JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        ItemDict = LoadJson<Data.ItemDict, int, ItemInfoBase>("ItemData", "player", settings).MakeDict();

    }

    Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        string text = File.ReadAllText($"Assets/Resources/Data/{path}.json");
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
    }

    // 역직렬화 세팅 설정
    static Loader LoadJson<Loader, Key, Value>(string path, string key, JsonSerializerSettings settings) where Loader : ILoader<Key, Value>
    {
        string text = File.ReadAllText($"Assets/Resources/Data/{path}.json");
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text, settings);
    }
}

