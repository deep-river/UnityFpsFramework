using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;

// 枚举ServerToClient消息id
public enum ServerToClientId : ushort
{
    sync = 1,
    playerSpawned,
    playerMovement,
}

// 枚举ClientToServer消息id
public enum ClientToServerId : ushort
{
    
    name = 1,
    input,
}

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _singleton;
    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public Server Server { get; private set; }
    // CurrentTick为服务端Tick计数，在每一FixedUpdate递增
    public ushort CurrentTick { get; private set; }

    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        // 最高FPS设为60
        Application.targetFrameRate = 60;

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Server = new Server();
        Server.Start(port, maxClientCount);

        // [Riptide特性]
        // Server.A += B 定义在服务器监听A事件，并执行B方法作为响应
        Server.ClientDisconnected += PlayerLeft;
    }

    private void FixedUpdate()
    {
        Server.Tick();

        // 每200次Tick，即每5秒(1 / 0.025 * 200 = 5sec)同步一次tick
        if (CurrentTick % 200 == 0)
            SendSync();

        CurrentTick++;
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if (Player.list.TryGetValue(e.Id, out Player player))
            Destroy(player.gameObject);
    }

    // 以UDP协议的方式广播帧同步消息，无需考虑可靠通信，在客户端实现纠错机制。
    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientId.sync);
        message.Add(CurrentTick);

        Server.SendToAll(message);
    }
}