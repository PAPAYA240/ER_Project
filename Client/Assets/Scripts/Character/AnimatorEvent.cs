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
}
