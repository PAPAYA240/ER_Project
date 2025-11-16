using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ChatHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private ScrollRect scrollRect;

    private CanvasGroup cg;

    // 메시지 큐
    private static Queue<(int playerId, string message)> messageQueue = new Queue<(int, string)>();

    private void Awake()
    {
        cg = inputField.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = inputField.gameObject.AddComponent<CanvasGroup>();

        HideInputField(); // 시작 시 숨김
    }

    public void EnqueueMessage(int playerId, string message)
    {
        messageQueue.Enqueue((playerId, message));
    }

    void Update()
    {
        while (messageQueue.Count > 0)
        {
            var msg = messageQueue.Dequeue();
            AddMessage(msg.playerId, msg.message);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // InputField가 보이지 않는 상태일 때
            if (!cg.interactable)
                ShowInputField();
            else
            {
                SendChat();
                HideInputField();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
            HideInputField();
    }

    private void ShowInputField()
    {
        cg.alpha = 0.8f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
        
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void HideInputField()
    {
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    private void SendChat()
    {
        if (inputField.text.Length <= 0) 
            return;

        C_Chat chatPkt = new C_Chat();
        chatPkt.Message = inputField.text;
        Managers.Network.Send(chatPkt);

        inputField.text = "";
    }

    private void AddMessage(int playerId, string message)
    {
        GameObject textPrefab = Resources.Load<GameObject>("Prefabs/UI/Chat/ChatText");
        GameObject inst = Instantiate(textPrefab, contentRect, false);

        inst.GetComponent<TMP_Text>().text = $"{playerId} : {message}";
    }
}
