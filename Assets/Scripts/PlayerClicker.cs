using Mirror;
using UnityEngine;

public class PlayerClicker : NetworkBehaviour
{
    public static PlayerClicker LocalPlayer { get; private set; }
    public static int localPlayerId = -1;
    
    [SyncVar] public int myPlayerId = -1;
    [SyncVar] public bool isDefeated = false;
    
    private Camera mainCamera;
    
    void Awake()
    {
    }
    
    public override void OnStartLocalPlayer()
    {
        LocalPlayer = this;
        
        FindCamera();
        
        CmdRequestPlayerId();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
    }
    
    void FindCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
            }
        }
    }
    
    void OnDestroy()
    {
        if (LocalPlayer == this) 
        {
            LocalPlayer = null;
            localPlayerId = -1;
        }
    }
    
    [Command]
    void CmdRequestPlayerId()
    {
        myPlayerId = (int)connectionToClient.connectionId;
        
        if (GameFieldManager.Instance != null)
        {
            GameFieldManager.Instance.GiveInitialNodesToPlayer(myPlayerId, 5);
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(myPlayerId);
        }
        
        TargetSetPlayerId(connectionToClient, myPlayerId);
    }
    
    [TargetRpc]
    void TargetSetPlayerId(NetworkConnection target, int playerId)
    {
        myPlayerId = playerId;
        localPlayerId = playerId;
        
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.UpdatePlayerInfo();
        }
    }
    
    void Update()
    {
        if (!isLocalPlayer || isDefeated) return;
        
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.gameOver) return;
        
        if (GameManager.Instance.currentPlayerId != myPlayerId) 
        {
            return;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }
    
    void HandleClick()
    {
        if (mainCamera == null) 
        {
            FindCamera();
            if (mainCamera == null) return;
        }
        
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        
        if (hit.collider != null)
        {
            NodeLogic clickedNode = hit.collider.GetComponent<NodeLogic>();
            
            if (clickedNode != null)
            {
                CmdProcessClick(clickedNode.gameObject);
            }
        }
    }
    
    [Command]
    void CmdProcessClick(GameObject nodeObject)
    {
        if (!GameManager.Instance.CanPlayerAct(myPlayerId))
        {
            return;
        }
        if (isDefeated) return;
        
        NodeLogic node = nodeObject.GetComponent<NodeLogic>();
        if (node == null) return;
        
        bool actionPerformed = false;
        
        if (node.OwnerId == myPlayerId)
        {
            node.IncreaseValue(1);
            actionPerformed = true;
        }
        else
        {
            bool hasNeighbor = false;
            foreach (NodeLogic connectedNode in node.ConnectedNodes)
            {
                if (connectedNode.OwnerId == myPlayerId)
                {
                    hasNeighbor = true;
                    break;
                }
            }
            
            if (!hasNeighbor && node.OwnerId != -1)
            {
                return;
            }
            
            if (node.OwnerId == -1)
            {
                node.CaptureNode(myPlayerId);
                actionPerformed = true;
            }
            else
            {
                if (node.Value > 0)
                {
                    node.DecreaseValue(1);
                    actionPerformed = true;
                }
            }
        }
        
        if (actionPerformed && GameManager.Instance != null)
        {
            GameManager.Instance.NextTurn();
        }
    }
    
    [ClientRpc]
    public void RpcDefeat()
    {
        isDefeated = true;
        enabled = false;
    }
}