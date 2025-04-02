using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using TacticsWarMud.TypeDefinitions;
using UnityEngine;
using ChainTactics;
using static CTCommon.ContractDefines;

public class PieceManager : MonoBehaviour
{
    private BoardManager boardManager;

    [SerializeField]
    private Transform pieceUIParent;
    [SerializeField]
    private PieceUI pieceUIPrefab;

    public List<Sprite> pieceIcons;

    [Space]

    [SerializeField]
    private PieceType currentSelectedPieceType;
    public PieceType CurrentSelectedPieceType => currentSelectedPieceType;

    [SerializeField]
    private List<PieceType> pieceTypes = new List<PieceType>();

    private List<PositionData> localSpawnPositions = new List<PositionData>();
    List<BigInteger> selectedUnits = new List<BigInteger>();
    List<BigInteger> noFortressLineup = new List<BigInteger>();

    private int unitsCount; // How many units are there?

    private CancellationTokenSource commitSpawnCTS;
    
    public List<Sprite> PieceIcons
    {
        get { return pieceIcons; }
    }

    private void Awake()
    {
        SubscribeEvents();
    }

    private void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();

        commitSpawnCTS = new CancellationTokenSource();

        unitsCount = Enum.GetValues(typeof(PieceType)).Length - 1;
        for (int i = 0; i < unitsCount; i++)
        {
            AddUnitSelectionToScreen();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            foreach (var i in localSpawnPositions)
            {
                Debug.LogError("A " + i.X + ", " + i.Y);
            }

            foreach (var i in selectedUnits)
            {
                Debug.LogError("B " + (PieceType)(int)i);
            }

            foreach (var i in noFortressLineup)
            {
                Debug.LogError("C " + (PieceType)(int)i);
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        HandleCts();
    }

    private void OnApplicationQuit()
    {
        UnsubscribeEvents();
        HandleCts();
    }

    private void AddUnitSelectionToScreen()
    {
        PieceUI pieceUI = Instantiate(pieceUIPrefab, pieceUIParent);
        int siblingIndex = pieceUI.transform.GetSiblingIndex() + 1;

        // @TODO: We also need a way to figure out the team, for now none.
        int pieceImageIndex = 0;
        switch((PieceType)siblingIndex)
        {
            case PieceType.FireMage:
                pieceImageIndex = 1;
                break;
            case PieceType.FootSoldier:
                pieceImageIndex = 2;
                break;
            case PieceType.Fortress:
                pieceImageIndex = 3;
                break;
            case PieceType.IceMage:
                pieceImageIndex = 4;
                break;
            case PieceType.Lancer:
                pieceImageIndex = 5;
                break;
            case PieceType.Priest:
                pieceImageIndex = 6;
                break;
            default:
                // Archer index is the default.
                break;
        }

        pieceUI.Initialize((PieceType)siblingIndex, pieceIcons[pieceImageIndex]);
    }

    private void OnSelectPiece(PieceType pieceType)
    {
        currentSelectedPieceType = pieceType;

        List<Tile> validSpawnTiles = boardManager.InitialSpawnTiles;
        foreach (Tile spawnTile in validSpawnTiles)
        {
            spawnTile.ToggleSpawnHover(true);
        }
    }

    private void OnDropPiece(Tile tile)
    {
        if (tile.IsOccupied() || !boardManager.IsValidSpawn(tile))
        {
            Debug.LogError("Invalid spawn");
            return;
        }

        pieceTypes.Add(currentSelectedPieceType);

        PositionData localPositionData = tile.GetLocalPositionData();
        tile.AddPieceTypeToTile(currentSelectedPieceType);

        localSpawnPositions.Add(localPositionData);

        currentSelectedPieceType = PieceType.Unknown;

        List<Tile> validSpawnTiles = boardManager.InitialSpawnTiles;
        foreach (Tile spawnTile in validSpawnTiles)
        {
            spawnTile.ToggleSpawnHover(false, false);
        }

        // Check if all units are placed on the board
        if (unitsCount == pieceTypes.Count)
        {
            WorldSettings.Instance.PlaceAllUnits();
        }
    }

    private void OnRemovePiece(Tile tile)
    {
        if (!tile.IsOccupied())
        {
            return;
        }

        tile.RemovePieceTypeFromTile();
        int index = localSpawnPositions.FindIndex(position => position == tile.GetLocalPositionData());
        localSpawnPositions.RemoveAt(index);
        pieceTypes.RemoveAt(index);
    }

    private async void OnCommitSpawn()
    {
        commitSpawnCTS?.Cancel();
        byte[] secret = new byte[pieceTypes.Count];

        for (int i = 1; i <= pieceTypes.Count; i++)
        {
            secret[i - 1] = (byte)i;
        }

        for (int i = 0; i < pieceTypes.Count; i++)
        {
            PieceType pieceType = pieceTypes[i];
            selectedUnits.Add((int)pieceType);

            if (pieceType == PieceType.Fortress)
            {
                continue;
            }

            noFortressLineup.Add((int)pieceType);
        }

        if (!await WorldSettings.Instance.ServerContext.SelectUnits(noFortressLineup, secret))
        {
            Debug.LogError($"A Last Receipt: {WorldSettings.Instance.ServerContext.GetLastReceipt()}");
            Debug.LogError($"A Last Error: {WorldSettings.Instance.ServerContext.GetLastError()}");
            Debug.LogError("Leaving game");
            await WorldSettings.Instance.ServerContext.Leave();
            commitSpawnCTS?.Cancel();
        }

        Debug.LogError("Select units done.");

        if (!await WorldSettings.Instance.ServerContext.RevealUnits(noFortressLineup, secret))
        {
            Debug.LogError($"A Last Receipt: {WorldSettings.Instance.ServerContext.GetLastReceipt()}");
            Debug.LogError($"A Last Error: {WorldSettings.Instance.ServerContext.GetLastError()}");
            Debug.LogError("Leaving game");
            await WorldSettings.Instance.ServerContext.Leave();
            commitSpawnCTS?.Cancel();
            return;
        }

        Debug.LogError("Reveal units done.");

        List<CTCommon.Piece> opponentPieces = WorldSettings.Instance.ServerContext.PlayerPieces[(WorldSettings.Instance.ServerContext.GetPlayerIndex() + 1) % 2];

        Debug.LogError("Enemy Units");
        StringBuilder sb = new StringBuilder();
        sb.AppendJoin(", ", opponentPieces.Select(piece => piece.type));

        Debug.LogError($"opponentPieces {opponentPieces.Count}: {sb}");

        await WorldSettings.Instance.ServerContext.SpawnUnits(selectedUnits, localSpawnPositions, secret);
        boardManager.RenderBoard(false, true);

        Debug.LogError("Reveal spawn done.");
    }

    private void SubscribeEvents()
    {
        WorldSettings.Instance.OnPieceSelected += OnSelectPiece;
        WorldSettings.Instance.OnPieceDrop += OnDropPiece;
        WorldSettings.Instance.OnPieceRemoved += OnRemovePiece;
        WorldSettings.Instance.OnCommitSpawn += OnCommitSpawn;
    }

    private void UnsubscribeEvents()
    {
        WorldSettings.Instance.OnPieceSelected -= OnSelectPiece;
        WorldSettings.Instance.OnPieceDrop -= OnDropPiece;
        WorldSettings.Instance.OnPieceRemoved -= OnRemovePiece;
        WorldSettings.Instance.OnCommitSpawn -= OnCommitSpawn;
    }

    private void HandleCts()
    {
        commitSpawnCTS?.Cancel();
        commitSpawnCTS?.Dispose();
    }
}