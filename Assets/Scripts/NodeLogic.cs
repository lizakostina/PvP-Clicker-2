using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class NodeLogic : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnValueChanged))]
    public int Value;
    
    [SyncVar(hook = nameof(OnOwnerChanged))]
    public int OwnerId = -1;
    
    public List<NodeLogic> ConnectedNodes = new List<NodeLogic>();
    
    private SpriteRenderer spriteRenderer;
    private TextMesh textMesh;
    private LineRenderer lineRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateTextMesh();
        InitializeLineRenderer(); 
        
        if (isServer)
        {
            Value = Random.Range(15, 25);
            OwnerId = -1;
            UpdateVisual();
        }
        else
        {
            UpdateVisual();
        }
    }
    
    void CreateTextMesh()
    {
        GameObject textGO = new GameObject("NodeValueText");
        textGO.transform.SetParent(transform);
        textGO.transform.localPosition = new Vector3(0, 0, -0.1f);
        
        textMesh = textGO.AddComponent<TextMesh>();
        textMesh.characterSize = 0.2f;
        textMesh.fontSize = 24;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        
        MeshRenderer meshRenderer = textGO.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 1;
        }
    }
    
    void OnValueChanged(int oldValue, int newValue)
    {
        UpdateVisual();
    }
    
    void OnOwnerChanged(int oldOwner, int newOwner)
    {
        UpdateVisual();
    }
    
    void UpdateVisual()
    {
        if (spriteRenderer == null) return;
        
        if (OwnerId == -1)
            spriteRenderer.color = Color.gray;
        else if (OwnerId == 0)
            spriteRenderer.color = Color.blue;
        else if (OwnerId == 1)
            spriteRenderer.color = Color.red;
        else if (OwnerId == 2)
            spriteRenderer.color = Color.green;
        else
            spriteRenderer.color = Color.yellow;
        
        if (textMesh != null)
        {
            textMesh.text = Value.ToString();
            
            if (OwnerId == -1 || OwnerId == 3)
                textMesh.color = Color.black;
            else
                textMesh.color = Color.white;
        }
    }
    
    [Server]
    public void DecreaseValue(int amount)
    {
        int oldOwner = OwnerId;
        Value -= amount;
        
        if (Value <= 0)
        {
            Value = 0;
            OwnerId = -1;
            
            if (oldOwner != -1)
            {
                StartCoroutine(CheckPlayerNodesAfterDelay(oldOwner));
            }
        }
    }
    
    [Server]
    public void IncreaseValue(int amount)
    {
        Value += amount;
    }
    
    [Server]
    public void CaptureNode(int newOwnerId)
    {
        int oldOwner = OwnerId;
        OwnerId = newOwnerId;
        if (Value <= 0) Value = 1;
        
        if (oldOwner != -1 && oldOwner != newOwnerId)
        {
            StartCoroutine(CheckPlayerNodesAfterDelay(oldOwner));
        }
    }
    
    [Server]
    IEnumerator CheckPlayerNodesAfterDelay(int playerId)
    {
        yield return new WaitForSeconds(0.2f);
        
        bool hasNodes = false;
        NodeLogic[] allNodes = FindObjectsOfType<NodeLogic>();
        
        foreach (NodeLogic node in allNodes)
        {
            if (node.OwnerId == playerId)
            {
                hasNodes = true;
                break;
            }
        }
        
        if (!hasNodes && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDefeated(playerId);
        }
    }
    void InitializeLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.gray;
        lineRenderer.endColor = Color.gray;
        lineRenderer.sortingOrder = -1;
        lineRenderer.positionCount = 0;
    }
    public void DrawConnections()
    {
        if (lineRenderer == null || ConnectedNodes == null || ConnectedNodes.Count == 0)
        {
            if (lineRenderer != null) lineRenderer.positionCount = 0;
            return;
        }
        
        lineRenderer.positionCount = ConnectedNodes.Count * 2;
        int positionIndex = 0;
        
        foreach (NodeLogic neighbor in ConnectedNodes)
        {
            if (neighbor == null) continue;
            
            lineRenderer.SetPosition(positionIndex, transform.position);
            lineRenderer.SetPosition(positionIndex + 1, neighbor.transform.position);
            positionIndex += 2;
        }
    }

    void Update()
    {
        DrawConnections();
    }
}