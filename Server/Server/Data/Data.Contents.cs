using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using Google.Protobuf.Protocol;
using Lucene.Net.Support;
using static Lucene.Net.Util.AttributeSource;
using static Server.Data.DataUtils;

namespace Server.Data
{
    #region Stat
    [Serializable]
    public class StatData : ILoader<CharacterType, StatInfo>
    {
        public List<StatInfo> stats = new List<StatInfo>();

        public Dictionary<CharacterType, StatInfo> MakeDict()
        {
            Dictionary<CharacterType, StatInfo> dict = new Dictionary<CharacterType, StatInfo>();
            foreach (StatInfo stat in stats)
            {
                stat.Hp = stat.MaxHp;
                dict.Add((CharacterType)Enum.Parse(typeof(CharacterType), stat.Name), stat);
            }
            return dict;
        }
    }
    #endregion

    #region Skill

    [Serializable]
    public class GameData : ILoader<CharacterType, Dictionary<KeyCode, SkillData>>
    {
        public Dictionary<string, Dictionary<string, SkillData>> characters = new Dictionary<string, Dictionary<string, SkillData>>();

        public Dictionary<CharacterType, Dictionary<KeyCode, SkillData>> MakeDict()
        {
            var nestedDict = new Dictionary<CharacterType, Dictionary<KeyCode, SkillData>>();

            foreach(var chars in characters)
            {
                CharacterType chartype = (CharacterType)Enum.Parse(typeof(CharacterType), chars.Key);

                var dict = new Dictionary<KeyCode, SkillData> ();
                foreach (var skills in chars.Value)
                {
                    KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), skills.Key);

                    dict.Add(keyCode, skills.Value);
                }

                nestedDict.Add(chartype, dict);
            }

            return nestedDict;
        }
    }

    [Serializable]
    public class SkillData
    {
        public string id;
        public string name;
        public string description;
        public string type;
        public int maxLevel;
        public Mechanics mechanics;
        public Scaling scaling;
        public Dictionary<int, SkillLevel> levels;
    }

    [Serializable]
    public class Mechanics
    {
        public string targetType;
        public string damageType;
        public float castTime;
        public bool areaOfEffect; //잘모름
        public float radius;
        public float range;

        public ProjectileData projectile;
    }

    [Serializable]
    public class ProjectileData
    {
        public bool enabled;
        public float speed;
        public float width;
        public float lifetime;
        public int maxTargets;
        public bool isPiercing;
        public bool isHoming;
    }


    [Serializable]
    public class Scaling
    {
        public float adRatio;
        public float apRatio;
        public float hpRatio;
    }

    [Serializable]
    public class SkillLevel
    {
        public int level;
        public float damage;
        public float cooldown;
        public int staminaCost;
        public List<EffectData> effects;
    }

    [Serializable]
    public class EffectData
    {
        public string type;    // Buff / Debuff / Burn 등
        public string stat;    // MoveSpeed / Defense / AttackSpeed 등
        public float value;    // 수치 (%는 그냥 숫자로 저장)
        public float duration; // 지속시간
        public string condition; // 옵션 (예: "HP<50%")
    }
    #endregion

    #region Monster
    [Serializable]
    public class MonsterData
    {
        public int id;
        public string name;
        public StatInfo stat;
        public List<MonsterSkill> skills;
    }
    public class ProjectileInfo
    {
        public string name;
        public float speed;
        public int range;
        public string prefab;
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
    public class MonsterSkillData
    {
        public int id;
        public string name;
        public MonsterSkill skillType;
        public float skillDuration;
        public int damage;
        public ProjectileInfo projectile;
    }

    [Serializable]
    public class MonsterSkillDict : ILoader<MonsterSkill, MonsterSkillData>
    {
        public List<MonsterSkillData> skillData = new List<MonsterSkillData>();

        public Dictionary<MonsterSkill, MonsterSkillData> MakeDict()
        {
            Dictionary<MonsterSkill, MonsterSkillData> dict = new Dictionary<MonsterSkill, MonsterSkillData>();
            foreach (MonsterSkillData data in skillData)
            {
                dict.Add(data.skillType, data);
            }
            return dict;
        }
    }


    #endregion

    #region StatGrowth

    #endregion

    #region Weapon
    public class WeaponData : ILoader<Weapon, WeaponInfo>
    {
        public Dictionary<string, WeaponInfo> stats = new Dictionary<string, WeaponInfo>();

        public Dictionary<Weapon, WeaponInfo> MakeDict()
        {
            Dictionary<Weapon, WeaponInfo> dict = new Dictionary<Weapon, WeaponInfo>();
            foreach (var pair in stats)
            {
                dict.Add((Weapon)Enum.Parse(typeof(Weapon), pair.Key), pair.Value);
            }
            return dict;
        }
    }
    #endregion

    #region WeaponMastery
    public class WeaponMasteryData : ILoader<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>>
    {
        public Dictionary<string, Dictionary<string, WeaponMasteryInfo>> stats
        = new Dictionary<string, Dictionary<string, WeaponMasteryInfo>>();
        
        public Dictionary<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>> MakeDict()
        {
            var nestedDict = new Dictionary<CharacterType, Dictionary<Weapon, WeaponMasteryInfo>>();
            foreach(var stat in stats)
            {
                CharacterType charType = (CharacterType)Enum.Parse(typeof(CharacterType), stat.Key);
                var newDict = new Dictionary<Weapon, WeaponMasteryInfo>();
                
                foreach (var dict in stat.Value)
                {
                    Weapon key = (Weapon)Enum.Parse(typeof(Weapon), dict.Key);
                    newDict.Add(key, dict.Value);
                }

                nestedDict.Add(charType, newDict);
            }

            return nestedDict;
        }
    }
    #endregion
}
