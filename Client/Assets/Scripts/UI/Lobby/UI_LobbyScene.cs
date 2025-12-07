using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyScene : UI_Scene
{
    enum GameObjects
    {
        LobbySceneBg,
        Slot0,
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
        Slot9,
        PlayerCnt,
        ObserverCnt
    }

    public Action<int> OnSlotClick = null;

    Text[] _userNames = new Text[10];
    Button[] _slotButtons = new Button[10];
    Image[] _slotImages = new Image[10];

    Sprite _emptySprite = null;
    Sprite _playerSprite = null;
    Sprite _otherSprite = null;

    Text _playerCnt = null;
    Text _observerCnt = null;

    readonly string _emptySlot = "비어 있음";

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));

        _emptySprite = Managers.Resource.Load<Sprite>("Sprite/Img_Custom_Slot_Empty");
        _playerSprite = Managers.Resource.Load<Sprite>("Sprite/Img_Custom_Slot_Player");
        _otherSprite = Managers.Resource.Load<Sprite>("Sprite/Img_Custom_Slot_Other");

        for(int i = 0; i < 10; ++i)
        {
            int index = i;
            GameObject go = GetObject((int)GameObjects.Slot0 + i);
            Text text = go.GetComponentInChildren<Text>();
            _userNames[i] = text;

            _slotButtons[i] = go.GetComponent<Button>();
            _slotButtons[i].onClick.AddListener(() => OnSlotClick?.Invoke(index));

            _slotImages[i] = go.GetComponent<Image>(); 
        }

        GameObject playerCntGo = GetObject((int)GameObjects.PlayerCnt);
        _playerCnt = playerCntGo.GetComponentInChildren<Text>();

        GameObject observerCntGo = GetObject((int)GameObjects.ObserverCnt);
        _observerCnt = observerCntGo.GetComponentInChildren<Text>();
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {

    }

    void Update()
    {
        UpdateScale();
    }

    public void SetNickname(int idx, string nickname = null)
    {
        if(nickname == "" || nickname == null)
            _userNames[idx].text = _emptySlot;
        else
            _userNames[idx].text = nickname;          
    }

    public void SetSlotImage(int idx, Slot type)
    {
        switch (type)
        {
            case Slot.Empty:
                _slotImages[idx].sprite = _emptySprite;
                break;
            case Slot.Player:
                _slotImages[idx].sprite = _playerSprite;
                break;
            case Slot.Other:
                _slotImages[idx].sprite = _otherSprite;
                break;
        }
    }

    public void SetCount(int playerCnt, int observerCnt)
    {
        _playerCnt.text = playerCnt.ToString();
        _observerCnt.text = observerCnt.ToString();
    }

    protected override void UpdateScale()
    {
        base.UpdateScale();

        RectTransform rc = GetObject((int)GameObjects.LobbySceneBg).GetComponent<RectTransform>();
        rc.sizeDelta = new Vector2(Screen.width, Screen.height) / _scaler.scaleFactor;
    }
}
