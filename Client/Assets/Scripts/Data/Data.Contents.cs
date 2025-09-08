using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;

namespace Data
{
    #region Test

    [Serializable]
    public class GameData : ILoader<String, CharacterData>
    {
        public List<CharacterData> characters = new List<CharacterData>();

        public Dictionary<string, CharacterData> MakeDict()
        {
            Dictionary<string, CharacterData> dict = new Dictionary<string, CharacterData>();
            foreach (CharacterData Data in characters)
                dict.Add(Data.name, Data);
            return dict;
        }
    }

    [Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public List<SkillData> skills;
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
        public List<SkillLevel> levels;
    }

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



    #region Skill
    //[Serializable]
    //public class SkillData
    //{
    //    public int id;
    //    public string name;
    //    public float cooldown;
    //    public float lastUsedTime;
    //    public int manaCost;
    //    public string uiTag;

    //    public int damage;
    //    public SkillType skillType;
    //    public ProjectileInfo projectile;
    //}

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
            foreach (SkillData data in skillData)
                dict.Add(data.name, data);
            return dict;
        }
    }

    #endregion
}