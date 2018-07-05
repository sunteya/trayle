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
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Drawing;

namespace trayle
{
    public partial class MainWindow : Window
    {
        SynchronizationContext _syncContext;
        Setting _setting;
        Process process;

        public MainWindow()
        {
            _syncContext = SynchronizationContext.Current;

            try
            {
                LoadSetting();
                InitializeComponent();
                InitializeTrayIcon();
                InitializeData();

                ActionButton_Click(actionButton, new RoutedEventArgs());
                ToggleWindowState();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                Environment.Exit(1);
            }
        }

        void LoadSetting()
        {
            var baseDir = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(baseDir, "trayle.yml");

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException("Can not found trayle.yml file");
            }

            var input = new StreamReader(configFile);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            _setting = deserializer.Deserialize<Setting>(input);

            foreach (var item in _setting.items)
            {
                if (item.name == null)
                {
                    item.name = item.command;
                }
            }
        }

        void InitializeTrayIcon()
        {
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon
            {
                Icon = Properties.Resources.Main,
                Visible = true
            };
            ni.Click += (sender, args) => ToggleWindowState();
        }

        void ToggleWindowState()
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        void InitializeData()
        {
            this.Title = _setting.title;
            foreach (var item in _setting.items)
            {
                itemsComboBox.Items.Add(item.name);
            }
            var selectedIndex = _setting.items.FindIndex(item => item.selected);
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
            var item = _setting.items[itemsComboBox.SelectedIndex];
            var pos = item.command.IndexOf(' ');
            var name = item.command.Substring(0, pos);
            var arguments = item.command.Substring(pos + 1);

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = name,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = arguments,
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

            _syncContext.Post(_ =>
            {
                var keepScrolledToEnd = (outputTextBox.VerticalOffset + outputTextBox.ViewportHeight) >= outputTextBox.ExtentHeight;
                outputTextBox.AppendText(output + "\r\n");
                if (keepScrolledToEnd)
                {
                    outputTextBox.ScrollToEnd();
                }
            }, null);
        }
    }

    class Setting
    {
        public string title { get; set; } = "Trayle";
        public List<Item> items { get; set; } = new List<Item>();
    }

    class Item
    {
        public string name { get; set; }
        public string command { get; set; }
        public bool selected { get; set; } = false;
    }
}
