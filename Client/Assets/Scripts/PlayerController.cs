using RiptideNetworking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform camTransform;

    // inputs数组用于记录按键输入
    private bool[] inputs;

    private void Start()
    {
        // 初始化inputs数组
        inputs = new bool[6];
    }

    // Update在每一帧调用
    private void Update()
    {
        // 在每一帧检测按键输入，避免在FixedUpdate的调用间隔期间漏掉按键输入
        if (Input.GetKey(KeyCode.W))
            inputs[0] = true;

        if (Input.GetKey(KeyCode.S))
            inputs[1] = true;

        if (Input.GetKey(KeyCode.A))
            inputs[2] = true;

        if (Input.GetKey(KeyCode.D))
            inputs[3] = true;

        if (Input.GetKey(KeyCode.Space))
            inputs[4] = true;

        if (Input.GetKey(KeyCode.LeftShift))
            inputs[5] = true;
    }

    // FixedUpdate在固定时间间隔调用
    private void FixedUpdate()
    {
        SendInput();

        // 重置inputs数组
        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = false;
    }

    private void SendInput()
    {
        Message message = Message.Create(MessageSendMode.unreliable, ClientToServerId.input);
        message.AddBools(inputs, false);
        message.AddVector3(camTransform.forward);
        NetworkManager.Singleton.Client.Send(message);
    }
}
