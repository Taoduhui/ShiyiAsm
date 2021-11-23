using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ShiyiAsm
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine commandLine = new CommandLine(args);
            commandLine.AddHandler("-comp", (para) =>
            {
                using (MemoryStream s = new MemoryStream(Templete.ResourceManager.GetObject("PesudoComp") as byte[]))
                {
                    using (ZipArchive zipArchive = new ZipArchive(s, ZipArchiveMode.Read))
                    {
                        try
                        {
                            zipArchive.ExtractToDirectory("./", para[1] == "--overwrite"); ;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        TempleteCreator.ReplaceKeyword("./", para[0]);
                        FileHelper.NameReplace("Templete", para[0], "./");
                    }
                }
                Console.WriteLine("{0} Component Created", para[0]);
            }, "创建伪组件: -comp [组件名] --overwrite/--skip", 2);
            commandLine.AddHandler("-page", (para) =>
            {
                using (MemoryStream s = new MemoryStream(Templete.ResourceManager.GetObject("ShiyiPageTemplete") as byte[]))
                {
                    using (ZipArchive zipArchive = new ZipArchive(s, ZipArchiveMode.Read))
                    {
                        try
                        {
                            zipArchive.ExtractToDirectory("./", para[1] == "--overwrite"); ;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        TempleteCreator.ReplaceKeyword("./", para[0]);
                        FileHelper.NameReplace("Templete", para[0], "./");
                    }
                }
                Console.WriteLine("{0} ShiyiPage Created", para[0]);
            }, "创建ShiyiPage: -page [页面名] --overwrite/--skip", 2);
            commandLine.AddHandler("-i", (para) =>
            {
                if (!File.Exists("./.vscode/settings.json"))
                {
                    Directory.CreateDirectory("./.vscode/");
                    using (var sw = File.CreateText("./.vscode/settings.json"))
                    {
                        sw.WriteLine("{}");
                        sw.Flush();
                    }
                }
                string json = "";
                using (Stream s = File.OpenRead("./.vscode/settings.json"))
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        dynamic setting = JObject.Parse(sr.ReadToEnd());
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.exclude","**/*.json"
                        }, "when", "$(basename).sajson");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.exclude","**/*.wxml"
                        }, "when", "$(basename).saml");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.exclude","**/*.wxss"
                        }, "when", "$(basename).sacss");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.associations"
                        }, "*.sajson", "json");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.associations"
                        }, "*.saml", "wxml");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.associations"
                        }, "*.sacss", "css");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.associations"
                        }, "*.cajson", "json");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.associations"
                        }, "*.caml", "wxml");
                        setting = AddItem(setting, new List<string>()
                        {
                            "files.associations"
                        }, "*.cacss", "css");
                        json = setting.ToString();
                    }
                }
                File.Delete("./.vscode/settings.json");
                using (Stream s = File.OpenWrite("./.vscode/settings.json"))
                {
                    using (StreamWriter sw = new StreamWriter(s))
                    {
                        sw.Write(json);
                    }
                }
                Console.WriteLine("Init Complete");
            }, "初始化vscode配置", 0);
            commandLine.AddHandler("-r", (para) =>
            {
                Assember.Start();
            }, "运行", 0);
            commandLine.AddHandler("-w", (para) =>
            {
                Assember.Watch(-1);
            }, "自动监听文件更改", 0);

            commandLine.Run();
        }

        static dynamic AddItem(dynamic src, List<string> path, string key, string value)
        {
            dynamic current = src;
            foreach (string dir in path)
            {
                if (current[dir] == null)
                {
                    current[dir] = JObject.FromObject(new { });
                }
                current = current[dir];
            }
            current[key] = value;
            return src;
        }
    }

    class CommandLine
    {
        struct Mapping
        {
            public Action<List<string>> handler;
            public string description;
            public int para;
            public Mapping(Action<List<string>> _handler, string _description, int _para)
            {
                handler = _handler;
                description = _description;
                para = _para;
            }
        }
        List<string> Args = new List<string>();
        Dictionary<string, Mapping> cmd = new Dictionary<string, Mapping>();
        public CommandLine(string[] _args)
        {
            Args = new List<string>(_args);
        }

        public void AddHandler(string arg, Action<List<string>> action, string description, int _paranum)
        {
            cmd[arg] = new Mapping(action, description, _paranum);
        }

        public void Run()
        {

            foreach (KeyValuePair<string, Mapping> pair in cmd)
            {
                int index = Args.IndexOf(pair.Key);
                if (index >= 0)
                {
                    List<string> ActionPara = new List<string>();
                    try
                    {
                        for (int i = 0; i < pair.Value.para; i++)
                        {
                            ActionPara.Add(Args[index + i + 1]);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Invaild Parameter!");
                        return;
                    }
                    pair.Value.handler(ActionPara);
                    return;
                }

            }
            PrintHelp();


        }

        public void PrintHelp()
        {
            Console.WriteLine("参数\t附加\t描述");
            foreach (var pair in cmd)
            {
                Console.WriteLine("{0}\t{1}\t{2}", pair.Key, pair.Value.para, pair.Value.description);
            }
        }
    }
}
