using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Chat;
using ExitGames.Client.Photon;
using Photon.Realtime;

public class ChatManager : MonoBehaviourPun, IChatClientListener
{

    public static ChatManager Instance;

    // 채팅을 총괄하는 객체
    ChatClient chatClient;

    // Input Field
    public TMP_InputField inputChat;

    // 채팅 채널
    string currChannel = "메타";

    public Button chatButton;

    // ChatItem Prefab
    public GameObject chatItemFactory;

    // Content 의 Transform
    public RectTransform trContent;

    // ChatView 의 Transform
    public RectTransform trChatView;

    float scrollSpeed = 0.6f;

    Coroutine chatCoroutine;

    Coroutine speechCoroutine;
    Coroutine otherSpeechCoroutine;

    public ScrollRect scrollRect;

    

    public Transform playerPos; // Photon 사용 시 변경 예정

    public GameObject playerObject;

    // 채팅이 추가되기 전의 Content 의 H(높이) 값을 가지고 있는 변수
    float prevContentH;

    public bool isStartChat = false;

    public GameObject speechBubble; // 개인 말풍선
    public GameObject otherSpeechBubble; // 다른 사람에게 보이는 말풍선

    public ChatSpeechBubble speech;
    public ChatOtherSpeechBubble otherSpeech;



    public PhotonView pv;

