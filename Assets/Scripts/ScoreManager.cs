using UnityEngine;
using UnityEngine.UI; // 如果你要連動UI畫面，需要這個

public class ScoreManager : MonoBehaviour
{
    // 使用靜態變數（Static），讓球可以直接存取，不用在場景中慢慢找
    public static int score = 0;

    void Start()
    {
        score = 0; // 遊戲開始時分數歸零
    }

    // 提供一個公開的方法讓球來呼叫
    public static void AddScore(int points)
    {
        score += points;
        Debug.Log("目前分數: " + score);

        // 這裡之後可以加入更新 UI 文字的程式碼，例如：scoreText.text = "Score: " + score;
    }
}