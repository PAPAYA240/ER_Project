using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class YukiController : MyPlayerController
{
    private Coroutine _coSkillE = null;

    public float dashDistance = 5f;
    public float dashDuration = 0.2f;

    private bool isDashing = false;

    protected override void Init()
    {
        base.Init();
        _attackRange = 1.5f;
    }

    protected override void UpdateSkillKeyInput()
    {
        if (IsKeyInput == false && Input.GetKeyDown(KeyCode.Q))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.Q;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.W))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.W;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.E))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.E;
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            _isUseSkill = true;
            _keyCode = KeyCode.R;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
    }

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

        Dash();
    }

    protected override void Skill_R()
    {
        PlayAnimation("SKILL_R", 0.1f);
    }

    #region Skill : E
    void Dash()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.transform.position.y;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mousePos);

        Vector3 direction = (mouseWorld - transform.position);
        direction.y = 0f;
        direction.Normalize();

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        StartCoroutine(DashCoroutine(direction));
    }

    IEnumerator DashCoroutine(Vector3 direction)
    {
        isDashing = true;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * dashDistance;

        while (elapsed < dashDuration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / dashDuration);
            elapsed += Time.deltaTime;
            UpdateTransform();
            yield return null;
        }

        transform.position = endPos;
        isDashing = false;
    }
    #endregion
}