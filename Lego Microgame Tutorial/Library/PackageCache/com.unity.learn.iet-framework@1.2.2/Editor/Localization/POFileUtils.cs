using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Good info on PO: http://pology.nedohodnik.net/doc/user/en_US/ch-poformat.html
    /// </summary>
    public static class POFileUtils
    {
        /// <summary>
        /// Currently supported languages, in addition to English.
        /// </summary>
        public static readonly Dictionary<SystemLanguage, string> SupportedLanguages = new Dictionary<SystemLanguage, string>
        {
            { SystemLanguage.Japanese, "ja" },
            { SystemLanguage.Korean, "ko" },
            { SystemLanguage.ChineseSimplified, "zh-hans" },
            { SystemLanguage.ChineseTraditional, "zh-hant" },
        };

        /// <summary>
        /// Creates a PO file header.
        /// https://www.gnu.org/software/trans-coord/manual/gnun/html_node/PO-Header.html
        /// https://www.gnu.org/software/gettext/manual/html_node/Header-Entry.html
        /// </summary>
        /// <param name="langCode"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static string CreateHeader(string langCode, string name, string version) =>
// NOTE We don't have POTs so for POT-Creation-Date I just picked something.
// TODO Value of Plural-Forms not probably true for all languages we support?
// TODO check if we want to fill something more to the header
$@"
msgid """"
msgstr """"
""Project-Id-Version: {name}@{version} \n""
""Report-Msgid-Bugs-To: \n""
""Language-Team: #devs-localization\n""
""POT-Creation-Date: 2020-05-15 21:02+03:00\n""
""PO-Revision-Date: {DateTime.Now.ToString(DateTimeFormat)}\n""
""Language: {langCode}\n""
""MIME-Version: 1.0\n""
""Content-Type: text/plain; charset=UTF-8\n""
""Content-Transfer-Encoding: 8bit\n""
""Plural-Forms: nplurals=2; plural=(n != 1);\n""
""X-Generator: com.unity.learn.iet-framework.authoring\n""
";

        /// <summary>
        /// Using the format given here https://www.gnu.org/software/trans-coord/manual/gnun/html_node/PO-Header.html
        /// </summary>
        public const string DateTimeFormat = "yyyy-MM-dd HH:mmK";

        /// <summary>
        /// http://pology.nedohodnik.net/doc/user/en_US/ch-poformat.html, "2.3.3. Escape Sequences"
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string EscapeString(string str)
        {
            // adapted from https://stackoverflow.com/a/14502246
            if (str.IsNullOrEmpty())
                return str;
            var literal = new StringBuilder(str.Length);
            foreach (var c in str)
            {
                switch (c)
                {
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        if (char.GetUnicodeCategory(c) == UnicodeCategory.Control)
                            literal.Append($@"\u{c:x4}");
                        else
                            literal.Append(c);
                        break;
                }
            }
            return literal.ToString();
        }

        /// <summary>
        /// https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html
        /// </summary>
        /// <remarks>
        /// The values are not escaped until they are serialized.
        /// </remarks>
        public class POEntry
        {
            /// <summary> # translator-comments </summary>
            public string TranslatorComments;
            /// <summary> #. extracted-comments  </summary>
            public string ExtractedComments;
            /// <summary> #: reference </summary>
            public string Reference;
            /// <summary> #, flag </summary>
            public string Flag;
            /// <summary> #| msgid "previous-untranslated-string" </summary>
            public string PreviousUntranslatedString;
            /// <summary> msgid "untranslated-string" </summary>
            public string UntranslatedString;
            /// <summary> msgstr "translated-string" </summary>
            public string TranslatedString;

            /// <summary>
            /// Does this entry contain the minimum information to be a valid entry.
            /// </summary>
            /// <returns></returns>
            public bool IsValid() => Reference.IsNotNullOrEmpty() && UntranslatedString.IsNotNullOrEmpty();

            /// <summary>
            /// Serializes this entry to a string representation.
            /// </summary>
            /// <remarks>
            /// All values will be escaped.
            /// </remarks>
            /// <returns></returns>
            public string Serialize()
            {
                return string.Format(
                    "{0}" +
                    "{1}" +
                    "{2}" +
                    "{3}" +
                    "{4}" +
                    "msgid \"{5}\"\n" +
                    "msgstr \"{6}\"",
                    TranslatorComments.IsNotNullOrEmpty() ? $"# {EscapeString(TranslatorComments)}\n" : string.Empty,
                    ExtractedComments.IsNotNullOrEmpty() ? $"#. {EscapeString(ExtractedComments)}\n" : string.Empty,
                    Reference.IsNotNullOrEmpty() ? $"#: {EscapeString(Reference)}\n" : string.Empty,
                    Flag.IsNotNullOrEmpty() ? $"#, {EscapeString(Flag)}\n" : string.Empty,
                    PreviousUntranslatedString.IsNotNullOrEmpty() ? $"#| {EscapeString(PreviousUntranslatedString)}" : string.Empty,
                    EscapeString(UntranslatedString),
                    EscapeString(TranslatedString)
                );
            }
        }

        /// <summary>
        /// Reads a PO file and creates a list of PO entries.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        // TODO Currently unused, implement a unit test at minimum if wanting to keep this around or make internal or remove.
        public static List<POEntry> ReadPOFile(string filepath)
        {
            const string str = "msgstr ";
            const string id = "msgid ";
            const string previd = "#| msgstr ";
            const string flag = "#,";
            const string reference = "#:";
            const string ecomment = "#.";
            const string tcomment = "#";

            var ret = new List<POEntry>();
            try
            {
                using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream, Utf8WithoutBom))
                {
                    var entry = new POEntry();
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.StartsWith(str))
                        {
                            entry.TranslatedString = line.Substring(str.Length);
                            entry.TranslatedString = entry.TranslatedString.Trim(new char[] {' ', '\"'});
                        }
                        if (line.StartsWith(id))
                        {
                            entry.UntranslatedString = line.Substring(id.Length);
                            entry.UntranslatedString = entry.UntranslatedString.Trim(new char[] { ' ', '\"' });
                        }
                        if (line.StartsWith(previd))
                        {
                            entry.PreviousUntranslatedString = line.Substring(previd.Length);
                            entry.PreviousUntranslatedString = entry.PreviousUntranslatedString.Trim(new char[] { ' ', '\"' });
                        }
                        if (line.StartsWith(flag)) entry.Flag = line.Substring(flag.Length).Trim();
                        if (line.StartsWith(reference)) entry.Reference = line.Substring(reference.Length).Trim();
                        if (line.StartsWith(ecomment)) entry.ExtractedComments = line.Substring(ecomment.Length).Trim();
                        if (line.StartsWith(tcomment)) entry.TranslatorComments = line.Substring(tcomment.Length).Trim();

                        if (line.IsNullOrWhitespace() && entry.IsValid())
                        {
                            ret.Add(entry);
                            entry = new POEntry();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return ret;
        }

        /// <summary>
        /// Writes a PO file.
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="projectVersion"></param>
        /// <param name="langCode"></param>
        /// <param name="entries"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static bool WritePOFile(string projectName, string projectVersion, string langCode, IEnumerable<POEntry> entries, string filepath)
        {
            try
            {
                using (var sw = new StreamWriter(filepath, append: false, Utf8WithoutBom))
                {
                    sw.Write(CreateHeader(langCode, projectName, projectVersion));
                    // Editor's handling of PO files seems very finicky, an empty line after the header
                    // and before the first entry required.
                    sw.WriteLine();
                    foreach (var entry in entries)
                    {
                        sw.WriteLine(entry.Serialize());
                        sw.WriteLine();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        // Let's be very explicit about this, using e.g. System.Text.Encoding.UTF8 gives UTF-8 with BOM...
        static UTF8Encoding Utf8WithoutBom => new UTF8Encoding();
    }
}
