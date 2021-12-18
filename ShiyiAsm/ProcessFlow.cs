using HtmlAgilityPack;
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
        List<string> SamlFiles;
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
        private Dictionary<string, string> Moustaches = new Dictionary<string, string>();
        public PesudoComponentSetting()
        {
            string[] SamlFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.caml", SearchOption.AllDirectories);
            foreach (string file in SamlFiles)
            {
                FileInfo fileinfo = new FileInfo(file);
                string name = fileinfo.Name.Replace(fileinfo.Extension, "");
                if (Mapping.ContainsKey(name))
                {
                    SystemUtils.WriteError(string.Format("{0} \n and \n {1} \n has same file name, {1} will be ignored", Mapping[name], file));
                }
                else
                {
                    Mapping.Add(fileinfo.Name.Replace(fileinfo.Extension, ""), file);
                }
            }
        }

        public string ProtectMoustache(string code, string ComponentId)
        {
            Regex MoustacheRegex = new Regex("\\{\\{[\\S\\s]+?\\}\\}");
            MatchCollection MatchedMoustache = MoustacheRegex.Matches(code);
            foreach (Match match in MatchedMoustache)
            {
                string Moustache = match.Value;
                string guid = Guid.NewGuid().ToString();
                code = code.Replace(Moustache, guid);
                code = RemoveSpaceAfter(code, "Com:");
                Moustache = Moustache.Replace("Com:", ComponentId);
                if (Moustache.Contains("Func:"))
                {
                    Moustache = Moustache.Replace("Func:", ComponentId);
                    Moustache = Moustache.Replace("{{", "").Replace("}}", "");
                }
                Moustaches.Add(guid, Moustache);
            }
            return code;
        }

        private string RemoveSpaceAfter(string code, string after)
        {
            Regex regex = new Regex(after + "[\\b]+");
            foreach (Match match in regex.Matches(code))
            {
                code = code.Replace(match.Value, after);
            }
            return code;
        }

        public string RecoveryMoustache(string code)
        {
            foreach (KeyValuePair<string, string> pair in Moustaches)
            {
                code = code.Replace(pair.Key, pair.Value);
            }
            return code;
        }

        public string SpecialCharProtect(string code)
        {
            return code;
        }

        public string SpecialCharRecovery(string code)
        {
            HtmlDocument SrcCodeXml = new HtmlDocument();
            SrcCodeXml.LoadHtml(code);
            code = SrcCodeXml.DocumentNode.OuterHtml;
            List<string> Replaced = new List<string>();

            HtmlNodeCollection inputs = SrcCodeXml.DocumentNode.SelectNodes("//input");
            if (inputs != null)
            {
                foreach (HtmlNode node in inputs)
                {
                    if (Replaced.Contains(node.OuterHtml)) { continue; }
                    code = code.Replace(node.OuterHtml, node.OuterHtml + "</input>");
                    Replaced.Add(node.OuterHtml);
                }
            }


            return code;
        }

        public override string Process(string code, string filepath)
        {
            IsChanged = false;
            if (!SupportExtension.Contains(new FileInfo(filepath).Extension))
            {
                return code;
            }
            HtmlDocument SrcCodeXml = new HtmlDocument();
            string CodeBuckup = code;
            code = SpecialCharProtect(code);
            code = ProtectMoustache(code, "");
            try
            {
                SrcCodeXml.LoadHtml(code);
            }
            catch (Exception ex)
            {
                SystemUtils.WriteError(string.Format("Error:\"{0}\" in {1}", ex.Message, filepath));
                return CodeBuckup;
            }

            bool IsRoot = false;
            HtmlNodeCollection Components = SrcCodeXml.DocumentNode.SelectNodes("//component");
            List<string> ExistComponents = Mapping.Keys.ToList<string>();
            if (Components != null)
            {
                foreach (HtmlNode Component in Components)
                {
                    string KeyLabel = Component.OuterHtml;
                    string ComponentName = Component.Attributes["using"].Value;
                    string ComponentId = Component.Attributes["id"].Value;
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

                                CompCode = SpecialCharProtect(CompCode);
                                CompCode = ProtectMoustache(CompCode, ComponentId);
                                HtmlNode CompElement = SrcCodeXml.CreateElement("view");
                                foreach (HtmlAttribute attribute in Component.Attributes)
                                {
                                    CompElement.SetAttributeValue(attribute.Name, attribute.Value);
                                }
                                CompElement.Attributes.Remove(CompElement.Attributes["using"]);
                                CompElement.Attributes.Remove(CompElement.Attributes["id"]);
                                CompElement.InnerHtml = CompCode;
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
            }

            code = SrcCodeXml.DocumentNode.OuterHtml;
            code = RecoveryMoustache(code);
            code = BindTwoWay(filepath, code);
            code = SpecialCharRecovery(code);
            if (IsRoot)
            {
                ApplyToPreProcess();
                CurrentCompnent = new PesudoCompnent();
            }
            return code;
        }

        private string BindTwoWay(string filepath, string code)
        {
            HtmlDocument SrcCodeXml = new HtmlDocument();
            SrcCodeXml.LoadHtml(code);
            HtmlNodeCollection BindTwoWayNodes = SrcCodeXml.DocumentNode.SelectNodes("//input[@bindtwoway]");
            ProcessBindTwoWayNodes(filepath, SrcCodeXml, BindTwoWayNodes, "value", "bind:input");
            BindTwoWayNodes = SrcCodeXml.DocumentNode.SelectNodes("//picker[@bindtwoway]");
            ProcessBindTwoWayNodes(filepath, SrcCodeXml, BindTwoWayNodes, "value", "bind:change");
            BindTwoWayNodes = SrcCodeXml.DocumentNode.SelectNodes("//radio-group[@bindtwoway]");
            ProcessBindTwoWayNodes(filepath, SrcCodeXml, BindTwoWayNodes, "value", "bind:change");
            return SrcCodeXml.DocumentNode.OuterHtml.Replace("bindtwoway=\"\"", ""); ;
        }

        private static void ProcessBindTwoWayNodes(
            string filepath,
            HtmlDocument SrcCodeXml,
            HtmlNodeCollection BindTwoWayNodes, string srcKey, string eventName)
        {
            if (BindTwoWayNodes != null)
            {
                foreach (HtmlNode BindTwoWayNode in BindTwoWayNodes)
                {
                    if (BindTwoWayNode.Attributes["value"] == null)
                    {
                        SystemUtils.WriteError(string.Format("Error:\"{0}\" in {1}", "BindTwoWay need value", filepath));
                        continue;
                    }
                    string value = BindTwoWayNode.Attributes[srcKey].Value;
                    string path = value.Replace("{{", "").Replace("}}", "");
                    BindTwoWayNode.SetAttributeValue(eventName, "DataChange");
                    BindTwoWayNode.SetAttributeValue("data-key", path);
                }
            }

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
