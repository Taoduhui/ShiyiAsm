using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShiyiAsm
{
    class Assember
    {
        public static void Start()
        {
            int Cnt = 0;
            Configuration.LoadConfiguration();
            Console.WriteLine("Run Before Command:");
            foreach (string cmd in Configuration.BeforeCmd)
            {
                Console.WriteLine("\t{0}", cmd);
                SystemUtils.RunCmd(cmd);
            }
            Console.WriteLine("");
            foreach (KeyValuePair<string, CodeSetting> pair in Configuration.CodeSettings)
            {
                foreach (string filepath in pair.Value.MatchedFiles)
                {
                    CodeSetting codeSetting = pair.Value;
                    ProcessFlow flow = new ProcessFlow(filepath);
                    string code = flow
                        .Process(codeSetting.AsmSetting)
                        .Process(codeSetting.PathMappingSetting)
                        .GetResult();
                    string Output = filepath;
                    FileInfo fileInfo = new FileInfo(filepath);
                    if (codeSetting.Rename != null && codeSetting.Rename != "")
                    {
                        Output = fileInfo.FullName.Replace(fileInfo.Extension, codeSetting.Rename.Replace("*", ""));
                    }
                    if (codeSetting.AsmSetting.IsChanged || codeSetting.PathMappingSetting.IsChanged)
                    {
                        if (File.Exists(Output))
                        {
                            File.Delete(Output);
                        }
                        using (Stream s = File.OpenWrite(Output))
                        {
                            using (StreamWriter sw = new StreamWriter(s))
                            {
                                sw.Write(code);
                            }
                        }
                        Cnt++;
                        Console.WriteLine(new FileInfo(Output).Name + " Generated");
                    }
                }
            }
            Console.WriteLine("{0} Files Affected\n", Cnt);
            Console.WriteLine("Run After Command:");
            foreach (string cmd in Configuration.AfterCmd)
            {
                Console.WriteLine("\t{0}", cmd);
                SystemUtils.RunCmd(cmd);
            }
            Console.WriteLine("Completed");
        }

        static FileSystemWatcher watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
        static int DelayParm = 0;
        public static void Watch(int time)
        {
            Start();
            if (time >= 0)
            {
                DelayParm = time;
            }
            else
            {
                DelayParm = 100;
            }
            DelayTime = DelayParm;
            Console.WriteLine("Start watching...");
            InitWatcher();
            while (true) { Console.ReadKey(); }
        }



        private static void InitWatcher()
        {
            watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.DirectoryName;
            watcher.IncludeSubdirectories = true;
            watcher.Created += Watcher_Notification;
            watcher.Changed += Watcher_Notification;
            watcher.Renamed += Watcher_Notification;
            watcher.EnableRaisingEvents = true;
        }

        static int DelayTime = 20;
        private static bool delay()
        {
            if (DelayTime != DelayParm)
            {
                DelayTime = DelayParm - 1;
                return false;
            }
            else
            {
                Console.WriteLine("Waiting For Completed");
                DelayTime = DelayParm;
                while (DelayTime != 0)
                {
                    DelayTime--;
                    Thread.Sleep(1);
                }
                DelayTime = DelayParm;
                return true;
            }

        }

        private static void Watcher_Notification(object sender, object e)
        {
            if (!delay()) { return; }
            Console.Clear();
            watcher.EnableRaisingEvents = false;
            Console.WriteLine("Detect Change");
            int err = 0;
            while (err >= 0 && err <= 3)
            {
                try
                {
                    Start();
                    err = -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    err++;
                }
            }
            InitWatcher();
        }
    }
}
