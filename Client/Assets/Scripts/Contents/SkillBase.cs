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
    protected MyPlayerController _player;
    protected Animator _animator;

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

    public virtual void Init()
    {
        _player = Managers.Object.MyPlayer;
        _animator = _player.GetComponentInChildren<Animator>();
    }

    public virtual void PlayAnimation(string triggerName)
    {
        if (_player == null || _animator == null)
            Init();

        _animator.SetTrigger(triggerName);

        //_player.UseSkill(1);
    }

    public abstract void Execute();
}

