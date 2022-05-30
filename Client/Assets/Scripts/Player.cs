using RiptideNetworking;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 在线玩家列表
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    public ushort Id { get; private set; } 
    // 是否Client对应的本地角色
    public bool IsLocal { get; private set; }

    [SerializeField] private PlayerAnimationManager animationManager;
    [SerializeField] private Transform camTransform;
    [SerializeField] private Interpolator interpolator;

    private string username;

    private void OnDestroy()
    {
        // 移出在线玩家列表
        list.Remove(Id);
    }

    private void Move(ushort tick, bool isTeleport, Vector3 newPosition, Vector3 forward)
    {
        // transform.position = newPosition;

        // 将目标移动信息插入futureTransformUpdates队列，通过在Interpolator中处理该队列移动角色位置
        interpolator.NewUpdate(tick, isTeleport, newPosition);
        
        if (!IsLocal)
        {
            camTransform.forward = forward;
            animationManager.AnimateBasedOnSpeed();
        }
    }

    public static void Spawn(ushort id, string username, Vector3 position)
    {
        Player player;
        if (id == NetworkManager.Singleton.Client.Id)
        {
            // 生成当前玩家实例
            player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.IsLocal = true;
        }
        else
        {
            // 生成其他在线玩家实例
            player = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            player.IsLocal = false;
        }

        // 绑定player属性，如果username为空则指定为Guest
        player.name = $"Player {id} (username)";
        player.Id = id;
        player.username = username;
        // 加入在线玩家列表
        list.Add(id, player);
    }

    // [Riptide特性]
    // 带有MessageHandler属性的方法表示：
    // 带有ID-playerSpawned的消息由SpawnPlayer方法处理
    [MessageHandler((ushort)ServerToClientId.playerSpawned)]
    private static void SpawnPlayer(Message message)
    {
        Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
    }

    // 带有ID-playerMovement的消息由PlayerMovement方法处理
    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void PlayerMovement(Message message)
    {
        if (list.TryGetValue(message.GetUShort(), out Player player))
            player.Move(message.GetUShort(), message.GetBool(), message.GetVector3(), message.GetVector3());
    }
}