    public PhotonView playerPv;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => pv != null);

        speechBubble.SetActive(false);

        currChannel = PhotonNetwork.CurrentRoom.Name;

        Connect();


        inputChat.onValueChanged.AddListener(OnValueChanged);

        // 채팅내용을 작성하고 엔터를 쳤을때 호출되는 함수 등록
        inputChat.onSubmit.AddListener(OnSubmit);

        inputChat.characterLimit = 50;

        trChatView.sizeDelta = new Vector2(550, 0);

        yield return new WaitUntil(() => PlayerInfo.instance.player != null);

        otherSpeechBubble = PlayerInfo.instance.player.transform.Find("Canvas_OtherSpeehBubble").gameObject;
        otherSpeech = otherSpeechBubble.GetComponent<ChatOtherSpeechBubble>();

        //otherSpeechBubble.SetActive(false);

        playerPv = PlayerInfo.instance.player.GetComponent<PhotonView>();
        playerObject = PlayerInfo.instance.player;
        playerPos = playerObject.transform.GetChild(2).transform;

    }



    void Update()
    {

        if (pv == null) return;


        // 채팅서버에서 오는 응답을 수신하기 위해서 계속 호출 해줘야 한다.        
        if (chatClient != null)
        {
            chatClient.Service();
        }

        HandleChatInput();

        if (isStartChat)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0)
            {
                // ScrollRect의 verticalNormalizedPosition을 업데이트
                scrollRect.verticalNormalizedPosition += scroll * scrollSpeed;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition); // 범위 제한
            }
        }
    }

    void HandleChatInput()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!isStartChat)
            {
                StartChat();
            }
            else
            {
                if (string.IsNullOrEmpty(inputChat.text))
                {
                    EndChat();
                    if (chatCoroutine == null)
                    {
                        chatCoroutine = StartCoroutine(ISetChatViewSize());
                    }
                }
                else
                {
                    //SubmitChat();
                    inputChat.text = "";
                    EndChat();
                }
            }
        }
    }

    void StartChat()
    {
        isStartChat = true;
        inputChat.interactable = true;
        inputChat.resetOnDeActivation = true;
        inputChat.ActivateInputField();
        SetChatViewSize();

        PlayerMove playerMove = PlayerInfo.instance.player.GetComponentInChildren<PlayerMove>();
        playerMove.isMove = false;
    }

    void EndChat()
    {
        isStartChat = false;
        inputChat.resetOnDeActivation = false;
        inputChat.DeactivateInputField();
        inputChat.interactable = false;

        PlayerMove playerMove = PlayerInfo.instance.player.GetComponentInChildren<PlayerMove>();
        playerMove.isMove = true;
    }

    void SubmitChat()
    {
        string message = inputChat.text;
        if (!string.IsNullOrEmpty(message))
        {
            chatClient.PublishMessage(currChannel, message);
            CreateChatItem(PhotonNetwork.NickName, message);
            inputChat.text = "";
        }

        EndChat();
    }

    void OnValueChanged(string s)
    {
        if (s.Length != 0)
        {
            SetChatViewSize();
        }
    }

    void SetChatViewSize()
    {
        // ISetChatViewSize가 실행 중일 때는 중지
        if (chatCoroutine != null)
        {
            StopCoroutine(chatCoroutine);
            chatCoroutine = StartCoroutine(ISetChatViewSize());
        }

        trChatView.sizeDelta = new Vector2(550, 350);


        // ScrollRect를 활성화하여 스크롤 가능하게 함
        scrollRect.enabled = true;
    }


    IEnumerator ISetChatViewSize()
    {
        yield return new WaitForSeconds(2);
        trChatView.sizeDelta = new Vector2(550, 0);


        // ScrollRect를 비활성화하여 키보드 입력을 통한 스크롤 방지
        scrollRect.enabled = false;

        AutoScrollBottom();

        // 코루틴이 완료되었으므로 chatCoroutine을 null로 설정
        chatCoroutine = null;
    }




    void Connect()
    {
        // 포톤 설정을 가져오자
        AppSettings photonSettings = PhotonNetwork.PhotonServerSettings.AppSettings;

        // 위 설정을 가지고 ChatAppSettings 셋팅
        ChatAppSettings chatAppSettings = new ChatAppSettings();
        chatAppSettings.AppIdChat = photonSettings.AppIdChat;
        chatAppSettings.AppVersion = photonSettings.AppVersion;
        chatAppSettings.FixedRegion = photonSettings.FixedRegion;
        chatAppSettings.NetworkLogging = photonSettings.NetworkLogging;
        chatAppSettings.Protocol = photonSettings.Protocol;
        chatAppSettings.EnableProtocolFallback = photonSettings.EnableProtocolFallback;
        chatAppSettings.Server = photonSettings.Server;
        chatAppSettings.Port = (ushort)photonSettings.Port;
        chatAppSettings.ProxyServer = photonSettings.ProxyServer;

        // ChatClinet 만들자.
        chatClient = new ChatClient(this);
        // 닉네임
        chatClient.AuthValues = new Photon.Chat.AuthenticationValues(PhotonNetwork.NickName);
        // 연결 시도
        chatClient.ConnectUsingSettings(chatAppSettings);
    }

    void OnSubmit(string s)
    {
        if (!isStartChat) return;
        // 만약에 s 의 길이가 0이면 함수를 나가자.
        if (s.Length == 0)
        {
            return;
        }

        // 귓속말인 판단
        // /w 아이디 메시지 (/w 김현진 안녕하세요 반갑습니다.)
        string[] splitChat = s.Split(" ", 3);

        if (splitChat[0] == "/w")
        {
            // 귓속말을 보내자
            // splitchat[1] : 아이디 , splitChat[2] : 내용
            chatClient.SendPrivateMessage(splitChat[1], splitChat[2]);
        }
        else
        {
            // 채팅을 보내자.
            chatClient.PublishMessage(currChannel, s);
        }

        trChatView.sizeDelta = new Vector2(550, 220);

        // 강제로 InputChat 을 활성화 하자.
        //inputChat.ActivateInputField();

        // 채팅 입력란 초기화
        inputChat.text = "";

        isStartChat = false;
        inputChat.DeactivateInputField();

        // chatCoroutine이 null이면 실행 중이 아니므로 새로운 코루틴 시작
        if (chatCoroutine == null)
        {
            chatCoroutine = StartCoroutine(ISetChatViewSize());
        }
    }

    void OwnPlayer(string sender ,object message)
    {
        if (sender == PhotonNetwork.LocalPlayer.NickName)
        {
            // 본인 클라이언트에서만 자신의 speechBubble 활성화
            speechBubble.SetActive(true);

            if (speechCoroutine != null)
            {
                StopCoroutine(speechCoroutine);
            }
            speechCoroutine = StartCoroutine(speech.PlayOwn(message.ToString(), speechCoroutine));

            if (otherSpeechCoroutine != null)
            {
                StopCoroutine(otherSpeechCoroutine);
            }
            otherSpeechCoroutine = StartCoroutine(otherSpeech.PlayOwn(sender, message.ToString(), otherSpeechCoroutine));

        }

    }




    void CreateChatItem(string sender, object message)
    {
        OwnPlayer(sender, message);

        // ChatItem 생성 (Content 의 자식으로)
        GameObject go = Instantiate(chatItemFactory, trContent);
        // 생성된 게임오브젝트에서 ChatItem 컴포넌트 가져온다.
        ChatItem chatItem = go.GetComponent<ChatItem>();
        // 가져온 컴포넌트에서 SetText 함수 실행
        chatItem.SetText(sender + " : " + message);
        // 가져온 컴포넌트의 onAutoScroll 변수에 AutoScrollBottom 을 설정
        chatItem.onAutoScroll = AutoScrollBottom;
        if (sender == PhotonNetwork.LocalPlayer.NickName)
        {
            // TMP_Text 컴포넌트 가져오자
            TMP_Text text = go.GetComponent<TMP_Text>();
            // 가져온 컴포넌트를 이용해서 색을 바꾸자
            text.color = Color.yellow;
        }
        else
        {
            // TMP_Text 컴포넌트 가져오자
            TMP_Text text = go.GetComponent<TMP_Text>();
            // 가져온 컴포넌트를 이용해서 색을 바꾸자
            text.color = Color.white;
        }
    }


    public void DebugReturn(DebugLevel level, string message)
    {
    }

    public void OnDisconnected()
    {
    }

    // 채팅 서버에 접속이 성공하면 호출되는 함수
    public void OnConnected()
    {
        print("채팅 서버 접속 성공!");
        // 전체 채널에 들어가자( 구독 )
        chatClient.Subscribe(currChannel);
    }

    public void OnChatStateChange(ChatState state)
    {
    }

    // 특정 채널에 다른 사람(나)이 메시지를 보내고 나한테 응답이 올때 호출 되는 함수
    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            print(senders[i] + " : " + messages[i]);

            CreateChatItem(senders[i], messages[i]);
        }
    }

    // 누군가 나한테 개인메시지를 보냈을 때
    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        //CreateChatItem(sender, message);
    }

    // 채팅 채널에 접속이 성공했을 때 들어오는 함수
    public void OnSubscribed(string[] channels, bool[] results)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            print(channels[i] + " 채널에 접속이 성공 했습니다");
        }
    }

    // 채팅 채널에서 나갔을 때 들어오는 함수
    public void OnUnsubscribed(string[] channels)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            print(channels[i] + " 채널에서 나갔습니다");
        }
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
    }

    public void OnUserSubscribed(string channel, string user)
    {
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
    }

    // 채팅 추가 되었을 때 맨밑으로 Content 위치를 옮기는 함수
    public void AutoScrollBottom()
    {
        if (trContent.sizeDelta.y > trChatView.sizeDelta.y)
        {
            if (prevContentH - trChatView.sizeDelta.y <= trContent.anchoredPosition.y)
            {
                trContent.anchoredPosition = new Vector2(trContent.anchoredPosition.x, trContent.sizeDelta.y - trChatView.sizeDelta.y);
            }
        }
    }
}
