using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;
using Nakama.TinyJson;

public class TryNakama : MonoBehaviour
{
    const string scheme = "http";
    const string host = "192.168.0.5";  // 서버ip
    const int port = 7350;
    const string serverKey = "defaultkey";

    public string chatMsg;

    private Client activeClient;
    private ISession activeSession;
    private ISocket activeSocket;    
    private IChannel activeChannel;

    [Button]
    public async UniTaskVoid AuthByEmail()
    {
        activeClient = new Client(scheme, host, port, serverKey);

        var email = "test@test.com";
        var password = "1234Test";
        activeSession = await activeClient.AuthenticateEmailAsync(email, password);
        Debug.LogFormat("세션 생성됨: {0}", activeSession.ToString());    
    }

    [Button]
    public async UniTaskVoid AuthByDeviceId()
    {
        activeClient = new Client(scheme, host, port, serverKey);

        // 로컬 저장된 디바이스ID가 있다면 가져오고 아니면 시스템에서 받아옴
        var deviceId = PlayerPrefs.GetString("deviceId", SystemInfo.deviceUniqueIdentifier);

        // 권한문제라던가 디바이스UID를 못받아왔다면 GUID하나 생성
        if (deviceId == SystemInfo.unsupportedIdentifier)
        {
            deviceId = System.Guid.NewGuid().ToString();
        }

        // 로컬에 저장
        PlayerPrefs.SetString("deviceId", deviceId);

        // 디바이스 UID로 로그인 -> 사일런트 로그인 구현에 적합
        activeSession = await activeClient.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier);
        Debug.LogFormat("세션 생성됨: {0}", activeSession.ToString());
    }

    [Button]
    public async UniTaskVoid ConnectSocket()
    {
        activeSocket = Socket.From(activeClient);
        activeSocket.Connected += () =>
        {
            Debug.Log("소켓 연결됨 !!");
        };
        activeSocket.Closed += () =>
        {
            Debug.Log("소켓 닫힘 !!");
        };
        activeSocket.ReceivedError += err =>
        {
            Debug.LogErrorFormat("소켓 에러: {0}", err);
        };

        await activeSocket.ConnectAsync(activeSession);
    }

    [Button]
    public async UniTaskVoid ConnectChat()
    {
        var roomName = "room";
        activeChannel = await activeSocket.JoinChatAsync(roomName, ChannelType.Room, persistence: true);

        Debug.LogFormat("채팅룸 참여함: {0}", activeChannel.Id);
    }

    [Button]
    public async UniTaskVoid SendChat()
    {
        string channelId = activeChannel.Id;
        var messageContent = new Dictionary<string, string>()
        {
            { "message", chatMsg }
        };

        await activeSocket.WriteChatMessageAsync(activeChannel, JsonWriter.ToJson(messageContent));

        Debug.Log("메세지 전송됨");
    }

    [Button]
    public async UniTaskVoid GetChatList()
    {
        var limit = 100;
        var forward = true;
        var id = activeChannel.Id;
        var result = await activeClient.ListChannelMessagesAsync(activeSession, id, limit, forward, cursor: null);

        foreach (var message in result.Messages)
        {
            Debug.LogFormat("{0}:{1}", message.Username, message.Content);
        }
    }

    private void OnApplicationQuit()
    {
        // 로그아웃
        if (activeSocket != null)
            activeSocket.CloseAsync();
        if (activeSession != null)
            activeClient.SessionLogoutAsync(activeSession);
    }
}
