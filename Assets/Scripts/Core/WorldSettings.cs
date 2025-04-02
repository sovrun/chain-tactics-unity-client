using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CTHeadless;
using TacticsWarMud.TypeDefinitions;
using static CTCommon.ContractDefines;

namespace ChainTactics
{
    public class WorldSettings : MonoBehaviour
    {
        #region Enums
        private enum EConnectionType
        {
            /**
             * Development configuration.
             */
            Development         = 0,
            /**
             * Production configuration.
             */
            Production          = 1
        }

        private enum EMessageType
        {
            /**
             * Normal message type.
             */
            Normal          = 0,
            /**
             * Warning message type.
             */
            Warning         = 1,
            /**
             * Error message type.
             */
            Error            = 2
        }
        #endregion
        
        #region Structs
        public struct FMessage
        {
            public Color MessageColor;
            public object Message;

            public FMessage(Color messageColor, object message)
            {
                MessageColor = messageColor;
                Message = message;
            }
        }
        #endregion
        
        #region Variables
        public static WorldSettings Instance;
        
        [Header("Chain Tactics Settings")]
        [SerializeField] private EConnectionType connectionType = EConnectionType.Development;
        [SerializeField] CTConnectionContext developmentConnectionConfiguration;
        [SerializeField] CTConnectionContext productionConnectionConfiguration;

        [Header("Chain Tactics Debug")] 
        [SerializeField] private bool isOnDebugMode = false;

        private int chainId = 0000000;

        private CTServerContext worldSettingsServerContext;
        private CancellationTokenSource cancellationTokenSource;
        
        private SynchronizationContext gameMainThread;

        private Action<int, string> OnLeave;
        private Action<CTCommon.Piece, List<PositionData>> OnMove;
        private Action<CTCommon.Piece, CTCommon.Piece> OnAttack;
        public Action OnMatchAcceptSuccessful;
        
        public Action<PieceType> OnPieceSelected;
        public Action<Tile> OnPieceDrop;
        public Action<Tile, PieceType> OnDropPieceSuccessful;
        public Action<Tile> OnPieceRemoved;
        public Action OnAllUnitsPlaced;
        public Action OnCommitSpawn;
        
        public delegate void OnCtContextNetworkReady();
        public OnCtContextNetworkReady OnGameIsReady;
        #endregion
        
        #region Properties
        public CTServerContext ServerContext { get; private set; }
        public string LocalPlayerPrivateKey { get; private set; }
        public bool IsGameReady { get; private set; }
        #endregion
        
        #region Standard Logic
        // Awake when an enabled script instance is being loaded. Use this to initialize variables or state before the application starts. 
        private void Awake()
        {
            if (Instance != null) Destroy(gameObject);
            else Instance = this;
            
            IsGameReady = false;
            LocalPlayerPrivateKey = "";
        }

        // Start is called before the first frame update.
        private void Start()
        {
            // We have to initialize the main thread.
            gameMainThread = SynchronizationContext.Current;
        }

        private void OnDestroy()
        {
            if (worldSettingsServerContext == null)
            {
                Log($"No active server context, just destroying", EMessageType.Error);
                return;
            }

            worldSettingsServerContext.Leave();
            CancelAndDisposeToken();
        }

        private void OnApplicationQuit()
        {
            if (worldSettingsServerContext == null)
            {
                Log($"No active server context, just quitting", EMessageType.Error);
                return;
            }
            
            worldSettingsServerContext.Leave();
            CancelAndDisposeToken();
        }
        #endregion
        
        #region Accessible Logic
        /**
         * <summary>Initializes server connection.</summary>
         */
        public void InitializeConnection()
        {
            // Check for connection configuration first.
            switch (connectionType)
            {
                case EConnectionType.Development when developmentConnectionConfiguration == null:
                    Log($"Server development connection configuration not set, please assign a development connection configuration to work", EMessageType.Error);
                    return;
                case EConnectionType.Production when productionConnectionConfiguration == null:
                    Log($"Server production connection configuration not set, please assign a production connection configuration to work", EMessageType.Error);
                    return;
            }

            // We save the local players private key.
            if (connectionType == EConnectionType.Development) LocalPlayerPrivateKey = developmentConnectionConfiguration.PrivateKey;
            else LocalPlayerPrivateKey = productionConnectionConfiguration.PrivateKey;
            
            // Then we run the initialize connection.
            Task.Run(Task_InitializeConnection).Wait();
        }
        
