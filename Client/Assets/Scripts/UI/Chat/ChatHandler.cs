using Google.Protobuf.Protocol;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private ScrollRect scrollRect;

    // 메시지 큐
    private static Queue<(int playerId, string message)> messageQueue = new Queue<(int, string)>();

    private void Start()
    {
        BindButton();
    }

    void BindButton()
    {
        button.onClick.AddListener(() =>
        {
            C_Chat chatPkt = new C_Chat();
            chatPkt.Message = inputField.text;

            Managers.Network.Send(chatPkt);

            inputField.text = "";
        });
    }

    public void EnqueueMessage(int playerId, string message)
    {
        lock (messageQueue)
        {
            messageQueue.Enqueue((playerId, message));
        }
    }

    void Update()
    {
        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                var msg = messageQueue.Dequeue();
                AddMessage(msg.playerId, msg.message);
            }
        }
    }

    public void AddMessage(int playerId, string message)
    {
        GameObject textPrefab = Resources.Load<GameObject>("Prefabs/UI/Chat/ChatText");
        GameObject inst = Instantiate(textPrefab, contentRect, false);

        inst.GetComponent<TMP_Text>().text = message;

        //$"[{playerId}] {message}"
    }
}
