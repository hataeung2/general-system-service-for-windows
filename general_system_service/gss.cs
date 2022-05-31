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
using System.IO;

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
    String pathString = @"C:\GeneralSystemService";

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
      String appName = pathString + @"\awatchdog.exe";
      FileInfo fi = new FileInfo(appName);
      if (fi.Exists)
      {
        ApplicationLoader.StartProcessAndBypassUAC(appName, out pi);
      } else
      {
        throw new Exception("No \"awatchdog.exe\" file exist.");
      }
    }

    protected override void OnStart(string[] args)
    {
#if DEBUG
      System.Diagnostics.Debugger.Launch(); // for service debug
#endif
      try
      {
        createProcess();

        timer = new System.Timers.Timer();
        timer.Interval = 1000; // 1sec
        timer.Elapsed += (this.timer_Elapsed);
        timer.Start();
      }
      catch (Exception e)
      {
        System.IO.Directory.CreateDirectory(pathString);

        String file = pathString + @"\gss-log.txt";
        FileStream fs = File.Open(file, FileMode.OpenOrCreate);
        
        byte[] log = new UTF8Encoding(true).GetBytes(
          String.Format("{0}] {1}", System.DateTime.Now.ToString(), e.Message.ToString())
          );

        StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        sw.WriteLine(log);
        sw.Flush();
        sw.Close();
        fs.Close();

        Stop();
        return;
      }

    }

    protected override void OnStop()
    {
    }
  }
}
