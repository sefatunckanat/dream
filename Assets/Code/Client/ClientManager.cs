using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using ParrelSync;

public class ClientManager : MonoBehaviour, INetEventListener
{
  private static ClientManager m_instance;
  public static ClientManager Init()
  {
    if (!m_instance)
    {
      m_instance = FindObjectOfType(typeof(ClientManager)) as ClientManager;
      if (!m_instance)
        throw new UnityException("Client Manager not found in current scene.");
    }
    return m_instance;
  }
  NetManager client;
  public bool connected = false;
  private NetDataWriter _writer;
  private PlayerInputPacket packet;
  NetPeer server;
  private PlayerState cachedPlayerState;

  public GameObject playerPrefab;
  public GameObject targetPlayer;
  public ushort lastReceiveTick = 0;
  public ushort delayTick = 100;

  [SerializeField]
  public List<PlayerState> playerStates = new List<PlayerState>();
  public ushort lastServerTick;
  private const int MaxStoredCommands = 60;
  void Awake()
  {
    DontDestroyOnLoad(this);
    cachedPlayerState = new PlayerState();
    client = new NetManager(this);
    _writer = new NetDataWriter();

    if (ClonesManager.IsClone())
      ConnectServer();
  }

  void ConnectServer()
  {
    client.Start();
    client.Connect("localhost", 3000, "SECRET");
  }

  void Update()
  {
    if (!ClonesManager.IsClone()) return;

    client.PollEvents();

    if (server == null && client.FirstPeer != null)
    {
      server = client.FirstPeer;
    }

    Vector2 inputs = new Vector2(
      Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0,
      Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
    );

    packet.Keys = 0;
    _writer.Reset();
    _writer.Put((byte)PacketType.Movement);

    if (inputs.x < -0.5f)
      packet.Keys |= MovementKeys.Left;
    if (inputs.x > 0.5f)
      packet.Keys |= MovementKeys.Right;
    if (inputs.y < -0.5f)
      packet.Keys |= MovementKeys.Up;
    if (inputs.y > 0.5f)
      packet.Keys |= MovementKeys.Down;
    if (Input.GetKey(KeyCode.Space))
      packet.Keys |= MovementKeys.Jump;

    packet.Id = lastReceiveTick;

    packet.Serialize(_writer);
    server.Send(_writer, DeliveryMethod.Unreliable);

    // Vector2 velocity = Vector2.zero;
    // if ((packet.Keys & NetworkPackets.MovementKeys.Up) != 0)
    //   velocity.y = -1f;
    // if ((packet.Keys & NetworkPackets.MovementKeys.Down) != 0)
    //   velocity.y = 1f;

    // if ((packet.Keys & NetworkPackets.MovementKeys.Left) != 0)
    //   velocity.x = -1f;
    // if ((packet.Keys & NetworkPackets.MovementKeys.Right) != 0)
    //   velocity.x = 1f;


    RenderPlayer();
  }

  public void RenderPlayer()
  {

    // Vector3 lerpPosition = Vector3.Lerp(targetPlayer.transform.position, playerStates[playerStates.Count - 1].Position, Time.deltaTime);
    // return;
    // if (playerStates.Count == 0) return;
    // if (playerStates.Count == 1)
    // {
    //   targetPlayer.transform.position = playerStates[0].Position;
    //   playerStates.Clear();
    //   return;
    // }
    // for (int i = 0; i < playerStates.Count - 2; i++)
    // {
    //   var to = playerStates[i];
    //   var from = playerStates[i + 1];


    //   print(string.Format("{0} {1} -> {2} {3}", to.Tick, to.Position, from.Tick, from.Position));
    //   Vector3 lerpPosition = Vector3.Lerp(to.Position, from.Position, Time.deltaTime);
    //   targetPlayer.transform.position = lerpPosition;
    // }
    // playerStates.Clear();
  }

  public void OnDestroy()
  {
    client.Stop();
    Destroy(gameObject);
  }

  void UpdateLocalState()
  {
    if (targetPlayer == null)
    {
      targetPlayer = Instantiate(playerPrefab);
      GameObject.FindObjectOfType<TPCamera>().target = targetPlayer.transform;

      targetPlayer.AddComponent<ClientPlayer>();
    }

    playerStates.Add(cachedPlayerState);
    if (playerStates.Count > MaxStoredCommands)
      playerStates.RemoveAt(0);
    ushort tick = cachedPlayerState.Tick;
    // print(string.Format("Tick:{0} Position:{1}", tick, cachedPlayerState.Position));
    targetPlayer.transform.position = Vector3.Lerp(targetPlayer.transform.position, cachedPlayerState.Position, (1 + Vector3.Distance(targetPlayer.transform.position, cachedPlayerState.Position)) / Time.deltaTime);
    lastReceiveTick = tick;
    targetPlayer.transform.localEulerAngles = new Vector3(0, cachedPlayerState.Rotation, 0);

    lastServerTick = cachedPlayerState.Tick;
  }

  public void OnPeerConnected(NetPeer peer)
  {
    Debug.Log("[C] Connected to server: " + peer.EndPoint);
    connected = true;
  }

  public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
  {
    connected = false;
    Debug.Log("[C] Disconnected to server: " + disconnectInfo.Reason);
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }

  public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
  {
    connected = false;
    Debug.Log("[C] Error Disconnected to server: " + socketError);
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
  }

  public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
  {
    byte packetType = reader.GetByte();
    if (packetType >= NetworkConfig.PacketTypesCount)
      return;
    PacketType pt = (PacketType)packetType;
    switch (pt)
    {
      case PacketType.PlayerState:
        cachedPlayerState.Deserialize(reader);
        UpdateLocalState();
        break;
    }
  }

  public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
  {
  }

  public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
  {
  }

  public void OnConnectionRequest(ConnectionRequest request)
  {
    request.Reject();
  }
}
