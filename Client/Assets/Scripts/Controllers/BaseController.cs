using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class BaseController : MonoBehaviour
{
    public int Id { get; set; }

    float _speedCoeff = 2.3f;

    public virtual StatInfo Stat
    {
        get { return ObjInfo.StatInfo; }
        set
        {
            if (ObjInfo.StatInfo.Equals(value))
                return;

            ObjInfo.StatInfo.MergeFrom(value);
        }
    }

    public float Speed
    {
        get { return Stat.MoveSpeed * _speedCoeff; }
        set { Stat.MoveSpeed = value; }
    }

    public virtual float Hp
    {
        get { return Stat.Hp; }
        set { Stat.Hp = value; }
    }

    public virtual float MaxHp
    {
        get { return Stat.MaxHp; }
        set { Stat.MaxHp = value; }
    }

    public virtual float Barrier
    {
        get { return Stat.Barrier; }
        set { Stat.Barrier = value; }
    }

    public virtual float Stamina
    {
        get { return Stat.Stamina; }
        set { Stat.Stamina = value; }
    }

    public virtual float MaxStamina
    {
        get { return Stat.MaxStamina; }
        set { Stat.MaxStamina = value; }
    }

    protected bool _updated = false;

    public PositionInfo PosInfo
    {
        get { return ObjInfo.PosInfo; }
        set
        {
            if (ObjInfo.PosInfo.Equals(value))
                return;

            CellPos = new Vector3(value.PosX, value.PosY, value.PosZ);
            State = value.State;
        }
    }

    public RotationInfo RotInfo
    {
        get { return ObjInfo.RotInfo; }
        set
        {
            if (value == null)
                return;

            if (ObjInfo.RotInfo.Equals(value))
                return;

            ObjInfo.RotInfo.Qx = value.Qx;
            ObjInfo.RotInfo.Qy = value.Qy;
            ObjInfo.RotInfo.Qz = value.Qz;
            ObjInfo.RotInfo.Qw = value.Qw;

            _updated = true;
        }
    }

    ObjectInfo _objectInfo = new ObjectInfo()
    {
        StatInfo = new StatInfo(),
        PosInfo = new PositionInfo(),
        RotInfo = new RotationInfo() { Qw = 1f }        
    }; 

    public ObjectInfo ObjInfo
    {
        get { return _objectInfo; }
        set { _objectInfo = value; PosInfo = value.PosInfo; RotInfo = value.RotInfo; Stat = value.StatInfo; }
    }

    public void SyncPos(bool isWarp = false)
    {
        transform.position = CellPos;
        transform.rotation = RotInfo;
        if (true == isWarp)
            _agent.Warp(CellPos);
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
    protected NavMeshAgent _agent;  // Player
    protected GameObject _coordPrefab; // 아비게일 표식

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

    protected virtual void UpdateAnimation() {}

    void Start()
    {
        Init();
        //업데이트 함수들 호출
        //Stat = Stat;
    }

    void Update()
    {
        UpdateController();
    }

    protected virtual void Init()
    {
        _animator = GetComponentInChildren<Animator>();

        UpdateAnimation();

        //_coordPrefab = Managers.Resource.Instantiate($"UI/Character/Abigail/AbigailCoord");
        //_coordPrefab.transform.SetParent(gameObject.transform);
        ////_coordPrefab.SetActive(false);
        //AbigailCoord abigailCoord = _coordPrefab.GetComponentInChildren<AbigailCoord>();
        //if( abigailCoord != null)
        //    abigailCoord.SetTarget(gameObject);
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
            case CreatureState.Attack:
                UpdateAttack();
                break;
            case CreatureState.Charging:
                UpdateCharging();
                break;
            case CreatureState.Skill:
                UpdateSkill();
                break;
            case CreatureState.Dead:
                UpdateDead();
                break;
            case CreatureState.Rest:
                UpdateRest();
                break;
        }
    }

   
    protected virtual void UpdateIdle()
    {
    }

    protected virtual void UpdateMoving()
    {
    }

    protected virtual void MoveToNextPos()
    {

    }

    protected virtual void UpdateAttack()
    {

    }

    protected virtual void UpdateCharging()
    {
    }
    protected virtual void UpdateSkill()
    {

    }

    protected virtual void UpdateDead()
    {

    }

    protected virtual void UpdateRest()
    {

    }
}
