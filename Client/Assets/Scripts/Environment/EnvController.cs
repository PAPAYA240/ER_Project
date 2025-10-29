using Google.Protobuf.Protocol;
using UnityEngine;

public class EnvController : BaseController
{
    [SerializeField] public EnvType _envType;

    // Components
    protected Animator animator;

    // State
    protected bool _isActive = false;
    protected bool _isCollecting = false;

    // Network
    private int _lastRequestObjectId = -1;

    protected override void Init()
    {
        base.Init();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }


    #region Interaction

    protected void OnTriggerEnter(Collider other)
    {
        if (!IsValidTrigger(other))
            return;

        _isCollecting = true;
        TryHandleInteraction();
        RequestCollect();
    }

    private bool IsValidTrigger(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("MyPlayer"))
            return false;
        if (!_isActive || _isCollecting)
            return false;
        return true;
    }

    protected virtual void TryHandleInteraction()
    {
        _isCollecting = false;
    }

    #endregion

    #region Network

    private void RequestCollect()
    {
        _lastRequestObjectId = Id;

        C_EnvRequest request = new C_EnvRequest
        {
            ObjectId = Id,
            EnvType = _envType,
        };
        Managers.Network.Send(request);
    }

    public void OnInteractionAuthorized()
    {
        if (_lastRequestObjectId == Id)
        {
            _lastRequestObjectId = -1;
            return;
        }
        TryHandleInteraction();
    }

    #endregion
}