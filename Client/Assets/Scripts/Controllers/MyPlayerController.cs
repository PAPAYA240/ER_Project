using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;
    Dictionary<KeyCode, bool> _isCoolDown = new Dictionary<KeyCode, bool>();

    int _mask = (1 << (int)Define.Layer.Map);
    Vector3 _dstPos = Vector3.zero;

    protected override void Init()
    {
        base.Init();
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);

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
                GetMouseInput();
                break;
            case CreatureState.Moving:
                GetMouseInput();
                break;
        }

        base.UpdateController();
    }

    protected override void UpdateIdle()
    {
        // �̵� ���·� ���� Ȯ��
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

    protected override void UpdateMoving()
    {
        Vector3 moveDir = _dstPos - transform.position;
        moveDir.y = 0.0f;

        // ���� ���� üũ
        float dist = moveDir.magnitude;
        if (dist < Speed * Time.deltaTime)
        {
            transform.position = _dstPos;
            State = CreatureState.Idle;
            _moveKeyPressed = false;
        }
        else
        {
            transform.position += moveDir.normalized * Speed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 20 * Time.deltaTime);
            State = CreatureState.Moving;
        }
    }

    // ��ų ��Ÿ��
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

    [SerializeField]
    public Vector3 _offset = new Vector3(0, 10, -10);
    [SerializeField]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 targetPos = transform.position + _offset;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPos, smoothSpeed * Time.deltaTime);
        Camera.main.transform.LookAt(transform.position);
    }

    // ���콺 �Է�
    void GetMouseInput()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (Input.GetMouseButton(1))
        {
            if (raycastHit)
            {
                _dstPos = hit.point;
                State = CreatureState.Moving;

                _moveKeyPressed = true;
            }
        }
    }

    // Ű���� �Է�
    void GetDirInput()
    {
        _moveKeyPressed = true;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            
        }
        else
            _moveKeyPressed = false;
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
