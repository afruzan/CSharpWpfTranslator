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

                if (LocalizePersianTexts(xaml, context, strings, strings_en, strings_ar, out var localizedXaml))
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


        private bool LocalizePersianTexts(string xamlFile, string context, Dictionary<string, string> strings, Dictionary<string, string> strings_en, Dictionary<string, string> strings_ar, out string localizedXaml)
        {
            var any = false;
            Exception? exception = null;

            localizedXaml = Regex.Replace(xamlFile, @"=""[^""]*""", m =>
            {
                try
                {
                    if (exception != null)
                    {
                        return m.Value;
                    }
                    var anyPersianLetter = m.Value?.Any(c => char.IsLetter(c) && !char.IsAscii(c)) ?? false;
                    if (anyPersianLetter)
                    {
                        var value = m.Value![2..^1];

                        if (Regex.IsMatch(value, @"^{\w+ .*}$")) // binding or other expressions..
                        {
                            var expression = Regex.Replace(value, @"='[^']*'", m2 =>
                            {
                                if (exception != null)
                                {
                                    return m2.Value;
                                }
                                try
                                {
                                    var anyPersianLetter2 = m2.Value?.Any(c => char.IsLetter(c) && !char.IsAscii(c)) ?? false;
                                    if (anyPersianLetter2)
                                    {
                                        var value2 = m2.Value![2..^1];

                                        any = true;

                                        var key2 = TranslateAndGetKey(value2, context, strings, strings_en, strings_ar);

                                        return $"={{StaticResource {key2}}}";
                                    }
                                    else
                                    {
                                        return m2.Value!;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    exception = ex;
                                    throw;
                                }
                            });
                            if (exception != null)
                            {
                                throw exception;
                            }
                            return $"=\"{expression}\"";
                        }
                        else
                        {
                            any = true;

                            var key = TranslateAndGetKey(value, context, strings, strings_en, strings_ar);

                            return $"=\"{{StaticResource {key}}}\"";
                        }
                    }
                    else
                    {
                        return m.Value!;
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }
            });
            if (exception != null)
            {
                throw exception;
            }

            return any;

            //var xaml = new XamlXmlReader(openFileDialog.FileName);
            //var file = new FileStream(saveFileDialog.FileName, FileMode.Create);
            //var xaml2 = new XamlXmlWriter(file, xaml.SchemaContext);

            //Type? propType = null;
            //while (xaml.Read())
            //{
            //    switch (xaml.NodeType)
            //    {
            //        case XamlNodeType.StartObject:
            //            xaml2.WriteStartObject(xaml.Type);
            //            break;
            //        case XamlNodeType.GetObject:
            //            xaml2.WriteGetObject();
            //            break;
            //        case XamlNodeType.EndObject:
            //            xaml2.WriteEndObject();
            //            break;
            //        case XamlNodeType.StartMember:
            //            xaml2.WriteStartMember(xaml.Member);
            //            propType = (xaml.Member.UnderlyingMember as PropertyInfo)?.PropertyType;
            //            break;
            //        case XamlNodeType.EndMember:
            //            xaml2.WriteEndMember();
            //            break;
            //        case XamlNodeType.Value:
            //            var lettersCount = xaml.Value?.ToString()?.Count(c => char.IsLetter(c)) ?? -1;
            //            if (propType == typeof(string) && lettersCount > 5)
            //            {
            //                xaml2.WriteValue(xaml.Value);
            //            }
            //            else
            //            {
            //                xaml2.WriteValue(xaml.Value);
            //            }
            //            break;
            //        case XamlNodeType.NamespaceDeclaration:
            //            xaml2.WriteNamespace(xaml.Namespace);
            //            break;
            //        default:
            //            throw new NotSupportedException();
            //    }
            //}

            //xaml2.Close();
            //file.Close();

        }

        private string TranslateAndGetKey(string value, string context, Dictionary<string, string> strings, Dictionary<string, string> strings_en, Dictionary<string, string> strings_ar)
        {
            var value_en = Translate(value, "en");
            value_en = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value_en);
            var value_ar = Translate(value, "ar");

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

        public string Translate(string word, string toLanguage = "en", string fromLanguage = "fa")
        {
            return TranslateAsync(word, toLanguage, fromLanguage).Result;
        }

        private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        public async Task<string> TranslateAsync(string word, string toLanguage = "en", string fromLanguage = "fa")
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";

            int retry = 0;
            while (true)
            {
                retry++;
                try
                {
                    var responce = await httpClient.GetAsync(url);
                    var result = await responce.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

                    result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                    return result;
                }
                catch (Exception ex)
                {
                    if (retry > 3)
                    {
                        throw new Exception("Translation request failed.", ex);
                    }
                    await Task.Delay(1000);
                }
            }
        }

    }
}
