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


namespace awatchdog
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    ProcMgnt pm;
    const uint kFontSize = 16;
    public Dictionary<string, TextBlock> stat_txt_list = new Dictionary<string, TextBlock>();
    private System.Timers.Timer timer;

    public MainWindow()
    {
      InitializeComponent();
      this.g1.MouseMove += this.onMouseMove;
    }

    private void onActivated(object sender, EventArgs e)
    {
      System.Diagnostics.Debug.WriteLine("activated");
    }

    private void onInitialized(object sender, EventArgs e)
    {
      System.Diagnostics.Debug.WriteLine("initialized");
      pm = new ProcMgnt();
      bool res = pm.init(this);
      if (false == res)
      {
        MessageBox.Show("Process management object open failed.");
      }

      this.Title = "awatchdog";
      prepareCtrls();
      timer = new System.Timers.Timer(1000);
      timer.Elapsed += onTimer;
      timer.AutoReset = true;
      timer.Start();
    }
    private void onTimer(Object src, System.Timers.ElapsedEventArgs e)
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
      pm.release();
    }

    private void onMouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        this.DragMove();
      }
    }
  }
}
