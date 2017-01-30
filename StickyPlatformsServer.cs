using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;
using MsgPack.Serialization;

namespace stickyplatforms_server
{
  public struct Color
  {
    [MessagePackMember(0)]
    public byte r;
    [MessagePackMember(1)]
    public byte g;
    [MessagePackMember(2)]
    public byte b;
    [MessagePackMember(3)]
    public byte a;

    public Color(byte r, byte g, byte b, byte a = 255)
    {
      this.r = r; this.g = g; this.b = b; this.a = a;
    }
  }

  public struct Vector2
  {
    [MessagePackMember(0)]
    public float x;
    [MessagePackMember(1)]
    public float y;

    public Vector2(float x, float y)
    {
      this.x = x;
      this.y = y;
    }
  }

  public class Player
  {
    [MessagePackMember(0)]
    public string name;
    [MessagePackMember(1)]
    public Color color;
    [MessagePackMember(2)]
    public Vector2 position;
    [MessagePackMember(3)]
    public Vector2 velocity;
    [MessagePackMember(4)]
    public int hp = 0;

    public Player(string name)
    {
      this.name = name;
    }
  }

  public struct SpawnMsg
  {
    [MessagePackMember(0)]
    public Color color;
    [MessagePackMember(1)]
    public Vector2 pos;
    [MessagePackMember(2)]
    public int hp;
  }

  class StickyPlatformsServer
  {

    // Map file for the scene
    private const string mMapFilename = "assets/maps/multiplayer.tmx";
    // Peer ID => Player
    private ConcurrentDictionary<long, Player> mPlayers = new ConcurrentDictionary<long, Player>();

    private ISceneHost mScene;

    Task joinGameProcedure(RequestContext<IScenePeerClient> ctx)
    {
      // Make sure the client sends a unique name
      string name = ctx.ReadObject<string>();

      foreach (KeyValuePair<long, Player> player in mPlayers)
      {
        if (player.Value.name == name)
        {
          throw new ClientException("The username you chose (\"" + name + "\") is already in use.");
        }
      }

      if (!mPlayers.TryAdd(ctx.RemotePeer.Id, new Player(name)))
      {
        throw new ClientException("The joinGame procedure must be called only once by clients.");
      }

      ctx.SendValue(mMapFilename);
      return Task.FromResult(true);
    }

    Task onDisconnect(DisconnectedArgs args)
    {
      Player player = new Player("");
      mPlayers.TryRemove(args.Peer.Id, out player);

      mScene.Broadcast("playerLeft", player.name);

      return Task.FromResult(true);
    }

    Task onPlayerSpawn(Packet<IScenePeerClient> packet)
    {
      if (!mPlayers.ContainsKey(packet.Connection.Id))
      {
        // TODO send an error message to the client ?
        return Task.FromResult(false);
      }

      Player thisPlayer = mPlayers[packet.Connection.Id];
      SpawnMsg msg = packet.ReadObject<SpawnMsg>();

      thisPlayer.color = msg.color;
      thisPlayer.position = msg.pos;
      thisPlayer.velocity = new Vector2();
      thisPlayer.hp = msg.hp;

      mScene.Broadcast("newPlayer", thisPlayer);

      return Task.FromResult(true);
    }

    Task updateHp(Packet<IScenePeerClient> packet)
    {
      Player thisPlayer = mPlayers[packet.Connection.Id];

      thisPlayer.hp = packet.ReadObject<int>();

      return Task.FromResult(true);
    }

    Task updatePhysics(Packet<IScenePeerClient> packet)
    {
      Player thisPlayer = mPlayers[packet.Connection.Id];

      thisPlayer.position = packet.ReadObject<Vector2>();
      thisPlayer.velocity = packet.ReadObject<Vector2>();

      return Task.FromResult(true);
    }

    public StickyPlatformsServer(ISceneHost scene)
    {
      mScene = scene;
      scene.AddProcedure("joinGame", joinGameProcedure);
      scene.AddProcedure("getPlayerList", ctx =>
      {
        ctx.SendValue(mPlayers.Values.Where(player => player.hp > 0));
        return Task.FromResult(true);
      });
      scene.Disconnected.Add(onDisconnect);
      scene.AddRoute("spawn", onPlayerSpawn, _ => _);
      scene.AddRoute("updateHp", updateHp, _ => _);
      scene.AddRoute("updatePhysics", updatePhysics, _ => _);
    }
  }
}
