using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiyiAsm
{
    class FileHelper
    {
        static List<string> CompExtentions = new List<string>() { ".caml", ".cacss", "cajson" };

        public static void NameReplace(string src, string target, string path)
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (string filepath in files)
            {
                FileInfo fileInfo = new FileInfo(filepath);
                string targetName = fileInfo.Name.Replace(fileInfo.Extension, "").Replace(src, target) + fileInfo.Extension;
                File.Move(fileInfo.FullName, fileInfo.DirectoryName + "/" + targetName, true);
            }
        }
        public static List<string> GetAllTargetFiles(string FileType, List<string> Exclude, List<string> FileRange)
        {
            List<string> TargetFiles = new List<string>();
            string[] res = new string[] { };
            if (FileRange.Count == 0)
            {
                res = Directory.GetFiles(Directory.GetCurrentDirectory(), FileType, SearchOption.AllDirectories);
            }
            else
            {
                List<string> ResList = new List<string>();
                foreach (string path in FileRange)
                {
                    if (FileType == "*.js")
                    {
                        ResList = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.js", SearchOption.AllDirectories).ToList();
                        break;
                    }
                    if (FileType == "*.saml" && CompExtentions.Contains(new FileInfo(path).Extension))
                    {
                        ResList = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.saml", SearchOption.AllDirectories).ToList();
                        break;
                    }
                    if (new FileInfo(path).Extension == FileType.Replace("*", ""))
                    {
                        ResList.Add(path);
                    }
                }
                res = ResList.ToArray();
            }

            foreach (string path in res)
            {
                bool skip = false;
                foreach (string exc in Exclude)
                {
                    if (path.Contains(exc.Replace("/", "\\")))
                    {
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    TargetFiles.Add(path);
                }
            }
            return TargetFiles;
        }


        public static int GetDirectoryDepth(string filename)
        {
            filename = filename.Replace("\\", "/");
            int Left = filename.Where(r => r == '/').Count();
            return Left;
        }

        public static string GenerateDotdot(int depth)
        {
            string Dotdot = "";
            for (int i = 0; i < depth; i++)
            {
                Dotdot += "../";
            }
            return Dotdot;
        }

        public static string GenerateDotdot(string filename, string targetDir)
        {
            filename = filename.Replace("\\", "/");
            targetDir = targetDir.Replace("\\", "/");
            string dotdot = "";
            string srcDir = new FileInfo(filename).DirectoryName;
            string SameParent = "";
            for (int i = 0; i < filename.Length && i < targetDir.Length; i++)
            {
                if (filename[i] != targetDir[i])
                {
                    break;
                }
                SameParent += filename[i];
            }
            if (SameParent[SameParent.Length - 1] != '/')
            {
                targetDir += "/";
                SameParent += "/";
            }
            int SameParentDepth = GetDirectoryDepth(SameParent);
            int srcDepth = GetDirectoryDepth(filename);
            dotdot += GenerateDotdot(srcDepth - SameParentDepth);
            if (dotdot == "") { dotdot = "./"; }
            string InPath = targetDir.Remove(0, SameParent.Length);
            if (InPath != "")
            {
                dotdot += InPath;
            }
            else
            {
                dotdot = dotdot.Remove(dotdot.Length - 1);
            }
            return dotdot;
        }
    }
}
