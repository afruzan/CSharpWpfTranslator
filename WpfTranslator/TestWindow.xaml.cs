using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace WpfTranslator
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultTextBox.Text = new LocalizerCSharpSyntaxRewriter(s => true, s => "^" + s + "$", "I18n.Get", "I18nDescription", new[] { "Log" }).LocalizeCode(SourceTextBox.Text);
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = "ERROR:\r\n" + ex.ToString();
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultTextBox.Text = new LocalizerCSharpSyntaxRewriter(s => false, s => "^" + s + "$", "I18n.Get", "I18nDescription", new[] { "Log" }).LocalizeCode(SourceTextBox.Text);
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = "ERROR:\r\n" + ex.ToString();
            }
        }
    }
}
