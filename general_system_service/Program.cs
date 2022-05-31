using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace general_system_service
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      Console.WriteLine("gss start");
      ServiceBase[] ServicesToRun;
      ServicesToRun = new ServiceBase[]
      {
         new gss()
      };
      ServiceBase.Run(ServicesToRun);
      Console.WriteLine("gss stop");
    }
  }
}
