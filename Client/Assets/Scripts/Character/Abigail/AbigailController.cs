using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbigailController : MyPlayerController
{

    // E
    float _warpRange = 5.5f;
    GameObject _skillTarget = null;

    protected override void UpdateSkillKeyInput()
    {
        if (IsKeyInput == false && Input.GetKeyDown(KeyCode.Q))
        {
            SetSkillInput(KeyCode.Q);
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.W))
        {
            SetSkillInput(KeyCode.W);
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.E))
        {
            GameObject target = TryGetAttackableObject();
            if (target == null)
                return;

            Vector3 pos = target.transform.position;
            if (Vector3.Distance(pos, transform.position) <= _warpRange)
            {
                _skillTarget = target;
                SetSkillInput(KeyCode.E);
            }
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            SetSkillInput(KeyCode.R);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            //Skill_F();
        }
    }

    //protected override void GetMouseInput()
    //{
    //    if (!_canDash && !_isDashing)
    //        base.GetMouseInput();

    //    if (Input.GetMouseButton(1) && _canDash)
    //        StartDash();
    //}

    //protected override void UpdateMoving()
    //{
    //    if (!_isDashing)
    //        base.UpdateMoving();
    //}

    protected override void UpdateSkill()
    {

    }
}
