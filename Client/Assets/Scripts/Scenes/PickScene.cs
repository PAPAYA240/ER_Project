using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickScene : BaseScene
{
    UI_PickSceneUI _pickSceneUI = null;

    public int PickIdx {  get; set; }

    CharacterType _characterType = CharacterType.CharacterNone;
    string _nickname = "";
    public string NickName 
    { 
        get { return _nickname; } 
        set { _nickname = value; ChangeNickname(_nickname, PickIdx); } 
    }
    //���� ���� �Ѱܾ� �Ǵ� ���� �� ���� ������ �־�� �� ����
    // �� ������ ���° �ε������� ( 0 ~ 7)
    // ĳ����
    // �г���
    // ����
    // Ư��

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Login;
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PickSceneUI");
        _pickSceneUI = go.GetComponent<UI_PickSceneUI>();
        _pickSceneUI.OnClickedPickButton += ClickedCharPickButton;
    }

    private void Start()
    {
        
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    Managers.Scene.LoadScene(Define.Scene.Game);
        //}
    }

    public override void Clear()
    {
        Debug.Log("PickScene Clear");
    }

    private void ClickedCharPickButton(string charName)
    {
        if (Enum.TryParse(charName, out _characterType))
        {
            C_Character pickPacket = new C_Character();
            pickPacket.CharType = _characterType;
            pickPacket.PickIdx = PickIdx;
            Managers.Network.Send(pickPacket);

            ChangePickImage(_characterType, PickIdx);
        }
    }

    public void ChangePickImage(CharacterType charType, int idx)
    {
        if (_pickSceneUI == null)
            return;

        _pickSceneUI.ChangePickImage(charType, idx);
    }

    private void ChangeNickname(string nickname, int idx)
    {
        if (_pickSceneUI == null)
            return;

        _pickSceneUI.ChangeNickname(nickname, idx);
    }

    public void Spawn(string nickname, int idx, CharacterType type)
    {
        ChangePickImage(type, idx);
        ChangeNickname(nickname, idx);
    }
}
