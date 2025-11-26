using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.GraphicsBuffer;

public class WardController : BaseController
{
    public int TeamIndex;

    private float elapsed = 0;
    public float _lifeTime = 15f; // 와드의 수명
    public float _destructionAnimationDuration = 3f; // 파괴 애니메이션 재생 시간

    private FogOfWarVision _vision;

    private int _layer1Team;
    private int _layer2Team;


    [Header("UI Canvas 참조")]
    public Canvas mainScreenCanvas; // <<--- 씬에 있는 메인 UI Canvas를 Inspector에서 연결해주세요!
                                    //       Render Mode가 Screen Space-Camera 또는 Overlay여야 합니다.

    [Header("Life Bar UI 프리팹")]
    public GameObject wardLifeBarUIPrefab; // <<--- Canvas가 포함되지 않은 WardLifeBarUI 프리팹을 연결해주세요!

    [Header("Life Bar UI 위치 오프셋")]
    public Vector3 uiOffset = new Vector3(0, 2f, 0); // 와드 머리 위 2m 지점에 Life Bar 표시

    private GameObject _lifeBarInstance; // Instantiate로 생성된 LifeBar UI GameObject 인스턴스
    private UI_WardLifeBar _lifeBarController; // Life Bar UI를 제어하는 스크립트

    private void Awake()
    {

        if (mainScreenCanvas == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("MainCanvas");

            if(go != null)
            {
                mainScreenCanvas = go.GetComponent<Canvas>();
            }
        }
    }

    void Start()
    {
        Init();
        StartCoroutine(LifecycleRoutine());    
    }

    protected override void Init()
    {
        base.Init();

        // vision process
        _vision = gameObject.AddComponent<FogOfWarVision>();
        _vision.ViewDistance = 13f;

        string layer1Name = $"FogTeam1";
        string layer2Name = $"FogTeam2";
        _layer1Team = LayerMask.NameToLayer(layer1Name);
        _layer2Team = LayerMask.NameToLayer(layer2Name);

        if(TeamIndex == 1)
            gameObject.layer = _layer1Team;
        else
            gameObject.layer = _layer2Team;

        if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team)
        {
            Managers.Object.MyPlayer.View.WardIds.Add(Id);
            //Debug.Log("")
        }

        if (mainScreenCanvas != null /*&& wardLifeBarUIPrefab != null*/)
        {
            InitializeLifeBarUI();
        }
        else
        {
            Debug.LogError("Life Bar UI를 초기화할 수 없습니다. 필요한 참조가 없습니다.");
        }

        _lifeBarController.SetMaxValue(_lifeTime);
    }

    IEnumerator LifecycleRoutine()
    {
        while(true)
        {
            elapsed += Time.deltaTime;
            _lifeBarController.SetValue(_lifeTime - elapsed);
            if (elapsed > _lifeTime)
                break;

            yield return null;
        }


        //yield return new WaitForSeconds(_lifeTime);

        if (_animator != null)
        {
            _vision.ViewDistance = 0.01f;
            _animator.SetTrigger("dead");
            if (_lifeBarInstance != null)
            {
                Destroy(_lifeBarInstance);
                _lifeBarInstance = null;
            }
            if (TeamIndex == Managers.Object.MyPlayer.ObjInfo.Player.Team)
            {
                Managers.Object.MyPlayer.View.WardIds.Remove(Id);
            }
            yield return new WaitForSeconds(_destructionAnimationDuration);
        }

        Managers.Object.Remove(Id);
    }

    private void InitializeLifeBarUI()
    {
        // 1. Life Bar UI 프리팹을 메인 UI Canvas의 자식으로 인스턴스화합니다.
        //    이렇게 해야 UI 시스템의 올바른 계층 구조 안에 들어가게 됩니다.
        //_lifeBarInstance = Instantiate(wardLifeBarUIPrefab, mainScreenCanvas.transform);
        _lifeBarInstance = Managers.Resource.Instantiate("UI/SubItem/WardLifeBarCanvas", /*gameObject.transform*/ mainScreenCanvas.transform);

        if (_lifeBarInstance == null)
        {
            Debug.LogError("WardLifeBarUI 프리팹 Instantiate 실패!");
            return;
        }

        // 2. Life Bar UI를 제어하는 스크립트를 가져옵니다.
        //    _lifeBarInstance GameObject 자체에 UI_BarNonText가 붙어있거나 자식에 붙어있을 수 있습니다.
        _lifeBarController = _lifeBarInstance.GetComponent<UI_WardLifeBar>();
        if (_lifeBarController == null) // 만약 루트에 없다면 자식에서 찾아봅니다.
        {
            _lifeBarController = _lifeBarInstance.GetComponentInChildren<UI_WardLifeBar>();
        }

        if (_lifeBarController == null)
        {
            Debug.LogError("UI_BarNonText 컴포넌트를 Life Bar UI 프리팹에서 찾을 수 없습니다!");
            Destroy(_lifeBarInstance); // UI 인스턴스 정리
            return;
        }

        // 스케일은 Canvas Scaler에 의해 조절되지만, 만약 UI 자체 스케일을 줄여야 한다면 여기서 조절
        // _lifeBarInstance.transform.localScale = Vector3.one; // 일반적으로 1로 유지
    }

    // 카메라 이동 후에도 UI가 오브젝트를 따라다니도록 LateUpdate에서 위치를 갱신합니다.
    void LateUpdate()
    {
        if (_lifeBarInstance == null || mainScreenCanvas == null || Camera.main == null) return;

        // 1. 와드의 월드 포지션 + 오프셋을 계산합니다.
        Vector3 wardWorldPos = transform.position + uiOffset;

        // 2. 월드 포지션을 스크린(픽셀) 포지션으로 변환합니다.
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(wardWorldPos);
        _lifeBarInstance.transform.position = screenPoint;
    }

    public void SetWardLifeBarActive(bool isActive)
    {
        if(null == _lifeBarInstance) return;
        _lifeBarInstance.SetActive(isActive);
    }
}
