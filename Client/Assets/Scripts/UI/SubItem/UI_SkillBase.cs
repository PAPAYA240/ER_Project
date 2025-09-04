using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UI_SkillBase : UI_Base
{
    public override void Init()
    {
        
    }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        
    }


    public Action OnLevelUp = null;

    protected int _skillLevel = 0;
    protected int _maxSkillLevel = 5;

    protected const string _yellow = "#B89249";
    protected const string _gray = "#505050";

    public abstract void SkillLevelUp();
    public abstract void UseSkill();
    public abstract void ActivateLevelUp(bool DoYouActivate);
    public abstract void SetImage(string path);
    public virtual void SetStaminaCost(int value) { }
    public virtual void SetMaxCool(float value) { }
    public virtual bool IsEnoughStamina(float curStamina) { return false; }

}
