using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WpfTranslator
{
    public class XamlLocalizer
    {
        private readonly Func<string, string> translatorGetResourceKeyFunc;

        public XamlLocalizer(Func<string, string> translatorGetResourceKeyFunc)
        {
            this.translatorGetResourceKeyFunc = translatorGetResourceKeyFunc;
        }

        public bool LocalizeXamlPersianTexts(string xamlFile, out string localizedXaml)
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

                                        var key2 = translatorGetResourceKeyFunc(value2);

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

                            var key = translatorGetResourceKeyFunc(value);

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
        }


    }
}