using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Protobuf.Protocol;
using static Server.Data.DataUtils;

namespace Server.Data
{
    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        public static Dictionary<CharacterType, StatInfo> StatDict { get; private set; } = new Dictionary<CharacterType, StatInfo>();

        public static Dictionary<int, int> ExpDict { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<CharacterType, StatInfo> StatGrowthDict { get; private set; } = new Dictionary<CharacterType, StatInfo>();

        public static Dictionary<CharacterType, Dictionary<KeyCode, SkillData>> SkillDict { get; private set; } 
            = new Dictionary<CharacterType, Dictionary<KeyCode, SkillData>>();

        public static Dictionary<Weapon, WeaponInfo> WeaponDict { get; private set; } = new Dictionary<Weapon, WeaponInfo>();

        public static Dictionary<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>> WeaponMasteryDict { get; private set; }
            = new Dictionary<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>>();

        public static Dictionary<string, MonsterData> MonsterDict { get; private set; } = new Dictionary<string, MonsterData>();
        public static Dictionary<MonsterSkill, MonsterSkillData> MonsterSkillDict { get; private set; } = new Dictionary<MonsterSkill, MonsterSkillData>();

        public static void LoadData()
        {
            // For PlayerData
            StatDict = LoadJson<Data.StatData, CharacterType, StatInfo>("StatData", "player").MakeDict();
            ExpDict = LoadJson<Data.ExpData, int, int>("ExpData", "player").MakeDict();
            SkillDict = LoadJson<Data.GameData, CharacterType, Dictionary<KeyCode, SkillData>>("newSkillData", "player").MakeDict();
            StatGrowthDict = LoadJson<Data.StatGrowthData, CharacterType, StatInfo>("StatGrowthData", "player").MakeDict();
            WeaponDict = LoadJson<Data.WeaponData, Weapon, WeaponInfo>("WeaponData", "player").MakeDict();
            WeaponMasteryDict = LoadJson<Data.WeaponMasteryData, CharacterType, Dictionary<Weapon, WeaponMasteryInfo>>("WeaponMasteryData", "player").MakeDict();

            // For MonsterData
            MonsterDict = LoadJson<Data.MonsterDict, string, Data.MonsterData>("MonsterData/MonsterData", "monster").MakeDict();
            MonsterSkillDict = LoadJson<Data.MonsterSkillDict, MonsterSkill, Data.MonsterSkillData>("MonsterData/MonsterSkillData", "monster").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string path, string key) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPaths[key]}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
