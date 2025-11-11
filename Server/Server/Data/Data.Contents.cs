using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.XPath;
using Google.Protobuf.Protocol;
using Lucene.Net.Messages;
using Lucene.Net.Support;
using Server.Game;
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

    [Serializable]
    public class ItemDict : ILoader<int, ItemInfoBase>
    {
        public List<ItemInfoBase> items = new List<ItemInfoBase>();
        public Dictionary<int, ItemInfoBase> MakeDict()
        {
            Dictionary<int, ItemInfoBase> dict = new Dictionary<int, ItemInfoBase>();
            foreach (ItemInfoBase item in items)
                dict.Add(item.Id, item);
            return dict;
        }
    }

    public abstract class ItemInfoBase
    {
        public int Id;      //식별 번호? UI에서의 숫자? 흠 이거랑 이름 둘 중 하나를 키값으로 딕셔너리를 만들까나.
        public string Name; //이름
        public ItemGrade Grade = ItemGrade.End;   //등급
        public string Description; //아이템 설명

        public virtual void Use() { }
    }

    //카메라 등의 아이템
    public class ConsumableItemInfo : ItemInfoBase
    {
        public int Count = 0; //개수
    }

    public class EquipItemInfo : ItemInfoBase
    {
        public EquipItemType Type = EquipItemType.End; // 어느 부위 인지
        public ItemStat ItemStat = new ItemStat();
    }

    public class ItemSet : ILoader<CharacterType, List<List<int>>>
    {
        public Dictionary<string, List<List<int>>> characters = new Dictionary<string, List<List<int>>>();

        public Dictionary<CharacterType, List<List<int>>> MakeDict()
        {
            Dictionary<CharacterType, List<List<int>>> dict = new Dictionary<CharacterType, List<List<int>>>();
            foreach (var pair in characters)
                dict.Add((CharacterType)Enum.Parse(typeof(CharacterType), pair.Key), pair.Value);
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
    public class SkillSpecData : ILoader<CharacterType, Dictionary<KeyCode, SkillVariants>>
    {
        public Dictionary<string, Dictionary<string, SkillVariantsWrapper>> characters =
        new Dictionary<string, Dictionary<string, SkillVariantsWrapper>>();

        [Serializable]
        public class SkillVariantsWrapper
        {
            public SkillVariants variants = new SkillVariants();
        }

        public Dictionary<CharacterType, Dictionary<KeyCode, SkillVariants>> MakeDict()
        {
            var result = new Dictionary<CharacterType, Dictionary<KeyCode, SkillVariants>>();

            foreach (var ch in characters)
            {
                if (!Enum.TryParse(ch.Key, true, out CharacterType ctype))
                    continue;

                var perChar = new Dictionary<KeyCode, SkillVariants>();

                foreach (var sk in ch.Value)
                {
                    if (!Enum.TryParse(sk.Key, true, out KeyCode kcode))
                        continue;

                    var wrap = sk.Value;
                    var v = wrap?.variants ?? new SkillVariants();

                    // 비워두면 “이 스킬은 서버 허가/충돌제안 흐름 없음”으로 간주
                    // 필요하다면 아래처럼 아예 사전에 넣지 않는 것도 가능:
                    if (v.IsEmpty) continue;

                    perChar[kcode] = v;
                }

                result[ctype] = perChar;
            }

            return result;
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
        public string skillType;
        public bool canMoveDuringCast;
        public Mechanics mechanics;
        public Scaling scaling;
        public Dictionary<int, SkillLevel> levels;
        public Dictionary<string, List<string>> descriptionInfo;
        public Dictionary<string, List<string>> popupInfo;
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
        public float chargeCoefficient; 

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
    public class MonstHitboxData : ILoader<MonsterType, Dictionary<MonsterSkill, SkillHitbox>>
    {
        public Dictionary<string, Dictionary<string, SkillHitbox>> hitbox = new Dictionary<string, Dictionary<string, SkillHitbox>>();
        public Dictionary<MonsterType, Dictionary<MonsterSkill, SkillHitbox>> MakeDict()
        {
            var nestedDict = new Dictionary<MonsterType, Dictionary<MonsterSkill, SkillHitbox>>();

            foreach (var chars in hitbox)
            {
                MonsterType chartype = (MonsterType)Enum.Parse(typeof(MonsterType), chars.Key);
                var dict = new Dictionary<MonsterSkill, SkillHitbox>();

                foreach (var skills in chars.Value)
                {
                    MonsterSkill keyCode = (MonsterSkill)Enum.Parse(typeof(MonsterSkill), skills.Key);
                    dict.Add(keyCode, skills.Value);
                    skills.Value.SetDefaultsIfEmpty();
                }
                nestedDict.Add(chartype, dict);
            }
            return nestedDict;
        }
    }

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
    public class EffectData // 버프 디버프 등의 상태효과
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
        public string valueType;    // Ratio / Flat
        public float duration; // 지속시간
        public string condition; // 옵션 (예: "HP<50%")
        public string subject; // 적용대상 Self / Ally / Enemy
        public float coeff; // 스킬 계수  ex) (+스킬 증폭의 2%)

        public float ratioPerTarget; // 대상 1명 추가당 증가량 (ex: 아비게일 W: 추가로 적중한 적 하나 당 보호막량 20% 증가)
        public float maxRatio;       // 최대 증가량

        //public Vector3 knockbackDistance; // 밀치기 거리
        //public float knockbackSpeed; // 밀치는 속도

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

    [Serializable]
    public class PlayerEffectDict : ILoader<CharacterType, List<string>>
    {
        public Dictionary<CharacterType, List<string>> effects = new Dictionary<CharacterType, List<string>>();

        public Dictionary<CharacterType, List<string>> MakeDict()
        {
            return effects.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<string>(kvp.Value)
            );
        }
    }

    #endregion

    #region Monster Skill
    [Serializable]
    public class MonsterData
    {
        public int id;
        public string name;
        public string attackType;
        public StatInfo stat;
        public List<MonsterSkill> skills;
        public float appearTime;
    }

    public class ProjectileInfo
    {
        public string name;
        public float speed;
        public int range;
        public string prefab;
    }

    [Serializable]
    public class MonsterDict : ILoader<MonsterType, MonsterData>
    {
        public List<MonsterData> monsters = new List<MonsterData>();
        public Dictionary<MonsterType, MonsterData> MakeDict()
        {
            Dictionary<MonsterType, MonsterData> dict = new Dictionary<MonsterType, MonsterData>();
            foreach (MonsterData monster in monsters)
            {
                MonsterType chartype = (MonsterType)Enum.Parse(typeof(MonsterType), monster.name);

                dict.Add(chartype, monster); 
            }
            return dict;
        }
    }

    public class MonsterSkillData
    {
        public int id;
        public string name;
        public string SkillBehavior;
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
    [System.Serializable]
    public class EnvObjectData : ILoader<EnvType, EnvInfo>
    {
        public List<EnvInfo> envs = new List<EnvInfo>();

        public Dictionary<EnvType, EnvInfo> MakeDict()
        {
            Dictionary<EnvType, EnvInfo> dict = new Dictionary<EnvType, EnvInfo>();
            foreach (EnvInfo data in envs)
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

    #region Animation
    public sealed class AnimationInfosData : ILoader<CharacterType, Dictionary<string, AnimLengthInfo>>
    {
        public int version;
        public List<CharacterDTO> characters;

        public Dictionary<CharacterType, Dictionary<string, AnimLengthInfo>> MakeDict()
        {
            var result = new Dictionary<CharacterType, Dictionary<string, AnimLengthInfo>>();

            if (characters == null)
                return result;

            foreach (var ch in characters)
            {
                if (!Enum.TryParse(ch.character, ignoreCase: true, out CharacterType ct))
                    continue;

                var map = new Dictionary<string, AnimLengthInfo>(StringComparer.OrdinalIgnoreCase); // key: animName

                if (ch.clips != null)
                {
                    foreach (var c in ch.clips)
                    {
                        if (string.IsNullOrEmpty(c.animName))
                            continue;

                        map[c.animName] = new AnimLengthInfo
                        {
                            Clip = c.clip,
                            AnimName = c.animName,
                            Length = (float)c.length,
                            FrameRate = c.frameRate,
                            Samples = c.samples,
                        };
                    }
                }

                result[ct] = map;
            }

            return result;
        }
    }

    public sealed class CharacterDTO
    {
        public string character;
        public string controller;
        public List<ClipDTO> clips;
    }

    public sealed class ClipDTO
    {
        public string clip;
        public double length;
        public float frameRate;
        public int samples;
        public string animName;
    }

    [Serializable]
    public class AnimLengthInfo
    {
        public string Clip;        // 원본 클립 이름
        public string AnimName;    // Animator 상태/별칭(클라와 매칭)
        public float Length;       // 초 단위
        public float FrameRate;
        public int Samples;
    }
    #endregion
}
