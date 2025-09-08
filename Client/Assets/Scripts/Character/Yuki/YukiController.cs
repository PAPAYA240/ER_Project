using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class YukiController : MyPlayerController
{
    protected override void Init()
    {
        base.Init();
    }

    protected override void UpdateKeyInput()
    {
        base.UpdateKeyInput();
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    ExecuteSkill(KeyCode.Q);
        //}
        //else if (Input.GetKeyDown(KeyCode.W))
        //{
        //    //ExecuteSkill("Rozzi_W");
        //}
        //else if (Input.GetKeyDown(KeyCode.D))
        //{
        //    State = CreatureState.Dead;
        //}
        //else if (Input.GetKeyDown(KeyCode.C))
        //{
        //    State = CreatureState.Idle;
        //    SetBoolAnimation("bFishing", false);
        //}
    }

    protected override void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (State == CreatureState.Idle)
        {
            _animator.CrossFadeInFixedTime("WAIT", 0.1f);
        }
        else if (State == CreatureState.Moving)
        {
            _animator.CrossFadeInFixedTime("RUN", 0.1f);
        }
        else if (State == CreatureState.Skill)
        {

        }
        else if (State == CreatureState.Dead)
        {
            TriggerAnimation("tDeath");
            //SetBoolAnimation("bFishing", true);
        }
    }

    protected override void UpdateIdle()
    {
        base.UpdateIdle();
    }

    protected override void UpdateMoving()
    {
        base.UpdateMoving();
    }

    protected override void UpdateDead()
    {
    }
}