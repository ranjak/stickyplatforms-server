using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
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

    // Map file for the scene
    private const string mMapFilename = "assets/maps/multiplayer.tmx";
    // Peer ID => Player
    private ConcurrentDictionary<long, Player> mPlayers;

    Task onConnecting(IScenePeerClient client)
    {
      // We need unique names
      string name = client.GetUserData<string>();

      foreach (KeyValuePair<long, Player> player in mPlayers)
      {
        if (player.Value.name == name)
        {
          throw new ClientException("The username you chose (\"" + name + "\") is already in use.");
        }
      }

      return Task.FromResult(true);
    }

    Task onConnected(IScenePeerClient client)
    {
      string name = client.GetUserData<string>();
      mPlayers[client.Id] = new Player(name);

      return Task.FromResult(true);
    }

    Task getMapProcedure(RequestContext<IScenePeerClient> ctx)
    {
      ctx.SendValue(mMapFilename);
      return Task.FromResult(true);
    }

    public StickyPlatformsServer(ISceneHost scene)
    {
      scene.Connecting.Add(onConnecting);
      scene.Connected.Add(onConnected);
      scene.AddProcedure("getMapFilename", getMapProcedure);
    }
  }
}
