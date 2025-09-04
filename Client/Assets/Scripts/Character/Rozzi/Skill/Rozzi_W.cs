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
        PlayAnimation("tSkill02");

        Debug.Log("Play Skill Animation");
    }
}