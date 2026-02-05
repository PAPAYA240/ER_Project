using UnityEngine;

public class PlayIndicatorNode : ActionNode
{
    private GameObject _indicator = null;
    public string _indicatorPrefabPath = null;
    private bool _isActive = false;

    public float _delayTime;
    private float _elapsedTime = 0f;

    public override void Enter(GameObject obj)
    {
        _elapsedTime = 0f;
        _isActive = false;

        if (_indicator == null)
        {
            _indicator = Managers.Resource.Instantiate(_indicatorPrefabPath);
        }
    }

    public override NodeStatus Execute(GameObject obj)
    {
        MonsterController monster = obj.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        if (_indicator == null)
            return NodeStatus.Failure;

        if (!_isActive)
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < _delayTime)
            {
                return NodeStatus.Running;
            }
            _indicator.SetActive(true);
            _isActive = true;
        }

        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear)
    {
        if (_indicator != null)
        {
            _indicator.SetActive(false);
        }

        _elapsedTime = 0f;
        _isActive = false;
    }
}
