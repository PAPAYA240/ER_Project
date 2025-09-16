using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SelectEvent : MonoBehaviour
{
    int _pickIdx = -1;
    List<Image> _images = new List<Image>();
    Dictionary<CharacterType, Sprite> _sprites = new Dictionary<CharacterType, Sprite>();

    public void Start()
    {
        LoadPickSprites();
    }

    public void SetPickIdx(int idx)
    {
        _pickIdx = idx;

        for (int i = 0; i < 4; ++i)
        {
            GameObject pick = GameObject.Find($"{i + 1}P");
            if (pick == null) return;

            _images.Add(pick.GetComponent<Image>());

            if (i == idx)
            {
                GameObject bg = Managers.Resource.Instantiate("UI/PickBG");
                bg.transform.position = pick.transform.position;
                bg.transform.SetParent(pick.transform.parent);
                pick.transform.SetParent(bg.transform);
            }
        }
    }

    public void OnCharacterClick()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        if (clickedButton == null)
            return;

        string btnName = clickedButton.name;

        if (btnName.StartsWith("Button"))
            btnName = btnName.Substring(6);
        else
            return;

        if (Enum.TryParse(btnName, out CharacterType type))
        {
            C_Character pickPacket = new C_Character();
            pickPacket.CharType = type;
            pickPacket.PickIdx = _pickIdx;
            Managers.Network.Send(pickPacket);

            ChangePickImage(type, _pickIdx);
        }
    }

    public void OnButtonStartClick()
    {
        Managers.Scene.LoadScene(Define.Scene.Game);
    }

    public void ChangePickImage(CharacterType charType, int idx)
    {
        Sprite sprite = _sprites[charType];
        _images[idx].sprite = sprite;
        _images[idx].preserveAspect = false;
        _images[idx].type = Image.Type.Simple;
    }

    void LoadPickSprites()
    {
        Sprite sprite = null;
        _sprites.Add(CharacterType.CharacterNone, sprite);

        sprite = Resources.Load<Sprite>("Textures/CharFull_Rozzi_S000");
        _sprites.Add(CharacterType.Rozzi, sprite);

        sprite = Resources.Load<Sprite>("Textures/CharFull_Yuki_S005");
        _sprites.Add(CharacterType.Yuki, sprite);

        sprite = Resources.Load<Sprite>("Textures/CharFull_Theodore_S000");
        _sprites.Add(CharacterType.Theodore, sprite);
    }
}
