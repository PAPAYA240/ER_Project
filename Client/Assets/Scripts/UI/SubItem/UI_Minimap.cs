using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UI_MinimapCharIcon;

public class UI_Minimap : UI_Base
{
    // 월드 오브젝트와 미니맵의 연동
    // 플레이어들(8명) 위치를 받아와서 갱신. > 내가 어떤 팀인지 우리팀 누군지 알아야 됨.
    // 각 팀 진영은 항상 보이게 처리. > 포그 메시를 각 진영 것을 그려놓는다.
    // 얘는 관리하는 애는 아니고 보여주는, 갱신하는 친구다.
    // 그래도 캐릭터 아이콘 정도는 새로 생성해줘야할 듯
    // 방에 들어가면 그 방 안에 있는 플레이어들 정보를 토대로 아이콘을 생성
    // 방에 누가 들어오면 모든 플레이어에게 들어왔다고 알림 > 그러면 새로 아이콘을 생성해서 추가.
    // 아이콘을 생성하는 코드가 필요하고, 월드 투 미니맵을 어떻게 할지도.

    public enum Images
    {
        TurbineIconLeft,
        TurbineIconCenter,
        TurbineIconRight,
        HealPackLL,
        HealPackLC,
        HealPackLR,
        HealPackRR,
        HealPackRC,
        HealPackRL,
        HealPackCR,
        HealPackCL,
        Fog
    }

    enum GameObjects
    {
        CharIcon_0,
        CharIcon_1,
        CharIcon_2,
        CharIcon_3,
        CharIcon_4,
        CharIcon_5,
        CharIcon_6,
        CharIcon_7
    }

    private int _charNum = 0;


    private Sprite _healpackUnrespawned;
    private Sprite _healpackRespawned;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        GetObject((int)GameObjects.CharIcon_0).SetActive(false);
        GetObject((int)GameObjects.CharIcon_1).SetActive(false);
        GetObject((int)GameObjects.CharIcon_2).SetActive(false);
        GetObject((int)GameObjects.CharIcon_3).SetActive(false);
        GetObject((int)GameObjects.CharIcon_4).SetActive(false);
        GetObject((int)GameObjects.CharIcon_5).SetActive(false);
        GetObject((int)GameObjects.CharIcon_6).SetActive(false);
        GetObject((int)GameObjects.CharIcon_7).SetActive(false);

        _healpackUnrespawned = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_SupportPackPoint");
        _healpackRespawned = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_SupportPackSpawned");
    }
    private void Awake()
    {
        Init();
    }

    void Start()
    {
        StartCoroutine(CoInitFog());

        // 기존 코드

        //GetImage((int)Images.Fog).material

        //Image img = GetImage((int)Images.Fog);
        //GameObject cam = GameObject.Find("FogCamera");
        //if (null == cam)
        //{

        //    Debug.Log("@Cam == null");
        //    return; 
        //}

        //FogCameraController fcc = cam.GetComponent<FogCameraController>();
        //if (null == fcc)
        //{

        //    Debug.Log("@fcc == null");
        //    return;
        //}

        //Texture texture = fcc.FogTexture;
        //Material newMat = Material.Instantiate(img.material);

        //if(null != newMat)
        //{
        //    Debug.Log("@Success to instantiate material");
        //    img.material = newMat;
        //    newMat.SetTexture("_VisionMask", texture);
        //}
    }

    IEnumerator CoInitFog()
    {
        Image img = GetImage((int)Images.Fog);

        FogCameraController fcc = null;

        while(fcc == null)
        {
            GameObject cam = GameObject.Find("FogCamera");
            if(cam != null)
                fcc = cam.GetComponent<FogCameraController>();

            yield return null;
        }

        Material newMat = Material.Instantiate(img.material);
        img.material = newMat;
        newMat.SetTexture("_VisionMask", fcc.FogTexture);
    }

    void Update()
    {
        
    }

    public void ActivatePlayerIcon(IconType iconType, PlayerController pc)
    {
        string objName = "CharIcon_" + _charNum;
        _charNum++;
        GameObjects obj = Enum.Parse<GameObjects>(objName);

        GameObject go = GetObject((int)obj);

        go.SetActive(true);

        UI_MinimapCharIcon ui_MinimapCharIcon = go.GetComponent<UI_MinimapCharIcon>();
        ui_MinimapCharIcon.Type = iconType;
        ui_MinimapCharIcon.SetCharIcon(pc.ObjInfo.Player.CharType);
        ui_MinimapCharIcon.Target = pc;
    }

    public void SetTurbineImage(Images img, Sprite sprite)
    {
        GetImage((int)img).sprite = sprite;
    }

    public void ChangeHealPackImage(Images img, bool isRespawned)
    {
        if(isRespawned)
        {
            GetImage((int)img).sprite = _healpackRespawned;
        }
        else
        {
            GetImage((int)img).sprite = _healpackUnrespawned;
        }
    }
}
