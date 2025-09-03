using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf.Protocol;

namespace Server.Data
{
    #region Stat
    [Serializable]
    public class StatData : ILoader<int, StatInfo>
    {
        public List<StatInfo> stats = new List<StatInfo>();

        public Dictionary<int, StatInfo> MakeDict()
        {
            Dictionary<int, StatInfo> dict = new Dictionary<int, StatInfo>();
            foreach (StatInfo stat in stats)
            {
                stat.Hp = stat.MaxHp;
                dict.Add(stat.Level, stat);
            }                
            return dict;
        }
    }
    #endregion

    #region Skill
    [Serializable]
    public class SkillData
    {
        public int id;
        public string name;
        public float cooldown;
        public float animationTime; // 애니메이션 시간
        public float lastUsedTime;
        public int manaCost;
        public string uiTag;

        public int damage;
        public SkillType skillType;
        public ProjectileInfo projectile;
    }

    public class ProjectileInfo
    {
        public string name;
        public float speed;
        public int range;
        public string prefab;
    }

    [Serializable]
    public class SkillDict : ILoader<string, SkillData>
    {
        public List<SkillData> skillData = new List<SkillData>();

        public Dictionary<string, SkillData> MakeDict()
        {
            Dictionary<string, SkillData> dict = new Dictionary<string, SkillData>();
            foreach (SkillData skillData in skillData)
                dict.Add(skillData.name, skillData);
            return dict;
        }
    }
    #endregion

    #region Monster
    [Serializable]
    public class MonsterData
    {
        public int id;
        public string name;
        public StatInfo stat;
        public List<string> skills;
    }

    [Serializable]
    public class MonsterDict : ILoader<string, MonsterData>
    {
        public List<MonsterData> monsters = new List<MonsterData>();
        public Dictionary<string, MonsterData> MakeDict()
        {
            Dictionary<string, MonsterData> dict = new Dictionary<string, MonsterData>();
            foreach (MonsterData monster in monsters)
                dict.Add(monster.name, monster);
            return dict;
        }
    }
    #endregion

}
