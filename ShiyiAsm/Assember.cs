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
        public static ProcessFlow CurrentProcessFlow;
        public static bool IsCompiling = false;
        public static bool TscIsCompiling = false;
        public static string ResultMsg = "";
        public static void Start()
        {
            Start(new List<string>());
        }
        public static void Start(List<string> FileRange)
        {
            IsCompiling = true;
            Configuration.LoadConfiguration(FileRange);
            string BeforeOutput = RunBeforeCmd();
            int Cnt = RunFlow();
            while (CodeSetting.PreProcessCode.Count > 0)
            {
                Configuration.LoadConfiguration(CodeSetting.PreProcessCode.Keys.ToList());
                RunFlow();
            }
            Console.WriteLine("{0} Files Affected\n", Cnt);
            string AfterOutput = RunAfterCmd();
            Console.WriteLine(BeforeOutput);
            Console.WriteLine(AfterOutput);
            Console.WriteLine(ResultMsg.Replace("\u001bc", ""));
            ResultMsg = "";
            Console.WriteLine("Completed");
            IsCompiling = false;
        }

        private static string RunAfterCmd()
        {
            string Output = "RunAfterCmd\n";
            foreach (string cmd in Configuration.AfterCmd)
            {
                Output += string.Format("\t{0}\n", cmd);
                Output += SystemUtils.RunCmd(cmd);
            }
            return Output;
        }

        private static int RunFlow()
        {
            int Cnt = 0;
            foreach (KeyValuePair<string, CodeSetting> pair in Configuration.CodeSettings)
            {
                foreach (string filepath in pair.Value.MatchedFiles)
                {
                    Cnt += Process(pair.Value, filepath);
                }
            }
            return Cnt;
        }

        private static int Process(CodeSetting _codeSetting, string filepath)
        {
            int Cnt = 0;
            CodeSetting codeSetting = _codeSetting;
            FileInfo fileInfo = new FileInfo(filepath);
            string Output = filepath;
            if (codeSetting.Rename != null && codeSetting.Rename != "")
            {
                Output = fileInfo.FullName.Replace(fileInfo.Extension, codeSetting.Rename.Replace("*", ""));
            }

            CurrentProcessFlow = new ProcessFlow(filepath, Output, codeSetting);
            for (int i = 0; i < codeSetting.ProcessFlows.Count; i++)
            {
                CurrentProcessFlow = CurrentProcessFlow.Process(codeSetting.ProcessFlows[i]);
            }
            string code = CurrentProcessFlow.GetResult();

            if (codeSetting.IsChanged() || CurrentProcessFlow.PreProcessed)
            {
                Cnt++;
                Console.WriteLine(new FileInfo(Output).Name + " Affected");
            }
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
            return Cnt;
        }

        public static void WatchTsc()
        {
            Watch(100, false, false);
            SystemUtils.RunCmdAsync("tsc -w", (data) =>
            {
                if (data.Contains("Starting compilation in watch mode") || data.Contains("Starting incremental compilation"))
                {
                    Console.Clear();
                    watcher.EnableRaisingEvents = false;
                    TscIsCompiling = true;
                }
                ResultMsg += data + "\n";
                if (data.Contains("Watching for file changes") && !IsCompiling)
                {
                    TscIsCompiling = false;
                    Start();
                    watcher.EnableRaisingEvents = true;
                }
            });
        }

        private static string RunBeforeCmd()
        {
            string Output = "RunBeforeCmd\n";
            foreach (string cmd in Configuration.BeforeCmd)
            {
                Output += string.Format("\t{0}\n", cmd);
                Output += SystemUtils.RunCmd(cmd);
            }
            return Output;
        }

        static FileSystemWatcher watcher = new FileSystemWatcher(Directory.GetCurrentDirectory());
        static int DelayParm = 0;
        public static void Watch(int time, bool DoFirst = true, bool waiting = true)
        {
            if (DoFirst)
            {
                Start();
            }
            if (time >= 0)
            {
                DelayParm = time;
            }
            else
            {
                DelayParm = 500;
            }
            DelayTime = DelayParm;
            Console.WriteLine("Start watching...");
            InitWatcher();
            if (waiting)
            {
                while (true) { Console.ReadKey(); }
            }

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
            if (TscIsCompiling || IsCompiling)
            {
                return false;
            }
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

        private static void Watcher_Notification(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                FileHelper.AllDir.Add(e.FullPath);
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                int DirIndex = FileHelper.AllDir.IndexOf(e.FullPath);
                if (DirIndex >= 0)
                {
                    FileHelper.AllDir.RemoveAt(DirIndex);
                }
            }
            else if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                int DirIndex = FileHelper.AllDir.IndexOf(e.FullPath);
                if (DirIndex >= 0)
                {
                    FileHelper.AllDir.RemoveAt(DirIndex);
                }
                FileHelper.AllDir.Add(e.FullPath);
            }

            if (!delay()) { return; }
            Console.Clear();
            if (!watcher.EnableRaisingEvents)
            {
                return;
            }
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
