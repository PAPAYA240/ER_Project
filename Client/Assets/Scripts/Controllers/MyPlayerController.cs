using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;

    int _mask = (1 << (int)Define.Layer.Map);
    Vector3 _dstPos = Vector3.zero;

    protected override void Init()
    {
        base.Init();

        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);
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
        // 이동 상태로 갈지 확인
        if (_moveKeyPressed)
        {
            State = CreatureState.Moving;
            return;
        }

        if (_coSkillCooltime == null && Input.GetKey(KeyCode.Space))
        {
            Debug.Log("Skill!");

            C_Skill skill = new C_Skill() { Info = new SkillInfo() };
            skill.Info.SkillId = 2;
            Managers.Network.Send(skill);

            _coSkillCooltime = StartCoroutine("CoInputCooltime", 0.2f);
        }
    }

    protected override void UpdateMoving()
    {
        Vector3 moveDir = _dstPos - transform.position;
        moveDir.y = 0.0f;

        // 도착 여부 체크
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

    Coroutine _coSkillCooltime;
    IEnumerator CoInputCooltime(float time)
    {
        yield return new WaitForSeconds(time);
        _coSkillCooltime = null;
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

    // 마우스 입력
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

    // 키보드 입력
    void GetDirInput()
    {
        _moveKeyPressed = true;

        if (Input.GetKey(KeyCode.W))
        {
            
        }
        else if (Input.GetKey(KeyCode.S))
        {
            
        }
        else if (Input.GetKey(KeyCode.A))
        {
            
        }
        else if (Input.GetKey(KeyCode.D))
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
