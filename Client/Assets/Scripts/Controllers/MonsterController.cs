using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

public class MonsterController : CreatureController
{
	Coroutine _coSkill;

    enum MonsterSkill
    {
        None = 0,
        Attack1 = 1,
        Attack2 = 2,
        Skill1 = 3,
        Skill2 = 4,
        Skill3 = 5
    }

    MonsterSkill _currentSkill 
    { 
        get  { 
            return _currentSkill; 
        } 
        set {
            _currentSkill = value; 
        } 
    }

    protected override void Init()
	{
		base.Init();
	}

    protected override void UpdateController()
    {
        base.UpdateController();
    }
    // 보간에 필요한 변수들
    Vector3 _lastPos;
    Vector3 _currentPos;
    float _posRatio;

    Quaternion _lastRot;
    Quaternion _currentRot;
    float _rotRatio;

    // 서버에서 패킷을 받을 때 호출되는 함수
    public void OnRecvMovePacket(S_Move movePacket)
    {
        _lastPos = transform.position;
        _currentPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);

        _posRatio = 0f;

        _lastRot = transform.rotation;
        _currentRot = new Quaternion(movePacket.RotInfo.Qx, movePacket.RotInfo.Qy, movePacket.RotInfo.Qz, movePacket.RotInfo.Qw);
        _rotRatio = 0f;
    }

    protected override void UpdateMoving()
    {
        const float interpolationPosSpeed = 1f; 
        const float interpolationRotSpeed = 2f;

        _posRatio += Time.deltaTime * interpolationPosSpeed;
        _rotRatio += Time.deltaTime * interpolationRotSpeed;

        _posRatio = Mathf.Clamp01(_posRatio);
        _rotRatio = Mathf.Clamp01(_rotRatio);

        transform.position = Vector3.Lerp(_lastPos, _currentPos, _posRatio);
        transform.rotation = Quaternion.Slerp(_lastRot, _currentRot, _rotRatio);
    }

    public override void OnDamaged()
	{
		//Managers.Object.Remove(Id);
		//Managers.Resource.Destroy(gameObject);
	}

    protected override void UpdateSkill()
    {
        Debug.Log("UpdateSkill");
        // 스킬 랜덤 사용
    }
    public override void UseSkill(int skillId)
    {
        Debug.Log("UpdateSkill");
        if (skillId == 1)
        {
            State = CreatureState.Skill;
        }
    }
}
