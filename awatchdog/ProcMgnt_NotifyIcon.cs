using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace awatchdog
{
  partial class ProcMgnt
  {
    NotifyIcon ni = new NotifyIcon();

    private void destroyNotifyIcon()
    {
      ni.Dispose();
    }
    private void createNotifyIcon()
    {
      // NotifyIcon
      ni.Icon = new Icon("awatchdog.ico");
      ni.Visible = true;
      ni.Text = "awatchdog";
      ni.ContextMenu = setContextMenu(ni);
      
    }

    private ContextMenu setContextMenu(NotifyIcon ni)
    {
      ContextMenu cm = new ContextMenu();
      MenuItem show = new MenuItem();
      show.Text = "Show";
      show.Click += delegate (object click, EventArgs ea)
      {
        mw.Show();
        mw.WindowState = System.Windows.WindowState.Normal;
      };
      cm.MenuItems.Add(show);
      MenuItem hide = new MenuItem();
      hide.Text = "Hide";
      hide.Click += delegate (object click, EventArgs ea)
      {
        mw.Hide();
        mw.WindowState = System.Windows.WindowState.Minimized;
      };
      cm.MenuItems.Add(hide);
      return cm;
    }
  }
}
