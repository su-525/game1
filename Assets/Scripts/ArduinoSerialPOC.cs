using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

public class ArduinoSerialPOC : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "/dev/cu.usbmodem114401"; // macOS 範例
    // public string portName = "COM3";              // Windows 範例
    public int baudRate = 9600;

    [Header("Button Mapping")]
    public string defaultButtonName = "BUTTON";

    // ====== 【新增】可變電阻速度控制相關變數 ======
    [Header("Potentiometer Settings")]
    public float minBallSpeed = 2f;  // 最小球速
    public float maxBallSpeed = 15f; // 最大球速

    // 靜態變數，方便 Ball 腳本直接讀取：ArduinoSerialPOC.CurrentBallSpeed
    public static float CurrentBallSpeed = 5f;
    private readonly object potLock = new object();
    private float rawPotSpeed = 5f;
    // ==========================================

    private static ArduinoSerialPOC instance;

    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning;

    private readonly object buttonStateLock = new object();
    private readonly Dictionary<string, bool> rawButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> buttonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> previousButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public static bool GetButton() { return GetButton(null); }
    public static bool GetButton(string buttonName)
    {
        if (instance == null) return false;
        return instance.GetButtonState(instance.buttonStates, buttonName);
    }

    public static bool GetButtonDown() { return GetButtonDown(null); }
    public static bool GetButtonDown(string buttonName)
    {
        if (instance == null) return false;
        string resolvedButtonName = instance.ResolveButtonName(buttonName);
        bool previous = instance.GetButtonState(instance.previousButtonStates, resolvedButtonName);
        bool current = instance.GetButtonState(instance.buttonStates, resolvedButtonName);
        return !previous && current;
    }

    public static bool GetButtonUp() { return GetButtonUp(null); }
    public static bool GetButtonUp(string buttonName)
    {
        if (instance == null) return false;
        string resolvedButtonName = instance.ResolveButtonName(buttonName);
        bool previous = instance.GetButtonState(instance.previousButtonStates, resolvedButtonName);
        bool current = instance.GetButtonState(instance.buttonStates, resolvedButtonName);
        return previous && !current;
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 100;

        try
        {
            serialPort.Open();
            isRunning = true;

            readThread = new Thread(ReadSerialLoop);
            readThread.Start();

            Debug.Log("Serial connected: " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Serial open failed: " + e.Message);
        }
    }

    void Update()
    {
        SyncButtonStates();

        // === Arduino Button -> Unity Input ===
        if (GetButtonDown())
        {
            Debug.Log("Unity sees button down: " + ResolveButtonName(null));
        }

        if (GetButtonUp())
        {
            Debug.Log("Unity sees button up: " + ResolveButtonName(null));
        }

        // === Unity -> Arduino LED ===
        if (Input.GetKeyDown(KeyCode.L)) { SendCommand("LED_ON"); }
        if (Input.GetKeyDown(KeyCode.K)) { SendCommand("LED_OFF"); }
    }

    public void SendCommand(string command)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.WriteLine(command);
            Debug.Log("Sent: " + command);
        }
    }

    // 修改此核心循環以適應複合資料格式
    void ReadSerialLoop()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string message = serialPort.ReadLine();
                message = message.Trim();

                if (string.IsNullOrWhiteSpace(message)) continue;

                // 【修改關鍵】：先用逗號拆開「按鈕訊息」與「電阻數值」
                string[] dataParts = message.Split(',');

                if (dataParts.Length == 2)
                {
                    string buttonMessage = dataParts[0].Trim();
                    string potMessage = dataParts[1].Trim();


                    Debug.Log($"[Serial 原始數據] 按鈕: {buttonMessage}, 電阻值: {potMessage}");

                    // 1. 處理按鈕部分 (只有當有實際事件發生時才解析)
                    if (buttonMessage != "NONE")
                    {
                        if (TryParseButtonMessage(buttonMessage, out string buttonName, out bool isPressed))
                        {
                            SetRawButtonState(buttonName, isPressed);
                            // 這裡可以選擇要不要 Log 按鈕事件
                        }
                    }

                    // 2. 處理可變電阻部分 (0 ~ 1023)
                    if (int.TryParse(potMessage, out int rawPotValue))
                    {
                        // 轉換為遊戲中的速度
                        float calculatedSpeed = Map(rawPotValue, 0, 1023, minBallSpeed, maxBallSpeed);
                        Debug.Log($"[速度轉換成功] 原始值: {rawPotValue} -> 轉換後球速: {calculatedSpeed}");

                        lock (potLock)
                        {
                            rawPotSpeed = calculatedSpeed;
                        }
                    }
                }
            }
            catch (System.TimeoutException) { /* ignore */ }
            catch (System.Exception e)
            {
                Debug.LogWarning("Serial read error: " + e.Message);
            }
        }
    }

    void OnDestroy() { Shutdown(); }
    void OnApplicationQuit() { Shutdown(); }

    private void Shutdown()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive) readThread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
        if (instance == this) instance = null;
    }

    private void SyncButtonStates()
    {
        previousButtonStates.Clear();
        foreach (KeyValuePair<string, bool> pair in buttonStates)
        {
            previousButtonStates[pair.Key] = pair.Value;
        }

        lock (buttonStateLock)
        {
            foreach (KeyValuePair<string, bool> pair in rawButtonStates)
            {
                buttonStates[pair.Key] = pair.Value;
            }
        }

        // 【新增】同步多執行緒傳回來的速度到主執行緒靜態變數中
        lock (potLock)
        {
            CurrentBallSpeed = rawPotSpeed;
        }
    }

    private void SetRawButtonState(string buttonName, bool isPressed)
    {
        string resolvedButtonName = ResolveButtonName(buttonName);
        lock (buttonStateLock)
        {
            rawButtonStates[resolvedButtonName] = isPressed;
        }
    }

    private bool GetButtonState(Dictionary<string, bool> states, string buttonName)
    {
        string resolvedButtonName = ResolveButtonName(buttonName);
        return states.TryGetValue(resolvedButtonName, out bool isPressed) && isPressed;
    }

    private string ResolveButtonName(string buttonName)
    {
        return string.IsNullOrWhiteSpace(buttonName) ? defaultButtonName : buttonName.Trim();
    }

    private bool TryParseButtonMessage(string message, out string buttonName, out bool isPressed)
    {
        buttonName = null;
        isPressed = false;

        if (string.IsNullOrWhiteSpace(message)) return false;

        string[] parts = message.Split('_');
        if (parts.Length < 2) return false;

        string action = parts[parts.Length - 1];
        if (action.Equals("DOWN", StringComparison.OrdinalIgnoreCase)) isPressed = true;
        else if (action.Equals("UP", StringComparison.OrdinalIgnoreCase)) isPressed = false;
        else return false;

        if (parts.Length == 2)
        {
            buttonName = parts[0];
            return true;
        }

        buttonName = string.Join("_", parts, 0, parts.Length - 1);
        return true;
    }

    // 【新增】仿 Arduino 的 Map 函數
    private float Map(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}