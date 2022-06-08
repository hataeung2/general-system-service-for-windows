using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net;
using System.Net.Http;


namespace awatchdog
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    const uint kFontSize = 11;

    static ProcMgnt pm;
    public Dictionary<string, TextBlock> stat_txt_list = new Dictionary<string, TextBlock>();
    private System.Timers.Timer timer;


    public static RoutedCommand cmd_Hide = new RoutedCommand();
    public static RoutedCommand cmd_Show = new RoutedCommand();
    public static RoutedCommand cmd_Config = new RoutedCommand();

    MouseGesture configGesture = new MouseGesture(MouseAction.LeftDoubleClick, ModifierKeys.Control);
    //System.Windows.Forms.NotifyIcon ni = null;
    //bool niExist = false;

    MonitorListEdit list_editor;

    public MainWindow()
    {
      InitializeComponent();
      this.g1.MouseMove += this.onMouseMove;
      this.g1.MouseRightButtonUp += this.onMouseRightButtonUp;
      cmd_Hide.InputGestures.Add(new KeyGesture(Key.H, ModifierKeys.Control));
      cmd_Show.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
      cmd_Config.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Control));
      CommandBindings.Add(new CommandBinding(cmd_Hide, onCmdHide));
      CommandBindings.Add(new CommandBinding(cmd_Show, onCmdShow));
      CommandBindings.Add(new CommandBinding(cmd_Config, onCmdConfig));
    }

    public async Task<bool> isInternetConnected()
    {
      try
      {
        using (var client = new HttpClient())
        {
          var uri = "http://google.com";
          var response = await client.GetAsync(uri);
          response.EnsureSuccessStatusCode();
          if (HttpStatusCode.OK != response.StatusCode)
          {
            return false;
          }
          string responseBody = await response.Content.ReadAsStringAsync();
          //string responseBody = await client.GetStringAsync(uri);

          Console.WriteLine(responseBody);
          return true;
        }
      }
      catch (HttpRequestException e)
      {
        Console.WriteLine(e.Message);
        return false;
      }
    }
    private void onInitialized(object sender, EventArgs e)
    {
      // mutext for only process
      bool flagMutex;
      Mutex m_hMutex = new Mutex(true, "awatchdog.exe", out flagMutex);
      if (flagMutex)
      {
        m_hMutex.ReleaseMutex();
      }
      else
      {
        MessageBox.Show("Another awatchdog.exe process is being used", "Error");
        Close(); return;
      }

      // internet access check
      var task = Task.Run(async () => await isInternetConnected());
      var res_connected = task.GetAwaiter();
      if (false == res_connected.GetResult())
      {
        MessageBox.Show("Internet should be connected", "Error");
        Close(); return;
      }

      // start initialize
      System.Diagnostics.Debug.WriteLine("initialized");
      pm = new ProcMgnt();
      list_editor = new MonitorListEdit(ref pm.process_list);
      
      bool res = pm.init(this);
      if (false == res)
      {
        MessageBox.Show("Process management object open failed.");
      }
      this.Title = "awatchdog";
      prepareCtrls();

      timer = new System.Timers.Timer(1000);
      timer.Elapsed += onTimerStatUpdate;
      timer.AutoReset = true;
      timer.Start();
    }
    private void onTimerStatUpdate(Object src, System.Timers.ElapsedEventArgs e)
    {
      this.g1.Dispatcher.Invoke(
      (ThreadStart)(() => {
        foreach (var tb in stat_txt_list)
        {
          tb.Value.Text = pm.process_list[tb.Key].status;
        }
      }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void prepareCtrls()
    {
      //https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-create-a-grid-element?view=netframeworkdesktop-4.8

      var row = new RowDefinition();
      row.MinHeight = 30;
      row.MaxHeight = 30;
      g1.RowDefinitions.Add(row);
      string[] colname = { "priority", "name", "status", "checkalive" };
      for (int idx = 0; idx < 4; ++idx)
      {
        g1.ColumnDefinitions.Add(new ColumnDefinition());
        TextBlock tb = new TextBlock();
        tb.Text = colname[idx];
        tb.FontSize = kFontSize;
        tb.FontWeight = FontWeights.Bold;
        
        var border = new Border();
        border.BorderThickness = new Thickness(1, 1, 1, 2);
        border.BorderBrush = Brushes.Black;
        border.Child = tb;
        Grid.SetColumn(border, idx);
        Grid.SetRow(border, 0);

        g1.Children.Add(border);
      }

      int rowno = 1;
      uint min_height = 40;
      uint max_height = 50;
      lock(pm.sync_door)
      {
        foreach (var pi in pm.process_list)
        {
          row = new RowDefinition();
          row.MinHeight = min_height;
          row.MaxHeight = max_height;
          g1.RowDefinitions.Add(row);

          TextBlock col1 = new TextBlock();
          col1.FontSize = kFontSize;
          col1.Text = Convert.ToString(pi.Value.priority);
          Grid.SetColumn(col1, 0);
          Grid.SetRow(col1, rowno);
          g1.Children.Add(col1);

          TextBlock col2 = new TextBlock();
          col2.FontSize = kFontSize;
          col2.Text = pi.Value.name;
          Grid.SetColumn(col2, 1);
          Grid.SetRow(col2, rowno);
          g1.Children.Add(col2);

          TextBlock col3 = new TextBlock();
          col3.FontSize = kFontSize;
          col3.Text = pi.Value.status;
          Grid.SetColumn(col3, 2);
          Grid.SetRow(col3, rowno);
          g1.Children.Add(col3);
          stat_txt_list.Add(pi.Value.name, col3);

          CheckBox cb = new CheckBox();
          cb.FontSize = kFontSize;
          cb.IsChecked = pi.Value.checkalive;
          cb.Checked += onCheckAliveChkboxChecked;
          cb.Unchecked += onCheckAliveChkboxUnchecked;
          cb.Name = pi.Key; // must started with char or '_'
          cb.Content = pi.Key;
          Grid.SetColumn(cb, 3);
          Grid.SetRow(cb, rowno);
          g1.Children.Add(cb);

          ++rowno;
        }
      }

      // window size set
      Application.Current.MainWindow.Height = rowno * max_height + max_height;

      g1.LayoutUpdated += new EventHandler(onStatusUpdate);
    }
    private void onStatusUpdate(object sender, EventArgs e)
    {
      foreach (var tb in stat_txt_list)
      {
        if ("dead" == tb.Value.Text)
        {
          tb.Value.Foreground = Brushes.MediumVioletRed;
        }
        else if ("unknown" == tb.Value.Text)
        {
          tb.Value.Foreground = Brushes.DarkGray;
        }
        else if ("opening" == tb.Value.Text)
        {
          tb.Value.Foreground = Brushes.DarkBlue;
        }
        else
        {
          tb.Value.Foreground = Brushes.Black;
        }
      }

    }
    private void onCheckAliveChkboxUnchecked(object sender, RoutedEventArgs e)
    {
      CheckBox cb = sender as CheckBox;
      pm.changeAliveCheckCond(cb.Name, false);
      this.g1.Dispatcher.Invoke(
        (ThreadStart)(() => {
          TextBlock tb = stat_txt_list[cb.Name];
          tb.Text = pm.process_list[cb.Name].status;
          stat_txt_list[cb.Name] = tb;
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void onCheckAliveChkboxChecked(object sender, RoutedEventArgs e)
    {
      CheckBox cb = sender as CheckBox;
      pm.changeAliveCheckCond(cb.Name, true);
      this.g1.Dispatcher.Invoke(
        (ThreadStart)(() => {
          TextBlock tb = stat_txt_list[cb.Name];
          tb.Text = pm.process_list[cb.Name].status;
          stat_txt_list[cb.Name] = tb;
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void onClosed(object sender, EventArgs e)
    {
      //destroyNotifyIcon();
      list_editor.Close();

      if (null != pm)
      {
        pm.release();
      }
    }

    private void onMouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        this.DragMove();
      }
    }
    private void onMouseRightButtonUp(object sender, MouseEventArgs e)
    {
      // context menu?
      if (e.RightButton == MouseButtonState.Released)
      {

      }
    }
    private void onMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (configGesture.Matches(null, e))
      {
        list_editor.set(ref pm.process_list);
        list_editor.Show();
      }
      //if (hideGesture.Matches(null, e))
      //{
      //  Hide();
      //  if (false == niExist) createNotifyIcon();
      //}
    }

    private void destroyNotifyIcon()
    {
      //if (null != ni) ni.Dispose();
      //niExist = false;
    }
    public void createNotifyIcon()
    {
      //ni = new System.Windows.Forms.NotifyIcon();
      //// NotifyIcon
      //ni.Icon = new System.Drawing.Icon("awatchdog.ico");
      //ni.Visible = true;
      //ni.Text = "awatchdog";
      //ni.ContextMenu = setContextMenu(ni);
      //niExist = true;
    }

    private System.Windows.Forms.ContextMenu setContextMenu(System.Windows.Forms.NotifyIcon ni)
    {
      System.Windows.Forms.ContextMenu cm = new System.Windows.Forms.ContextMenu();
      System.Windows.Forms.MenuItem show = new System.Windows.Forms.MenuItem();
      show.Text = "Show";
      show.Click += delegate (object click, EventArgs ea)
      {
        Show();
        WindowState = System.Windows.WindowState.Normal;
        //destroyNotifyIcon();
      };
      cm.MenuItems.Add(show);
      System.Windows.Forms.MenuItem hide = new System.Windows.Forms.MenuItem();
      hide.Text = "Hide";
      hide.Click += delegate (object click, EventArgs ea)
      {
        Hide();
        WindowState = System.Windows.WindowState.Minimized;
      };
      cm.MenuItems.Add(hide);
      return cm;
    }

    private void onCmdHide(object sender, ExecutedRoutedEventArgs e)
    {
      this.Left = -this.Width;
      this.Top = -this.Height;
      this.Opacity = 10;
    }
    private void onCmdShow(object sender, ExecutedRoutedEventArgs e)
    {
      this.Left = 0;
      this.Top = 0;
      this.Opacity = 100;
    }
    private void onCmdConfig(object sender, ExecutedRoutedEventArgs e)
    {
      list_editor.set(ref pm.process_list);
      list_editor.Show();
    }


  }
}
