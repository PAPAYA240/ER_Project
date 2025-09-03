using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class SkillBase
{
    public PlayerController _player;
    public Animator _animator;
    public int _objectId;

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

    public virtual void PlayAnimation(string triggerName)
    {
        if (_player == null || _animator == null)
            return;

        _animator.SetTrigger(triggerName);
    }

    public abstract void Execute();
}