        // Performs the actual async server connection.
        private async void Task_InitializeConnection()
        {
            if (connectionType == EConnectionType.Development)
            {
                var newPrivateKey = ValidateAndUpdatePrivateKeyPrefix(developmentConnectionConfiguration.PrivateKey);
                chainId = developmentConnectionConfiguration.ChainId;

                var stackMessage = new List<FMessage>();
                stackMessage.Add(new FMessage(Color.white, $"Connecting to server on development environment using "));
                stackMessage.Add(new FMessage(Color.green, newPrivateKey));
                stackMessage.Add(new FMessage(Color.white, $" with chain id of "));
                stackMessage.Add(new FMessage(Color.green, chainId));
                Log(stackMessage, EMessageType.Normal);

                worldSettingsServerContext = new CTServerContext(developmentConnectionConfiguration.Url, newPrivateKey, developmentConnectionConfiguration.WorldAddress, developmentConnectionConfiguration.SequencerUrl);
                worldSettingsServerContext.OnLeave += OnLeave;
                worldSettingsServerContext.OnMove += OnMove;
                worldSettingsServerContext.OnAttack += OnAttack;

                await worldSettingsServerContext.GetBalance();

                if (worldSettingsServerContext == null)
                {
                    Log($"Connection error, unable to obtain server context", EMessageType.Error);
                    return;
                }

                // We store server context for later use.
                ServerContext = worldSettingsServerContext;
                
                // Invoke back to main thread.
                gameMainThread.Post(_ =>
                {
                    // We tell the game we are ready.
                    OnGameIsReady?.Invoke();
                    IsGameReady = true;
                }, null);

                cancellationTokenSource = new CancellationTokenSource();
                GameHealthCheck(cancellationTokenSource);
            }
            else
            {
                var newPrivateKey = ValidateAndUpdatePrivateKeyPrefix(productionConnectionConfiguration.PrivateKey);
                chainId = productionConnectionConfiguration.ChainId;

                var stackMessage = new List<FMessage>();
                stackMessage.Add(new FMessage(Color.white, $"Connecting to server on production environment using "));
                stackMessage.Add(new FMessage(Color.green, newPrivateKey));
                stackMessage.Add(new FMessage(Color.white, $" with chain id of "));
                stackMessage.Add(new FMessage(Color.green, chainId));
                Log(stackMessage, EMessageType.Normal);

                worldSettingsServerContext = new CTServerContext(productionConnectionConfiguration.Url, newPrivateKey, productionConnectionConfiguration.WorldAddress, productionConnectionConfiguration.SequencerUrl);
                worldSettingsServerContext.OnLeave += OnLeave;
                worldSettingsServerContext.OnMove += OnMove;
                worldSettingsServerContext.OnAttack += OnAttack;

                await worldSettingsServerContext.GetBalance();

                if (worldSettingsServerContext == null)
                {
                    Log($"Connection error, unable to obtain server context", EMessageType.Error);
                    return;
                }

                // We store server context for later use.
                ServerContext = worldSettingsServerContext;
                
                // Invoke back to main thread.
                gameMainThread.Post(_ =>
                {
                    // We tell the game we are ready.
                    OnGameIsReady?.Invoke();
                    IsGameReady = true;
                }, null);

                cancellationTokenSource = new CancellationTokenSource();
            }
        }
        
        /**
         * <summary>Is called when the piece is drop.</summary>
         */
        public void DropPiece(Tile tile)
        {
            OnPieceDrop(tile);
        }
        
        /**
         * <summary>Is called when the piece is removed.</summary>
         */
        public void RemovePiece(Tile tile)
        {
            OnPieceRemoved(tile);
        }
        
        /**
         * <summary>Is called when the piece is selected.</summary>
         */
        public void SelectPiece(PieceType pieceType)
        {
            OnPieceSelected(pieceType);
        }
        
        /**
         * <summary>Is called when player wants to commit the spawn.</summary>
         */
        public void CommitSpawn()
        {
            OnCommitSpawn.Invoke();
        }  
        
        /**
         * <summary>Is called when ready to place all units.</summary>
         */
        public void PlaceAllUnits()
        {
            OnAllUnitsPlaced.Invoke();
        }

        /**
         * <summary>Returns the unit positions</summary>
         * <param name="ctContext">Chain tactics context</param>
         */
        public List<Vector2Int> GetUnitPositions(CTContext ctContext)
        {
            Log($"Getting unit positions", EMessageType.Normal);

            var unitPositions = new List<Vector2Int>();

            var boardWidth = CTContext.kBoardW;
            var boardHeight = CTContext.kBoardH;

            for (var y = 0; y < boardHeight; y++)
            {
                for (var x = 0; x < boardWidth; x++)
                {
                    var cell = ctContext.Board[y * boardWidth + x];

                    if (cell.piece != null)
                    {
                        unitPositions.Add(new Vector2Int(x, y));
                    }
                }
            }

            return unitPositions;
        }
        #endregion
        
