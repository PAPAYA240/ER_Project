using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbigailController : MyPlayerController
{
    // E
    float _warpRange = 6.2f;
    float _warpRadius = 1.2f;
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
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.y = 0;
            if (Vector3.Distance(mousePos, CellPos) > _warpRange)
                return;

            GameObject target = TryGetAttackableObject(_warpRadius);
            if (target == null)
                return;

            Vector3 pos = transform.position;
            pos.y = 0;
            if (Vector3.Distance(mousePos, pos) <= _warpRange)
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

    #region Skill
    protected override void Skill_Q()
    {
        PlayAnimation("SKILL_Q", 0.1f);
    }

    protected override void Skill_W()
    {
        PlayAnimation("SKILL_W", 0.1f);
    }

    protected override void Skill_E()
    {
        PlayAnimation("SKILL_E", 0.1f);
    }

    protected override void Skill_R()
    {
        PlayAnimation("SKILL_R", 0.1f);
    }
    #endregion
}
