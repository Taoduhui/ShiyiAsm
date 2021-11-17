using System;
using Microsoft.Extensions.Configuration;

namespace ShiyiAsm
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();
            if (config["w"] == null)
            {
                Assember.Start();
            }
            else
            {
                Assember.Watch(Convert.ToInt32(config["w"]));
            }
        }
    }
}
