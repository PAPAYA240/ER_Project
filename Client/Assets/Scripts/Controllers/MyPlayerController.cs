using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;
    Dictionary<KeyCode, bool> _isCoolDown = new Dictionary<KeyCode, bool>();

    protected override void Init()
    {
        base.Init();

        _isCoolDown[KeyCode.Q] = false;
        _isCoolDown[KeyCode.W] = false;
        _isCoolDown[KeyCode.E] = false;
        _isCoolDown[KeyCode.R] = false;
    }

    protected override void UpdateController()
    {
        switch (State)
        {
            case CreatureState.Idle:
                GetDirInput();
                break;
            case CreatureState.Moving:
                GetDirInput();
                break;
        }

        base.UpdateController();
    }

    protected override void UpdateIdle()
    {
        // 이동 상태로 갈지 확인
        if (_moveKeyPressed)
        {
            State = CreatureState.Moving;
            return;
        }

        InputSkill();
    }

    void InputSkill()
    {
        if (!_isCoolDown[KeyCode.Q] && Input.GetKey(KeyCode.Q))
        {
            Debug.Log("Skill!");

            C_Skill skill = new C_Skill() { Info = new SkillInfo() };
            skill.Info.SkillId = 1;
            Managers.Network.Send(skill);

            StartCoroutine(CoInputCooltime(KeyCode.Q, 0.2f));
        }
        else if (!_isCoolDown[KeyCode.W] && Input.GetKey(KeyCode.W))
        {
            Debug.Log("Skill!");

            C_Skill skill = new C_Skill() { Info = new SkillInfo() };
            skill.Info.SkillId = 2;
            Managers.Network.Send(skill);

            StartCoroutine(CoInputCooltime(KeyCode.W, 3f));
        }
        else if (!_isCoolDown[KeyCode.E] && Input.GetKey(KeyCode.E))
        {
            Debug.Log("Skill!");

            C_Skill skill = new C_Skill() { Info = new SkillInfo() };
            skill.Info.SkillId = 3;
            Managers.Network.Send(skill);

            StartCoroutine(CoInputCooltime(KeyCode.E, 5f));
        }
        else if (!_isCoolDown[KeyCode.R] && Input.GetKey(KeyCode.R))
        {
            Debug.Log("Skill!");

            C_Skill skill = new C_Skill() { Info = new SkillInfo() };
            skill.Info.SkillId = 4;
            Managers.Network.Send(skill);

            StartCoroutine(CoInputCooltime(KeyCode.R, 10f));
        }
    }

    // 스킬 쿨타임
    //Coroutine _coSkillCooltime;
    IEnumerator CoInputCooltime(KeyCode key, float time)
    {
        _isCoolDown[key] = true;

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            yield return null;

        }

        _isCoolDown[key] = false;
        //_coSkillCooltime = null;
    }

    void LateUpdate()
    {
        Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }

    // 키보드 입력
    void GetDirInput()
    {
        _moveKeyPressed = true;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            Dir = MoveDir.Up;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            Dir = MoveDir.Down;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            Dir = MoveDir.Left;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            Dir = MoveDir.Right;
        }
        else
        {
            _moveKeyPressed = false;
        }
    }

    protected override void MoveToNextPos()
    {
        if (_moveKeyPressed == false)
        {
            State = CreatureState.Idle;
            CheckUpdatedFlag();
            return;
        }

        Vector3Int destPos = CellPos;

        switch (Dir)
        {
            case MoveDir.Up:
                destPos += Vector3Int.up;
                break;
            case MoveDir.Down:
                destPos += Vector3Int.down;
                break;
            case MoveDir.Left:
                destPos += Vector3Int.left;
                break;
            case MoveDir.Right:
                destPos += Vector3Int.right;
                break;
        }

        if (Managers.Map.CanGo(destPos))
        {
            if (Managers.Object.FindCreature(destPos) == null)
            {
                CellPos = destPos;
            }
        }

        CheckUpdatedFlag();
    }

    protected override void CheckUpdatedFlag()
    {
        if (_updated)
        {
            C_Move movePacket = new C_Move();
            movePacket.PosInfo = PosInfo;
            Managers.Network.Send(movePacket);
            _updated = false;
        }
    }
}
