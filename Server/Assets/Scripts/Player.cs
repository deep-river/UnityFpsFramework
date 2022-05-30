using RiptideNetworking;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 在线玩家列表
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; }
    public string Username { get; private set; }
    public PlayerMovement Movement => movement;

     [SerializeField] private PlayerMovement movement;

    private void OnDestroy()
    {
        // 移出在线玩家列表
        list.Remove(Id);
    }

    // 新玩家加入处理逻辑
    public static void Spawn(ushort id, string username)
    {
        // 将在线列表中所有玩家信息发送给新加入玩家
        foreach (Player otherPlayer in list.Values)
            otherPlayer.SendSpawned(id);

        // 实例化角色prefab
        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity).GetComponent<Player>();
        // 绑定player属性，如果username为空则指定为Guest
        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.Id = id;
        player.Username = string.IsNullOrEmpty(username) ? $"Guest {id}" : username;

        player.SendSpawned();
        // 加入在线玩家列表
        list.Add(id, player);
    }

    // 向在线玩家广播新加入玩家出生信息
    private void SendSpawned()
    {
        // Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.playerSpawned);
        // message.AddUShort(Id);
        // message.AddString(Username);
        // message.AddVector3(transform.position);
        // NetworkManager.Singleton.Server.SendToAll(message);

        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)));
    }

    // 向新加入玩家发送在线玩家信息
    private void SendSpawned(ushort toClientId)
    {
        NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, ServerToClientId.playerSpawned)), toClientId);
    }

    // 构造Riptide的Message结构
    private Message AddSpawnData(Message message)
    {
        message.AddUShort(Id);
        message.AddString(Username);
        message.AddVector3(transform.position); // 出生位置
        return message;
    }

    // [Riptide特性]
    // 带有MessageHandler属性的方法表示：
    // 带有ID-name的消息由Name方法处理
    [MessageHandler((ushort)ClientToServerId.name)]
    private static void Name(ushort fromClientId, Message message)
    {
        Spawn(fromClientId, message.GetString());
    }

    // // 带有ID-input的消息由Input方法处理
    [MessageHandler((ushort)ClientToServerId.input)]
    private static void Input(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player))
            player.Movement.SetInput(message.GetBools(6), message.GetVector3());
    }
}