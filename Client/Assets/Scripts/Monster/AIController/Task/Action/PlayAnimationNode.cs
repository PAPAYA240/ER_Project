using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Data;



#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif

public abstract class AnimationControlNode : ActionNode
{
    protected Animator _animator;
    protected MonsterController _controller;
    protected NavMeshAgent _agent;
    protected string _waitAnim = "WAIT";

    protected readonly int MoveVelocityHash = Animator.StringToHash("moveVelocity");
    protected bool _hasMoveVelocityParam = false;

    protected bool Check(GameObject owner)
    {
        if (_controller == null)
            _controller = owner.GetComponentInChildren<MonsterController>();

        if (_animator == null)
            _animator = owner.GetComponentInChildren<Animator>();

        if (_agent == null)
            _agent = owner.GetComponentInChildren<NavMeshAgent>();

        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.nameHash == MoveVelocityHash &&
                param.type == AnimatorControllerParameterType.Float)
            {
                _hasMoveVelocityParam = true;
                break;
            }
        }

        return (_controller != null && _animator != null);
    }
}

public class PlayAnimation : AnimationControlNode
{
    public List<string> chainAnimNames;

    private int _currentChainIndex = 0;
    private string _currentAnimName;
    private bool _play = false;
    public override void Enter(GameObject obj)
    {
        if (!Check(obj))
            return;

        if (_hasMoveVelocityParam)
            _animator.SetFloat(MoveVelocityHash, 0);

        _play = false;
        _currentChainIndex = 0;
    }
    public override NodeStatus Execute(GameObject owner)
    {
        if(!_play)
        {
             int animHash = Animator.StringToHash(chainAnimNames[_currentChainIndex]);
            if(_animator.HasState(0, animHash))
                Play(chainAnimNames[_currentChainIndex]); 
            else
                return NodeStatus.Failure;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(_currentAnimName))
        {
            if (stateInfo.normalizedTime >= 0.95f)
            {
                ++_currentChainIndex;

                // 애니메이션 끝
                if (_currentChainIndex >= chainAnimNames.Count)
                {
                    return NodeStatus.Success;
                }

                // 다음 애니메이션 재생
                Play(chainAnimNames[_currentChainIndex]);
                return NodeStatus.Running;
            }
        }
        return NodeStatus.Running;
    }

    private void Play(string anim)
    {
        _play = true;
        _currentAnimName = anim;
        _animator.CrossFadeInFixedTime(anim, 0.1f, 0);

        _controller.Sound.GetEffect3D(_currentAnimName, _controller.transform.position);
    }

    private void ClearAnim()
    {
        if (_controller.State == CreatureState.Dead)
            return;

        int waitAnimHash = Animator.StringToHash(_waitAnim);
        if (_animator != null && _animator.HasState(0, waitAnimHash))
            _animator?.CrossFadeInFixedTime(_waitAnim, 0.1f, 0);
    }
    public override void Exit(GameObject obj, bool clear)
    {
        ClearAnim();
        _currentAnimName = string.Empty;
        _currentChainIndex = 0;
        _play = false;
    }
}

#region 조건 Anim
// 움직임에 사용될 애니메이션 노드
public class PlayAnimatorFloatNode : AnimationControlNode
{
    private Vector3 _lastPos;
    private bool _isFirstFrame = true;

    public override void Enter(GameObject obj)
    {
    }

    public override NodeStatus Execute(GameObject owner)
    {
        if (Check(owner) == false)
            return NodeStatus.Failure;

        if (_controller.State != CreatureState.Moving)
        {
            if (_hasMoveVelocityParam)
                _animator.SetFloat(MoveVelocityHash, 0);
            return NodeStatus.Failure;
        }

        float speed = 0;
        if (!_isFirstFrame)
        {
            float distance = Vector3.Distance(owner.transform.position, _lastPos);

            speed = distance / Time.deltaTime;
        }

        _isFirstFrame = false;

        if (_hasMoveVelocityParam)
            _animator.SetFloat(MoveVelocityHash, speed);

        _lastPos = owner.transform.position;

        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear)
    {
    }

}

#endregion