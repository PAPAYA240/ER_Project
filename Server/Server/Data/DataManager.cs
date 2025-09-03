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
        public static Dictionary<int, Data.Skill> MonsterSkillDict { get; private set; } = new Dictionary<int, Data.Skill>();
        public static Dictionary<string, Data.SkillData> SkillDict { get; private set; } = new Dictionary<string, Data.SkillData>();

        public static void LoadData()
        {
            StatDict = LoadJson<Data.StatData, int, StatInfo>("StatData").MakeDict();
            MonsterSkillDict = LoadJson<Data.SkillData, int, Data.Skill>("MonsterSkillData").MakeDict();
            SkillDict = LoadJson<Data.SkillDict, string, Data.SkillData>("SkillData").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
