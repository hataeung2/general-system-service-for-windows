using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Threading;
using System.Diagnostics;

namespace awatchdog
{
  public class ProcInfo
  {
    public ProcInfo() { }
    public ProcInfo(ProcInfo pi)
    {
      priority = pi.priority;
      name = pi.name;
      filepath = pi.filepath;
      checkalive = pi.checkalive;
      status = pi.status;
      pid = pi.pid;
    }
    public uint priority { get; set; }
    public string name { get; set; }
    public string filepath { get; set; }
    public bool checkalive { get; set; }
    public string status { get; set; }
    public int pid { get; set; }
  }
  partial class ProcMgnt
  {
    String pathStr = @"C:\GeneralSystemService";
    MainWindow mw;
    public Dictionary<string, ProcInfo> process_list = new Dictionary<string, ProcInfo>();
    public System.Object sync_door = new System.Object();
    Thread t;
    public bool thread_run_cond = true;

    public void release()
    {
      thread_run_cond = false;
      if (null != t && t.IsAlive) t.Join();
      closeProcs();
    }
    public bool init(MainWindow mw)
    {
      this.mw = mw;
      bool res = getProcessList();
      if (res)
      {
        Dictionary<string, int> pid_list = new Dictionary<string, int>();
        // create process
        foreach (var p in process_list)
        {
          int pid = createProcess(p.Value.filepath, p.Value.name);
          pid_list.Add(p.Value.name, pid);
        }
        foreach (var pid in pid_list)
        {
          ProcInfo pi = process_list[pid.Key];
          pi.pid = pid.Value;
          process_list[pid.Key] = pi;
        }

        // start thread
        t = new Thread(new ThreadStart(run));
        t.Start();
      }
      return res;
    }
    private bool getProcessList()
    {
      // db에서 실행경로, 우선순위 읽어들임. 체크박스에 바인딩.
      // 체크 되어 있으면 죽으면 자동 실행. 아니면 그냥 프로세스이름, 상태 표시
      try
      {
        string strConn = String.Format(@"Data Source={0}\awatchdog.db", pathStr);
        using (SQLiteConnection conn = new SQLiteConnection(strConn))
        {
          conn.Open();
          string sql = "SELECT * FROM process_list ORDER BY priority ASC";
          SQLiteCommand cmd = new SQLiteCommand(sql, conn);
          SQLiteDataReader rdr = cmd.ExecuteReader();
          while (rdr.Read())
          {
            var pi = new ProcInfo();
            pi.priority = Convert.ToUInt32(rdr["priority"]);
            pi.filepath = rdr["filepath"].ToString();
            pi.name = rdr["name"].ToString();
            pi.status = rdr["status"].ToString();
            pi.checkalive = Convert.ToBoolean(rdr["checkalive"]);
            process_list.Add(pi.name, pi);
          }
          rdr.Close();
        }

        return true;
      }
      catch
      {
        return false;
      }
    }
    public void changeAliveCheckCond(string process_name, bool chkalive)
    {
      lock (sync_door)
      {
        ProcInfo pi;
        if (process_list.TryGetValue(process_name, out pi))
        {
          pi.checkalive = chkalive;
          if (false == pi.checkalive) {
            pi.status = "unknown";
          }
          process_list[process_name] = pi;
        }
      }
    }
    private int/*pid*/ createProcess(string filename, string process_name)
    {
      System.Diagnostics.ProcessStartInfo si = new System.Diagnostics.ProcessStartInfo();
      System.Diagnostics.Process p = new System.Diagnostics.Process();
      si.FileName = filename;
      
      p.StartInfo = si;
      try {
        p.Start();
        ProcInfo pi;
        if (process_list.TryGetValue(process_name, out pi)) {
          return p.Id;
        } else {
          return 0;
        }
      } catch {
        System.Diagnostics.Debug.WriteLine("process start failed");
        return 0;
      }
    }

    public void run()
    {
      Dictionary<string/*name*/, string/*status*/> stat_list = new Dictionary<string, string>();
      while (thread_run_cond)
      {
        lock (sync_door)
        {
          foreach (var p in process_list) { 
            if (0 == p.Value.pid)
            {
              continue;
            }
            if (p.Value.checkalive) {
              try {
                Process proc = Process.GetProcessById(p.Value.pid);
                stat_list[p.Value.name] = "alive";
              } catch {
                if ("opening" == p.Value.status) {
                  stat_list[p.Value.name] = "opening...";
                } else {
                  System.Diagnostics.Debug.WriteLine("process no exist with pid " + p.Value.pid.ToString());
                  stat_list[p.Value.name] = "dead";
                }
              }
            } else {
              stat_list[p.Value.name] = "unknown";
            }
          }


          foreach (var stat in stat_list) {
            ProcInfo pi = process_list[stat.Key];
            pi.status = stat.Value;
            if ("dead" == stat.Value) {
              int pid = createProcess(pi.filepath, pi.name);
              pi.pid = pid;
              pi.status = "opening";
            }
            process_list[stat.Key] = pi;
          }

          Thread.Sleep(500);

        }
      }
    }

    private void closeProcs()
    {
      lock (sync_door)
      {
        foreach (var p in process_list) {
          try {
            Process proc = Process.GetProcessById(p.Value.pid);
            proc.Kill();
          } catch {
            System.Diagnostics.Debug.WriteLine("process no exist with pid " + p.Value.pid.ToString());
          }
        }
      }
    }
  }
}
