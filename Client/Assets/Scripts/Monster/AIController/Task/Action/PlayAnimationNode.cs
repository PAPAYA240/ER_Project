using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Data;

public abstract class AnimationControlNode : ActionNode
{
    protected MonsterController _controller;
    protected Animator _animator;
    protected NavMeshAgent _agent;

    protected readonly int _waitAnimHash = Animator.StringToHash("WAIT");
    protected readonly int _isWaitParamHash = Animator.StringToHash("bWait");
    protected readonly int _moveVelocityHash = Animator.StringToHash("moveVelocity");

    protected bool _hasMoveVelocityParam = false;
    private bool _isInitialized = false;

    protected bool Initialize(GameObject owner)
    {
        if (_isInitialized) 
            return true;

        _controller = owner.GetComponentInChildren<MonsterController>();
        _animator = owner.GetComponentInChildren<Animator>();
        _agent = owner.GetComponentInChildren<NavMeshAgent>();

        if (_animator != null)
        {
            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == _moveVelocityHash && param.type == AnimatorControllerParameterType.Float)
                {
                    _hasMoveVelocityParam = true;
                    break;
                }
            }
        }

        _isInitialized = (_controller != null && _animator != null);
        return _isInitialized;
    }
}

#region Trigger Animation
public class PlayAnimation : AnimationControlNode
{
    public List<string> chainAnimNames;

    private int _currentIndex = 0;
    private bool _isPlaying = false;
    private string _currentAnimName;

    private const float CROSSFADE_DURATION = 0.1f;
    private const float TRANSITION_THRESHOLD = 0.95f;

    public override void Enter(GameObject obj)
    {
        if (!Initialize(obj))
            return;

        if (_hasMoveVelocityParam)
            _animator.SetFloat(_moveVelocityHash, 0);

        _isPlaying = false;
        _currentIndex = 0;
    }

    public override NodeStatus Execute(GameObject owner)
    {
        if(!_isPlaying)
        {
             int animHash = Animator.StringToHash(chainAnimNames[_currentIndex]);
            if (_animator.HasState(0, animHash))
            {
                Play();
            }
            else
            {
                return NodeStatus.Failure;
            }
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(_currentAnimName))
        {
            if (stateInfo.normalizedTime >= TRANSITION_THRESHOLD)
            {
                ++_currentIndex;
                if (_currentIndex >= chainAnimNames.Count)
                {
                    return NodeStatus.Success;
                }
                return NodeStatus.Running;
            }
        }
        return NodeStatus.Running;
    }

    private void Play()
    {
        _isPlaying = true;
        _currentAnimName = chainAnimNames[_currentIndex];
        _animator?.CrossFadeInFixedTime(_currentAnimName, CROSSFADE_DURATION, 0);
        _controller.Sound.GetEffect3D(_currentAnimName, _controller.transform.position);
    }

    public override void Exit(GameObject obj, bool clear)
    {
        ClearAnim(clear);

        _currentAnimName = string.Empty;
        _isPlaying = false;
        _currentIndex = 0;
    }

    private void ClearAnim(bool clear)
    {
        if (_controller.State == CreatureState.Dead || !clear)
            return;

        _animator.SetBool(_isWaitParamHash, true);
        if (_animator != null && _animator.HasState(0, _waitAnimHash))
        {
            _animator?.CrossFadeInFixedTime(_waitAnimHash, CROSSFADE_DURATION, 0);
        }
    }

}
#endregion

#region Float Animation
public class PlayAnimatorFloatNode : AnimationControlNode
{
    private Vector3 _lastPos;
    private bool _isFirstFrame = true;

    public override void Enter(GameObject obj) { }

    public override NodeStatus Execute(GameObject owner)
    {
        if (!Initialize(owner))
            return NodeStatus.Failure;

        if (_controller.State != CreatureState.Moving)
        {
            if (_hasMoveVelocityParam)
                _animator.SetFloat(_moveVelocityHash, 0);
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
            _animator.SetFloat(_moveVelocityHash, speed);

        _lastPos = owner.transform.position;

        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear) { }
}

#endregion