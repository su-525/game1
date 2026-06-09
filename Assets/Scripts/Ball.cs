using UnityEngine;

public class Ball : MonoBehaviour
{
    public int scoreValue = 10;
    private Rigidbody rb;

    // ★★★ 關鍵就在這行！你可能漏掉了這一行宣告 ★★★
    private Vector3 currentDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 遊戲開始時，決定一個隨機方向
        currentDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized;
    }

    void Update()
    {
        // 每一幀根據 Arduino 傳過來的最新速度，重新計算速度向量
        // 這裡需要用到 currentDirection，所以上方必須宣告
        rb.linearVelocity = currentDirection * ArduinoSerialPOC.CurrentBallSpeed;

        // 備註：如果你是舊版 Unity 請用 rb.velocity = ...
    }

    // 當球撞到牆壁反彈時，修正它的前進方向
   

    // 將原本的 OnTriggerEnter 改為 OnCollisionEnter
    private void OnCollisionEnter(Collision collision)
    {
        // 1. 先處理反彈 (這段邏輯移到這裡)
        Vector3 normal = collision.contacts[0].normal;
        currentDirection = Vector3.Reflect(currentDirection, normal).normalized;

        // 2. 處理加分
        if (collision.gameObject.CompareTag("Player"))
        {
            ScoreManager.AddScore(scoreValue);
            // --- 在 Console 顯示訊息 ---
            Debug.Log("得分！目前獲得的分數為: " + scoreValue);
            Destroy(gameObject);
        }
    }
}