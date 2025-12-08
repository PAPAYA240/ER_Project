using Google.Protobuf.Protocol;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;

public class ChatHandler : MonoBehaviour
{
    public static bool IsChatting { get; private set; }

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text placeholderText;

    private CanvasGroup cg;
    private PlayerController player;
    private ChatType chatType;

    // 메시지 큐
    private static Queue<(string playerName, string message, ChatType chatType, CharacterType charType)> messageQueue = new Queue<(string, string, ChatType, CharacterType)>();

    private void Awake()
    {
        cg = inputField.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = inputField.gameObject.AddComponent<CanvasGroup>();

        HideInputField(); // 시작 시 숨김
    }

    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
    }

    public void EnqueueMessage(string playerName, string message, ChatType chatType, CharacterType charType)
    {
        messageQueue.Enqueue((playerName, message, chatType, charType));
    }

    void Update()
    {
        while (messageQueue.Count > 0)
        {
            var msg = messageQueue.Dequeue();
            AddMessage(msg.playerName, msg.message, msg.chatType, msg.charType);
        }

        // 시프트 + 엔터 (팀챗)
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && Input.GetKey(KeyCode.LeftShift))
        {
            ShowInputField(isTeam: false);
            return;
        }

        // 엔터 (전체챗)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!cg.interactable)
                ShowInputField(isTeam: true);
            else
            {
                SendChat();
                HideInputField();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            HideInputField();
    }

    private void ShowInputField(bool isTeam)
    {
        IsChatting = true;

        cg.alpha = 0.8f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        inputField.text = "";

        chatType = isTeam ? ChatType.Team : ChatType.All;

        inputField.ActivateInputField();
        inputField.Select();

        inputField.caretPosition = 0;
    }

    private void HideInputField()
    {
        IsChatting = false;

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    private void SendChat()
    {
        string msg = inputField.text.Trim();
        if (msg.Length <= 0)
            return;

        C_Chat chatPkt = new C_Chat();

        chatPkt.ChatType = chatType;
        chatPkt.Message = msg;

        Managers.Network.Send(chatPkt);

        inputField.text = "";
    }

    private void AddMessage(string playerName, string message, ChatType type, CharacterType charType)
    {
        GameObject textPrefab = Resources.Load<GameObject>("Prefabs/UI/Chat/ChatText");
        GameObject inst = Instantiate(textPrefab, contentRect, false);

        string prefix = type == ChatType.Team ? "<color=#52D1FF>[팀]</color>" : "<color=#FFD400>[전체]</color>";

        inst.GetComponent<TMP_Text>().text = $"{prefix} <color=#01DCE3>{playerName}({CharacterName(charType)})</color> : {message}";
    }

    private string CharacterName(CharacterType charType)
    {
        string charName = "";

        switch (charType)
        {
            case CharacterType.Abigail:
                charName = "아비게일";
                break;
            case CharacterType.Rozzi:
                charName = "로지";
                break;
            case CharacterType.Yuki:
                charName = "유키";
                break;
            case CharacterType.Hyunwoo:
                charName = "현우";
                break;
            case CharacterType.Theodore:
                charName = "테오도르";
                break;
        }

        return charName;
    }
}
