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
using System.IO;
using System.Diagnostics;
using YamlDotNet.RepresentationModel;
using System.Threading;

namespace trayle
{
    public partial class MainWindow : Window
    {
        SynchronizationContext _syncContext;
        string title = "Trayle";
        List<Item> items = new List<Item>();
        Process process;

        public MainWindow()
        {
            _syncContext = SynchronizationContext.Current;

            try
            {
                LoadConfig();
                InitializeComponent();
                InitializeData();

                ActionButton_Click(actionButton, new RoutedEventArgs());
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                Environment.Exit(1);
            }
        }

        void LoadConfig()
        {
            var baseDir = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(baseDir, "trayle.yml");

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException("Can not found trayle.yml file");
            }

            var yaml = new YamlStream();
            yaml.Load(new StreamReader(configFile));
            var mapping = (YamlMappingNode) yaml.Documents[0].RootNode;

            var titleKey = new YamlScalarNode("title");
            if (mapping.Children.ContainsKey(titleKey))
            {
                this.title = mapping.Children[titleKey].ToString();
            }

            var itemNodes = (YamlSequenceNode) mapping.Children[new YamlScalarNode("items")];
            foreach (YamlMappingNode itemNode in itemNodes)
            {
                var item = new Item();
                item.command = itemNode.Children[new YamlScalarNode("command")].ToString();

                var selectedKey = new YamlScalarNode("selected");
                if (itemNode.Children.ContainsKey(selectedKey))
                {
                    item.selected = itemNode.Children[selectedKey].ToString() == "true";
                }
                
                var nameKey = new YamlScalarNode("name");
                if (itemNode.Children.ContainsKey(nameKey))
                {
                    item.name = itemNode.Children[nameKey].ToString();
                }
                else
                {
                    item.name = item.command;
                }
                items.Add(item);
            }
        }

        void InitializeData()
        {
            foreach (var item in items)
            {
                itemsComboBox.Items.Add(item.name);
            }
            var selectedIndex = items.FindIndex(item => item.selected);
            itemsComboBox.SelectedIndex = selectedIndex == -1 ? 0 : selectedIndex;            
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (process == null)
            {
                SwitchStopUI();
                StartProcess();
                
            }
            else
            {
                StopProcess();
                SwitchStartUI();
            }
            
        }

        void SwitchStartUI()
        {
            actionButton.Content = "Start  ▶";
            itemsComboBox.IsEnabled = true;
        }

        void SwitchStopUI()
        {
            actionButton.Content = "Stop  ◼";
            itemsComboBox.IsEnabled = false;
            outputTextBox.Text = "";
        }

        void StopProcess()
        {
            if (process == null || process.HasExited)
            {
                return;
            }

            process.Kill();
            process = null;
        }


        void StartProcess()
        {
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gost.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    // CreateNoWindow = true,
                    Arguments = "-L socks://:9091 -L http://:9090 -D",
                }
            };

            process.OutputDataReceived += (sender, args) => Display(args.Data);
            process.ErrorDataReceived += (sender, args) => Display(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            new JobManagement.Job().AddProcess(process.Id);
        }

        void Display(string output)
        {
            if (output == null)
            {
                return;
            }

            Debug.Print(output);
            _syncContext.Post(_ =>
            {
                outputTextBox.AppendText(output + "\r\n");
                outputTextBox.ScrollToEnd();
            }, null);
        }
    }

    class Item
    {
        public string name;
        public string command;
        public bool selected = false;
    }
}
