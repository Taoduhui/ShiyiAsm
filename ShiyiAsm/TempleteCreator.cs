using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiyiAsm
{
    class TempleteCreator
    {
        static List<string> SurpportExtension = new List<string>() { ".caml", ".cacss", ".cajson", ".ts", ".saml", ".sacss", ".sajson" };
        public static void ReplaceKeyword(string path, string key)
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (string filepath in files)
            {
                if (!SurpportExtension.Contains(new FileInfo(filepath).Extension))
                {
                    continue;
                }
                string code = "";
                using (Stream s = File.OpenRead(filepath))
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        code = sr.ReadToEnd();
                    }
                }
                code = code.Replace("{{ShiyiAsm:Templete}}", key);
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
                using (Stream s = File.OpenWrite(filepath))
                {
                    using (StreamWriter sw = new StreamWriter(s))
                    {
                        sw.Write(code);
                    }
                }
            }
        }
    }
}
