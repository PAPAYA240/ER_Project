using Google.Protobuf.Protocol;
using System;
using System.Collections;
using UnityEngine;

public class HyunwooInputController : PlayerInputController
{
    float _chargeTime = 0;
    Coroutine _coCharge = null;

    const float _fullCharge = 1.2f;
    const float _maxCharge = 3.2f;

    public override C_SkillInput GetSkillCommand()
    {
        // 배열 순서대로 키다운 검사 -> 처음 눌린 키에 대해 바로 생성/리턴
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            var key = _skillKeys[i];
            if (!Input.GetKeyDown(key))
                continue;
            
            if (key == KeyCode.R && _skill.SkillDict[KeyCode.R].CurLevel > 0 
                && _skill.CoolDownDict[KeyCode.R].coolTime == 0
                && _player.State != CreatureState.Skill 
                && _player.State != CreatureState.Stun)
            {
                if(null != _coCharge)
                {
                    StopCoroutine(_coCharge);
                    _coCharge = null;
                }
                _chargeTime = 0;
                _coCharge = StartCoroutine(CoCharge());
                _player.UI.PlayerInterface.SetChargingBar(DataManager.SkillDict[CharacterType.Hyunwoo][KeyCode.R].name, _fullCharge, _maxCharge);
            }

            return _skill.TryCast((int)key, GetAttackableUnderCursorID(), GetMouseWorldPosition());
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            if (null != _coCharge)
            {
                StopCoroutine(_coCharge);
                _coCharge = null;
            }

            _player.UI.PlayerInterface.StopChargingBar();

            C_ChargingSkill packet = new C_ChargingSkill();

            if(_chargeTime > _fullCharge)
                packet.CharginRatio = 1f;
            else
                packet.CharginRatio = _chargeTime / _fullCharge;

            Managers.Network.Send(packet);
            return null;
        }

        return null;
    }

    IEnumerator CoCharge()
    {
        while(_chargeTime < 3.2)
        {
            _chargeTime += Time.deltaTime;

            //Debug.Log($"charge time : {_chargeTime}");

            yield return null;
        }
        _chargeTime = 0;
    }
}
