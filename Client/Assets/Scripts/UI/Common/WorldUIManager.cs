using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WorldUIManager 
{
    public static WorldUIManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas _worldUICanvas;        // WorldUICanvas
    [SerializeField] private GameObject _emoticonBubble;    // EmoticonBubble 프리팹

    // objectId 기준으로 이모티콘 UI를 관리
    private readonly Dictionary<int, UI_Emoticon> _emoticonDict = new();
    private readonly HashSet<int> _hiddenOwners = new();

    public void Init()
    {
        // 이모티콘 버블 프리팹 로드
        _emoticonBubble = Managers.Resource.Load<GameObject>("Prefabs/UI/Common/EmoticonUI");
        //_emoticonBubble = Managers.Resource.Load<GameObject>("UI/Common/EmoticonBubble");
        if (_emoticonBubble == null)
            Debug.LogError("[WorldUIManager] EmoticonBubble 프리팹을 찾을 수 없습니다.");
    }

    /// <summary>필요할 때 EmoticonCanvas를 찾아서 캐싱</summary>
    private bool EnsureCanvas()
    {
        if (_worldUICanvas != null)
            return true;

        // 1) 씬에 미리 배치해 둔 WorldUICanvas 찾기
        GameObject canvasGo = GameObject.Find("WorldUICanvas");
        if (canvasGo == null)
        {
            Debug.LogError("[WorldUIManager] WorldUICanvas 를 씬에서 찾을 수 없습니다.");
            return false;
        }

        _worldUICanvas = canvasGo.GetComponent<Canvas>();
        return _worldUICanvas != null;
    }

    /// <summary>
    /// 플레이어(또는 캐릭터)가 생성될 때 호출.
    /// 이 캐릭터의 UI를 _worldUICanvas 밑에 만든다.
    /// </summary>
    public void RegisterEmoticonUI(int objectId, Transform target)
    {
        if (_emoticonBubble == null)
            return;

        if (!EnsureCanvas())
            return;

        if (_emoticonDict.ContainsKey(objectId))
            return;

       // GameObject go = Instantiate(_emoticonBubble, _worldUICanvas.transform);
        GameObject go = Managers.Resource.Instantiate("UI/Common/EmoticonUI", _worldUICanvas.transform);
        var follower = go.GetComponentInChildren<UI_Follower>(true);
        var ui = go.GetComponentInChildren<UI_Emoticon>(true);

        if (follower == null || ui == null)
        {
            Debug.LogError("[WorldUIManager] EmoticonBubble에 UI_Follower 또는 UI_Emoticon이 없습니다.");
            //Destroy(go);
            return;
        }

        follower.SetTarget(target);

        bool hidden = _hiddenOwners.Contains(objectId);
        ui.SetVisible(!hidden);

        _emoticonDict[objectId] = ui;
    }

    /// <summary>
    /// 캐릭터가 사라질 때 이모티콘 UI 정리.
    /// </summary>
    public void UnregisterEmoticonUI(int objectId)
    {
        if (_emoticonDict.TryGetValue(objectId, out var ui))
        {
            //if (ui != null)
            //    Destroy(ui.gameObject);
            _emoticonDict.Remove(objectId);
            _hiddenOwners.Remove(objectId);
        }
    }

    /// <summary>
    /// 해당 objectId 캐릭터의 이모티콘을 재생.
    /// </summary>
    public void PlayEmoticon(int objectId, int emoticonIndex)
    {
        if (_emoticonDict.TryGetValue(objectId, out var ui) && ui != null)
        {
            ui.Play(emoticonIndex);

            PlayerController pc = Managers.Object.FindById(objectId).GetComponentInChildren<PlayerController>();
            Managers.Object.MyPlayer.Sound.GetEffect3D("Emoticon", pc.transform.position);
        }
    }

    public void HideEmoticon(int objectId)
    {
        if (_emoticonDict.TryGetValue(objectId, out var ui) && ui != null)
        {
            ui.Hide();
        }
    }

    public void SetEmoticonVisibility(int ownerId, bool visible)
    {
        if (visible)
            _hiddenOwners.Remove(ownerId);
        else
            _hiddenOwners.Add(ownerId);

        if (_emoticonDict.TryGetValue(ownerId, out var ui) && ui != null)
        {
            ui.SetVisible(visible);
        }
    }
}