        #region Private Logic
        // Validates and update the assigned private key prefix.
        private static string ValidateAndUpdatePrivateKeyPrefix(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                // Default to "0x" if input is empty or null
                return "0x";
            }

            return input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? input : "0x" + input;
        }
        
        // Cancels and disposes the generated token.
        private void CancelAndDisposeToken()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }    

        // Performs a game health check on development configuration.
        private async void GameHealthCheck(CancellationTokenSource cts)
        {
            try
            {
                var token = cts.Token;

                while (!token.IsCancellationRequested)
                {
                    if (await worldSettingsServerContext.GetMatchStatus() == MatchStatusTypes.Finished)
                    {
                        // @TODO: Handle finish here...
                    }
                    
                    Log("Game health check", EMessageType.Normal);
                    await Task.Delay(500, token);
                }
            }
            catch (TaskCanceledException)
            {
                Log("Game health check cancelled", EMessageType.Normal);
            }
            catch (Exception e)
            {
                Log($"Game health check encountered an error: {e.Message}", EMessageType.Error);
            }
            finally
            {
                if (!cts.IsCancellationRequested) cts.Cancel();
                
                cts.Dispose();
                Log("Game health check token source disposed successfully", EMessageType.Normal);
            }
        }
        #endregion
        
        #region Debugging
        /**
         * <summary>Prints out logs.</summary>
         * <param name="message">Message to print</param>
         * <param name="messageType">Message type</param>
         */
        private void Log(object message, EMessageType messageType)
        {
            if (!isOnDebugMode) return;

            switch (messageType)
            {
                case EMessageType.Error:
                    Debug.LogError($"World Settings::Error - " + message);
                    break;
                case EMessageType.Warning:
                    Debug.LogWarning($"World Settings::Warning - " + message);
                    break;
                case EMessageType.Normal:
                default:
                    Debug.Log($"World Settings::Log - " + message);
                    break;
            }
        }

        /**
         * <summary>Prints out log messages, think of this as append style if you want certain words to have a unique color.</summary>
         * <param name="message">Message format</param>
         * <param name="messageType">Message type</param>
         */
        private void Log(List<FMessage> message, EMessageType messageType)
        {
            if (!isOnDebugMode) return;
            
            // Create default message first.
            var defaultColor = Color.white;
            // Create default message.
            var defaultMessage = "";
            var completeMessage = "";
            
            switch (messageType)
            {
                case EMessageType.Error:
                    defaultColor = Color.red;
                    defaultMessage = "World Settings::Error - ";
                    completeMessage = string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(defaultColor.r * 255f), (byte)(defaultColor.g * 255f), (byte)(defaultColor.b * 255f), defaultMessage);

                    foreach (var item in message)
                    {
                        completeMessage += string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(item.MessageColor.r * 255f), (byte)(item.MessageColor.g * 255f), (byte)(item.MessageColor.b * 255f), item.Message);
                    }
                    
                    Debug.Log(completeMessage);
                    break;
                case EMessageType.Warning:
                    defaultColor = Color.yellow;
                    defaultMessage = "World Settings::Warning - ";
                    completeMessage = string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(defaultColor.r * 255f), (byte)(defaultColor.g * 255f), (byte)(defaultColor.b * 255f), defaultMessage);

                    foreach (var item in message)
                    {
                        completeMessage += string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(item.MessageColor.r * 255f), (byte)(item.MessageColor.g * 255f), (byte)(item.MessageColor.b * 255f), item.Message);
                    }
                    
                    Debug.Log(completeMessage);
                    break;
                case EMessageType.Normal:
                default:
                    defaultColor = Color.white;
                    defaultMessage = "World Settings::Log - ";
                    completeMessage = string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(defaultColor.r * 255f), (byte)(defaultColor.g * 255f), (byte)(defaultColor.b * 255f), defaultMessage);

                    foreach (var item in message)
                    {
                        completeMessage += string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(item.MessageColor.r * 255f), (byte)(item.MessageColor.g * 255f), (byte)(item.MessageColor.b * 255f), item.Message);
                    }
                    
                    Debug.Log(completeMessage);
                    break;
            }            
        }
        #endregion
    }
}