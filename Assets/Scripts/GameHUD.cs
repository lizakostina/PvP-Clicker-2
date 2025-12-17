using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance;
    
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text playerStatusText;
    
    private float updateTimer = 0f;
    private float updateInterval = 0.5f;
    private int lastPlayerId = -2;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        FindUIElements();
        
        if (turnText != null)
        {
            turnText.text = "Подключение...";
            turnText.enabled = true;
        }
        
        if (playerStatusText != null)
        {
            playerStatusText.text = "Ожидание подключения...";
        }
    }
    
    void FindUIElements()
    {
        if (turnText == null)
        {
            GameObject turnObj = GameObject.Find("TurnText");
            if (turnObj != null) turnText = turnObj.GetComponent<TMP_Text>();
        }
        
        if (playerStatusText == null)
        {
            GameObject statusObj = GameObject.Find("PlayerStatusText");
            if (statusObj != null) playerStatusText = statusObj.GetComponent<TMP_Text>();
        }
    }
    
    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            UpdateHUD();
            updateTimer = 0f;
        }
    }
    
    void UpdateHUD()
    {
        int currentPlayerId = PlayerClicker.localPlayerId;
        
        if (playerStatusText != null)
        {
            if (currentPlayerId != -1)
            {
                playerStatusText.text = $"Вы игрок {currentPlayerId}";
            }
            else
            {
                playerStatusText.text = "Подключение...";
            }
        }
        
        if (turnText != null)
        {
            if (currentPlayerId != -1)
            {
                string colorName = GetPlayerColorName(currentPlayerId);
                string playerText = $"Игрок {currentPlayerId} ({colorName})";
                
                if (currentPlayerId != lastPlayerId)
                {
                    turnText.text = playerText;
                    
                    if (currentPlayerId == 0)
                        turnText.color = Color.blue;
                    else if (currentPlayerId == 1)
                        turnText.color = Color.red;
                    else if (currentPlayerId == 2)
                        turnText.color = Color.green;
                    else
                        turnText.color = Color.yellow;
                        
                    lastPlayerId = currentPlayerId;
                }
            }
            else if (turnText.text != "Ожидание подключения...")
            {
                turnText.text = "Ожидание подключения...";
                turnText.color = Color.white;
            }
        }
    }
    
    public void UpdatePlayerInfo()
    {
        UpdateHUD();
    }
    
    string GetPlayerColorName(int playerId)
    {
        switch (playerId)
        {
            case 0: return "Синий";
            case 1: return "Красный";
            case 2: return "Зеленый";
            case 3: return "Желтый";
            default: return "Игрок";
        }
    }
}