using Google.Protobuf.Protocol;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TheodoreInputController : PlayerInputController
{
    private const float EFFECT_DURATION = 10f;
    private const float CANCEL_DURATION = 0.5f;

    private float _elapsedTime = 0f;
    private float _elapsedCancelTime = 0f;

    private KeyCode? _currentSkillKey = null;
    private Coroutine _cancelCoroutine = null;

    protected override void ChargeSkill(KeyCode key)
    {
        _currentSkillKey = key;
        _player.Indicator.EnableIndicator(_player.ObjInfo.Player.CharType, key);

        switch (key)
        {
        case KeyCode.Q:
            StartCoroutine(ChargingSkill(key, onCancel: () => CancelSkill(key)));
                break;

        case KeyCode.D:
                {
                    StartCoroutine(InputSkill(key,
                    onConfirm: () => SniperSkill(key),
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
    private void SniperSkill(KeyCode key)
    {
        CameraController cc = Camera.main.gameObject.GetComponent<CameraController>();
        if (cc == null)
            return;
        Vector3 cameraForward = transform.forward;
        Vector3 playerPosition = _player.transform.position;
        Vector3 targetPosition = playerPosition + cameraForward * 10f;
 
        StartCoroutine(
                cc.CameraZoomOut(targetPosition,
                zoomOutDistance : 40f,
                duration : 12f)
            );
        SendSkillInputPacket(key);
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
        if (_elapsedTime > EFFECT_DURATION)
            yield break;

        SendSkillInputPacket(key);
    }

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

        return base.GetSetMoveTarget();
    }

    private IEnumerator CancelCooldown()
    {
        float elapsedTime = 0f;
        while (elapsedTime < CANCEL_DURATION)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _cancelCoroutine = null;
    }
    #endregion

    #region 스킬 실행 및 취소
    private void ExecuteSkill(KeyCode key)
    {
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);
        SendSkillInputPacket(key);
        _currentSkillKey = null;
    }
    private void CancelSkill(KeyCode key)
    {
        _player.Indicator.DisableIndicator(_player.ObjInfo.Player.CharType, key);
        _elapsedTime = 0;
        _currentSkillKey = null;
    }
    #endregion

    #region Packet
    private void SendSkillInputPacket(KeyCode key)
    {
        C_SkillInput skillCmd = _skill.TryCast(
            (int)key,
            GetAttackableUnderCursorID(),
            GetMouseWorldPosition()
        );

        if (skillCmd != null)
        {
            Managers.Network.Send(skillCmd);
        }
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
    #endregion
}