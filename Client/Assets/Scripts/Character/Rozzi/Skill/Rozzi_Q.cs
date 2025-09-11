using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Rozzi_Q : SkillBase
{
    public override void Execute()
    {
        _animator.CrossFadeInFixedTime("ROZZI_Q", 0.1f);

        // Debug.Log("Play Skill Animation");
    }
}