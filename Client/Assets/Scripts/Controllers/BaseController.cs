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

    float _speedCoeff = 3.2f;

    StatInfo _stat = new StatInfo();
    public virtual StatInfo Stat
    {
        get { return _stat; }
        set
        {
            if (_stat.Equals(value))
                return;

            _stat.MergeFrom(value);
        }
    }

    public float Speed
    {
        get { return Stat.MoveSpeed * _speedCoeff; }
        set { Stat.MoveSpeed = value; }
    }

    public virtual int Hp
    {
        get { return Stat.Hp; }
        set
        {
            Stat.Hp = value;
        }
    }

    public virtual int Stamina
    {
        get { return Stat.Stamina; }
        set { Stat.Stamina = value; }
    }

    protected bool _updated = false;

    PositionInfo _positionInfo = new PositionInfo();
    RotationInfo _rotationInfo = new RotationInfo() { Qw = 1.0f };
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

    ObjectInfo _ObjectInfo;
    public ObjectInfo ObjInfo
    {
        get { return _ObjectInfo; }
        set { _ObjectInfo = value; }
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
    protected NavMeshAgent _navMeshAgent;
    protected NavMeshAgent _agent;  // Player

    public virtual CreatureState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            if (_agent != null && _agent.isActiveAndEnabled &&
                (State == CreatureState.Moving && value != CreatureState.Moving))
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            PosInfo.State = value;
            UpdateAnimation();
            _updated = true;
        }
    }

    protected virtual void UpdateAnimation() {}

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
        _navMeshAgent = GetComponent<NavMeshAgent>();

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
            case CreatureState.Rest:
                UpdateRest();
                break;
        }
    }

    // 뼈 찾는 함수
    public Transform FindInDescendants(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindInDescendants(child, name);
            if (result != null)
                return result;
        }
        return null;
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
