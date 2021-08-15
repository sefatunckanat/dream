﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using ParrelSync;

public class ClientManager : MonoBehaviour, INetEventListener
{

  NetManager client;
  public bool connected = false;
  private NetDataWriter _writer;
  private PlayerInputPacket packet;
  NetPeer server;
  private PlayerState cachedPlayerState;

  public GameObject playerPrefab;
  public GameObject targetPlayer;

  void Awake()
  {
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
  }

  public void OnDestroy()
  {
    client.Stop();
    Destroy(gameObject);
  }

  void UpdateLocalState()
  {
    if (targetPlayer == null)
      targetPlayer = Instantiate(playerPrefab);

    print(cachedPlayerState.Position);

    targetPlayer.transform.position = cachedPlayerState.Position;
    // targetPlayer.transform.localEulerAngles = new Vector3(0, cachedPlayerState.Rotation, 0);
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