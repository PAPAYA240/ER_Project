using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Rozzi_W : SkillBase
{
    public override void Execute()
    {
        _animator.CrossFadeInFixedTime("ROZZI_W", 0.1f);

        Debug.Log("Play Skill Animation");
    }
}