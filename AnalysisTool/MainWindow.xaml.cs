﻿using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AnalysisTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OpenFolderDialog GetPath;
        FileFolder FileInfo;
        public MainWindow()
        {
            InitializeComponent();
            Setbinding();
         
        }
        private void Setbinding()
        {
            GetPath = new OpenFolderDialog();
            FileInfo=new FileFolder();
            Binding bindingpath = new Binding();
            bindingpath.Source=FileInfo;
            bindingpath.Path = new PropertyPath("Path");
            this.path.SetBinding(TextBox.TextProperty, bindingpath);

            Binding bindingNum = new Binding();
            bindingNum.Source = FileInfo;
            bindingNum.Path = new PropertyPath("Num");
            this.numlabel.SetBinding(Label.ContentProperty,bindingNum);

            this.fList.ItemsSource = FileInfo.FileList;

            //DataGrid绑定数据
            this.data_grid.ItemsSource=FileInfo.Nodes;
        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            FileInfo.FileList.Clear();
            GetPath.ShowDialog();
            FileInfo.Path = GetPath.FolderName;
            if(FileInfo.Path!="")
            {
                foreach (var file in Directory.GetFiles(FileInfo.Path))
                {
                    if (System.IO.Path.GetExtension(file).ToLower() == ".log")
                    {
                        FileInfo.FileList.Add(System.IO.Path.GetFileName(file).ToString());
                    }
                }
                FileInfo.Num = FileInfo.FileList.Count;
            }
        }

        private void Cleardatabutton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("原始数据即将被清楚并无法恢复！");
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
           FileInfo.AnalysisDataFile();
        }

    }

    public class FileFolder:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _path;
        private int _num;
        private ObservableCollection<string> _filelist=new ObservableCollection<string>();
        private ObservableCollection<NodeInfo> _nodes=new ObservableCollection<NodeInfo>();
        public string Path
        { 
            get { return _path; } 
            set 
            { 
                _path = value; 
                if(this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this,new PropertyChangedEventArgs("Path"));
                }
            } 
        }
        public int Num
        {
            get { return _num; }
            set
            {
                _num = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Num"));
                }
            }
        }
        public ObservableCollection<string> FileList 
        { 
            get { return _filelist; } 
            set 
            { 
                _filelist = value;
            } 
        }
        public ObservableCollection<NodeInfo> Nodes
        {
            get { return _nodes; }
            set
            {
                _nodes = value;
            }
        }
        public  void AnalysisDataFile()
        {
            Nodes.Clear();
            if(FileList.Count>0)
            {
                foreach (var file in FileList)
                {
                    string path= this.Path+"\\"+file;
                    //string path = "C:\\Users\\wpch\\Desktop\\测\\runner_C_2024_06_14-11_29_18.log";
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    string[] lines = File.ReadAllLines(path, System.Text.Encoding.GetEncoding(936));
                    IEnumerable<string> time = from line in lines let fields = line.Split('\t') select fields[0];
                    //获取写入时间
                    int[] Starttime = time.First<string>().Gettime();
                    int[] Stoptime = time.Last<string>().Gettime();
                    //获取序列列表
                    IEnumerable<string> Tasklist = (from line in lines let fields = line.Split("\t") select fields[2]).Distinct<string>();
                    //获取序列节点列表
                    IEnumerable<string> Nodelist = (from line in lines let fields = line.Split("\t") select fields[3]).Distinct<string>();
                    //建立数据对象 并释放原数据
                    IEnumerable<itemInfo> datas = from line in lines let fields = line.Split('\t') select (itemInfo)$"{fields[2]},{fields[3]},{fields[8]}";
                    //建立节点数据信息
                    var nodegroup = from data in datas group data by data.parentName;
                    foreach (var g in nodegroup)
                    {
                        if (g != null)
                        {
                            var node = from ng in g group ng by ng.itemName into s select new { name = s.Key, Average = s.Average(ng => ng.time), Max = s.Max(ng => ng.time), Min = s.Min(ng => ng.time) };
                            foreach (var s2 in node)
                            {
                                NodeInfo info = new NodeInfo();
                                info.TaskName = g.Key;
                                info.NodeName = s2.name;
                                info.time_average = s2.Average;
                                info.time_max = s2.Max;
                                info.time_min = s2.Min;
                                this.Nodes.Add(info);
                            }
                        }
                    }
                }
            }
        }
    }
    public class NodeInfo
    {
        public string NodeName { get; set; }
        public string TaskName { get; set; }
        public float time_average {get; set; }
        public float time_max {get; set; }
        public float time_min {get; set; }
    }
    class itemInfo
    {
        public string parentName { get; set; }
        public string itemName { get; set; }
        public float time { get; set; }
        public static explicit operator itemInfo(string str)
        {
            itemInfo info = new itemInfo();
            info.parentName = str.Split(",")[0];
            info.itemName = str.Split(",")[1];
            info.time = float.Parse(str.Split(",")[2]);
            return info;
        }
    }

    static class  StringExtensionsMethod
    {
        //2024-6-14_11:41:53.862
        public static int[] Gettime(this string str)
        {
            int[] time = new int[7];
            time[0] = int.Parse(str.Split("_")[0].Split("-")[0]);
            time[1] = int.Parse(str.Split("_")[0].Split('-')[1]);
            time[2] = int.Parse(str.Split("_")[0].Split('-')[2]);
            time[3] = int.Parse(str.Split("_")[1].Split(':')[0]);
            time[4] = int.Parse(str.Split("_")[1].Split(':')[1]);
            time[5] = int.Parse(str.Split("_")[1].Split(':')[2].Split(".")[0]);
            time[6] = int.Parse(str.Split("_")[1].Split(':')[2].Split(".")[1]);
            return time;
        }

    }
}