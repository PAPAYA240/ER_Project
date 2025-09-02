using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

public class BaseController : MonoBehaviour
{
    public int Id { get; set; }

    StatInfo _stat = new StatInfo();
    public virtual StatInfo Stat
    {
        get { return _stat; }
        set
        {
            if (_stat.Equals(value))
                return;

            _stat.Hp = value.Hp;
            _stat.MaxHp = value.MaxHp;
            _stat.Speed = value.Speed;
        }
    }

    public float Speed
    {
        get { return Stat.Speed; }
        set { Stat.Speed = value; }
    }

    public virtual int Hp
    {
        get { return Stat.Hp; }
        set
        {
            Stat.Hp = value;
        }
    }

    protected bool _updated = false;

    PositionInfo _positionInfo = new PositionInfo();
    RotationInfo _rotationInfo = new RotationInfo();
    public PositionInfo PosInfo
    {
        get { return _positionInfo; }
        set
        {
            if (_positionInfo.Equals(value))
                return;

            CellPos = new Vector3(value.PosX, value.PosY, value.PosZ);
            State = value.State;
        }
    }

    public RotationInfo RotInfo
    {
        get { return _rotationInfo; }
        set
        {
            if (value == null)  
                return;

            if (_rotationInfo.Equals(value)) 
                return;

            _rotationInfo.Qx = value.Qx;
            _rotationInfo.Qy = value.Qy;
            _rotationInfo.Qz = value.Qz;
            _rotationInfo.Qw = value.Qw;
        }
    }

    ObjectInfo _ObjectInfo = new ObjectInfo();
    public ObjectInfo ObjInfo
    {
        get { return _ObjectInfo; }
        set { }
    }

    public void SyncPos()
    {
        transform.position = CellPos;
        transform.rotation = RotInfo;
    }

    public Vector3 CellPos
    {
        get
        {
            return new Vector3(PosInfo.PosX, PosInfo.PosY, PosInfo.PosZ);
        }

        set
        {
            if (PosInfo.PosX == value.x && PosInfo.PosY == value.y && PosInfo.PosZ == value.z)
                return;

            PosInfo.PosX = value.x;
            PosInfo.PosY = value.y;
            PosInfo.PosZ = value.z;
            _updated = true;
        }
    }

    protected Animator _animator;

    public virtual CreatureState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            PosInfo.State = value;
            UpdateAnimation();
            _updated = true;
        }
    }

    protected virtual void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (State == CreatureState.Idle)
        {

        }
        else if (State == CreatureState.Moving)
        {

        }
        else if (State == CreatureState.Skill)
        {

        }
        else
        {

        }
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        UpdateController();
    }

    protected virtual void Init()
    {
        _animator = GetComponentInChildren<Animator>();

        SyncPos();

        UpdateAnimation();
    }

    protected virtual void UpdateController()
    {
        switch (State)
        {
            case CreatureState.Idle:
                UpdateIdle();
                break;
            case CreatureState.Moving:
                UpdateMoving();
                break;
            case CreatureState.Skill:
                UpdateSkill();
                break;
            case CreatureState.Dead:
                UpdateDead();
                break;
        }
    }

    protected virtual void UpdateIdle()
    {
       
    }

    // ������ �̵��ϴ� ���� ó��
    protected virtual void UpdateMoving()
    {
        ////Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
        //Vector3 moveDir = destPos - transform.position;

        //// ���� ���� üũ
        //float dist = moveDir.magnitude;
        //if (dist < Speed * Time.deltaTime)
        //{
        //    transform.position = destPos;
        //    MoveToNextPos();
        //}
        //else
        //{
        //    transform.position += moveDir.normalized * Speed * Time.deltaTime;
        //    State = CreatureState.Moving;
        //}
    }

    protected virtual void MoveToNextPos()
    {

    }

    protected virtual void UpdateSkill()
    {

    }

    protected virtual void UpdateDead()
    {

    }
}
