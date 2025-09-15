using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SkillBase
{
    public PlayerController _player;
    public Animator _animator;

    public KeyCode _keyCode;

    SkillData _skillData = new SkillData();
    public virtual SkillData SkillData
    {
        get { return _skillData; }
        set
        {
            if (_skillData.Equals(value))
                return;

            _skillData = value;
        }
    }

    public int CurLevel { get; set; } 
    public int MaxLevel { get { return SkillData.maxLevel; } }
    public float Cooldown { get; set; }
    public float MaxCooldown { get; set; }
    public float CurLevelCooldown { 
        get 
        { 
            if(CurLevel > 0 &&  CurLevel <= MaxLevel)
                return SkillData.levels[CurLevel].cooldown;
            return SkillData.levels[1].cooldown;
        } 
    }
}

