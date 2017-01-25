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
    public void Run(IAppBuilder builder)
    {
      builder.SceneTemplate("gameSession", scene =>
      {
        new StickyPlatformsServer(scene);
      });
    }

  }

}

