using Google.Protobuf.Protocol;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

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

        _controller.GetComponentInChildren<Yuki_SkillQAttack>(true)?.PlayEffect();
    }
}
