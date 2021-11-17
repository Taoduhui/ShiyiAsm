using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiyiAsm
{
    class ProcessFlow
    {
        string code;
        string filepath;
        public ProcessFlow(string _filepath)
        {
            filepath = _filepath;
            using (Stream s = File.OpenRead(filepath))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    code = sr.ReadToEnd();
                }
            }
        }

        public ProcessFlow Process(ProcessFlowSetting flowSetting)
        {
            code = flowSetting.Process(code, filepath);
            return this;
        }

        public string GetResult()
        {
            return code;
        }
    }

    abstract class ProcessFlowSetting
    {
        public bool IsChanged = false;
        public abstract string Process(string code, string filepath);
    }

    class PathMappingSetting : ProcessFlowSetting
    {
        Dictionary<string, string> Mapping = new Dictionary<string, string>();

        public PathMappingSetting(Dictionary<string, string> _Mapping)
        {
            Mapping = _Mapping;
        }
        public override string Process(string code, string filepath)
        {
            IsChanged = false;
            foreach (string key in Mapping.Keys)
            {
                if (code.Contains(key))
                {
                    IsChanged = true;
                    string TargetDir = Mapping[key];
                    string Dotdot = FileHelper.GenerateDotdot(filepath, TargetDir);
                    code = code.Replace(key, Dotdot);
                }

            }
            return code;
        }
    }

    class AsmSetting : ProcessFlowSetting
    {
        protected Dictionary<string, string> Mapping = new Dictionary<string, string>();

        public AsmSetting()
        {
            string[] SamlFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.saml", SearchOption.AllDirectories);
            foreach (string file in SamlFiles)
            {
                FileInfo fileinfo = new FileInfo(file);
                Mapping.Add(fileinfo.Name.Replace(fileinfo.Extension, ""), file);
            }
        }

        public override string Process(string code, string filepath)
        {
            IsChanged = false;
            foreach (string key in Mapping.Keys)
            {
                string KeyLabel = "{{Ref:" + key + "}}";
                if (code.Contains(KeyLabel))
                {
                    IsChanged = true;
                    string RefFile = Mapping[key];
                    using (Stream s = File.OpenRead(RefFile))
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            code = code.Replace(KeyLabel, sr.ReadToEnd());
                        }
                    }
                }
            }
            return code;
        }
    }
}
