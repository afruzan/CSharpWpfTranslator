using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xaml;

namespace WpfTranslator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = MessageBox.Show("Are you sure to start? xaml files will be overrited!", "Confirm", MessageBoxButton.YesNo);
            if (dialog == MessageBoxResult.Yes)
            {
                Button1.IsEnabled = false;
                var dir = TextBox1.Text;
                try
                {
                    await Task.Run(() => LocalizeProject(dir, 100,
                        (c, t) => Dispatcher.InvokeAsync(() =>
                        {
                            TextBlock1.Text = $"{c} of {t} Processed.";
                        })));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error");
#if DEBUG
                    throw;
#endif
                }
                MessageBox.Show("Done!", "Done");
                Button1.IsEnabled = true;
            }
        }

        private async Task LocalizeProject(string dir, int threadCount, Action<int, int> onProcessed)
        {
            var xaml_files = Directory.EnumerateFiles(dir, "*.xaml", SearchOption.AllDirectories).ToArray();

            int counter = 0;
            int total = xaml_files.Length + 3;
            onProcessed(0, total);

            string resources = "";
            string resources_en = "";
            string resources_ar = "";
            var concat_lock = new object();

            await Parallel.ForEachAsync(xaml_files, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, async (file, stop) =>
            {
                //await Task.WhenAll(xaml_files.Chunk(xaml_files.Length / threadCount).Select(files => Task.Run(async () =>
                //{
                //    foreach (var file in files)
                //    {
                var xaml = await File.ReadAllTextAsync(file);
                var context = Path.GetFileNameWithoutExtension(file);

                Dictionary<string, string> strings = new();
                Dictionary<string, string> strings_en = new();
                Dictionary<string, string> strings_ar = new();

                if (new XamlLocalizer(text => TranslateAndGetKey(text, context, strings, strings_en, strings_ar)).LocalizeXamlPersianTexts(xaml, out var localizedXaml))
                {
                    await File.WriteAllTextAsync(file, localizedXaml);

                    var res = CreateResources(context, strings);
                    var res_en = CreateResources(context, strings_en);
                    var res_ar = CreateResources(context, strings_ar);

                    lock (concat_lock)
                    {
                        resources += res;
                        resources_en += res_en;
                        resources_ar += res_ar;
                    }
                }

                lock (concat_lock)
                {
                    counter++;
                }
                onProcessed(counter, total);
                //    }
                //})).ToArray());
            });

            var resource_file = CreateResourceFile(resources);
            await File.WriteAllTextAsync(Path.Combine(dir, "Strings.xaml"), resource_file);

            onProcessed(Interlocked.Increment(ref counter), total);

            var resource_en_file = CreateResourceFile(resources_en);
            await File.WriteAllTextAsync(Path.Combine(dir, "Strings_EN.xaml"), resource_en_file);

            onProcessed(Interlocked.Increment(ref counter), total);

            var resource_ar_file = CreateResourceFile(resources_ar);
            await File.WriteAllTextAsync(Path.Combine(dir, "Strings_AR.xaml"), resource_ar_file);

            onProcessed(Interlocked.Increment(ref counter), total);
        }


        private string CreateResourceFile(string content)
        {
            return @$"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:system=""clr-namespace:System;assembly=mscorlib"">
{content}
</ResourceDictionary>
";
        }

        private string CreateResources(string header, Dictionary<string, string> strings)
        {
            var xaml = string.Join("\r\n", strings.Select(item =>
            @$"    <system:String x:Key=""{item.Key}"">{item.Value}</system:String>"));

            return @$"

    <!-- {header} -->

{xaml}
";
        }

        private string TranslateAndGetKey(string value, string context, Dictionary<string, string> strings, Dictionary<string, string> strings_en, Dictionary<string, string> strings_ar)
        {
            var value_en = Translator.Translate(value, "en");
            value_en = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value_en);
            var value_ar = Translator.Translate(value, "ar");

            var keyName = new string(value_en.Where(c => char.IsLetterOrDigit(c)).ToArray());
            var key = $"{keyName}_{context}";

            int keyIndex = 1;
            while (strings.TryGetValue(key, out var existing))
            {
                if (existing == value)
                {
                    return key;
                }
                else
                {
                    keyIndex++;
                    key = $"{keyName}_{keyIndex}_{context}";
                }
            }

            strings.Add(key, value);
            strings_en.Add(key, value_en);
            strings_ar.Add(key, value_ar);

            return key;
        }

    }
}
