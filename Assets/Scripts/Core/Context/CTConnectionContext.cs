using UnityEngine;

[CreateAssetMenu(fileName = "NewServerConnectionConfiguration", menuName = "Chain Tactics/New Server Connection Configuration", order = 0)]
public class CTConnectionContext : ScriptableObject
{
    [Header("ChainTactics Server Connection Configuration Settings")]
    // The rpc url.
    public string Url = "https://sovrun-testchain.rpc.caldera.xyz/http";
    // The players private wallet address.
    public string PrivateKey = "Your private key";
    // The game's world address.
    public string WorldAddress = "0xa577ad383c296ed61ddbe215e55738a21de5818c";
    // The sequencer url.
    public string SequencerUrl = "https://ctserver.ae58a.chaintactics.io/chainTacticsSequencerHub";
    // The games chain id.
    public int ChainId = 2427925;
}
