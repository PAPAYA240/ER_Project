using Google.Protobuf.Protocol;
using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.GraphicsBuffer;

public class WardController : BaseController
{
    public int TeamIndex;

    private float elapsed = 0;
    public float _lifeTime = 15f; // �͵��� ����
    public float _destructionAnimationDuration = 3f; // �ı� �ִϸ��̼� ��� �ð�

    private FogOfWarVision _vision;

    private int _layer1Team;
    private int _layer2Team;


    [Header("UI Canvas ����")]
    public Canvas mainScreenCanvas; // <<--- ���� �ִ� ���� UI Canvas�� Inspector���� �������ּ���!
                                    //       Render Mode�� Screen Space-Camera �Ǵ� Overlay���� �մϴ�.

    [Header("Life Bar UI ������")]
    public GameObject wardLifeBarUIPrefab; // <<--- Canvas�� ���Ե��� ���� WardLifeBarUI �������� �������ּ���!

    [Header("Life Bar UI ��ġ ������")]
    public Vector3 uiOffset = new Vector3(0, 2f, 0); // �͵� �Ӹ� �� 2m ������ Life Bar ǥ��

    private GameObject _lifeBarInstance; // Instantiate�� ������ LifeBar UI GameObject �ν��Ͻ�
    private UI_WardLifeBar _lifeBarController; // Life Bar UI�� �����ϴ� ��ũ��Ʈ

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
            //Debug.LogError("Life Bar UI�� �ʱ�ȭ�� �� �����ϴ�. �ʿ��� ������ �����ϴ�.");
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
        // 1. Life Bar UI �������� ���� UI Canvas�� �ڽ����� �ν��Ͻ�ȭ�մϴ�.
        //    �̷��� �ؾ� UI �ý����� �ùٸ� ���� ���� �ȿ� ���� �˴ϴ�.
        //_lifeBarInstance = Instantiate(wardLifeBarUIPrefab, mainScreenCanvas.transform);
        _lifeBarInstance = Managers.Resource.Instantiate("UI/SubItem/WardLifeBarCanvas", /*gameObject.transform*/ mainScreenCanvas.transform);

        if (_lifeBarInstance == null)
        {
            //Debug.LogError("WardLifeBarUI ������ Instantiate ����!");
            return;
        }

        // 2. Life Bar UI�� �����ϴ� ��ũ��Ʈ�� �����ɴϴ�.
        //    _lifeBarInstance GameObject ��ü�� UI_BarNonText�� �پ��ְų� �ڽĿ� �پ����� �� �ֽ��ϴ�.
        _lifeBarController = _lifeBarInstance.GetComponent<UI_WardLifeBar>();
        if (_lifeBarController == null) // ���� ��Ʈ�� ���ٸ� �ڽĿ��� ã�ƺ��ϴ�.
        {
            _lifeBarController = _lifeBarInstance.GetComponentInChildren<UI_WardLifeBar>();
        }

        if (_lifeBarController == null)
        {
            //Debug.LogError("UI_BarNonText ������Ʈ�� Life Bar UI �����տ��� ã�� �� �����ϴ�!");
            Destroy(_lifeBarInstance); // UI �ν��Ͻ� ����
            return;
        }

        // �������� Canvas Scaler�� ���� ����������, ���� UI ��ü �������� �ٿ��� �Ѵٸ� ���⼭ ����
        // _lifeBarInstance.transform.localScale = Vector3.one; // �Ϲ������� 1�� ����
    }

    // ī�޶� �̵� �Ŀ��� UI�� ������Ʈ�� ����ٴϵ��� LateUpdate���� ��ġ�� �����մϴ�.
    void LateUpdate()
    {
        if (_lifeBarInstance == null || mainScreenCanvas == null || Camera.main == null) return;

        // 1. �͵��� ���� ������ + �������� ����մϴ�.
        Vector3 wardWorldPos = transform.position + uiOffset;

        // 2. ���� �������� ��ũ��(�ȼ�) ���������� ��ȯ�մϴ�.
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(wardWorldPos);
        _lifeBarInstance.transform.position = screenPoint;
    }

    public void SetWardLifeBarActive(bool isActive)
    {
        if(null == _lifeBarInstance) return;
        _lifeBarInstance.SetActive(isActive);
    }
}
