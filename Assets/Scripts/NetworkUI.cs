using Mirror;
using UnityEngine;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text statusText;
    
    private bool menuHidden = false;
    
    void Start()
    {
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                return;
            }
        }
        
        if (menuPanel != null && !menuPanel.activeSelf)
        {
            menuPanel.SetActive(true);
        }
        
        NetworkManagerHUD hud = networkManager.GetComponent<NetworkManagerHUD>();
        if (hud != null) hud.enabled = false;
        
        if (ipInputField != null)
            ipInputField.text = "localhost";
            
        UpdateStatus("Готов к подключению");
    }
    
    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = "Статус: " + message;
    }
    
    public void OnHostButton()
    {
        if (networkManager == null) 
        {
            UpdateStatus("Ошибка: NetworkManager не найден");
            return;
        }
        
        UpdateStatus("Запуск хоста...");
        
        try
        {
            networkManager.StartHost();
            
            if (networkManager.isNetworkActive)
            {
                UpdateStatus("Хост запущен");
                
                Invoke(nameof(HideMenu), 0.5f);
            }
            else
            {
                UpdateStatus("Ошибка: сеть не активна");
            }
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Ошибка: {e.Message}");
        }
    }
    
    public void OnClientButton()
    {
        if (networkManager == null) 
        {
            UpdateStatus("Ошибка: NetworkManager не найден");
            return;
        }
        
        UpdateStatus("Подключение...");
        
        if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
        {
            networkManager.networkAddress = ipInputField.text;
        }
        
        try
        {
            networkManager.StartClient();
            UpdateStatus($"Подключение к {networkManager.networkAddress}");
            
            Invoke(nameof(HideMenu), 1f);
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Ошибка: {e.Message}");
        }
    }
    
    private void HideMenu()
    {
        if (menuHidden) return;
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
            menuHidden = true;
        }
    }
    
    public void OnQuitButton()
    {
        Application.Quit();
    }
}