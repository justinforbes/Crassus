﻿using Spartacus.ProcMon;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Spartacus
{
    class Program
    {
        static void Main(string[] args)
        {
            string appVersion = String.Format("{0}.{1}.{2}", Assembly.GetExecutingAssembly().GetName().Version.Major.ToString(), Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString(), Assembly.GetExecutingAssembly().GetName().Version.Build.ToString());
            if (args.Length == 0)
            {
                string help = 
$@"Spartacus v{appVersion} [ Accenture Security ]
- For more information visit https://github.com/Accenture/Spartacus

Usage: Spartacus.exe [options]

--pml                   Location (file) to store the ProcMon event log file. If the file exists,
                        it will be overwritten. When used with --existing-log it will indicate
                        the event log file to read from and will not be overwritten.

Examples:


Parse an existing PML event log output

    --pml C:\tmp\Bootlog.PML

";
                Logger.Info(help, true, false);

#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            
            Logger.Info($"Spartacus v{appVersion}");

            try
            {
                // This will parse everything into RuntimeData.*
                CommandLineParser cmdParser = new CommandLineParser(args);
            } catch (Exception e) {
                Logger.Error(e.Message);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

//            try
//            {
                if (RuntimeData.DetectProxyingDLLs)
                {
                    Logger.Info("Starting DLL Proxying detection");
                    Logger.Info("", true, false);
                    Logger.Info("This feature is not to be relied upon - I just thought it'd be cool to have.", true, false);
                    Logger.Info("The way it works is by checking if a process has 2 or more DLLs loaded that share the same name but different location.", true, false);
                    Logger.Info("For instance 'version.dll' within the application's directory and C:\\Windows\\System32.", true, false);
                    Logger.Info("", true, false);
                    Logger.Info("There is no progress indicator - when a DLL is found it will be displayed here - hit CTRL-C to exit.");

                    Detect detector = new Detect();
                    detector.Run();
                }
                else
                {
                    Manager manager = new Manager();

                    if (!RuntimeData.ProcessExistingLog)
                    {
                        Logger.Verbose("Making sure there are no ProcessMonitor instances...");
                        manager.TerminateProcessMonitor();

                        if (RuntimeData.ProcMonLogFile != "" && File.Exists(RuntimeData.ProcMonLogFile))
                        {
                            Logger.Verbose("Deleting previous log file: " + RuntimeData.ProcMonLogFile);
                            File.Delete(RuntimeData.ProcMonLogFile);
                        }

                        Logger.Info("Getting PMC file...");
                        string pmcFile = manager.GetPMCFile();

                        Logger.Info("Executing ProcessMonitor...");
                        manager.StartProcessMonitor();

                        Logger.Info("Process Monitor has started...");

                        Logger.Warning("Press ENTER when you want to terminate Process Monitor and parse its output...", false, true);
                        Console.ReadLine();

                        Logger.Info("Terminating Process Monitor...");
                        manager.TerminateProcessMonitor();
                    }

                    Logger.Info("Reading events file...");
                    ProcMonPML log = new ProcMonPML(RuntimeData.ProcMonLogFile);

                    Logger.Info("Found " + String.Format("{0:N0}", log.TotalEvents()) + " events...");

                    EventProcessor processor = new EventProcessor(log);
                    processor.Run();

                    Logger.Info("CSV Output stored in: " + RuntimeData.CsvOutputFile);
                    if (RuntimeData.ExportsOutputDirectory != "")
                    {
                        Logger.Info("Proxy DLLs stored in: " + RuntimeData.ExportsOutputDirectory);
                    }
                }
//            }
//            catch (Exception e)
//            {
//                Logger.Error(e.Message);
//#if DEBUG
//                Console.ReadLine();
//#endif
//                return;
//            }

            Logger.Success("All done");

#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
