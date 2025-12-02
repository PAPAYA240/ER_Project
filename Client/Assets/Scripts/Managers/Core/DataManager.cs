using Data;
using Google.Protobuf.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
using static Data.SkillEffectList;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public class DataManager
{
    public static Dictionary<CharacterType, StatInfo> StatDict { get; private set; } = new Dictionary<CharacterType, StatInfo>();
    public static Dictionary<CharacterType, Dictionary<KeyCode, SkillData>> SkillDict { get; private set; }
        = new Dictionary<CharacterType, Dictionary<KeyCode, SkillData>>();

    public static Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>> SkillHitboxDict { get; private set; } 
        = new Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>>();
    public static Dictionary<MonsterType, Dictionary<MonsterSkill, SkillHitbox>> MonstSkillHitboxDict { get; private set; }
         = new Dictionary<MonsterType, Dictionary<MonsterSkill, SkillHitbox>>();

    public static Dictionary<CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> SoundDict { get; private set; }
         = new Dictionary<CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>();

    public static Dictionary<MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> SoundMcDict { get; private set; }
     = new Dictionary<MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>();

    public static Dictionary<CharacterType, Dictionary<KeyCode, SkillVariants>> SkillSpecDict { get; private set; }
            = new Dictionary<CharacterType, Dictionary<KeyCode, SkillVariants>>();

    public static Dictionary<MonsterSkill, List<EffectData>> MonsterSkillDict { get; private set; } = new Dictionary<MonsterSkill, List<EffectData>>();
    
    public static Dictionary<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>> PlayerFxDict { get; private set; }
        = new Dictionary<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>();

    public static Dictionary<string, SkillEffectList> CommonFxDict { get; private set; }
        = new Dictionary<string, SkillEffectList>();

    public static Dictionary<CharacterType, Dictionary<KeyCode, SkillIndicatorConfig>> IndicatorDict { get; private set; }
        = new Dictionary<CharacterType, Dictionary<KeyCode, SkillIndicatorConfig>>();

    public static Dictionary<int, ItemInfoBase> ItemDict { get; private set; } = new Dictionary<int, ItemInfoBase>();

    public static Dictionary<AbigailSound, List<string>> AbigailAudioDict = new Dictionary<AbigailSound, List<string>>();

    public void Init()
    {
        // For PlayerData
        StatDict = LoadJson<Data.StatData, CharacterType, StatInfo>("StatData").MakeDict();
        SkillDict = LoadJson<Data.GameData, CharacterType, Dictionary<KeyCode, SkillData>>("SkillData").MakeDict();
        SkillHitboxDict = LoadJson<Data.HitboxData, CharacterType, Dictionary<KeyCode, SkillHitbox>>("HitboxData").MakeDict();
        SkillSpecDict = LoadJson<Data.SkillSpecData, CharacterType, Dictionary<KeyCode, SkillVariants>>("SkillSpecData").MakeDict();
        
        IndicatorDict = LoadJson<Data.IndicatorData, CharacterType, Dictionary<KeyCode, SkillIndicatorConfig>>("IndicatorData").MakeDict();
        SkillHitboxDict = LoadJson<Data.HitboxData, CharacterType, Dictionary<KeyCode, SkillHitbox>>("HitboxData").MakeDict();
        SoundDict = LoadJson<Data.SoundDict, CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>("SoundData").MakeDict();
        PlayerFxDict = LoadJson<Data.PlayerEffectDict, CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>("PlayerEffectData").MakeDict();
        CommonFxDict = LoadJson<Data.PlayerEffectDict, CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>("PlayerEffectData").MakeCommonDict();

        // For Monster Data
        MonsterSkillDict = LoadJson<Data.MonsterEffectDict, MonsterSkill, List<EffectData>>("MonsterData/MonsterEffectData").MakeDict();
        MonstSkillHitboxDict = LoadJson<Data.MonstHitboxData, MonsterType, Dictionary<MonsterSkill, SkillHitbox>>("MonsterData/MonsterHitboxData").MakeDict();
        SoundMcDict = LoadJson<Data.SoundMcDict, MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>("MonsterData/MonsterSoundData").MakeDict();

        // For Item
        JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        ItemDict = LoadJson<Data.ItemDict, int, ItemInfoBase>("ItemData", "player", settings).MakeDict();

        // For Sound
        AbigailAudioDict = LoadJson<Data.AbigailSoundData, AbigailSound, List<string>>("AbigailSound").MakeDict();
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

    #region Fx Data
    public List<EffectData> GetEffectsByPrefabName(string targetPrefabName)
    {
        List<EffectData> matchingEffectData = new List<EffectData>();

        if (string.IsNullOrEmpty(targetPrefabName) || DataManager.PlayerFxDict == null)
            return matchingEffectData;

        foreach (var charEffectEntry in DataManager.PlayerFxDict)
        {
            var creatureStateEffects = charEffectEntry.Value;

            foreach (var stateEffectEntry in creatureStateEffects)
            {
                var keyCodeEffects = stateEffectEntry.Value;

                foreach (var keyCodeEntry in keyCodeEffects)
                {
                    var skillEffectList = keyCodeEntry.Value as SkillEffectList;
                    var allEffectLists = new List<List<EffectData>>
                {
                    skillEffectList?.Caster,
                    skillEffectList?.HitTarget,
                    skillEffectList?.Select
                };

                    foreach (var effectDataList in allEffectLists)
                    {
                        if (effectDataList == null) continue;

                        foreach (var effectData in effectDataList)
                        {
                            if (effectData != null &&
                                effectData.prefabName.Equals(targetPrefabName, System.StringComparison.OrdinalIgnoreCase))
                            {
                                matchingEffectData.Add(effectData);
                            }
                        }
                    }
                }
            }
        }
        return matchingEffectData;
    }

    public List<EffectData> GetSkillEffectList(CharacterType charType, CreatureState state, KeyCode keyCode, EffectType type = EffectType.Caster)
    {
        if (PlayerFxDict == null || !PlayerFxDict.TryGetValue(charType, out var stateDict))
            return null;

        if (!stateDict.TryGetValue(state, out var keyCodeDict))
            return null;

        if (keyCodeDict.TryGetValue(keyCode, out var effectList))
        {
            if (type == EffectType.Caster)
                return effectList.Caster;

            else if (type == EffectType.HitTarget)
                return effectList.HitTarget;

            else if (type == EffectType.Select)
                return effectList.Select;
            else
                return null;
        }
        else
            return null;
    }
    #endregion
}

