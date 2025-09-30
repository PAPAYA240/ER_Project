using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Xml.XPath;
using Google.Protobuf.Protocol;
using Lucene.Net.Messages;
using Lucene.Net.Support;
using static Lucene.Net.Util.AttributeSource;
using static Server.Data.DataUtils;

namespace Server.Data
{
    #region Stat
    [Serializable]
    public class StatData : ILoader<CharacterType, StatInfo>
    {
        public Dictionary<string, StatInfo> stats = new Dictionary<string, StatInfo>();

        public Dictionary<CharacterType, StatInfo> MakeDict()
        {
            Dictionary<CharacterType, StatInfo> dict = new Dictionary<CharacterType, StatInfo>();
            foreach (var pair in stats)
            {
                pair.Value.Hp = pair.Value.MaxHp;
                dict.Add((CharacterType)Enum.Parse(typeof(CharacterType), pair.Key), pair.Value);
            }
            return dict;
        }
    }

    public class ExpData : ILoader<int, int>
    {
        public Dictionary<int, int> exp = new Dictionary<int, int>();

        public Dictionary<int, int> MakeDict()
        {
            return exp;
        }
    }

    public class StatGrowthData : ILoader<CharacterType, StatInfo>
    {
        public Dictionary<string, StatInfo> growth = new Dictionary<string, StatInfo>();
        public Dictionary<CharacterType, StatInfo> MakeDict()
        {
            Dictionary<CharacterType, StatInfo> dict = new Dictionary<CharacterType, StatInfo>();
            foreach (var pair in growth)
            {
                dict.Add((CharacterType)Enum.Parse(typeof(CharacterType), pair.Key), pair.Value);
            }
            return dict;
        }
    }
    #endregion

    #region Item

    public abstract class ItemInfoBase
    {
        public int Id;      //식별 번호? UI에서의 숫자?
        public string Name; //이름
        public int Grade;   //등급?
    }

    //카메라
    public class ConsumableItemInfo : ItemInfoBase
    {
        public int Count = 0; //개수
    }

    public class EquipItemInfo : ItemInfoBase
    {
        public ItemType Type = ItemType.End; // 어느 부위 인지
        public ItemStat ItemStat = new ItemStat();
    }

    public class ItemStat 
    {
        public float AttackDamage;                  //공격력
        public float AttackSpeed;                   //공격속도
        public float CriticalRatio;                 //치명타 확률
        public float CriticalDamage;                //치명타 피해량
        public float AttackRange;                   //기본 공격 사거리
        public float FixedSkillAmplification;       //고정 스킬 증폭
        public float PercentageSkillAmplification;  //퍼센트 스킬 증폭
        public float SkillAcceleration;             //스킬 가속
        public float FixedDefensePenetration;       //고정 방어 관통
        public float PercentageDefensePenetration;  //퍼센트 방어 관통
        public float FixedSpeed;                    //고정 이동 속도
        public float PercentageSpeed;               //퍼센트 이동 속도
        public float MaxHp;                         //최대 체력
        public float HpRegen;                       //체력 재생
        public float MaxStamina;                    //최대 스테미나
        public float StaminaRegen;                  //스테미나 재생
        public float Defense;                       //방어력
        public float LifeSteal;                     //생명력 흡수
        public float Omnivamp;                      //모든 피해 흡혈
        public float HealingPower;                  //주는 회복 증가
        public float SlowResistance;                //둔화 효과 저항
        public float CCResistance;                  //방해 효과 저항(속박, 기절)
        public float AdaptiveStat;                  //적응형 능력치 공격력 : 스킬증폭 = 1 : 2
        public float Vision;                        //시야
        public float AttackDamagePerLevel;          //레벨 당 공격력
        public float SkillAmplificationPerLevel;    //레벨 당 스킬 증폭
        public float MaxHpPerLevel;                 //레벨 당 최대 체력
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
        public float srcCurHpRatio; // 내 현재체력 비례
        public float srcMaxHpRatio; // 내 최대체력 비례
        public float dstCurHpRatio; // 타겟 현재체력 비례
        public float dstMaxHpRatio; // 타겟 최대체력 비례
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
    #endregion

    #region Hitbox
    [Serializable]
    public class HitboxData : ILoader<CharacterType, Dictionary<KeyCode, SkillHitbox>>
    {

        public Dictionary<string, Dictionary<string, SkillHitbox>> hitbox = new Dictionary<string, Dictionary<string, SkillHitbox>>();

        public Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>> MakeDict()
        {
            var nestedDict = new Dictionary<CharacterType, Dictionary<KeyCode, SkillHitbox>>();

            foreach (var chars in hitbox)
            {
                CharacterType chartype = (CharacterType)Enum.Parse(typeof(CharacterType), chars.Key);
                var dict = new Dictionary<KeyCode, SkillHitbox>();

                foreach (var skills in chars.Value)
                {
                    KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), skills.Key);
                    dict.Add(keyCode, skills.Value);
                    skills.Value.SetDefaultsIfEmpty();
                }
                nestedDict.Add(chartype, dict);
            }

            return nestedDict;
        }
    }
    #endregion

    #region Effect
    [Serializable]
    public class EffectData
    {
        public enum EEffectTarget
        {
            Self,
            Target,
            Ground,
        }

        public string type;    // Buff / Debuff / Burn 등
        public string stat;    // MoveSpeed / Defense / AttackSpeed 등
        public float value;    // 수치 (%는 그냥 숫자로 저장)
        public float duration; // 지속시간
        public string condition; // 옵션 (예: "HP<50%")

        public string prefabName; // 프리팹 이름
        public float delayTime; // 이펙트 시작 시간
        public Vector3 position;
        public Quaternion rotation;
        public string sound;
        public EEffectTarget target; // 이펙트가 표시될 위치

        public EffectData(string name, EEffectTarget target, float duration, string sound, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            this.prefabName = name;
            this.target = target;
            this.duration = duration;
            this.sound = sound;
            this.position = position;
            this.rotation = rotation;
        }
    }

    #endregion

    #region Monster Skill
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
        public float skillCoolTime;
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
    //public class EnvObjectData
    //{
    //    public int id = 1;
    //    public string name;
    //    public int healAmount;
    //    public int cooldown;
    //}
    #region Environment
    public class EnvObjectData : ILoader<EnvType, EnvInfo>
    {
        public Dictionary<string, EnvInfo> stats = new Dictionary<string, EnvInfo>();
        public Dictionary<EnvType, EnvInfo> MakeDict()
        {
            Dictionary<EnvType, EnvInfo> dict = new Dictionary<EnvType, EnvInfo>();

            foreach (EnvInfo data in stats.Values)
            {
                dict.Add(data.EnvType, data);
            }
            return dict;
        }
    }
    #endregion

    #region System
    public class PhaseData : ILoader<int, int>
    {
        public Dictionary<int, int> phase = new Dictionary<int, int>();

        public Dictionary<int, int> MakeDict()
        {
            return phase;
        }
    }

    public class RespawnData : ILoader<int, int>
    {
        public Dictionary<int, int> respawn = new Dictionary<int, int>();

        public Dictionary<int, int> MakeDict()
        {
            return respawn;
        }
    }
    #endregion
}
