using Mirror;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    
    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public TMP_Text winnerText;
    public TMP_Text playerStatusText;
    public TMP_Text turnInfoText;
    
    [SyncVar] public bool gameOver = false;
    [SyncVar] public int winnerId = -1;
    [SyncVar] public int currentPlayerId = -1;
    [SyncVar] public float turnTimeRemaining = 30f;
    
    private HashSet<int> activePlayers = new HashSet<int>();
    private List<int> playerTurnOrder = new List<int>();
    private int currentPlayerIndex = 0;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (isServer)
        {
            Invoke(nameof(StartGame), 3f);
        }
        if (turnInfoText != null)
        {
            turnInfoText.text = "Ожидание игроков...";
        }
    }
    
    [Server]
    void StartGame()
    {
        if (playerTurnOrder.Count > 0)
        {
            currentPlayerId = playerTurnOrder[0];
            turnTimeRemaining = 30f;
        }
        RpcGameStarted();
    }
    
    [ClientRpc]
    void RpcGameStarted()
    {
        if (playerStatusText != null)
        {
            playerStatusText.text = "Игра началась!";
        }
    }
    
    [Server]
    public void RegisterPlayer(int playerId)
    {
        if (activePlayers.Contains(playerId))
            return;
        
        activePlayers.Add(playerId);
        playerTurnOrder.Add(playerId);

        if (activePlayers.Count == 1)
        {
            Invoke(nameof(StartGame), 3f);
        }
        UpdatePlayerStatus();
    }
    
    [Server]
    public void PlayerDefeated(int playerId)
    {
        if (!activePlayers.Contains(playerId))
            return;
        
        activePlayers.Remove(playerId);
        playerTurnOrder.Remove(playerId);

        if (currentPlayerId == playerId)
        {
            NextTurn();
        }

        if (playerTurnOrder.Count > 0)
        {
            currentPlayerIndex = playerTurnOrder.IndexOf(currentPlayerId);
            if (currentPlayerIndex == -1) currentPlayerIndex = 0;
        }
        
        UpdatePlayerStatus();
        RpcPlayerLost(playerId);
        CheckForWinner();
    }
    
    [Server]
    public void CheckForWinner()
    {
        if (gameOver) return;
        
        if (activePlayers.Count == 1)
        {
            foreach (int playerId in activePlayers)
            {
                winnerId = playerId;
                gameOver = true;
                RpcGameOver(winnerId);
                break;
            }
        }
        else if (activePlayers.Count == 0)
        {
            winnerId = -1;
            gameOver = true;
            RpcGameOver(winnerId);
        }
    }
    
    [ClientRpc]
    void RpcPlayerLost(int playerId)
    {
        if (PlayerClicker.localPlayerId == playerId)
        {
            if (playerStatusText != null)
                playerStatusText.text = "Вы проиграли!";
        }
    }
    
    [ClientRpc]
    void RpcGameOver(int winnerId)
    {
        StartCoroutine(ShowGameOverDelayed(winnerId));
    }
    
    private IEnumerator ShowGameOverDelayed(int winnerId)
    {
        yield return new WaitForSeconds(1f);
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (winnerText != null)
            {
                if (winnerId == -1)
                    winnerText.text = "НИЧЬЯ!\nВсе игроки проиграли";
                else if (PlayerClicker.localPlayerId == winnerId)
                    winnerText.text = "ПОБЕДА!";
                else
                    winnerText.text = $"ИГРА ОКОНЧЕНА\nПобедил Игрок {winnerId}";
            }
        }
        
        PlayerClicker[] players = FindObjectsOfType<PlayerClicker>();
        foreach (PlayerClicker player in players)
        {
            if (player.isLocalPlayer)
            {
                player.enabled = false;
            }
        }
    }
    
    [Server]
    public void UpdatePlayerStatus()
    {
        RpcUpdatePlayerStatus(activePlayers.Count);
    }
    
    void Update()
    {
        if (isServer && !gameOver && currentPlayerId != -1)
        {
            turnTimeRemaining -= Time.deltaTime;
            
            if (turnTimeRemaining <= 0f)
            {
                NextTurn();
            }
        }
        
        if (turnInfoText != null && currentPlayerId != -1)
        {
            int localPlayerId = PlayerClicker.localPlayerId;
            string turnText = "";
            
            if (localPlayerId == currentPlayerId)
            {
                turnText = $"ВАШ ХОД!\nВремени осталось: {Mathf.CeilToInt(turnTimeRemaining)}с";
                turnInfoText.color = Color.green;
            }
            else
            {
                turnText = $"Ход игрока {currentPlayerId}\nВремени осталось: {Mathf.CeilToInt(turnTimeRemaining)}с";
                turnInfoText.color = Color.yellow;
            }
            
            turnInfoText.text = turnText;
        }
    }
    
    [ClientRpc]
    void RpcUpdatePlayerStatus(int activeCount)
    {
        if (playerStatusText != null)
            playerStatusText.text = $"Активных игроков: {activeCount}";
    }
    
    [Server]
    public void NextTurn()
    {
        if (gameOver || playerTurnOrder.Count == 0) 
        {
            return;
        }
        
        currentPlayerIndex = playerTurnOrder.IndexOf(currentPlayerId);
        if (currentPlayerIndex == -1)
        {
            currentPlayerIndex = 0;
        }
        
        currentPlayerIndex = (currentPlayerIndex + 1) % playerTurnOrder.Count;
        currentPlayerId = playerTurnOrder[currentPlayerIndex];
        turnTimeRemaining = 30f;
    }

    [Server]
    public bool CanPlayerAct(int playerId)
    {
        return !gameOver && currentPlayerId == playerId;
    }
}