using CSharpTranslator;
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
            var code = @"using Vira;

namespace System.ComponentModel
{
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string _resourceKey;
        public LocalizedDescriptionAttribute(string resourceKey)
        {
            _resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                string displayName = LocalizationManager.GetString(""STRING!"") ?? $""ONE{_resourceKey}TWO"";

                return string.IsNullOrEmpty(displayName)
                    ? string.Format(@""[[{0}]]"", _resourceKey)
                    : displayName;

                return string.IsNullOrEmpty(displayName)
                    ? string.Format($@""[[{0}]]"", _resourceKey)
                    : displayName;

                return string.IsNullOrEmpty(displayName)
                    ? string.Format(@""[[{0}]]
Line2
"", _resourceKey)
                    : displayName;
            }
        }
    }

}
";

            var sucess = SyntaxFactory.ParseSyntaxTree(code).TryGetRoot(out var code_parsed);
            if (sucess)
            {
                var newCode = new LocalizerCSharpSyntaxRewriter().Visit(code_parsed)?.ToFullString();

                Debugger.Break();
            }

            Debugger.Break();

        }
    }
}
