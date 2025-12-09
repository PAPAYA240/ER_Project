using System;
using System.Collections;
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
                    if (v.IsEmpty)
                        continue;

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

    public class SkillSpec
    {
        public SkillNeed needs;
        public SkillLimits limits;        
    }

    public class SkillNeed
    {
        public bool endBlocked;
        public bool endPass;
        public bool behindBlocked;
        public bool candidateTargetId;
    }

    public class SkillLimits
    {
        public float baseMaxDist;
        public float extraMaxBehind;
        //public float speed;
    }

    public class SkillVariants
    {
        // 없는 건 null 허용
        public SkillSpec cast;
        public SkillSpec followup;

        public bool IsEmpty => cast == null && followup == null;
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

    #region Indicator
    [Serializable]
    public class SkillIndicatorConfig
    {
        public string indicatorPrefabPath;
        public string prefabName;
        public List<string> invokeFuncs = new List<string>();
        public Vector3 targetScale = Vector3.one;
        public float scaleSpeed = 1.5f;
    }

    [Serializable]
    public class IndicatorData : ILoader<CharacterType, Dictionary<KeyCode, SkillIndicatorConfig>>
    {
        public Dictionary<string, Dictionary<string, SkillIndicatorConfig>> characters = new Dictionary<string, Dictionary<string, SkillIndicatorConfig>>();

        public Dictionary<CharacterType, Dictionary<KeyCode, SkillIndicatorConfig>> MakeDict()
        {
            var nestedDict = new Dictionary<CharacterType, Dictionary<KeyCode, SkillIndicatorConfig>>();

            foreach (var chars in characters)
            {
                CharacterType chartype = (CharacterType)Enum.Parse(typeof(CharacterType), chars.Key);

                var dict = new Dictionary<KeyCode, SkillIndicatorConfig>();
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
    #endregion

    #region Effect

    [Serializable]
    public class SkillEffectList
    {
        public enum EffectType
        {
            Caster,     // 시전자 이펙트
            HitTarget,  // 피격자 이펙트
            Select      // 선택에 따른 이펙트
        }
        public List<EffectData> Caster { get; set; }  // 시전자 이펙트
        public List<EffectData> HitTarget { get; set; } // 피격자 이펙트
        public List<EffectData> Select { get; set; } // 선택에 따른 이펙트
    }

    [Serializable]
    public class EffectData
    {
        public string type;    // Buff / Debuff / Burn 등
        public string stat;    // MoveSpeed / Defense / AttackSpeed 등
        public float value;    // 수치 (%는 그냥 숫자로 저장) 
        public float duration; // 지속시간
        public string condition; // 옵션 (예: "HP<50%")

        public string attachBoneName; // 뼈대에 달고자 한다면 그 뼈의 이름

        #region 추가
        public enum EEffectTarget
        {
            Default,    // static, 기본 위치 (캐스터 or 타겟)
            Self,       // 캐스터의 위치에 부착 (자식으로)
            Target,     // 아직까지 Target만 따라감
            TargetNoRotation,   // 타겟 위치만 따라감, 회전 반영X
            TargetUI,   // UI처럼 위치만 반영, 회전 반영X, 화면에 고정
            Mouse,      // 마우스 따라감
            Shot,       // 발사체
            Enemy,      // 적에게 부착
            EnemyHit,   // 처음 재생할 때만 타겟 transform 반영
        }
        public float speed;
        public string prefabName;
        public float delayTime;
        public string skillType;
        public Vector3 position; // 부모or 기본 포지션에 추가적으로 옮겨줄 위치
        public Quaternion rotation = Quaternion.identity;
        public string sound;
        public EEffectTarget target; // 이펙트가 표시될 위치
        #endregion

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
    public class PlayerEffectDict : ILoader<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>
    {
        public Dictionary<string, Dictionary<string, Dictionary<string, SkillEffectList>>> effects
            = new Dictionary<string, Dictionary<string, Dictionary<string, SkillEffectList>>>();

        public Dictionary<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>> MakeDict()
        {
            var finalDict = new Dictionary<CharacterType, Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>>();

            foreach (var charEntry in effects)
            {
                if (!Enum.TryParse(charEntry.Key, true, out CharacterType charType))
                    continue;

                var stateDict = new Dictionary<CreatureState, Dictionary<KeyCode, SkillEffectList>>();

                foreach (var stateEntry in charEntry.Value)
                {
                    if (!Enum.TryParse(stateEntry.Key, true, out CreatureState creatureState))
                        continue;

                    var keyCodeDict = new Dictionary<KeyCode, SkillEffectList>();

                    foreach (var skillEntry in stateEntry.Value)
                    {
                        if (!Enum.TryParse(skillEntry.Key, true, out KeyCode keyCode))
                            continue;

                        keyCodeDict.Add(keyCode, skillEntry.Value);
                    }
                    stateDict.Add(creatureState, keyCodeDict);
                }
                finalDict.Add(charType, stateDict);
            }
            return finalDict;
        }

        public Dictionary<string, SkillEffectList> MakeCommonDict()
        {
            var commonDict = new Dictionary<string, SkillEffectList>();

            if (!effects.TryGetValue("Common", out var commonNode))
                return commonDict;

            if (!commonNode.TryGetValue("Fx", out var fxNode))
                return commonDict;

            foreach (var fxEntry in fxNode)
            {
                string fxName = fxEntry.Key;            // Blink, Debuff_Slow ...
                SkillEffectList list = fxEntry.Value;   // Caster / HitTarget / Select

                commonDict[fxName] = list;
            }

            return commonDict;
        }
    }
    #endregion

    #region Monster Skill

    [Serializable]
    public class MonsterData
    {
        public int id;
        public string name;
        public float attackDist; // 공격 범위
        public float activeDist; // 활동 범위
        public StatInfo stat;
        public List<MonsterSkill> skills;
        public float appearTime;
        public int activePhase;
        public float deadTime;
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

    #region Sound
    public class SoundData
    {
        public string Path;
        public float Duration;
    }
    public class ClipInfo
    {
        public AudioClip Clip;
        public float Duration;
    }
    [Serializable]
    public class SoundDict : ILoader<CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>
    {
        public Dictionary<string, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> soundDict
            = new Dictionary<string, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>();

        public Dictionary<CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> MakeDict()
        {
            Dictionary<CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> dict
                = new Dictionary<CharacterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>();

            foreach (var charEntry in soundDict)
            {
                if (!Enum.TryParse(charEntry.Key, true, out CharacterType charType))
                    continue;

                var soundTypeDict = new Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>();

                foreach (var soundTypeEntry in charEntry.Value)
                {
                    Define.Sound soundType = soundTypeEntry.Key;

                    var keyCodeDict = new Dictionary<string, List<SoundData>>();

                    foreach (var keyCodeEntry in soundTypeEntry.Value)
                    {
                        keyCodeDict.Add(keyCodeEntry.Key, keyCodeEntry.Value);
                    }

                    soundTypeDict.Add(soundType, keyCodeDict);
                }
                dict.Add(charType, soundTypeDict);
            }
            return dict;
        }
    }

    public class SoundMcDict : ILoader<MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>
    {
        public Dictionary<string, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> soundDict
            = new Dictionary<string, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>();

        public Dictionary<MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> MakeDict()
        {
            Dictionary<MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>> dict
                = new Dictionary<MonsterType, Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>>();

            foreach (var charEntry in soundDict)
            {
                if (!Enum.TryParse(charEntry.Key, true, out MonsterType charType))
                    continue;

                var soundTypeDict = new Dictionary<Define.Sound, Dictionary<string, List<SoundData>>>();

                foreach (var soundTypeEntry in charEntry.Value)
                {
                    Define.Sound soundType = soundTypeEntry.Key;

                    var keyCodeDict = new Dictionary<string, List<SoundData>>();

                    foreach (var keyCodeEntry in soundTypeEntry.Value)
                    {
                        keyCodeDict.Add(keyCodeEntry.Key, keyCodeEntry.Value);
                    }

                    soundTypeDict.Add(soundType, keyCodeDict);
                }
                dict.Add(charType, soundTypeDict);
            }
            return dict;
        }
    }
    #endregion

    #region AbigailSound
    public class AbigailSoundData : ILoader<AbigailSound, List<string>>
    {
        public Dictionary<string, List<string>> abigailSoundDict = new Dictionary<string, List<string>>();

        public Dictionary<AbigailSound, List<string>> MakeDict()
        {
            Dictionary<AbigailSound, List<string>> dict = new Dictionary<AbigailSound, List<string>>();

            foreach(var entry in abigailSoundDict)
            {
                if(Enum.TryParse(entry.Key, out AbigailSound soundType))
                    dict[soundType] = entry.Value;
            }

            return dict;
        }
    }
    #endregion
}
