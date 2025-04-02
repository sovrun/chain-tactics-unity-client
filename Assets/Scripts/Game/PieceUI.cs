using System;
using ChainTactics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CTCommon.ContractDefines;

public class PieceUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private PieceType pieceType;
    [SerializeField]
    private TextMeshProUGUI typeText;
    [SerializeField]
    private Image typeImage;

    [SerializeField]
    private GameObject highlightImage;
    [SerializeField]
    private GameObject inactiveTile;

    [SerializeField]
    private Tile tile;  // Tracks where the piece is dropped
    [SerializeField]
    private bool isHighlighted;
    [SerializeField]
    private bool isInactive;    // Cannot use this piece anymore if true

    private void Awake()
    {
        WorldSettings.Instance.OnPieceSelected += OnSelectPiece;
        WorldSettings.Instance.OnPieceDrop += OnDropPiece;
        WorldSettings.Instance.OnPieceRemoved += OnRemovePiece;
    }
    
    private void OnDestroy()
    {
        WorldSettings.Instance.OnPieceSelected -= OnSelectPiece;
        WorldSettings.Instance.OnPieceDrop -= OnDropPiece;
    }

    private void OnApplicationQuit()
    {
        WorldSettings.Instance.OnPieceSelected -= OnSelectPiece;
        WorldSettings.Instance.OnPieceDrop -= OnDropPiece;
    }

    public void Initialize(PieceType pieceType, Sprite pieceSprite)
    {
        gameObject.name = $"{pieceType}";
        this.pieceType = pieceType;
        typeText.text = Util.GetAbbreviation(gameObject.name);
        typeImage.sprite = pieceSprite;
    }

    public string GetAbbreviatedName()
    {
        return typeText.text;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isHighlighted || isInactive)
        {
            return;
        }

        WorldSettings.Instance.SelectPiece(pieceType);
    }

    private void OnSelectPiece(PieceType pieceType)
    {
        if (this.pieceType != pieceType || isInactive)
        {
            if (isHighlighted)
            {
                isHighlighted = false;
                highlightImage.SetActive(false);
            }

            return;
        }

        isHighlighted = true;
        highlightImage.SetActive(true);
    }

    private void OnDropPiece(Tile tile)
    {
        if (!isHighlighted || this.tile != null)
        {
            return;
        }

        this.tile = tile;

        isHighlighted = false;
        highlightImage.SetActive(false);

        if (isInactive)
        {
            return;
        }

        isInactive = true;
        inactiveTile.SetActive(true);
    }

    private void OnRemovePiece(Tile tile)
    {
        if (this.tile == null || this.tile != tile || !isInactive)
        {
            return;
        }

        isInactive = false;
        inactiveTile.SetActive(false);
        highlightImage.SetActive(false);
    }
}