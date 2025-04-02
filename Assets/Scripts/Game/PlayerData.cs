using CTHeadless;
using System.Collections.Generic;
using UnityEngine;
using ChainTactics;

public class PlayerData : MonoBehaviour
{
    [SerializeField]
    protected bool isOwner = false;
    [SerializeField]
    protected int playerIndex;

    protected List<CTCommon.Piece> playerPieces = new List<CTCommon.Piece>();
    public List<CTCommon.Piece> PlayerPieces
    {
        get { return playerPieces; }
        set { playerPieces = value; }
    }

    private void Start()
    {
        // networkManager = FindObjectOfType<NetworkManager>();
        // networkManager.onCTServerContextReady += (context) =>
        // {
        //     if (context == null)
        //     {
        //         Debug.LogError($"ctContext is null");
        //         return;
        //     }
        //
        //     ctServerContext = context;
        // };
        WorldSettings.Instance.OnGameIsReady += () =>
        {
            WorldSettings.Instance.OnMatchAcceptSuccessful += OnAcceptMatch;
        };
    }

    private void OnDisable()
    {
        WorldSettings.Instance.OnMatchAcceptSuccessful -= OnAcceptMatch;
    }

    private void OnAcceptMatch()
    {
        //playerIndex = ctServerContext.GetPlayerIndex();
        playerIndex = WorldSettings.Instance.ServerContext.GetPlayerIndex();
    }
}
