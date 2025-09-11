using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Rozzi_R : SkillBase
{
    public override void Execute()
    {
        _animator.CrossFadeInFixedTime("ROZZI_R", 0.1f);

        // Debug.Log("Play Skill Animation");
    }
}