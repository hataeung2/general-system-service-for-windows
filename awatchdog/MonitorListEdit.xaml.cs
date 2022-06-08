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
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;
using System.Data.SQLite;


namespace awatchdog
{
  /// <summary>
  /// Interaction logic for MonitorListEdit.xaml
  /// </summary>
  public partial class MonitorListEdit : Window
  {
    Dictionary<uint, ProcInfo> process_list_local = new Dictionary<uint, ProcInfo>();
    ProcInfo selectedItem = null;
    const int kUnselected = -1;
    int selectedIdx = kUnselected;

    public MonitorListEdit(ref Dictionary<string, ProcInfo> pl)
    {
      InitializeComponent();

      var col_priority = new DataGridTextColumn();
      var col_filepath = new DataGridTextColumn();
      var col_name = new DataGridTextColumn();
      //var col_status = new DataGridTextColumn();
      var col_checkalive = new DataGridTextColumn();
      this.dg.Columns.Add(col_priority);
      this.dg.Columns.Add(col_filepath);
      this.dg.Columns.Add(col_name);
      //this.dg.Columns.Add(col_status);
      this.dg.Columns.Add(col_checkalive);
      col_priority.Binding = new Binding("priority");
      col_filepath.Binding = new Binding("filepath");
      col_name.Binding = new Binding("name");
      //col_status.Binding = new Binding("status");
      col_checkalive.Binding = new Binding("checkalive");
      col_priority.Header = "Priority";
      col_filepath.Header = "FilePath";
      col_filepath.MinWidth = 340;
      col_name.Header = "Name";
      //col_status.Header = "Status";
      col_checkalive.Header = "CheckAlive";

      set(ref pl);
    }

    public void set(ref Dictionary<string, ProcInfo> pl)
    {
      foreach (var p in pl)
      {
        var copied = new ProcInfo(p.Value);
        copied.status = "-";
        process_list_local.Add(p.Value.priority, copied);
      }
      prepare();
    }

    public void prepare()
    {
      foreach (var p in process_list_local)
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
        process_list_local.Clear();
        this.dg.Items.Clear();

        this.Visibility = Visibility.Hidden;
        e.Cancel = true;
      }
    }

