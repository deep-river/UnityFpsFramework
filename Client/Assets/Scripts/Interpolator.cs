using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    // timeElapsed为当次移动已经过的时间
    [SerializeField] private float timeElapsed = 0f;
    // timeToReachTarget为平滑移动到目标位置所需的用时
    [SerializeField] private float timeToReachTarget = 0.05f;
    // 最小位移阈值
    [SerializeField] private float movementThreshold = 0.05f;

    // 玩家移动指令队列
    private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

    private float squareMovementThreshold;
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;

    private void Start()
    {
        squareMovementThreshold = movementThreshold * movementThreshold;
        to = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position);
        from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position);
        previous = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position);
    }

    private void Update()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (NetworkManager.Singleton.ServerTick >= futureTransformUpdates[i].Tick)
            {
                if (futureTransformUpdates[i].IsTeleport)
                {
                    // 处理传送类型的移动：直接传送
                    to = futureTransformUpdates[i];
                    from = to;
                    previous = to;
                    transform.position = to.Position;
                } 
                else 
                {
                    previous = to;
                    to = futureTransformUpdates[i];
                    from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position);
                }

                futureTransformUpdates.RemoveAt(i);
                i--;
                timeElapsed = 0f;
                // 根据Tick计算平滑移动到目标位置所需的用时
                timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
            }
        }

        timeElapsed += Time.deltaTime;
        InterpolatePosition(timeElapsed / timeToReachTarget);
    }

    private void InterpolatePosition(float lerpAmount)
    {
        // 位移距离小于最小位移阈值且角色尚未到达目标位置时，利用Lerp使其平滑移动到目标位置
        if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
        {
            if (to.Position != from.Position)
                // 根据timeElapsed/timeToReachTarget - 运动已持续时间/预计移动总用时 作为参数决定Lerp每段位移距离
                transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);
            return;
        }
        // 位移距离超过最小位移阈值时，为避免服务端发来的消息延迟或丢失，此处使用LerpUnclamped，使角色在超过to.Position后仍保持移动
        // 在角色运动不改变运动方向时可较为准确地模拟未来一段时间的运动
        transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
    }

    public void NewUpdate(ushort tick, bool isTeleport, Vector3 position)
    {
        // Tick小于当前插值Tick，且并非传送移动类型时，说明消息超时，对其不作处理
        // 如果为传送移动类型，则仍将角色传送到目标位置
        if (tick <= NetworkManager.Singleton.InterpolationTick && !isTeleport)
            return;

        // 根据Tick排序，将消息插入到指令队列中合适的位置
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (tick < futureTransformUpdates[i].Tick)
            {
                futureTransformUpdates.Insert(i, new TransformUpdate(tick, isTeleport, position));
                return;
            }
        }

        // 插入指令队列尾部
        futureTransformUpdates.Add(new TransformUpdate(tick, isTeleport, position));
    }
}
