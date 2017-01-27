using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;

namespace stickyplatforms_server
{
  class StickyPlatformsServer
  {
    private struct Color
    {
      public byte r, g, b, a;

      public Color(byte r, byte g, byte b, byte a = 255)
      {
        this.r = r; this.g = g; this.b = b; this.a = a;
      }
    }

    private struct Vector2
    {
      public float x, y;

      public Vector2(float x, float y)
      {
        this.x = x;
        this.y = y;
      }
    }

    private class Player
    {
      public string name;
      public Color color;
      public Vector2 position;
      public Vector2 velocity;
      public int hp;

      public Player(string name)
      {
        this.name = name;
      }
    }

    private struct SpawnMsg
    {
      public Color color;
      public Vector2 pos;
      public int hp;
    }

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

    public StickyPlatformsServer(ISceneHost scene)
    {
      mScene = scene;
      scene.AddProcedure("joinGame", joinGameProcedure);
      scene.AddProcedure("getPlayerList", ctx =>
      {
        ctx.SendValue(mPlayers.Values);
        return Task.FromResult(true);
      });
      scene.Disconnected.Add(onDisconnect);
      scene.AddRoute("spawn", onPlayerSpawn, options => { return options; });
    }
  }
}
