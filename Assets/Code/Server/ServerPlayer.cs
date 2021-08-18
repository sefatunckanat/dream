using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System;

public class ServerPlayer : MonoBehaviour
{
  [Header("Config")]
  private ServerManager serverManager;
  public bool initialized = false;
  public NetPeer peer;
  public string endPoint;
  public int index;
  private Vector3 _initialPosition = new Vector3(0, 20, 0);

  [Header("Inputs & Controls")]
  public Vector2 playerInput = new Vector2(0, 0);
  public bool playerInputSpace = false;
  public float moveSpeed = 3f;
  private bool groundedPlayer;
  private CharacterController characterController;
  private float gravityValue = -9.81f;

  [Header("Interpolation")]
  [SerializeField]
  public List<PlayerState> snapshots = new List<PlayerState>();

  [Serializable]
  public class Snapshot
  {
    public ushort Tick;
    public Vector3 Position;
  }

  public void Init(NetPeer peer)
  {
    serverManager = ServerManager.Init();
    this.peer = peer;
    this.index = peer.Id;
    this.endPoint = peer.EndPoint.ToString();

    this.transform.position = _initialPosition;
    characterController = gameObject.GetComponent<CharacterController>();

    initialized = true;
  }

  public void ApplyInput(PlayerInputPacket command, float delta)
  {
    if ((command.Keys & MovementKeys.Up) != 0)
      playerInput.y = -1f;
    else if ((command.Keys & MovementKeys.Down) != 0)
      playerInput.y = 1f;
    else
      playerInput.y = 0;

    if ((command.Keys & MovementKeys.Left) != 0)
      playerInput.x = -1f;
    else if ((command.Keys & MovementKeys.Right) != 0)
      playerInput.x = 1f;
    else
      playerInput.x = 0;

    if ((command.Keys & MovementKeys.Jump) != 0)
      playerInputSpace = true;
    else
      playerInputSpace = false;
  }

  private float yVelocity = 0;
  public void _Update(float delta)
  {
    if (!initialized) return;
    groundedPlayer = characterController.isGrounded;

    Vector3 _moveDirection = Vector3.right * playerInput.x + Vector3.forward * playerInput.y;
    _moveDirection *= moveSpeed;

    if (groundedPlayer)
    {
      yVelocity = 0f;

      if (playerInputSpace)
        yVelocity = 5f;
    }
    yVelocity += gravityValue * delta;

    _moveDirection.y = yVelocity;

    this.characterController.Move(_moveDirection * delta);
    // this.gameObject.transform.position += playerVelocity;
    // this.transform.name = peer.Ping.ToString();

    snapshots.Add(new PlayerState() { Tick = serverManager.Tick, Position = transform.position, Rotation = transform.rotation.y, Id = (byte)this.index });
  }
}
