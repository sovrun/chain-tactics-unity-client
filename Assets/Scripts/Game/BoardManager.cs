using CTCommon;
using CTHeadless;
using System.Collections;
using System.Collections.Generic;
using ChainTactics;
using TacticsWarMud.TypeDefinitions;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    #region Variables
    [SerializeField]
    private float cameraXOffset = 0f;
    [SerializeField]
    private float cameraYOffset = 0f;

    [Space]

    [SerializeField]
    private Tile tilePrefab;
    [SerializeField]
    private Sprite emptySprite;
    [SerializeField]
    private Sprite playerSprite;
    [SerializeField]
    private Sprite enemySprite;
    [SerializeField]
    private Sprite cursorSprite;

    private Tile[,] gridObjects;
    [SerializeField]
    private List<Tile> initialSpawnTiles = new List<Tile>(); // Valid tiles during spawn phase
    public List<Tile> InitialSpawnTiles => initialSpawnTiles;

    [SerializeField]
    private List<Cell> boardCells = new List<Cell>(); // Store cell data

    private bool isOnFirstRender = true;
    private Coroutine renderCoroutine;
    #endregion

    #region Common Logic

    private void Awake()
    {
        WorldSettings.Instance.OnGameIsReady += () => { RenderBoard(false, false); };        
    }
    
    private void Start()
    {
        // We have to add a delay before initializing the world settings as there
        // are some subscribing events still going on that we need to wait.
        StartCoroutine(IBoardInitialization());
    }
    #endregion

    #region Render Board Logic
    public void RenderBoard(bool loop, bool drawUnits = true)
    {
        renderCoroutine = StartCoroutine(RenderBoard_IEnum(loop, drawUnits));
    }

    private IEnumerator RenderBoard_IEnum(bool loop, bool drawUnits)
    {
        // We wait until the server context is ready.
        if(WorldSettings.Instance.ServerContext == null) yield break;
        
        // We initialize grid objects if there is none.
        //if (gridObjects == null) gridObjects = new Tile[CTContext.kBoardW, CTContext.kBoardH];
        gridObjects ??= new Tile[CTContext.kBoardW, CTContext.kBoardH];

        do
        {
            Debug.Log($"Rendering board");

            // Th tile size.
            const float tileSize = 1f;
            // Padding between tiles.
            const float tilePadding = 0.1f;
            // Adjusted size with padding
            const float effectiveTileSize = tileSize + tilePadding;

            var boardWidth = CTContext.kBoardW * effectiveTileSize;
            var boardHeight = CTContext.kBoardH * effectiveTileSize;

            // Correctly center the board.
            var boardOffset = new Vector3((CTContext.kBoardW - 1) * effectiveTileSize * 0.5f, -(CTContext.kBoardH - 1) * effectiveTileSize * 0.5f, 0);

            // 1 - based index
            for (var y = 1; y < CTContext.kBoardH + 1; y++)
            {
                for (var x = 1; x < CTContext.kBoardW + 1; x++)
                {
                    var cell = WorldSettings.Instance.ServerContext.Board[(y - 1) * CTContext.kBoardW + (x - 1)];

                    Tile tile;
                    var tilePosition = new Vector3((x - 1) * effectiveTileSize, (y - 1) * effectiveTileSize, 0) - boardOffset;

                    if (gridObjects[x - 1, y - 1] == null)
                    {
                        tile = Instantiate(tilePrefab, tilePosition, Quaternion.identity, transform);
                        gridObjects[x - 1, y - 1] = tile;
                        tile.transform.localScale = new Vector3(tileSize, tileSize, 1);
                        tile.name = $"[{x},{y}]";
                        tile.SetLocalPositionData(new PositionData { X = (uint)x, Y = (uint)y });
                    }
                    else
                    {
                        // Overwrite tile, if any.
                        tile = gridObjects[x - 1, y - 1];
                        tile.transform.position = tilePosition;
                        tile.transform.localScale = new Vector3(tileSize, tileSize, 1);
                        tile.name = $"[{x},{y}]";
                        tile.SetLocalPositionData(new PositionData { X = (uint)x, Y = (uint)y });
                        tile.RemovePieceTypeFromTile();
                    }

                    if (drawUnits) DrawUnitOnBoard(cell, tile);

                    // This is the last two rows, make some adjustments due to index 1 as the based indexing.
                    if (isOnFirstRender && y < 3) initialSpawnTiles.Add(tile);
                }
            }

            if (isOnFirstRender)
            {
                AdjustCameraPosition(effectiveTileSize, boardWidth, boardHeight);
                isOnFirstRender = false;
            }

            yield return new WaitForEndOfFrame();

        } while (loop);
    }

    private void DrawUnitOnBoard(Cell cell, Tile tile)
    {
        //int playerIndex = ctServerContext.GetPlayerIndex();
        var playerIndex = WorldSettings.Instance.ServerContext.GetPlayerIndex();

        if (cell.piece != null)
        {
            var owner = cell.piece.owner == playerIndex ? "you" : "the enemy";
            tile.AddPieceTypeToTile(cell.piece.type, cell.piece.owner == playerIndex);
        }
        else
        {
            tile.RemovePieceTypeFromTile();
        }
    }

    private void AdjustCameraPosition(float effectiveTileSize, float boardWidth, float boardHeight)
    {
        var mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // Compute the correct board center.
            var boardCenter = new Vector3(0f, (CTContext.kBoardH * 0.5f - 0.5f) * effectiveTileSize, -10f);

            // Apply camera offset separately.
            boardCenter.x += cameraXOffset;
            boardCenter.y += cameraYOffset;

            mainCamera.transform.position = boardCenter;

            // Compute correct camera size to fit the board.
            var aspectRatio = (float)Screen.width / Screen.height;
            // Padding.
            var verticalSize = (boardHeight * 0.5f) + effectiveTileSize;
            var horizontalSize = ((boardWidth * 0.5f) / aspectRatio) + effectiveTileSize;
            mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    public bool IsValidSpawn(Tile tile)
    {
        return initialSpawnTiles.Contains(tile);
    }
    #endregion
    
    #region Board Manager Initialization

    private IEnumerator IBoardInitialization()
    {
        // We wait for 1 seconds.
        yield return new WaitForSeconds(1.0f);
        WorldSettings.Instance.OnGameIsReady += () => { RenderBoard(false, false); };
        WorldSettings.Instance.InitializeConnection();
    }
    #endregion
}