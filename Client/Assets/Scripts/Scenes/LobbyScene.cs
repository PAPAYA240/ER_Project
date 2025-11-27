using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.UI;


public class LobbyScene : BaseScene
{
    UI_LobbyScene _lobbySceneUI = null;

    GameObject _nicknamePopUp = null;
    UI_NicknamePopUp _nicknamePopUpUI = null;

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Lobby;
        GameObject lobbySceneUI = Managers.Resource.Instantiate("UI/LobbyScene/LobbySceneUI");
        _lobbySceneUI = lobbySceneUI.GetComponent<UI_LobbyScene>();
        _lobbySceneUI.OnSlotClick -= OnSlotClick;
        _lobbySceneUI.OnSlotClick += OnSlotClick;

        _nicknamePopUp = Managers.Resource.Instantiate("UI/LobbyScene/NicknamePopUp");
        _nicknamePopUpUI = _nicknamePopUp.GetComponent<UI_NicknamePopUp>();
        if (_nicknamePopUpUI == null)
        {
            Debug.Log("NicknamePopUpUI Null");
            return;
        }

        _nicknamePopUpUI.OnSkip -= OnSkipBtnClick;
        _nicknamePopUpUI.OnSkip += OnSkipBtnClick;
        _nicknamePopUpUI.OnConfirm -= OnConfirmBtnClick;
        _nicknamePopUpUI.OnConfirm += OnConfirmBtnClick;
    }

    void Start()
    {
        
    }

    void OnSkipBtnClick()
    {
        SendEnterLobbyPkt("");
        _nicknamePopUp.SetActive(false);
    }

    void OnConfirmBtnClick(string nickname)
    {
        if (nickname == null || nickname.Length == 0)
            return;

        SendEnterLobbyPkt(nickname);
        _nicknamePopUp.SetActive(false);
    }

    void OnSlotClick(int idx)
    {
        C_SlotClick slotClickPkt = new C_SlotClick();
        slotClickPkt.SlotIdx = idx;
        Managers.Network.Send(slotClickPkt);
    }

    void SendEnterLobbyPkt(string nickname)
    {
        C_EnterLobby enterLobbyPkt = new C_EnterLobby();
        enterLobbyPkt.Nickname = nickname;
        Managers.Network.Send(enterLobbyPkt);
    }

    public override void Clear()
    {
        Debug.Log("LobbyScene Clear");
    }
}

