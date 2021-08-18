using LiteNetLib.Utils;
using System;
using UnityEngine;

public static class Extensions
{
  public static void Put(this NetDataWriter writer, Vector2 vector)
  {
    writer.Put(vector.x);
    writer.Put(vector.y);
  }

  public static void Put(this NetDataWriter writer, Vector3 vector)
  {
    writer.Put(vector.x);
    writer.Put(vector.y);
    writer.Put(vector.z);
  }

  public static Vector2 GetVector2(this NetDataReader reader)
  {
    Vector2 v;
    v.x = reader.GetFloat();
    v.y = reader.GetFloat();
    return v;
  }
  public static Vector3 GetVector3(this NetDataReader reader)
  {
    Vector3 v;
    v.x = reader.GetFloat();
    v.y = reader.GetFloat();
    v.z = reader.GetFloat();
    return v;
  }
}


public enum PacketType : byte
{
  Movement,
  Spawn,
  ServerState,
  Serialized,
  PlayerState
}

[Flags]
public enum MovementKeys : byte
{
  Left = 1 << 1,
  Right = 1 << 2,
  Up = 1 << 3,
  Down = 1 << 4,
  Jump = 1 << 5
}

public struct PlayerInputPacket : INetSerializable
{
  public ushort Id;
  public MovementKeys Keys;
  public ushort ServerTick;

  public void Serialize(NetDataWriter writer)
  {
    writer.Put(Id);
    writer.Put((byte)Keys);
    writer.Put(ServerTick);
  }

  public void Deserialize(NetDataReader reader)
  {
    Id = reader.GetUShort();
    Keys = (MovementKeys)reader.GetByte();
    ServerTick = reader.GetUShort();
  }
}

[Serializable]
public struct PlayerState : INetSerializable
{
  public byte Id;
  public Vector3 Position;
  public float Rotation;
  public ushort Tick;
  public void Serialize(NetDataWriter writer)
  {
    writer.Put(Id);
    writer.Put(Position);
    writer.Put(Rotation);
    writer.Put(Tick);
  }

  public void Deserialize(NetDataReader reader)
  {
    Id = reader.GetByte();
    Position = reader.GetVector3();
    Rotation = reader.GetFloat();
    Tick = reader.GetUShort();
  }
}