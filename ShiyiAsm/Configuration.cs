using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ShiyiAsm
{

    class CodeSetting
    {
        public List<string> MatchedFiles = new List<string>();
        public AsmSetting AsmSetting;
        public PathMappingSetting PathMappingSetting;
        public string Rename;
    }

    class Configuration
    {
        public static bool IsWatching = false;
        public static Dictionary<string, CodeSetting> CodeSettings = new Dictionary<string, CodeSetting>();
        public static List<string> BeforeCmd = new List<string>();
        public static List<string> AfterCmd = new List<string>();

        public static void LoadConfiguration()
        {
            CodeSettings = new Dictionary<string, CodeSetting>();
            AfterCmd = new List<string>();
            BeforeCmd = new List<string>();
            XmlDocument ConfigurationDocument = new XmlDocument();
            string CurrentDir = Directory.GetCurrentDirectory() + "\\";
            string Config = "";
            using (Stream s = File.OpenRead(CurrentDir + "ShiyiAsm.xml"))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    Config = sr.ReadToEnd();
                }
            }
            ConfigurationDocument.LoadXml(Config);

            #region LoadCommand

            XmlNodeList BeforeCmdKeys = ConfigurationDocument.SelectNodes("//BeforeCmd");
            foreach (XmlNode node in BeforeCmdKeys)
            {
                BeforeCmd.Add(node.InnerText);
            }

            XmlNodeList AfterCmdKeys = ConfigurationDocument.SelectNodes("//AfterCmd");
            foreach (XmlNode node in AfterCmdKeys)
            {
                AfterCmd.Add(node.InnerText);
            }
            #endregion

            #region LoadGlobalSetting
            XmlNodeList GlobalKeys = ConfigurationDocument.SelectNodes("//GlobalAlias");
            Dictionary<string, string> GlobalAlias = new Dictionary<string, string>();
            foreach (XmlNode node in GlobalKeys)
            {
                GlobalAlias.Add(node.Attributes["key"].Value, CurrentDir + node.InnerText);
            }

            XmlNodeList ExcludeKeys = ConfigurationDocument.SelectNodes("//Exclude");
            List<string> Exclude = new List<string>();
            foreach (XmlNode node in ExcludeKeys)
            {
                Exclude.Add(CurrentDir + node.InnerText);
            }
            #endregion

            XmlNodeList FileTypes = ConfigurationDocument.SelectNodes("//FileType");
            foreach (XmlNode node in FileTypes)
            {
                string FileType = node.Attributes["type"].Value;
                CodeSetting codeSetting = new CodeSetting();
                codeSetting.MatchedFiles = FileHelper.GetAllTargetFiles(FileType, Exclude);
                codeSetting.Rename = node.Attributes["to"] == null ? null : node.Attributes["to"].Value;

                XmlNodeList keys = node.SelectNodes(".//Alias");
                Dictionary<string, string> Alias = new Dictionary<string, string>();
                foreach (XmlNode key in keys)
                {
                    Alias.Add(key.Attributes["key"].Value, CurrentDir + key.InnerText);
                }
                foreach (KeyValuePair<string, string> global in GlobalAlias)
                {
                    Alias.Add(global.Key, global.Value);
                }
                codeSetting.PathMappingSetting = new PathMappingSetting(Alias);

                codeSetting.AsmSetting = new AsmSetting();
                CodeSettings.Add(FileType, codeSetting);
            }
        }
    }


}
