using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TheodoreInputController : PlayerInputController
{
    private float _effectDuration = 10f;
    private float _elapsedTime = 0f;

    protected override void ChargeSkill(KeyCode key)
    {
        _player.Indicator.EnableIndicator(_player.ObjInfo.Player.CharType, key);

        if (key == KeyCode.Q)
        {
            StartCoroutine(ChargingSkill(key));
            return;
        }

        Action onConfirm = () => CallSkill(key);
        Action onCancel = () => CancelSkill();
        StartCoroutine(InputSkill(key, onConfirm, onCancel));
    }
    private IEnumerator InputSkill(KeyCode key, Action onConfirm, Action onCancel)
    {
        while (!Input.GetKeyUp(key) && !Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButtonDown(1))
            {
                onCancel?.Invoke();
                yield break;
            }
            yield return null;
        }
        onConfirm?.Invoke();
    }
    private void CallSkill(KeyCode key)
    {
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);

        C_SkillInput skillCmd = _skill.TryCast((int)key, GetAttackableUnderCursorID(), GetMouseWorldPosition());
        if (skillCmd != null)
            Managers.Network.Send(skillCmd);
    }
    private void CancelSkill()
    {
    }
    private IEnumerator ChargingSkill(KeyCode key)
    {
        C_SkillPrepare PreparePacket = new C_SkillPrepare()
        {
            ObjectId = _player.ObjInfo.ObjectId,
            SkillKey = (int)key
        };
        Managers.Network.Send(PreparePacket);

        while (Input.GetKey(key) &&  _elapsedTime < _effectDuration)
        {
            _elapsedTime += Time.deltaTime;
            yield return null;
        }

        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);
        if (_elapsedTime > _effectDuration)
        {
            _elapsedTime = 0;
            yield break;
        }
        else
        {
            C_SkillInput skillCmd = _skill.TryCast((int)key, GetAttackableUnderCursorID(), GetMouseWorldPosition());
            if (skillCmd != null)
                Managers.Network.Send(skillCmd);
        }
    }
}