    private void onBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      e.Cancel = true;
    }

    private void onSelect(object sender, SelectionChangedEventArgs e)
    {
      DataGrid dg = (sender as DataGrid);
      selectedItem = dg.SelectedItem as ProcInfo;
      selectedIdx = dg.SelectedIndex;

      if (kUnselected == selectedIdx || null == selectedItem) return;

      tb_Priority.Text = selectedItem.priority.ToString();
      tb_FilePath.Text = selectedItem.filepath;
      tb_Name.Text = selectedItem.name;
      cb_CheckAlive.IsChecked = selectedItem.checkalive;
    }

    private void onClickNew(object sender, RoutedEventArgs e)
    {
      var name_to_use = tb_Name.Text;
      foreach (var n in process_list_local)
      {
        if (n.Value.name == name_to_use)
        {
          MessageBox.Show("'name' identifier duplicate. use different name for each items.");
          return;
        }
      }
      selectedItem = null;
      selectedIdx = kUnselected;

      var pi = new ProcInfo();
      pi.priority = (uint)process_list_local.Count + 1;
      pi.name = name_to_use;
      pi.filepath = tb_FilePath.Text;
      pi.checkalive = (bool)cb_CheckAlive.IsChecked;
      
      process_list_local.Add(pi.priority, pi);
      dg.Items.Add(pi);

      tb_Priority.Text = pi.priority.ToString();
    }

    /**
     * @brief: 
     * delete all the items in awatchdog.db and re-insert from 'process_list_local' 
     * then close 'awatchdog' for restarting by the 'gss'
     */
    private void onClickApply(object sender, RoutedEventArgs e)
    {
      MessageBoxResult ans = MessageBox.Show("'awatchdog' is going to be restarted. Are you sure to proceed?", "Warning", MessageBoxButton.YesNo);
      if (MessageBoxResult.Yes == ans)
      {
        try
        {
          const int kQueryFailed = -1;
          string strConn = String.Format(@"Data Source={0}\awatchdog.db", ProcMgnt.pathStr);
          using (SQLiteConnection conn = new SQLiteConnection(strConn))
          {
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
              string sql = "DELETE FROM process_list";
              SQLiteCommand cmd = new SQLiteCommand(sql, conn);
              int qry_res = cmd.ExecuteNonQuery();
              if (kQueryFailed == qry_res)
              {
                trans.Rollback();
                MessageBox.Show("Failed to DELETE the items.");
              }

              foreach (var p in process_list_local)
              {
                sql = String.Format(@"INSERT INTO process_list VALUES({0}, '{1}', '{2}', '', {3})",
                  p.Value.priority, p.Value.name, p.Value.filepath, p.Value.checkalive
                );
                try
                {
                  cmd = new SQLiteCommand(sql, conn);
                }
                catch
                {
                  MessageBox.Show("QUERY failed. Please check if the policy violation.");
                }
                if (kQueryFailed == (qry_res = cmd.ExecuteNonQuery()))
                {
                  break;
                }
              }
              if (kQueryFailed == qry_res)
              {
                trans.Rollback();
                MessageBox.Show("Failed to INSERT one of the items.");
              } 
              else
              {
                trans.Commit();
              }
            }
            conn.Close();
            Close();
            App.Current.MainWindow.Close();
          }
        }
        catch
        {
          MessageBox.Show("Failed to open the database.");
        }
      }
    }
    private void onClickDelete(object sender, RoutedEventArgs e)
    {
      if (kUnselected == selectedIdx) return;

      MessageBoxResult res = MessageBox.Show("Are you sure to delete?", "", MessageBoxButton.YesNo);
      if (MessageBoxResult.Yes == res)
      {
        uint selected = (uint)selectedIdx + 1;
        if (process_list_local.TryGetValue(selected, out selectedItem))
        {
          uint cnt_to_reach = (uint)process_list_local.Count;
          process_list_local.Remove(selected);
          for (uint idx = selected + 1; idx <= cnt_to_reach; ++idx)
          {
            process_list_local[idx].priority -= 1;
            process_list_local[idx - 1] = process_list_local[idx];
            process_list_local.Remove(idx);
          }
        }
        this.dg.Items.Remove(selectedItem);
        setFocus(dg.Items.Count - 1);
      }
    }
    private void onMouseDoubleClick_FilePath(object sender, MouseButtonEventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog();
      dlg.Filter = "EXE files (*.exe)|*.exe|Batch files (*.bat)|*.bat|All files (*.*)|*.*";
      if (true == dlg.ShowDialog())
      {
        this.tb_FilePath.Text = dlg.FileName;
      }
    }

    private void onFilepathTxtChanged(object sender, TextChangedEventArgs e)
    {
      if (null == selectedItem) return;
      selectedItem.filepath = (sender as TextBox).Text;
      process_list_local[selectedItem.priority].filepath= selectedItem.filepath;
      this.dg.Items.Refresh();
    }
    private void onNameTxtChanged(object sender, TextChangedEventArgs e)
    {
      if (null == selectedItem) return;
      selectedItem.name = (sender as TextBox).Text;
      process_list_local[selectedItem.priority].name = selectedItem.name;
      this.dg.Items.Refresh();
    }
    private void onCheckAliveClick(object sender, RoutedEventArgs e)
    {
      if (null == selectedItem) return;
      bool ischecked = (bool)(sender as CheckBox).IsChecked;
      selectedItem.checkalive = ischecked;
      process_list_local[selectedItem.priority].checkalive = selectedItem.checkalive;
      this.dg.Items.Refresh();
    }

    private void swapItems(uint idxDn, uint idxUp)
    {

      ProcInfo piDn;
      process_list_local.TryGetValue(idxDn, out piDn);
      process_list_local.Remove(piDn.priority);
      this.dg.Items.Remove(piDn);
      ProcInfo piUp;
      process_list_local.TryGetValue(idxUp, out piUp);
      process_list_local.Remove(piUp.priority);
      this.dg.Items.Remove(piUp);

      piDn.priority -= 1;
      piUp.priority += 1;

      process_list_local.Add(piDn.priority, piDn);
      this.dg.Items.Add(piDn);
      process_list_local.Add(piUp.priority, piUp);
      this.dg.Items.Add(piUp);

      dg.Items.SortDescriptions.Clear();
      dg.Items.SortDescriptions.Add(new SortDescription("priority", ListSortDirection.Ascending));
      dg.Items.Refresh();
    }
    private void onClickUp(object sender, RoutedEventArgs e)
    {
      if (kUnselected != selectedIdx && 0 < selectedIdx)
      {
        var idxafter = selectedIdx - 1;
        swapItems((uint)selectedIdx + 1, (uint)selectedIdx);
        setFocus(idxafter);
      }
    }
    private void onClickDown(object sender, RoutedEventArgs e)
    {
      if (kUnselected != selectedIdx && process_list_local.Count - 1 > selectedIdx)
      {
        var idxafter = selectedIdx + 1;
        swapItems((uint)selectedIdx + 2, (uint)selectedIdx + 1);
        setFocus(idxafter);
      }
    }
    private void setFocus(int idx)
    {
      selectedIdx = idx;
      selectedItem = dg.Items[selectedIdx] as ProcInfo;
      DataGridRow row = dg.ItemContainerGenerator.ContainerFromIndex(selectedIdx) as DataGridRow;
      if (row == null)
      {
        /* bring the data item (Product object) into view
         * in case it has been virtualized away */
        dg.ScrollIntoView(selectedItem);
        row = dg.ItemContainerGenerator.ContainerFromIndex(selectedIdx) as DataGridRow;
      }
      row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
      tb_Priority.Text = (selectedIdx + 1).ToString();
    }

  }
}
