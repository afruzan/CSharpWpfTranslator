using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WpfTranslator
{
    public static class Translator
    {

        public static string Translate(string word, string toLanguage = "en", string fromLanguage = "fa")
        {
            return TranslateAsync(word, toLanguage, fromLanguage).Result;
        }

        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        public static async Task<string> TranslateAsync(string word, string toLanguage = "en", string fromLanguage = "fa")
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
