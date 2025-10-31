using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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

        public static Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>> SkillHitboxDict { get; private set; } =
            new Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>>();

        public static Dictionary<CharacterType, Dictionary<KeyCode, SkillVariants>> SkillSpecDict { get; private set; }
            = new Dictionary<CharacterType, Dictionary<KeyCode, SkillVariants>>();

        public static Dictionary<MonsterType, Dictionary<MonsterSkill, SkillHitbox>> MonstSkillHitboxDict { get; private set; } =
            new Dictionary<MonsterType, Dictionary<MonsterSkill, SkillHitbox>>();
        public static Dictionary<Weapon, WeaponInfo> WeaponDict { get; private set; } = new Dictionary<Weapon, WeaponInfo>();

        public static Dictionary<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>> WeaponMasteryDict { get; private set; }
            = new Dictionary<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>>();

        public static Dictionary<CharacterType, Dictionary<string, AnimLengthInfo>> AnimLengthInfoDict { get; private set; } = new Dictionary<CharacterType, Dictionary<string, AnimLengthInfo>>();

        public static Dictionary<MonsterType, MonsterData> MonsterDict { get; private set; } = new Dictionary<MonsterType, MonsterData>();
        public static Dictionary<MonsterSkill, MonsterSkillData> MonsterSkillDict { get; private set; } = new Dictionary<MonsterSkill, MonsterSkillData>();
        public static Dictionary<EnvType, EnvInfo> EnvDict { get; private set; } = new Dictionary<EnvType, EnvInfo>();

        public static Dictionary<int, int> PhaseDict { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<int, int> RespawnDict { get; private set; } = new Dictionary<int, int>();

        public static Dictionary<int, ItemInfoBase> ItemDict { get; private set; } = new Dictionary<int, ItemInfoBase>();
       
        public static Dictionary<CharacterType, List<List<int>>> ItemSetDict { get; private set; } = new Dictionary<CharacterType, List<List<int>>>();

        public static void LoadData()
        {
            // For PlayerData
            StatDict = LoadJson<Data.StatData, CharacterType, StatInfo>("StatData", "player").MakeDict();
            ExpDict = LoadJson<Data.ExpData, int, int>("ExpData", "player").MakeDict();
            SkillDict = LoadJson<Data.GameData, CharacterType, Dictionary<KeyCode, SkillData>>("newSkillData", "player").MakeDict();
            SkillSpecDict = LoadJson<Data.SkillSpecData, CharacterType, Dictionary<KeyCode, SkillVariants>>("SkillSpecData", "player").MakeDict();
            SkillHitboxDict = LoadJson<Data.HitboxData, CharacterType, Dictionary<KeyCode, SkillHitbox>>("HitboxData", "player").MakeDict();
            StatGrowthDict = LoadJson<Data.StatGrowthData, CharacterType, StatInfo>("StatGrowthData", "player").MakeDict();
            WeaponDict = LoadJson<Data.WeaponData, Weapon, WeaponInfo>("WeaponData", "player").MakeDict();
            WeaponMasteryDict = LoadJson<Data.WeaponMasteryData, CharacterType, Dictionary<Weapon, WeaponMasteryInfo>>("WeaponMasteryData", "player").MakeDict();
            AnimLengthInfoDict = LoadJson<Data.AnimationInfosData, CharacterType, Dictionary<string, AnimLengthInfo>>("AnimationInfos", "player").MakeDict();

            // For MonsterData
            MonsterDict = LoadJson<Data.MonsterDict, MonsterType, Data.MonsterData>("MonsterData/MonsterData", "monster").MakeDict();
            MonsterSkillDict = LoadJson<Data.MonsterSkillDict, MonsterSkill, Data.MonsterSkillData>("MonsterData/MonsterSkillData", "monster").MakeDict();
            MonstSkillHitboxDict = LoadJson<Data.MonstHitboxData, MonsterType, Dictionary<MonsterSkill, SkillHitbox>>("MonsterData/HitboxData", "monster").MakeDict();

            // For EnvironmentData
            EnvDict = LoadJson<Data.EnvObjectData, EnvType, EnvInfo>("Env/EnvData", "monster").MakeDict();

            // For Item
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            ItemDict = LoadJson<Data.ItemDict, int, ItemInfoBase>("ItemData", "player", settings).MakeDict();
            ItemSetDict = LoadJson<Data.ItemSet, CharacterType, List<List<int>>>("ItemSetData", "player").MakeDict();

            // For System
            PhaseDict = LoadJson<Data.PhaseData, int, int>("PhaseData", "player").MakeDict();
            RespawnDict = LoadJson<Data.RespawnData, int, int>("RespawnData", "player").MakeDict();
        }
        

        static Loader LoadJson<Loader, Key, Value>(string path, string key) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPaths[key]}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }

        // 역직렬화 세팅 설정
        static Loader LoadJson<Loader, Key, Value>(string path, string key, JsonSerializerSettings settings) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPaths[key]}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text, settings);
        }
    }
}
