using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ShiyiAsm
{
    class ProcessFlow
    {
        string code;
        public string SrcFilePath;
        public string TargetFilePath;
        public CodeSetting codeSetting;
        public bool PreProcessed = false;
        public ProcessFlow(string _filepath, string _TargetFilePath, CodeSetting _codeSetting)
        {
            SrcFilePath = _filepath;
            codeSetting = _codeSetting;
            if (CodeSetting.PreProcessCode.ContainsKey(_filepath))
            {
                code = CodeSetting.PreProcessCode[_filepath];
                CodeSetting.PreProcessCode.Remove(_filepath);
                PreProcessed = true;
            }
            else
            {
                using (Stream s = File.OpenRead(SrcFilePath))
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        code = sr.ReadToEnd();
                    }
                }
            }
            TargetFilePath = _TargetFilePath;

        }

        public ProcessFlow Process(ProcessFlowSetting flowSetting)
        {
            code = flowSetting.Process(code, SrcFilePath);
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

    class PesudoCompnent
    {
        public bool IsInited = false;
        public string Caml = "";
        public string SrcSamlPath = "";
        public string Sajson = "{\"usingComponents\": {}}";
        public string SrcSajsonPath = "";
        public void AddSaJson(string key, string value)
        {
            dynamic compJson = JObject.Parse(Sajson);
            compJson.usingComponents[key] = value;
            Sajson = JObject.FromObject(compJson).ToString();
        }

        public void CombineSaJson(string code)
        {
            dynamic compJson = JObject.Parse(code);
            var usingComponents = JObject.FromObject(compJson.usingComponents);
            foreach (var item in usingComponents)
            {
                AddSaJson(item.Name, usingComponents[item.Name].ToString());
            }
        }
        public string Sacss = "";
        public string SrcSacssPath = "";
    }

    class PesudoComponentSetting : ProcessFlowSetting
    {
        public static PesudoCompnent CurrentCompnent = new PesudoCompnent();
        private List<string> SupportExtension = new List<string>() { ".caml", ".saml" };
        protected Dictionary<string, string> Mapping = new Dictionary<string, string>();
        public PesudoComponentSetting()
        {
            string[] SamlFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.caml", SearchOption.AllDirectories);
            foreach (string file in SamlFiles)
            {
                FileInfo fileinfo = new FileInfo(file);
                Mapping.Add(fileinfo.Name.Replace(fileinfo.Extension, ""), file);
            }
        }

        public override string Process(string code, string filepath)
        {
            IsChanged = false;
            if (!SupportExtension.Contains(new FileInfo(filepath).Extension))
            {
                return code;
            }
            string RootLabel = "Root" + Guid.NewGuid().ToString();
            code = String.Format("<{0}>{1}</{2}>", RootLabel, code, RootLabel);
            code = code.Replace("bind:", "bind");
            XmlDocument SrcCodeXml = new XmlDocument();
            try
            {
                SrcCodeXml.LoadXml(code);
            }
            catch (Exception ex)
            {
                return code;
            }

            bool IsRoot = false;
            XmlNodeList Components = SrcCodeXml.SelectNodes("//Component");
            List<string> ExistComponents = Mapping.Keys.ToList<string>();
            foreach (XmlNode Component in Components)
            {
                string KeyLabel = Component.OuterXml;
                string ComponentName = Component.Attributes["Using"].Value;
                string ComponentId = Component.Attributes["Id"].Value;
                if (ExistComponents.Contains(ComponentName))
                {
                    IsChanged = true;
                    string ComFile = Mapping[ComponentName];
                    string TargetFilePath = Assember.CurrentProcessFlow.TargetFilePath;
                    FileInfo TargetFileInfo = new FileInfo(TargetFilePath);
                    string TargetBasePath = TargetFilePath.Replace(TargetFileInfo.Extension, ".");
                    FileInfo SrcComFileInfo = new FileInfo(ComFile);
                    string SrcComBasePath = ComFile.Replace(SrcComFileInfo.Extension, ".");

                    if (!CurrentCompnent.IsInited)
                    {
                        CurrentCompnent.IsInited = true;
                        IsRoot = true;
                        CurrentCompnent.Caml = code;
                        string SrcFilePath = Assember.CurrentProcessFlow.SrcFilePath;
                        FileInfo SrcFileInfo = new FileInfo(SrcFilePath);
                        string SrcBasePath = SrcFilePath.Replace(SrcFileInfo.Extension, ".");
                        CurrentCompnent.SrcSamlPath = SrcBasePath + "saml";
                        CurrentCompnent.SrcSajsonPath = SrcBasePath + "sajson";
                        CurrentCompnent.SrcSacssPath = SrcBasePath + "sacss";
                        if (File.Exists(SrcBasePath + "sajson"))
                        {
                            using (Stream s = File.OpenRead(SrcBasePath + "sajson"))
                            {
                                using (StreamReader sr = new StreamReader(s))
                                {
                                    CurrentCompnent.CombineSaJson(sr.ReadToEnd());
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Compnent Incomplete!");
                        }
                        if (File.Exists(SrcBasePath + "sacss"))
                        {
                            using (Stream s = File.OpenRead(SrcBasePath + "sacss"))
                            {
                                using (StreamReader sr = new StreamReader(s))
                                {
                                    CurrentCompnent.Sacss += "\n\r" + sr.ReadToEnd();
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Compnent Incomplete!");
                        }
                    }

                    #region 注入wxml

                    using (Stream s = File.OpenRead(ComFile))
                    {
                        using (StreamReader sr = new StreamReader(s))
                        {
                            string CompCode = sr.ReadToEnd();
                            if (CompCode.Replace(" ", "").Contains("<Component"))
                            {
                                CompCode = Process(CompCode, ComFile);
                            }
                            CompCode = CompCode.Replace("Com:", ComponentId);
                            Regex ComFuncRegex = new Regex("{{Func:[A-Za-z0-9_]+}}");
                            MatchCollection Funcs = ComFuncRegex.Matches(CompCode);
                            foreach (Match match in Funcs)
                            {
                                string func = match.Value.Replace("{{Func:", ComponentId).Replace("}}", "");
                                CompCode = CompCode.Replace(match.Value, func);
                            }
                            XmlElement CompElement = SrcCodeXml.CreateElement("view");
                            foreach (XmlAttribute attribute in Component.Attributes)
                            {
                                CompElement.SetAttribute(attribute.Name, attribute.Value);
                            }
                            CompElement.RemoveAttribute("Using");
                            CompElement.RemoveAttribute("Id");
                            CompElement.InnerXml = CompCode;
                            Component.ParentNode.ReplaceChild(CompElement, Component);
                        }
                    }
                    #endregion

                    if (File.Exists(SrcComBasePath + "cajson"))
                    {
                        using (Stream s = File.OpenRead(SrcComBasePath + "cajson"))
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                CurrentCompnent.CombineSaJson(sr.ReadToEnd());
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Compnent Incomplete!");
                    }
                    if (File.Exists(SrcComBasePath + "cacss"))
                    {
                        using (Stream s = File.OpenRead(SrcComBasePath + "cacss"))
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                CurrentCompnent.Sacss += sr.ReadToEnd();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Compnent Incomplete!");
                    }
                }
            }
            code = SrcCodeXml.OuterXml;
            code = code.Replace("<" + RootLabel + ">", "").Replace("</" + RootLabel + ">", "");
            if (IsRoot)
            {
                ApplyToPreProcess();
                CurrentCompnent = new PesudoCompnent();
            }
            return code;
        }

        private static void ApplyToPreProcess()
        {
            if (CodeSetting.PreProcessCode.ContainsKey(CurrentCompnent.SrcSajsonPath))
            {
                CodeSetting.PreProcessCode[CurrentCompnent.SrcSajsonPath] = CurrentCompnent.Sajson;
            }
            else
            {
                CodeSetting.PreProcessCode.Add(CurrentCompnent.SrcSajsonPath, CurrentCompnent.Sajson);
            }
            if (CodeSetting.PreProcessCode.ContainsKey(CurrentCompnent.SrcSacssPath))
            {
                CodeSetting.PreProcessCode[CurrentCompnent.SrcSacssPath] = CurrentCompnent.Sacss;
            }
            else
            {
                CodeSetting.PreProcessCode.Add(CurrentCompnent.SrcSacssPath, CurrentCompnent.Sacss);
            }
        }
    }
}
