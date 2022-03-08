using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

// install
// installutil general_system_service.exe
// uninstall
// installutil /u general_system_service.exe

namespace general_system_service
{
  public partial class gss : ServiceBase
  {
    public gss()
    {
      InitializeComponent();
    }
    private Timer timer;
    protected ApplicationLoader.PROCESS_INFORMATION pi;
    protected Process p;

    protected void timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      try {
        p = Process.GetProcessById((int)pi.dwProcessId);
      } catch (InvalidOperationException) {
        createProcess();
      } catch (ArgumentException) {
        createProcess();
      }
    }

    protected void createProcess()
    {
      String appName = "awatchdog.exe";
      ApplicationLoader.StartProcessAndBypassUAC(appName, out pi);
    }

    protected override void OnStart(string[] args)
    {
      createProcess();
#if DEBUG 
      System.Diagnostics.Debugger.Launch(); // for service debug
#endif
      timer = new System.Timers.Timer();
      timer.Interval = 1000; // 1sec
      timer.Elapsed += (this.timer_Elapsed);
      timer.Start();
    }

    protected override void OnStop()
    {
    }
  }
}
