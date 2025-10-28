using Google.Protobuf.Protocol;
using UnityEngine;

public class EnvController : BaseController
{
    [SerializeField] public EnvType _envType;

    protected Animator animator;
    protected bool _isActive = false;

    protected override void Init()
    {
        base.Init();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }
    public void InitializeFromServer(EnvInfo data)
    {
    }
    void Update()
    {
    }

    protected void OnTriggerEnter(Collider other)
    {
        //if (_isActive == false)
        //    return;

        //if (other.gameObject.layer == LayerMask.NameToLayer("MyPlayer"))
        //    RequestCollect();
    }

    void RequestCollect()
    {
        // 서버에 수집 요청 전송
        C_EnvRequest request = new C_EnvRequest
        {
            ObjectId = Id,
            EnvType = _envType,
        };
        Managers.Network.Send(request);
    }

    public void OnInteractionAuthorized()
    {
        TryHandleInteraction();
    }

    protected virtual void TryHandleInteraction() { }
}
