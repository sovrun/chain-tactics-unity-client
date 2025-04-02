using System;
using System.Runtime.InteropServices;
using ChainTactics;
using TacticsWarMud.TypeDefinitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static CTCommon.ContractDefines;

public class Tile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
{
    private PieceManager pieceManager;

    [SerializeField]
    private PieceType occupyingPiece = PieceType.Unknown;
    public PieceType OccupyingPiece => occupyingPiece;

    [SerializeField]
    private GameObject hoverTile;
    [SerializeField]
    private GameObject spawnHoverTile;
    [SerializeField]
    private GameObject clickTile;

    [Header("Owner Tile")]
    [SerializeField]
    private GameObject ownerTile_P1;
    [SerializeField]
    private GameObject ownerTile_P2;

    [SerializeField]
    private TextMeshProUGUI displayNameText;
    
    [SerializeField]
    private Image typeImage;

    private PositionData positionData;

    private bool holdState = false;

    private void Start()
    {
        pieceManager = FindObjectOfType<PieceManager>();
        Reset();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (pieceManager.CurrentSelectedPieceType != PieceType.Unknown)
            {
                int pieceImageIndex = 0;
                switch((PieceType)pieceManager.CurrentSelectedPieceType)
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

                typeImage.sprite = pieceManager.PieceIcons[pieceImageIndex];
                typeImage.gameObject.SetActive(true);
                
                WorldSettings.Instance.DropPiece(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            typeImage.sprite = null;
            typeImage.gameObject.SetActive(false);
            
            WorldSettings.Instance.RemovePiece(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (holdState)
        {
            return;
        }

        hoverTile.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (holdState)
        {
            return;
        }

        hoverTile.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdState)
        {
            return;
        }

        clickTile.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (holdState)
        {
            return;
        }

        clickTile.SetActive(true);
    }

    private void Reset()
    {
        hoverTile.SetActive(false);
        clickTile.SetActive(false);
        spawnHoverTile.SetActive(false);

        displayNameText.text = string.Empty;
    }

    public void AddPieceTypeToTile(PieceType pieceType)
    {
        occupyingPiece = pieceType;
        displayNameText.text = Util.GetAbbreviation(pieceType.ToString());
        
        int pieceImageIndex = 0;
        switch((PieceType)pieceType)
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

        typeImage.sprite = pieceManager.PieceIcons[pieceImageIndex];
        typeImage.gameObject.SetActive(true);        

        ownerTile_P1.SetActive(false);
        ownerTile_P2.SetActive(false);
    }

    public void AddPieceTypeToTile(PieceType pieceType, bool isOwner)
    {
        occupyingPiece = pieceType;
        displayNameText.text = Util.GetAbbreviation(pieceType.ToString());
        
        int pieceImageIndex = 0;
        switch((PieceType)pieceType)
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

        typeImage.sprite = pieceManager.PieceIcons[pieceImageIndex];
        typeImage.gameObject.SetActive(true);         

        ownerTile_P1.SetActive(isOwner);
        ownerTile_P2.SetActive(!isOwner);
    }

    public void RemovePieceTypeFromTile()
    {
        occupyingPiece = PieceType.Unknown;
        displayNameText.text = string.Empty;
        
        typeImage.sprite = null;
        typeImage.gameObject.SetActive(false);

        ownerTile_P1.SetActive(false);
        ownerTile_P2.SetActive(false);
    }

    public bool IsOccupied()
    {
        return occupyingPiece != PieceType.Unknown;
    }

    public void SetLocalPositionData(PositionData positionData)
    {
        this.positionData = positionData;
    }

    public PositionData GetLocalPositionData()
    {
        return positionData;
    }

    public void ToggleSpawnHover(bool state, bool holdState = false)
    {
        this.holdState = holdState;
        spawnHoverTile.SetActive(state);
    }
}
