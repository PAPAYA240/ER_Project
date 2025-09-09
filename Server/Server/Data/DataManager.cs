using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Data
{
    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        public static Dictionary<int, StatInfo> StatDict { get; private set; } = new Dictionary<int, StatInfo>();
        public static Dictionary<string, Data.CharacterData> GameData { get; private set; } = new Dictionary<string, Data.CharacterData>();

        public static Dictionary<string, MonsterData> MonsterDict { get; private set; } = new Dictionary<string, MonsterData>();
        public static Dictionary<MonsterSkill, MonsterSkillData> MonsterSkillDict { get; private set; } = new Dictionary<MonsterSkill, MonsterSkillData>();
        public static void LoadData()
        {
            // For PlayerData
            StatDict = LoadJson<Data.StatData, int, StatInfo>("StatData", "player").MakeDict();
            GameData = LoadJson<Data.GameData, string, Data.CharacterData>("newSkillData", "player").MakeDict();

            // For MonsterData
            MonsterDict = LoadJson<Data.MonsterDict, string, Data.MonsterData>("MonsterData", "monster").MakeDict();
            MonsterSkillDict = LoadJson<Data.MonsterSkillDict, MonsterSkill, Data.MonsterSkillData>("MonsterSkillData", "monster").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string path, string key) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPaths[key]}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
