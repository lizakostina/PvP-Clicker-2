using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class GameFieldManager : NetworkBehaviour
{
    public static GameFieldManager Instance;
    
    public GameObject nodePrefab;
    public int gridSize = 3;
    public float spacing = 2.5f;
    
    private List<NodeLogic> allNodes = new List<NodeLogic>();
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        CreateGrid();
    }
    
    void CreateGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                float offset = (gridSize - 1) * spacing / 2f;
                Vector3 position = new Vector3(
                    x * spacing - offset,
                    y * spacing - offset,
                    0
                );
                
                GameObject nodeObj = Instantiate(nodePrefab, position, Quaternion.identity);
                NetworkServer.Spawn(nodeObj);
                
                NodeLogic nodeLogic = nodeObj.GetComponent<NodeLogic>();
                allNodes.Add(nodeLogic);
                nodeObj.name = $"Node_{x}_{y}";
            }
        }
        
        SetUpConnections();
    }
    
    void SetUpConnections()
    {
        for (int i = 0; i < allNodes.Count; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;
            
            allNodes[i].ConnectedNodes.Clear();
            
            if (x > 0) allNodes[i].ConnectedNodes.Add(allNodes[i - 1]);
            if (x < gridSize - 1) allNodes[i].ConnectedNodes.Add(allNodes[i + 1]);
            if (y > 0) allNodes[i].ConnectedNodes.Add(allNodes[i - gridSize]);
            if (y < gridSize - 1) allNodes[i].ConnectedNodes.Add(allNodes[i + gridSize]);
        }
    }
    
    [Server]
    public void GiveNodeToPlayer(int playerId)
    {
        foreach (NodeLogic node in allNodes)
        {
            if (node.OwnerId == -1)
            {
                node.CaptureNode(playerId);
                return;
            }
        }
    }

    [Server]
    public void GiveInitialNodesToPlayer(int playerId, int nodeCount = 3)
    {
        List<NodeLogic> freeNodes = new List<NodeLogic>();
        foreach (NodeLogic node in allNodes)
        {
            if (node.OwnerId == -1)
                freeNodes.Add(node);
        }
        
        ShuffleList(freeNodes);
        
        int nodesGiven = 0;
        for (int i = 0; i < Mathf.Min(nodeCount, freeNodes.Count); i++)
        {
            freeNodes[i].CaptureNode(playerId);
            nodesGiven++;
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
     
    [Server]
    public void ForceUpdateConnections()
    {
        SetUpConnections();
        
        foreach (NodeLogic node in allNodes)
        {
            if (node != null)
            {
                node.DrawConnections();
            }
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F7) && isServer)
        {
            ForceUpdateConnections();
        }
    }
}
