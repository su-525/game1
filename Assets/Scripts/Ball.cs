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
    private void OnCollisionEnter(Collision collision)
    {
        // 如果撞到牆壁，利用物理反射計算新方向
        currentDirection = Vector3.Reflect(currentDirection, collision.contacts[0].normal).normalized;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager.AddScore(scoreValue);
            Destroy(gameObject);
        }
    }
}