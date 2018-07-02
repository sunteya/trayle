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

namespace trayle
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string title = "Trayle";
        List<Item> items = new List<Item>();
        bool running = false;

        public MainWindow()
        {
            LoadConfig();
            InitializeComponent();
            LoadData();
        }

        private void LoadConfig()
        {
            var baseDir = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(baseDir, "trayle.yml");

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

        private void LoadData()
        {
            foreach (var item in items)
            {
                itemsComboBox.Items.Add(item.name);
            }
            var selectedIndex = items.FindIndex(item => item.selected);
            itemsComboBox.SelectedIndex = selectedIndex == -1 ? 0 : selectedIndex;            
            actionButton_Click(actionButton, new RoutedEventArgs());
        }

        private void actionButton_Click(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                running = false;
                actionButton.Content = "Start";
                itemsComboBox.IsEnabled = true;
            }
            else
            {
                running = true;
                actionButton.Content = "Stop";
                itemsComboBox.IsEnabled = false;
            }
            
        }
    }

    class Item
    {
        public string name;
        public string command;
        public bool selected = false;
    }
}
