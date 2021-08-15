using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;

[System.Serializable]
public class ServerPlayer
{
  public NetPeer peer;
  public string endPoint;
  public int index;
  public Vector3 Position = new Vector3(0, 20, 0);
  public GameObject gameObject;

  [Header("-")]
  public bool groundedPlayer;
  public CharacterController characterController;
  private float gravityValue = -9.81f;
  public Vector2 playerInput = new Vector2(0, 0);
  public bool playerInputSpace = false;
  public float moveSpeed = 3f;

  public ServerPlayer(NetPeer peer, int index, GameObject prefab)
  {
    this.peer = peer;
    this.index = peer.Id;
    this.endPoint = peer.EndPoint.ToString();

    this.gameObject = UnityEngine.GameObject.Instantiate(prefab);
    this.gameObject.transform.position = Position;
    this.gameObject.name = "C" + peer.Id.ToString() + '-' + peer.EndPoint.ToString();

    characterController = this.gameObject.GetComponent<CharacterController>();
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
  public void Update(float delta)
  {
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
  }
}
