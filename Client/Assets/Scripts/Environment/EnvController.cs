using Google.Protobuf.Protocol;
using UnityEngine;

public class EnvController : BaseController
{
    [SerializeField] public EnvType Type;

    // Components
    protected Animator animator;

    // State
    protected bool _isActive = false;

    protected override void Init()
    {
        base.Init();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    #region Interaction
    protected GameObject _triggerCreature = null;
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsValidTrigger(other))
            return;

        _triggerCreature = other.gameObject;
        RequestCollect(other.gameObject.GetComponent<PlayerController>());
    }
    
    protected virtual void OnTriggerExit(Collider other)
    {
    }

    private bool IsValidTrigger(Collider other)
    {
        PlayerController pc = other.gameObject.GetComponent<PlayerController>();
        if (pc == null)
            return false;

        if (!_isActive)
            return false;

        return true;
    }

    protected virtual void TryHandleInteraction(PlayerController target)
    {
    }

    #endregion

    #region Network

    protected void RequestCollect(PlayerController player)
    {
        if (player == null)
            return;

        C_EnvRequest request = new C_EnvRequest
        {
            ObjectId = Id,
            EnvType = Type,
            TargetId = player.Id,
        };
        Managers.Network.Send(request);
    }

    public void OnInteractionAuthorized(PlayerController target)
    {
        TryHandleInteraction(target);
    }

    #endregion
}