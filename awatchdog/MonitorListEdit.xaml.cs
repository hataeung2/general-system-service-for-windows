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
using System.Windows.Shapes;

namespace awatchdog
{
  /// <summary>
  /// Interaction logic for MonitorListEdit.xaml
  /// </summary>
  public partial class MonitorListEdit : Window
  {
    Dictionary<string, ProcInfo> process_list;
    
    public MonitorListEdit(ref Dictionary<string, ProcInfo> pl)
    {
      InitializeComponent();

      var col_priority = new DataGridTextColumn();
      var col_filepath = new DataGridTextColumn();
      var col_name = new DataGridTextColumn();
      var col_status = new DataGridTextColumn();
      var col_checkalive = new DataGridTextColumn();
      this.dg.Columns.Add(col_priority);
      this.dg.Columns.Add(col_filepath);
      this.dg.Columns.Add(col_name);
      this.dg.Columns.Add(col_status);
      this.dg.Columns.Add(col_checkalive);
      col_priority.Binding = new Binding("priority");
      col_filepath.Binding = new Binding("filepath");
      col_name.Binding = new Binding("name");
      col_status.Binding = new Binding("status");
      col_checkalive.Binding = new Binding("checkalive");
      col_priority.Header = "Priority";
      col_filepath.Header = "FilePath";
      col_filepath.MinWidth = 340;
      col_name.Header = "Name";
      col_status.Header = "Status";
      col_checkalive.Header = "CheckAlive";

      set(ref pl);
    }

    public void set(ref Dictionary<string, ProcInfo> pl)
    {
      process_list = pl;
      prepare();
    }

    public void prepare()
    { 
      this.dg.Items.Clear();

      foreach (var p in process_list)
      {
        this.dg.Items.Add(p.Value);
      }
    }

    private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (Visibility.Collapsed == this.Visibility)
      {
        return;
      } else if (Visibility.Visible == this.Visibility)
      {
        this.Visibility = Visibility.Hidden;
        e.Cancel = true;
      }
    }

    private void onBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      e.Cancel = true;
    }

  }
}
