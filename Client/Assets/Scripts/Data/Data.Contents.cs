using System;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;

namespace Data
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
        public int Id;      //식별 번호? UI에서의 숫자?
        public string Name; //이름
        public ItemGrade Grade = ItemGrade.End;   //등급
        public string Description; //아이템 설명
    }

    //카메라
    public class ConsumableItemInfo : ItemInfoBase
    {
        public int Count = 0; //개수
    }

    public class EquipItemInfo : ItemInfoBase
    {
        public EquipItemType Type = EquipItemType.End; // 어느 부위 인지
        public ItemStat ItemStat = new ItemStat();
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

            foreach (var chars in characters)
            {
                CharacterType chartype = (CharacterType)Enum.Parse(typeof(CharacterType), chars.Key);

                var dict = new Dictionary<KeyCode, SkillData>();
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

    public class ProjectileInfo
    {
        public string name;
        public float speed;
        public int range;
        public string prefab;
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
            Self,       // 캐스터의 위치에 부착 (자식으로)
            Relative,   // 캐스터의 회전을 고려한 상대적 위치
            Target,     // 특정 타겟의 위치
            Ground,     // 월드 좌표의 특정 위치
            Shoot       // 발사체
        }

        public string type;    // Buff / Debuff / Burn 등
        public string stat;    // MoveSpeed / Defense / AttackSpeed 등
        public float value;    // 수치 (%는 그냥 숫자로 저장) 
        public float duration; // 지속시간
        public string condition; // 옵션 (예: "HP<50%")

        // + 추가
        public string prefabName;
        public float delayTime;
        public string skillType;
        public Vector3 position; // 부모or 기본 포지션에 추가적으로 옮겨줄 위치
        public Quaternion rotation;
        public string sound;
        public EEffectTarget target; // 이펙트가 표시될 위치

        public EffectData(string name, EEffectTarget target, float duration, string sound, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            //this.prefabName = name;
            this.target = target;
            this.duration = duration;
            this.sound = sound;
            this.position = position;
            this.rotation = rotation;
        }
    }

    [Serializable]
    public class MonsterEffectDict : ILoader<MonsterSkill, List<EffectData>>
    {
        public List<EffectData> effectData = new List<EffectData>();

        public Dictionary<MonsterSkill, List<EffectData>> MakeDict()
        {
            var dict = new Dictionary<MonsterSkill, List<EffectData>>();
            foreach (EffectData data in effectData)
            {
                if (Enum.TryParse<MonsterSkill>(data.skillType, out MonsterSkill monsterSkill))
                {
                    if (dict.ContainsKey(monsterSkill))
                        dict[monsterSkill].Add(data);
                    else
                        dict.Add(monsterSkill, new List<EffectData> { data });
                }
            }
            return dict;
        }
    }

    [Serializable]
    public class PlayerEffectDict : ILoader<CharacterType, Dictionary<KeyCode, List<EffectData>>>
    {
        public Dictionary<string, Dictionary<string, List<EffectData>>> effects
            = new Dictionary<string, Dictionary<string, List<EffectData>>>();

        public Dictionary<CharacterType, Dictionary<KeyCode, List<EffectData>>> MakeDict()
        {
            var nestedDict = new Dictionary<CharacterType, Dictionary<KeyCode, List<EffectData>>>();

            foreach (var chars in effects)
            {
                CharacterType charType = (CharacterType)Enum.Parse(typeof(CharacterType), chars.Key);
                var skillDict = new Dictionary<KeyCode, List<EffectData>>();

                foreach (var skills in chars.Value)
                {
                    KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), skills.Key);
                    skillDict.Add(keyCode, skills.Value);
                }
                nestedDict.Add(charType, skillDict);
            }
            return nestedDict;
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
}