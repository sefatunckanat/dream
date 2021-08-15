using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using ParrelSync;

public class ServerManager : MonoBehaviour, INetEventListener
{

  public int GAME_PORT = 3000;
  NetManager server;
  LogicTimer logicTimer;
  private ushort serverTick;
  public ushort Tick => serverTick;

  [Header("Player List")]
  public List<ServerPlayer> serverPlayers;
  public GameObject playerPrefab;
  private NetDataWriter _cachedWriter = new NetDataWriter();
  private NetPacketProcessor _packetProcessor;


  void Awake()
  {
    logicTimer = new LogicTimer(onLogicUpdate);
    server = new NetManager(this);

    _packetProcessor = new NetPacketProcessor();

    _packetProcessor.RegisterNestedType((w, v) => w.Put(v), r => r.GetVector2());
    _packetProcessor.RegisterNestedType<PlayerState>();

    if (!ClonesManager.IsClone())
      StartServer();
  }

  NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
  {
    _cachedWriter.Reset();
    _cachedWriter.Put((byte)type);
    packet.Serialize(_cachedWriter);
    return _cachedWriter;
  }

  NetDataWriter WritePacket<T>(T packet) where T : class, new()
  {
    _cachedWriter.Reset();
    _cachedWriter.Put((byte)PacketType.Serialized);
    _packetProcessor.Write(_cachedWriter, packet);
    return _cachedWriter;
  }


  void StartServer()
  {
    server.Start(GAME_PORT);
    logicTimer.Start();

    Debug.Log("[S] Server started.");
  }

  void onLogicUpdate()
  {
    serverTick = (ushort)((serverTick + 1) % NetworkConfig.MaxGameSequence);

    foreach (var serverPlayer in serverPlayers)
    {
      serverPlayer.Update(LogicTimer.FixedDelta);
    }
    if (serverTick % 2 == 0)
    {
      // Send Data
      foreach (var serverPlayer in serverPlayers)
      {
        var position = serverPlayer.gameObject.transform.position;
        // var rotation = serverPlayer.gameObject.transform.localEulerAngles.y;
        PlayerState ps = new PlayerState
        {
          Id = ((byte)serverPlayer.peer.Id),
          Position = position,
          Rotation = 0,
          Tick = serverTick
        };
        serverPlayer.peer.Send(WriteSerializable(PacketType.PlayerState, ps), DeliveryMethod.ReliableOrdered);
      }
    }
  }

  void OnDestroy()
  {
    server.Stop();
    logicTimer.Stop();
  }

  void Update()
  {
    server.PollEvents();
    logicTimer.Update();
  }

  PlayerInputPacket _cachedCommand = new PlayerInputPacket();
  private void OnInputReceived(NetPacketReader reader, NetPeer peer)
  {
    _cachedCommand.Deserialize(reader);
    byte playerId = (byte)peer.Id;
    var serverPlayer = serverPlayers.Find((player) => { return player.peer.Id == playerId; });
    serverPlayer.ApplyInput(_cachedCommand, LogicTimer.FixedDelta);
  }

  #region Peer Listener
  void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
  {
    Debug.Log("[S] Client Disconnected. " + peer.EndPoint);
    byte playerId = (byte)peer.Id;
    var serverPlayer = serverPlayers.Find((player) => { return player.peer.Id == playerId; });
    Destroy(serverPlayer.gameObject);
    serverPlayers.Remove(serverPlayer);
  }

  public void OnPeerConnected(NetPeer peer)
  {
    Debug.Log("[S] New Client Connected. " + peer.EndPoint);
    var serverPlayer = new ServerPlayer(peer, serverPlayers.Count, playerPrefab);
    serverPlayers.Add(serverPlayer);
  }

  public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
  {
    Debug.Log("[S] NetworkError: " + socketError);
  }

  public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
  {
    byte packetType = reader.GetByte();
    if (packetType >= NetworkConfig.PacketTypesCount)
      return;
    PacketType pt = (PacketType)packetType;
    switch (pt)
    {
      case PacketType.Movement:
        OnInputReceived(reader, peer);
        break;
      default:
        Debug.Log("Unhandled packet: " + pt);
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
    request.Accept();
  }

  #endregion

  #region Helpers

  #endregion
}
