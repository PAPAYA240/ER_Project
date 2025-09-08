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
        public static Dictionary<string, Data.SkillData> SkillDict { get; private set; } = new Dictionary<string, Data.SkillData>();
        public static Dictionary<string, MonsterData> MonsterDict { get; private set; } = new Dictionary<string, MonsterData>();
        public static Dictionary<MonsterSkill, MonsterSkillData> MonsterSkillDict { get; private set; } = new Dictionary<MonsterSkill, MonsterSkillData>();
        public static void LoadData()
        {
            // TEM
            StatDict = LoadJsonServer<Data.StatData, int, StatInfo>("StatData").MakeDict();
            SkillDict = LoadJsonServer<Data.SkillDict, string, Data.SkillData>("SkillData").MakeDict();

            // For MonsterData
            MonsterDict = LoadJson<Data.MonsterDict, string, Data.MonsterData>("MonsterData").MakeDict();
            MonsterSkillDict = LoadJson<Data.MonsterSkillDict, MonsterSkill, Data.MonsterSkillData>("MonsterSkillData").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }

        // TEMP
        static Loader LoadJsonServer<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.ConfigServer.dataPath}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
