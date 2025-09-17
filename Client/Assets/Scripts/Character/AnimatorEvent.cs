using Google.Protobuf.Protocol;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorEvent : MonoBehaviour
{
    [SerializeField] 
    private PlayerController _controller;

    private void Awake()
    {
        _controller = GetComponentInParent<PlayerController>();
    }

    public void OnSkillEnd()
    {
        if (_controller == null)
            return;

        _controller.State = CreatureState.Idle;
        _controller.IsKeyInput = false;
        Debug.Log("(Animation Event)");
    }
    public void OnAttackTiming()
    {
        if (_controller == null)
            return;

        _controller.OnAttackTiming();
    }

    //public void OnAttackEnd()
    //{
    //    if (_controller == null)
    //        return;

    //    if (_controller.AttackCount < _controller.MaxAttackCount)
    //    {
    //        _controller.AttackCount++;
    //        _controller.State = CreatureState.Idle;
    //    }
    //    else
    //    {
    //        _controller.AttackCount = 1;
    //        _controller.State = CreatureState.Idle;
    //    }
    //}
}
