using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class TheodoreInputController : PlayerInputController
{
    private const float EFFECT_DURATION = 10f;
    private const float CANCEL_DURATION = 0.5f;
    private const float SNIPER_AIM_DURATION = 10f;

    const bool SKIP_STATE_CHECK = false;

    private float _elapsedTime = 0f;
    private KeyCode? _currentSkillKey = null;
    private Coroutine _cancelCoroutine = null;

    private void Start()
    {
    }

    protected override Vector3 GetAttackStopPosition(Vector3 from, Vector3 target)
    {
        Vector3 dir = target - from;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon)
            return target;
        dir /= dist;

        float stop = Mathf.Max(0.05f, _player.AttackRange); 

        return target - dir * stop;
    }


    protected override void ChargeSkill(KeyCode key)
    {
        if (!UseSkill(key))
            return;

        _player.Indicator.EnableIndicator(_player.ObjInfo.Player.CharType, key);
        switch (key)
        {
        case KeyCode.Q:
                StartCoroutine(ChargingSkill(key, onCancel: () => CancelSkill(key)));
                break;

        case KeyCode.D:
                {
                    StartCoroutine(InputSkill(key,
                    onConfirm: () => ExecuteSniperSkill(key),
                    onCancel: () => CancelSkill(key)));
                }
                break;
         case KeyCode.W:
         case KeyCode.E:
         case KeyCode.R:
                {
                    StartCoroutine(InputSkill(key,
                    onConfirm: () => ExecuteSkill(key),
                    onCancel: () => CancelSkill(key)));
                }
                break;
        }
    }
    #region 스킬 실행
    private void ExecuteSkill(KeyCode key)
    {
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);
        SendSkillInputPacket(key);
        _currentSkillKey = null;
    }
    private void ExecuteSniperSkill(KeyCode key)
    {
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);
        StartCoroutine(SniperSkill(key));
    }

    private const float SNIPER_DISTANCE = 15f;
    private const float SNIPER_ZOOM_DURATION = 10f;
    private IEnumerator SniperSkill(KeyCode key)
    {
        _player.LookAtMouse();

        CameraController cc = Camera.main.gameObject.GetComponent<CameraController>();
        if (cc == null)
            yield break;

        SendSkillInputPacket(key);
        _player.Indicator.EnableIndicator(_player.ObjInfo.Player.CharType, KeyCode.F1);
        //_player.Indicator.FindIndicatorObject("Center");
        Vector3 aimCenter = transform.position +( _player.transform.forward * SNIPER_DISTANCE);
        cc.StartAimMode(aimCenter, zoomOutDistance: SNIPER_ZOOM_DURATION);

        float elapsed = 0;
        while (elapsed < SNIPER_AIM_DURATION)
        {
            elapsed += Time.deltaTime;
            // 스킬 취소
            if (Input.GetMouseButtonDown(1))
            {
                cc.EndAimMode();
                _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, KeyCode.F1);
                yield break;
            }

            // 스킬 공격
            if (Input.GetMouseButtonDown(0))
            {
                //cc.EndAimMode();
                SendSkillExecutePacket(key);
            }
            yield return null;
        }

        cc.EndAimMode();
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, KeyCode.F1);
    }
    #endregion

    #region 스킬 입력 처리
    private bool UseSkill(KeyCode key)
    {
        _skill.SkillDict.TryGetValue(key, out SkillBase skill);
        if (skill == null || skill.CurLevel <= 0)
            return false;
        if (_skill.CoolDownDict.TryGetValue(key, out PlayerSkillController.CoolTime coolTimeInfo))
        {
            if (coolTimeInfo.isCoolDown)
                return false;
        }
        _currentSkillKey = key;
        return true;
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

    private IEnumerator ChargingSkill(KeyCode key, Action onCancel)
    {
        SendSkillPreparePacket(key);

        while (Input.GetKey(key) &&  _elapsedTime < EFFECT_DURATION)
        {
            _elapsedTime += Time.deltaTime;
            yield return null;
        }

        onCancel.Invoke();
        if (_elapsedTime < EFFECT_DURATION)
        {
            SendSkillInputPacket(key, SKIP_STATE_CHECK);
        }
    }
    private void CancelSkill(KeyCode key)
    {
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);
        _elapsedTime = 0;
        _currentSkillKey = null;
    }
#endregion

    #region 이동 처리
    public override C_SetMoveTarget GetSetMoveTarget()
    {
        if (_cancelCoroutine != null)
            return null;

        // 스킬 키 + 우클릭 동시 입력 시 쿨다운 시작
        if (_currentSkillKey != null &&
            _currentSkillKey != KeyCode.Q &&
            Input.GetKey((KeyCode)_currentSkillKey) && 
            Input.GetMouseButton(1))
        {
            _cancelCoroutine = StartCoroutine(CancelCooldown());
            return null;
        }

        if (_currentSkillKey != null && (KeyCode)_currentSkillKey == KeyCode.D && Input.GetMouseButton(1))
        {
            _currentSkillKey = null;
            _cancelCoroutine = StartCoroutine(CancelCooldown(0.9f));
            return null;
        }
        return base.GetSetMoveTarget();
    }

    private IEnumerator CancelCooldown(float duration = CANCEL_DURATION)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _cancelCoroutine = null;
    }
    #endregion

    #region Packet
    private void SendSkillInputPacket(KeyCode key, bool checkSkillState = true)
    {
        C_SkillInput skillCmd = _skill.TryCast(
            (int)key,
            GetAttackableUnderCursorID(),
            GetMouseWorldPosition(),
            checkSkillState
        );

        if (skillCmd != null)
            Managers.Network.Send(skillCmd);
    }
    private void SendSkillPreparePacket(KeyCode key)
    {
        C_SkillPrepare preparePacket = new C_SkillPrepare
        {
            ObjectId = _player.ObjInfo.ObjectId,
            SkillKey = (int)key
        };
        Managers.Network.Send(preparePacket);
    }
    private void SendSkillExecutePacket(KeyCode key)
    {
        C_SkillExecute executePacket = new C_SkillExecute
        {
            ObjectId = _player.ObjInfo.ObjectId,
            SkillKey = (int)key
        };
        Managers.Network.Send(executePacket);
    }
    #endregion
}