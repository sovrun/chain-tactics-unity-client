using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using ChainTactics;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject mainPanel;
    [SerializeField]
    private GameObject uiElementsPanel;
    [SerializeField]
    private GameObject piecesPanel;

    [Header("Text")]
    [SerializeField]
    private TextMeshProUGUI idText;
    [SerializeField]
    private TextMeshProUGUI playerIndexText;
    [SerializeField]
    private TextMeshProUGUI feedbackText;

    [Header("Buttons")]
    [SerializeField]
    private Button findMatchButton;
    [SerializeField]
    private Button acceptMatchButton;
    [SerializeField]
    private Button cancelMatchButton;
    [SerializeField]
    private Button commitButton;

    private CancellationTokenSource findMatchCts;
    private CancellationTokenSource acceptMatchCts;

    private void Awake()
    {
        CTCommon.Logger.LogFn = (string msg, CTCommon.Logger.LogLevel logLevel) =>
        {
            switch (logLevel)
            {
                case CTCommon.Logger.LogLevel.Trace:
                    Debug.Log(msg);
                    break;
                case CTCommon.Logger.LogLevel.Info:
                    Debug.LogWarning(msg);
                    break;
                case CTCommon.Logger.LogLevel.Debug:
                    Debug.Log(msg);
                    break;
                case CTCommon.Logger.LogLevel.Error:
                    Debug.LogError(msg);
                    break;

                default:
                    Debug.Log(msg);
                    break;
            }
        };
        
        InitializeEvents();
        InitializeVariables();

        WorldSettings.Instance.OnGameIsReady += () =>
        {
            // We activate the button so that player knows they can start finding match.
            findMatchButton.interactable = true;

            idText.text = WorldSettings.Instance.LocalPlayerPrivateKey;
        };
    }
    
    private void OnDestroy()
    {
        HandleCTS();
    }

    private void OnApplicationQuit()
    {
        HandleCTS();
    }

    private void InitializeEvents()
    {
        findMatchButton.onClick.AddListener(FindMatch);
        acceptMatchButton.onClick.AddListener(AcceptMatch);
        cancelMatchButton.onClick.AddListener(CancelMatch);
        commitButton.onClick.AddListener(CommitSpawn);
        
        WorldSettings.Instance.OnAllUnitsPlaced += () => { commitButton.gameObject.SetActive(true); };
        WorldSettings.Instance.OnPieceRemoved += (x) => { commitButton.gameObject.SetActive(false); };
    }

    private void InitializeVariables()
    {
        findMatchCts = new CancellationTokenSource();
        acceptMatchCts = new CancellationTokenSource();

        mainPanel.SetActive(true);
        uiElementsPanel.SetActive(true);
        piecesPanel.SetActive(false);

        idText.text = string.Empty;
        feedbackText.text = string.Empty;

        commitButton.gameObject.SetActive(false);
    }

    #region Events
    private async void FindMatch()
    {
        feedbackText.text = $"Finding match...";
        findMatchButton.gameObject.SetActive(false);
        cancelMatchButton.gameObject.SetActive(true);

        Debug.LogError("Finding Match");

        await WorldSettings.Instance.ServerContext.FindMatch(findMatchCts.Token);

        feedbackText.text = $"Match found!";

        acceptMatchButton.gameObject.SetActive(true);
        cancelMatchButton.gameObject.SetActive(false);
    }

    private async void AcceptMatch()
    {
        acceptMatchButton.interactable = false;
        await WorldSettings.Instance.ServerContext.AcceptMatch(acceptMatchCts.Token);

        acceptMatchCts?.Dispose();

        playerIndexText.text = $"Player index: {WorldSettings.Instance.ServerContext.GetPlayerIndex()}";

        uiElementsPanel.SetActive(false);
        piecesPanel.SetActive(true);
        acceptMatchButton.interactable = true;
        acceptMatchButton.gameObject.SetActive(false);
    }

    private async void CancelMatch()
    {
        findMatchCts?.Cancel();
        await WorldSettings.Instance.ServerContext.Leave();

        feedbackText.text = string.Empty;
        findMatchButton.gameObject.SetActive(true);
        cancelMatchButton.gameObject.SetActive(false);
    }

    private void CommitSpawn()
    {
        WorldSettings.Instance.CommitSpawn();
    }
    #endregion

    private void HandleCTS()
    {
        findMatchCts?.Cancel();
        findMatchCts?.Dispose();

        acceptMatchCts?.Cancel();
        acceptMatchCts?.Dispose();
    }
}