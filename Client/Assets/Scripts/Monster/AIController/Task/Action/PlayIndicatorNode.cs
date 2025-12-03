using UnityEngine;

public class PlayIndicatorNode : ActionNode
{
    public string indicatorPrefabPath;
    public float delayTime;

    private GameObject indicator = null;
    private bool _isActive = false;
    private float _elapsedTime = 0f;

    public override void Enter(GameObject obj)
    {
        _isActive = false;
        _elapsedTime = 0f;
        if(indicator == null)
            indicator = Managers.Resource.Instantiate(indicatorPrefabPath);
    }

    public override NodeStatus Execute(GameObject obj)
    {
        MonsterController monster = obj.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        if (indicator == null)
            return NodeStatus.Failure;

        if (!_isActive)
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < delayTime)
                return NodeStatus.Running;

            indicator.SetActive(true);
            _isActive = true;
        }
        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear)
    {
        if (indicator != null)
            indicator.SetActive(false);

        _elapsedTime = 0f;
        _isActive = false;
    }
}
