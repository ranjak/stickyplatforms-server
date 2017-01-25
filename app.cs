using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Plugins;
using Stormancer.Server;

namespace stickyplatforms_server
{
  public class App
  {
    private struct Color
    {
      public byte r, g, b, a;

      public Color(byte r, byte g, byte b, byte a=0)
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

    public void Run(IAppBuilder builder)
    {
      builder.SceneTemplate("gameSession", scene =>
      {
        scene.Connecting.Add(client =>
        {
          // We need unique names
          string name = client.GetUserData<string>();

          foreach (KeyValuePair<long, Player> player in mPlayers) {
            if (player.Value.name == name) {
              throw new ClientException("The username you chose (\"" + name + "\") is already in use.");
            }
          }

          return Task.FromResult(true);
        });

        scene.Connected.Add(client =>
        {
          string name = client.GetUserData<string>();
          mPlayers[client.Id] = new Player(name);

          return Task.FromResult(true);
        });

      });
    }

    // Peer ID => Player
    private Dictionary<long, Player> mPlayers;
    private string mMapFilename;
  }

}

