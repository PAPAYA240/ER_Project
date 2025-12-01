using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using UnityEngine.InputSystem;

public class PlayIndicatorNode : ActionNode
{
    public string indicatorPrefabPath;
    public float delayTime;

    private GameObject indicator = null;
    private Canvas canvs = null;
    private bool _isActive = false;
    private float _elapsedTime = 0f;

    public override void Enter(GameObject obj)
    {
        _isActive = false;
        _elapsedTime = 0f;
    }

    public override NodeStatus Execute(GameObject obj)
    {
        MonsterController monster = obj.GetComponentInChildren<MonsterController>();
        if (monster == null)
            return NodeStatus.Failure;

        if (!_isActive)
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime < delayTime)
                return NodeStatus.Running;

            if (canvs == null)
            {
                indicator = Managers.Resource.Instantiate(indicatorPrefabPath);
                if (indicator == null)
                    return NodeStatus.Failure;

                indicator.transform.SetParent(obj.transform);
                indicator.transform.localRotation = Quaternion.identity;

                canvs = indicator.GetComponent<Canvas>();
                if (canvs == null)
                    return NodeStatus.Failure;

                canvs.transform.SetParent(obj.transform, false);
                canvs.transform.localPosition = Vector3.zero;
                canvs.renderMode = RenderMode.WorldSpace;
            }

            if (canvs)
            {
                canvs.enabled = true;
                indicator.SetActive(true);
            }

            _isActive = true;
        }

        return NodeStatus.Running;
    }

    public override void Exit(GameObject obj, bool clear)
    {
        if (canvs)
        {
            canvs.enabled = false;
            indicator.SetActive(false);
        }

        _elapsedTime = 0f;
        _isActive = false;
    }
}
