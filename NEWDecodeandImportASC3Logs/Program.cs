﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Dynamic;
using System.Timers;
using MOE.Common;
using System.Collections;

namespace NEWDecodeandImportASC3Logs
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {

                var appSettings = ConfigurationManager.AppSettings;
                List<string> dirList = new List<string>();
                string cwd = appSettings["ASC3LogsPath"];
                int minutesToWait = Convert.ToInt32(ConfigurationManager.AppSettings["MinutesToWait"]);
                foreach (string s in Directory.GetDirectories(cwd))
                {
                    dirList.Add(s);
                }


                // Added by Andre to put command Line Args to this program.

                dirList.Sort();
                var firstFolder = cwd + " ";
                var lastFolder = cwd + "zzzzzzzzz";
                if (args.Length == 0)
                {
                    Console.WriteLine("There are no args.  Do the entire list of folders");
                }
                else if (args.Length == 1)
                {
                    Console.WriteLine(" Only have one arg {0}.  Skip unitl this is the sub-directory", args[0]);
                    firstFolder = cwd + args[0];
                    Console.WriteLine("This is the first folder {0}", firstFolder);
                }
                else
                {
                    Console.WriteLine("There are two or more args : First folder is {0}, last folder is {1} ", args[0], args[1]);
                    firstFolder = cwd + args[0];
                    lastFolder = cwd + args[1];
                }

                foreach (var dir in dirList)

                {
                    if (String.Compare(dir.Trim(), firstFolder.Trim()) >= 0
                        && String.Compare(dir.Trim(), lastFolder.Trim()) < 0)
                    {
                        Console.WriteLine(dir);
                    }
                }

                SimplePartitioner<string> sp = new SimplePartitioner<string>(dirList);
                ParallelOptions optionsMain = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(appSettings["MaxThreadsMain"]) };
                Parallel.ForEach(sp, optionsMain, dir =>
                {
                    if (String.Compare(dir.Trim(), firstFolder.Trim()) >= 0
                        && String.Compare(dir.Trim(), lastFolder.Trim()) < 0)
                    {
                        var toDelete = new ConcurrentBag<string>();
                        var mergedEventsTable = new BlockingCollection<MOE.Common.Data.MOE.Controller_Event_LogRow>();
                        if (Convert.ToBoolean(appSettings["WriteToConsole"]))
                        {
                            Console.WriteLine("-----------------------------Starting Signal " + dir);
                        }
                        string signalId;
                        string[] fileNames;
                        GetFileNamesAndSignalId(dir, out signalId, out fileNames);
                        foreach (var fileName in fileNames)
                        {
                            try
                            {
                                MOE.Common.Business.LogDecoder.Asc3Decoder.DecodeAsc3File(fileName, signalId,
                                    mergedEventsTable, Convert.ToDateTime(appSettings["EarliestAcceptableDate"]));
                                toDelete.Add(fileName);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                        MOE.Common.Data.MOE.Controller_Event_LogDataTable elTable = CreateDataTableForImport();
                        AddEventsToImportTable(mergedEventsTable, elTable);
                        mergedEventsTable.Dispose();
                        BulkImportRecordsAndDeleteFiles(appSettings, toDelete, elTable);
                    }
                });

                Console.WriteLine(DateTime.Now.ToString("t")  +
                    " is time to take a nap, and then Start again.  Program will Wait {0} minutes. ", minutesToWait);
                System.Threading.Thread.Sleep(minutesToWait * 60 * 1000);
            }
        }

        private static void GetFileNamesAndSignalId(string dir, out string signalId, out string[] fileNames)
        {
            string[] strsplit = dir.Split(new char[] { '\\' });
            signalId = strsplit.Last();
            fileNames = Directory.GetFiles(dir, "*.dat?");
        }

        private static void BulkImportRecordsAndDeleteFiles(NameValueCollection appSettings, ConcurrentBag<string> toDelete, MOE.Common.Data.MOE.Controller_Event_LogDataTable elTable)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SPM"].ConnectionString;
            string destTable = appSettings["DestinationTableNAme"];
            MOE.Common.Business.BulkCopyOptions options = new MOE.Common.Business.BulkCopyOptions(connectionString, destTable,
                Convert.ToBoolean(appSettings["WriteToConsole"]),
                Convert.ToBoolean(appSettings["forceNonParallel"]),
                Convert.ToInt32(appSettings["MaxThreads"]),
                Convert.ToBoolean(appSettings["DeleteFile"]),
                Convert.ToDateTime(appSettings["EarliestAcceptableDate"]),
                Convert.ToInt32(appSettings["BulkCopyBatchSize"]),
                Convert.ToInt32(appSettings["BulkCopyTimeOut"]));
            if (elTable.Count > 0)
            {
                if (MOE.Common.Business.SignalFtp.BulktoDb(elTable, options, destTable) && Convert.ToBoolean(appSettings["DeleteFile"]))
                {
                    DeleteFiles(toDelete);
                }
            }
            else
            {
                ConcurrentBag<String> td = new ConcurrentBag<String>();
                foreach (string s in toDelete)
                {
                    if (s.Contains("1970_01_01"))
                    {
                        td.Add(s);
                    }
                }
                if (td.Count > 0)
                {
                    DeleteFiles(td);
                }
            }
        }

        private static void AddEventsToImportTable(BlockingCollection<MOE.Common.Data.MOE.Controller_Event_LogRow> mergedEventsTable, MOE.Common.Data.MOE.Controller_Event_LogDataTable elTable)
        {
            foreach (var r in mergedEventsTable)
            {
                try
                {
                    elTable.AddController_Event_LogRow(r.SignalID, r.Timestamp, r.EventCode,
                        r.EventParam);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static MOE.Common.Data.MOE.Controller_Event_LogDataTable CreateDataTableForImport()
        {
            MOE.Common.Data.MOE.Controller_Event_LogDataTable elTable = new MOE.Common.Data.MOE.Controller_Event_LogDataTable();
            //UniqueConstraint custUnique =
            //new UniqueConstraint(new DataColumn[] { elTable.Columns["SignalId"],
            //                            elTable.Columns["Timestamp"],
            //                            elTable.Columns["EventCode"],
            //                            elTable.Columns["EventParam"]
            //                });

            //elTable.Constraints.Add(custUnique);
            return elTable;
        }

        static public bool SaveAsCsv(DataTable datatable, string path)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<string> columnNames = datatable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));
            foreach (DataRow row in datatable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }
            File.WriteAllText(path, sb.ToString());
            return true;
        }

        public static void DeleteFiles(ConcurrentBag<string> files)
        {
            foreach (string f in files)
            {
                try
                {
                    if (File.Exists(f))
                    {
                        File.Delete(f);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
