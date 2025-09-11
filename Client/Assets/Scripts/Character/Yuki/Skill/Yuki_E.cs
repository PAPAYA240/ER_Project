using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Yuki_E : SkillBase
{
    public override void Execute()
    {
        _animator.CrossFadeInFixedTime("YUKI_E", 0.1f);

        Debug.Log("Play Skill Animation");
    }
}
