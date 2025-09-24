using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnvironmentInteraction
{
    void Interact(EnvController controller);
}

public class EnvController : BaseController
{
    private long _nextUseTime = 0;
    public EnvType _envType;
    protected Animator animator;

    private IEnvironmentInteraction _interactionStrategy;
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
    }

    public void SetNextUseTime(long time)
    {
        _nextUseTime = time;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            transform.parent.gameObject.SetActive(false);
            Debug.Log("충돌");
        }
    }
}
