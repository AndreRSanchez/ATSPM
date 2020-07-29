﻿//extern alias SharpSNMP;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.EntityFrameworkCore.Internal;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;
using MOE.Common.Properties;

namespace MOE.Common.Business
{
    public class SignalFtp
    {
        private Models.Signal Signal { get; set; }
        private SignalFtpOptions SignalFtpOptions { get; set; }
        private string FileToBeDeleted { get; set; }
        public static FtpSslValidation OnValidateCertificate { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;
            var y = (SignalFtp) obj;
            return this != null && y != null && Signal.SignalID == y.Signal.SignalID;
        }

        public override int GetHashCode()
        {
            return this == null ? 0 : Signal.SignalID.GetHashCode();
        }


        public override string ToString()
        {
            var signalName = Signal.SignalID + " : " + Signal.PrimaryName + " @ " + Signal.SecondaryName;

            return signalName;
            //return base.ToString();
        }

        public SignalFtp()
        {
            
        }

        public SignalFtp(Models.Signal signal, SignalFtpOptions signalFtpOptions)
        {
            Signal = signal;
            SignalFtpOptions = signalFtpOptions;
        }

        private bool TransferFile(FtpClient ftpClient, FtpListItem ftpListItem)
        {
            try
            {
                string localFileName = ftpListItem.Name; 
                //Console.WriteLine(@"Transfering " + ftpListItem.Name + @" from " + Signal.ControllerType.FTPDirectory + @" on " + Signal.IPAddress  + @" to " + SignalFtpOptions.LocalDirectory + Signal.SignalID);
                //chek to see if the local dir exists.
                //if not, make it.
                if (!Directory.Exists(SignalFtpOptions.LocalDirectory))
                {
                    Directory.CreateDirectory(SignalFtpOptions.LocalDirectory);
                }
                if (SignalFtpOptions.RenameDuplicateFiles && File.Exists( SignalFtpOptions.LocalDirectory + Signal.SignalID + @"\" + ftpListItem.Name))
                {
                    char[] fileExtension = new[] {'.', 'd', 'a', 't'};
                    string tempFileName = localFileName.TrimEnd(fileExtension);
                    localFileName = tempFileName + "-" + Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds) + ".dat";
                    FileToBeDeleted = localFileName;
                }
                if(!ftpClient.DownloadFile(SignalFtpOptions.LocalDirectory + Signal.SignalID+ @"\" + localFileName, ".."+Signal.ControllerType.FTPDirectory +@"/" + ftpListItem.Name))
                {
                    Console.WriteLine(@"Unable to download file "+ Signal.ControllerType.FTPDirectory + @"/" + ftpListItem.Name);
                    var errorLog = ApplicationEventRepositoryFactory.Create();
                    errorLog.QuickAdd("FTPFromAllControllers",
                        "MOE.Common.Business.SignalFTP", "TransferFile", ApplicationEvent.SeverityLevels.High, Signal.ControllerType.FTPDirectory + " @ " + Signal.IPAddress + " - " + "Unable to download file " + Signal.ControllerType.FTPDirectory + @"/" + ftpListItem.Name);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                var errorLog = ApplicationEventRepositoryFactory.Create();
                errorLog.QuickAdd("FTPFromAllControllers",
                    "MOE.Common.Business.SignalFTP", "TransferFile", ApplicationEvent.SeverityLevels.High, "SignalID " + Signal.SignalID + " > " + Signal.ControllerType.FTPDirectory + @"/" + ftpListItem.Name  + " File can't be downloaded. Error Mesage " + e.Message);
                File.Delete(SignalFtpOptions.LocalDirectory + Signal.SignalID + @"\" + FileToBeDeleted);
                return false;
            }
        }


        public static void Delete1EosFiles()
        {
            List<string> hostList = new List<string>
            {
                "10.235.15.143",
                "10.235.6.27",
                "10.235.11.27",
                "10.235.9.239",
                "10.235.11.15",
                "10.167.6.39",
                "10.167.5.203",
                "10.162.10.143",
                "10.167.6.155",
                "10.210.233.15",
                "10.210.232.143",
                "10.163.6.15",
                "10.163.6.15",
                "10.164.5.167",
                "10.164.6.217",
                "10.164.6.203",
                "10.164.6.191",
                "10.164.6.169",
                "10.204.12.27",
                "10.210.197.15",
                "10.168.6.203",
                "10.212.12.179",
                "10.212.19.39",
                "10.138.205.15",
                "10.138.204.143",
                "10.138.203.15",
                "10.138.205.143",
                "10.138.202.15",
                "10.138.203.143",
                "10.209.2.102"
            };




            foreach (var host in hostList)
            {
                FtpClient sftpTest = new FtpClient
                {
                    Credentials = new NetworkCredential("econolite", "ecpi2ecpi"),
                    DataConnectionType = FtpDataConnectionType.PASV,
                    DataConnectionEncryption = true,
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    EnableThreadSafeDataConnections = true,
                    EncryptionMode = FtpEncryptionMode.None
                };
                //X509Certificate2 cert = new X509Certificate2(@"C:\Temp\MakeCert\certificate.crt");
                //sftpTest.ClientCertificates.Add(cert);

                sftpTest.Host = host;
                Console.WriteLine(" Before Connection, host is {0}", host);
                sftpTest.Connect();
                Console.WriteLine("After Connection for {0}", host);
                var filePattern = ".datZ";
                int counter = 0;
                sftpTest.SetWorkingDirectory("set1");

                using (sftpTest)
                {
                    FtpListItem[] remoteFiles = sftpTest.GetListing();
                    Console.WriteLine("    There are {0} files.", remoteFiles.Length);
                    foreach (var getFtpFile in remoteFiles)
                    {
                        if (getFtpFile.Type == FtpFileSystemObjectType.File && getFtpFile.Name.EndsWith(filePattern))
                        {
                            string fileName = getFtpFile.Name;
                            sftpTest.DeleteFile(fileName);
                            Console.Write(" {0}.  This file is deleted -> {1}\r", counter, fileName);
                            counter++;
                        }
                        if (counter > 20) break;
                    }
                }
                sftpTest.Disconnect();
                Console.WriteLine();
                Console.WriteLine("Finished for Host {0} ", host);
            }
            System.Environment.Exit(1);
            Console.WriteLine("Need to Stop now");
        }


        private static bool OnValidadateCertiticate()
        {
            return true;
        }

        public void GetCurrentRecords()
        {

            var errorRepository = ApplicationEventRepositoryFactory.Create();
            FtpClient ftpClient = new FtpClient(Signal.IPAddress);
            ftpClient.Credentials = new NetworkCredential(Signal.ControllerType.UserName, Signal.ControllerType.Password);
            ftpClient.ConnectTimeout = SignalFtpOptions.FtpConectionTimeoutInSeconds * 1000;
            ftpClient.ReadTimeout = SignalFtpOptions.FtpReadTimeoutInSeconds * 1000;
            if (Signal.ControllerType.ActiveFTP)
            {
            //    ftpClient.DataConnectionType = FtpDataConnectionType.AutoActive;
                ftpClient.DataConnectionType = FtpDataConnectionType.AutoActive;
            }

            var filePattern = ".dat";
            var maximumFilesToTransfer = SignalFtpOptions.MaximumNumberOfFilesTransferAtOneTime;
            if (Signal.ControllerTypeID == 9)
            {
                filePattern = ".datZ";
                maximumFilesToTransfer  *= 12;
            }
            using (ftpClient)
            {
                try
                {
                    ftpClient.Connect();
                }
                catch (AggregateException)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "GetCurrentRecords_ConnectToController", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Connection Failure - One or more errors occured");
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Connection Failure - One or more errors occured before connection established");
                    return;
                }
                catch (SocketException ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "GetCurrentRecords_ConnectToController", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    return;
                }
                catch (IOException ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "GetCurrentRecords_ConnectToController", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    return;
                }
                catch (Exception ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "GetCurrentRecords_ConnectToController", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    return;
                }
                
                if (ftpClient.IsConnected && ftpClient.DirectoryExists(".." + Signal.ControllerType.FTPDirectory))
                {
                    try
                    {
                        FtpListItem[] remoteFiles = ftpClient.GetListing(".." + Signal.ControllerType.FTPDirectory);
                        var retrievedFiles = new List<string>();
                        if (remoteFiles != null)
                        {
                            DateTime localDate = DateTime.Now;
                            if (SignalFtpOptions.SkipCurrentLog)
                            {
                                localDate = localDate.AddMinutes(-16);
                            }
                            else
                            {
                                localDate = localDate.AddMinutes(120);
                            }
                            var  fileTransferedCounter = 1;
                            foreach (var ftpFile in remoteFiles)
                            {
                                if(fileTransferedCounter > maximumFilesToTransfer) { break; }
                                if(ftpFile.Type == FtpFileSystemObjectType.File && ftpFile.Name.Contains(filePattern) && ftpFile.Created < localDate)
                                {
                                    try
                                    {
                                        if (TransferFile(ftpClient, ftpFile))
                                        {
                                            retrievedFiles.Add(ftpFile.Name);
                                            fileTransferedCounter++;
                                        }
                                    }
                                    //If there is an error, Print the error and try the file again.
                                    catch (AggregateException)
                                    {
                                        errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal",
                                            "GetCurrentRecords_TransferFiles",
                                            Models.ApplicationEvent.SeverityLevels.Medium,
                                            Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Transfer Task Timed Out");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage =
                                            "Exception:" + ex.Message + " While Transfering file: " + ftpFile +
                                            " from signal" + Signal.SignalID + " @ " + Signal.ControllerType.FTPDirectory + " on " + Signal.IPAddress + " to " +
                                            SignalFtpOptions.LocalDirectory + Signal.SignalID;
                                        Console.WriteLine(errorMessage);
                                        errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal",
                                            "GetCurrentRecords_TransferFiles",
                                            Models.ApplicationEvent.SeverityLevels.Medium, errorMessage);
                                        //retryFiles.Add(ftpFile.Name);
                                        break;
                                    }
                                    if(SignalFtpOptions.WaitBetweenFileDownloadInMilliseconds  > 0)
                                        Thread.Sleep(SignalFtpOptions.WaitBetweenFileDownloadInMilliseconds);
                                }
                            }
                            //Delete the files we downloaded.  We don't want to have to deal with the file more than once.  If we delete the file form the controller once we capture it, it will reduce redundancy.
                            if (SignalFtpOptions.DeleteAfterFtp)
                            {
                                if (Signal.ControllerTypeID == 9)

                                {
                                    ftpClient.Disconnect();
                                    DeleteAllEosFiles(Signal, retrievedFiles);
                                }
                                else
                                {
                                    DeleteFilesFromFtpServer(ftpClient, retrievedFiles); //, Token);
                                }
                            }
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "GetCurrentRecords_RetrieveFiles",
                            Models.ApplicationEvent.SeverityLevels.Medium,
                            Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                        Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    }
                    //ftp.Close();
                }

                else
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "GetCurrentRecords_ConnectToController", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Cannot find directory " + Signal.ControllerType.FTPDirectory);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Cannot find directory " + Signal.ControllerType.FTPDirectory);
                    return;
                }
                //Turn Logging off.
                //The ASC3 controller stoploggin if the current file is removed.  to make sure logging continues, we must turn the loggin feature off on the 
                //controller, then turn it back on.
                try
                {
                    if (SignalFtpOptions.SkipCurrentLog && CheckAsc3LoggingOverSnmp())
                    {
                        //Do Nothing
                    }
                    else
                    {
                        try
                        {
                            TurnOffAsc3LoggingOverSnmp();
                            Thread.Sleep(SignalFtpOptions.SnmpTimeout);
                            TurnOnAsc3LoggingOverSnmp();
                        }
                        catch
                        {

                        }
                    }
                }
                catch
                {
                }
            }
        }

        private void DeleteAllEosFiles(Signal signal, List<string> retrievedFiles)
        {
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine("Start of DeleteAllEosFIles");
            //Console.WriteLine("The Host is {0}", signal.IPAddress.ToString());
            FtpClient sftpEos = new FtpClient
            {
            Credentials = new NetworkCredential(signal.ControllerType.UserName, signal.ControllerType.Password),
                DataConnectionType = FtpDataConnectionType.PASV,
                DataConnectionEncryption = true,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                EnableThreadSafeDataConnections = true,
                EncryptionMode = FtpEncryptionMode.None
            };
            sftpEos.Host = signal.IPAddress.ToString();
            sftpEos.Connect();
            Console.WriteLine("After Connection for {0} signalId {1} is connected ",
                signal.IPAddress.ToString(), signal.SignalID, sftpEos.IsConnected.ToString());
            var errorRepository = ApplicationEventRepositoryFactory.Create();
            var filePattern = ".datZ";
            var remotePWD = sftpEos.GetWorkingDirectory();
            using (sftpEos)
            {
                //Console.WriteLine(" In the retrieved files, there are {0} files.  PWD is {1}", retrievedFiles.Count(), remotePWD);
                foreach (var ftpFileName in retrievedFiles)
                {
                   if (ftpFileName.EndsWith(filePattern))
                   {
                        try
                        {
                            sftpEos.DeleteFile("../" + Signal.ControllerType.FTPDirectory + "/" + ftpFileName);
                           // Console.Write(" This file is deleted -> ..{0}/{1}\r", Signal.ControllerType.FTPDirectory, ftpFileName);
                        }
                        catch (AggregateException)
                        {
                            errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Connection Failure one or more errors occured deleting the file via FTP");
                            Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Connection Failure one or more errors occured deleting the file via FTP");
                        }
                        catch (SocketException ex)
                        {
                            errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        catch (IOException ex)
                        {
                            errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                        catch (Exception ex)
                        {
                            errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                            Console.WriteLine("Exception:" + ex.Message + " While Deleting file: " + ftpFileName + " from " + Signal.ControllerType.FTPDirectory + " on " + Signal.IPAddress);
                        }
                        if (SignalFtpOptions.WaitBetweenFileDownloadInMilliseconds > 0)
                            Thread.Sleep(SignalFtpOptions.WaitBetweenFileDownloadInMilliseconds);
                   }
                }
            }
            sftpEos.Disconnect();
        }

        private void DeleteFilesFromFtpServer(FtpClient ftpClient, List<String> filesToDelete)
        {
            var errorRepository = ApplicationEventRepositoryFactory.Create();

            foreach (var ftpFile in filesToDelete)
            {
                try
                {
                    ftpClient.DeleteFile(Signal.ControllerType.FTPDirectory + "/" + ftpFile);
                    //Console.Write("For Signal {0], this file is deleted: {1}\r", Signal.SignalID, ftpFile);
                }
                catch (AggregateException)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Connection Failure one or more errors occured deleting the file via FTP");
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + "Connection Failure one or more errors occured deleting the file via FTP");
                }
                catch (SocketException ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(ex.Message);
                }
                catch (IOException ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(ex.Message);
                }
                catch (Exception ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "DeleteFilesFromFTPServer", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine("Exception:" + ex.Message + " While Deleting file: " + ftpFile + " from " + Signal.ControllerType.FTPDirectory + " on " + Signal.IPAddress);
                }
                if(SignalFtpOptions.WaitBetweenFileDownloadInMilliseconds > 0)
                    Thread.Sleep(SignalFtpOptions.WaitBetweenFileDownloadInMilliseconds);
            }
        }

        private void TurnOffAsc3LoggingOverSnmp()
        {
            var errorRepository = ApplicationEventRepositoryFactory.Create();

            for (var counter = 0; counter < SignalFtpOptions.SnmpRetry; counter++)
            {
                try
                {
                    SmnpSet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "0", "i", SignalFtpOptions.SnmpPort);
                }
                catch (SnmpException ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", " TurnOffASC3LoggingOverSNMP",
                        ApplicationEvent.SeverityLevels.Medium, Signal.IPAddress + " " + ex.Message);
                    Console.WriteLine(ex);
                }
                var snmpState = 10;
                try
                {
                    snmpState = SnmpGet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "0", "i");
                }
                catch (SnmpException ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "TurnOffASC3LoggingOverSNMP_Get", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex);
                }
                if (snmpState == 0)
                    break;
                Thread.Sleep(SignalFtpOptions.SnmpTimeout);
            }
        }

        private void TurnOnAsc3LoggingOverSnmp()
        {
            var errorRepository = ApplicationEventRepositoryFactory.Create();

            for (var counter = 0; counter < SignalFtpOptions.SnmpRetry; counter++)
            {
                try
                {
                    SmnpSet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "1", "i", SignalFtpOptions.SnmpPort);
                }
                catch (Exception ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", " TurnOnASC3LoggingOverSNMP_Set", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex);
                }
                var snmpState = 10;
                try
                {
                    snmpState = SnmpGet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "1", "i");
                }
                catch (Exception ex)
                {
                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", " TurnOnASC3LoggingOverSNMP_Get", Models.ApplicationEvent.SeverityLevels.Medium, Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex);
                }
                if (snmpState == 1)
                    break;
                Thread.Sleep(SignalFtpOptions.SnmpTimeout);
            }
        }

        private bool CheckAsc3LoggingOverSnmp()
        {
            MOE.Common.Models.Repositories.IApplicationEventRepository errorRepository =
                MOE.Common.Models.Repositories.ApplicationEventRepositoryFactory.Create();
            bool success = false;
            for (int counter = 0; counter < SignalFtpOptions.SnmpRetry; counter++)
            {
                int snmpState = 10;
                try
                {
                    snmpState = SnmpGet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "1", "i");
                }
                catch (Exception ex)
                {

                    errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", " CheckASC3LoggingOverSNMP_Get",
                        Models.ApplicationEvent.SeverityLevels.Medium,
                        Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex.Message);
                    Console.WriteLine(Signal.SignalID + " @ " + Signal.IPAddress + " - " + ex);
                }
                if (snmpState == 1)
                {
                    success = true;
                    break;
                }
                Thread.Sleep(SignalFtpOptions.SnmpTimeout);
            }
            return success;
        }

        //public static bool DecodeAsc3File(string fileName, string Signal.SignalID, BulkCopyOptions options)
        //{
        //    var encoding = Encoding.ASCII;
        //    try
        //    {
        //        using (var br = new BinaryReader(File.Open(fileName, FileMode.Open), encoding))
        //        {
        //            var elTable = new DataTable();
        //            var custUnique =
        //                new UniqueConstraint(new[]
        //                {
        //                    elTable.Columns["SignalID"],
        //                    elTable.Columns["Timestamp"],
        //                    elTable.Columns["EventCode"],
        //                    elTable.Columns["EventParam"]
        //                });

        //            elTable.Constraints.Add(custUnique);

        //            if (br.BaseStream.Position + 21 < br.BaseStream.Length)
        //            {
        //                //Find the start Date
        //                var dateString = "";
        //                for (var i = 1; i < 21; i++)
        //                {
        //                    var c = br.ReadChar();
        //                    dateString += c;
        //                }

        //                //Console.WriteLine(dateString);
        //                var startTime = new DateTime();
        //                if (DateTime.TryParse(dateString, out startTime) &&
        //                    br.BaseStream.Position < br.BaseStream.Length)
        //                {
        //                    //find  line feed characters, that should take us to the end of the header.
        //                    // First line break is after Version
        //                    // Second LF is after FileName
        //                    // Third LF is after Interseciton number, which isn't used as far as I can tell
        //                    // Fourth LF is after IP address
        //                    // Fifth is after MAC Address
        //                    // Sixth is after "Controller data log beginning:," and then the date
        //                    // Seven is after "Phases in use:," and then the list of phases, seperated by commas

        //                    var i = 0;

        //                    while (i < 7 && br.BaseStream.Position < br.BaseStream.Length)
        //                    {
        //                        var c = br.ReadChar();
        //                        //Console.WriteLine(c.ToString());
        //                        if (c == '\n')
        //                            i++;
        //                    }

        //                    //The first record alwasy seems to be missing a timestamp.  Econolite assumes the first even occures at the same time
        //                    // the second event occurs.  I am going to tag the first event with secondevet.timestamp - 1/10 second
        //                    var firstEventCode = new int();
        //                    var firstEventParam = new int();


        //                    if (br.BaseStream.Position + sizeof(char) < br.BaseStream.Length)
        //                        firstEventCode = Convert.ToInt32(br.ReadChar());

        //                    if (br.BaseStream.Position + sizeof(char) < br.BaseStream.Length)
        //                        firstEventParam = Convert.ToInt32(br.ReadChar());

        //                    var firstEventEntered = false;
        //                    //MOE.Common.Business.ControllerEvent firstEvent = new MOE.Common.Business.ControllerEvent(SignalID, StartTime, firstEventCode, firstEventParam);


        //                    //After that, we can probably start reading
        //                    while (br.BaseStream.Position + sizeof(byte) * 4 <= br.BaseStream.Length
        //                    ) //we need ot make sure we are more that 4 characters from the end
        //                    {
        //                        var eventTime = new DateTime();
        //                        var eventCode = new int();
        //                        var eventParam = new int();

        //                        //MOE.Common.Business.ControllerEvent controllerEvent = null;
        //                        for (var eventPart = 1; eventPart < 4; eventPart++)
        //                        {
        //                            //getting the time offset
        //                            if (eventPart == 1)
        //                            {
        //                                var rawoffset = new byte[2];
        //                                //char[] offset = new char[2];
        //                                rawoffset = br.ReadBytes(2);
        //                                Array.Reverse(rawoffset);
        //                                int offset = BitConverter.ToInt16(rawoffset, 0);

        //                                var tenths = Convert.ToDouble(offset) / 10;

        //                                eventTime = startTime.AddSeconds(tenths);
        //                            }

        //                            //getting the EventCode
        //                            if (eventPart == 2)
        //                                eventCode = Convert.ToInt32(br.ReadByte());

        //                            if (eventPart == 3)
        //                                eventParam = Convert.ToInt32(br.ReadByte());
        //                        }

        //                        //controllerEvent = new MOE.Common.Business.ControllerEvent(SignalID, EventTime, EventCode, EventParam);

        //                        if (eventTime <= DateTime.Now && eventTime > Settings.Default.EarliestAcceptableDate)
        //                            if (!firstEventEntered)
        //                            {
        //                                try
        //                                {
        //                                    elTable.Rows.Add(Signal.SignalID, eventTime.AddMilliseconds(-1), firstEventCode,
        //                                        firstEventParam);
        //                                }
        //                                catch
        //                                {
        //                                }
        //                                try
        //                                {
        //                                    elTable.Rows.Add(Signal.SignalID, eventTime, eventCode, eventParam);
        //                                }
        //                                catch
        //                                {
        //                                }
        //                                firstEventEntered = true;
        //                            }
        //                            else
        //                            {
        //                                try
        //                                {
        //                                    elTable.Rows.Add(Signal.SignalID, eventTime, eventCode, eventParam);
        //                                }
        //                                catch
        //                                {
        //                                }
        //                            }
        //                    }
        //                }
        //                //this is what we do when the datestring doesn't parse
        //                else
        //                {
        //                    return TestNameAndLength(fileName);
        //                }
        //            }


        //            else
        //            {
        //                return TestNameAndLength(fileName);
        //            }


        //            if (BulktoDb(elTable, options))
        //                return true;
        //            return SplitBulkToDb(elTable, options);
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}


        //If the file is tiny, it is likely empty, and we want to delte it (return true)
        //If the file has INT in the name, it is likely the old file version, and we want to delete it (return true)
        public static bool TestNameAndLength(string filePath)
        {
            try
            {
                var f = new FileInfo(filePath);
                if (f.Name.Contains("INT") || f.Length < 367)
                    return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool SaveAsCsv(DataTable table, string path)
        {
            var sb = new StringBuilder();

            var columnNames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in table.Rows)
            {
                var fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(path, sb.ToString());
            return true;
        }


        public static bool BulktoDb(DataTable elTable, BulkCopyOptions options, string tableName)
        {
            MOE.Common.Models.Repositories.IApplicationEventRepository errorRepository = MOE.Common.Models.Repositories.ApplicationEventRepositoryFactory.Create();
            using (options.Connection)
            {
                using (var bulkCopy =
                    new SqlBulkCopy(options.ConnectionString, SqlBulkCopyOptions.UseInternalTransaction))
                {
                    for (var i = 1;; i++)
                    {
                        try
                        {
                            options.Connection.Open();
                        }
                        catch
                        {
                            Thread.Sleep(Settings.Default.SleepTime);
                        }
                        if (options.Connection.State == ConnectionState.Open)
                        {
                            if (Settings.Default.WriteToConsole)
                                Console.WriteLine("DB connection established");

                            break;
                        }
                    }
                    var sigId = "";
                    if (elTable.Rows.Count > 0)
                    {
                        var row = elTable.Rows[0];
                        sigId = row[0].ToString();
                    }

                    if (options.Connection.State == ConnectionState.Open)
                    {
                        bulkCopy.BulkCopyTimeout = Settings.Default.BulkCopyTimeout;

                        bulkCopy.BatchSize = Settings.Default.BulkCopyBatchSize;

                        if (Settings.Default.WriteToConsole)
                        {
                            Console.WriteLine(" For Signal {0} There are rows {1}", sigId, elTable.Rows.Count);
                            bulkCopy.SqlRowsCopied +=
                                OnSqlRowsCopied;
                            //Console.WriteLine( " For Signal {0}", sigId);
                            bulkCopy.NotifyAfter = Settings.Default.BulkCopyBatchSize;
                        }
                        bulkCopy.DestinationTableName = tableName;

                        if (elTable.Rows.Count > 0)
                        {
                            try
                            {
                                bulkCopy.WriteToServer(elTable);
                                if (Settings.Default.WriteToConsole)
                                {
                                    Console.WriteLine("!!!!!!!!! The bulk insert executed for Signal " + sigId +
                                                      " !!!!!!!!!");
                                }
                                options.Connection.Close();
                                return true;
                            }
                            catch (SqlException ex)
                            {
                                if (ex.Number == 2601)
                                {
                                    if (Properties.Settings.Default.WriteToConsole)
                                    {
                                        errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "BulktoDB_SQL.ex",
                                            Models.ApplicationEvent.SeverityLevels.Medium,
                                            "There is a permission error - " + sigId + " - " + ex.Message);
                                        Console.WriteLine("**** There is a permission error - " + sigId + " *****");
                                    }
                                }
                                else
                                {
                                    if (Properties.Settings.Default.WriteToConsole)
                                    {
                                        //Console.WriteLine("****DATABASE ERROR*****");
                                        errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "BulktoDB_SQL.ex",
                                            Models.ApplicationEvent.SeverityLevels.Medium,
                                            "General Error - " + sigId + " - " + ex.Message);
                                        Console.WriteLine("DATABASE ERROR - " + sigId + " - " + ex.Message);
                                    }
                                }
                                options.Connection.Close();
                                return false;
                            }
                            catch (Exception ex)
                            {
                                errorRepository.QuickAdd("FTPFromAllcontrollers", "Signal", "BulktoDB_Reg.ex",
                                    Models.ApplicationEvent.SeverityLevels.Medium,
                                    "General Error - " + sigId + " - " + ex.Message);
                                Console.WriteLine(ex);
                                return false;
                            }
                        }
                        else
                        {
                            options.Connection.Close();
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }


        private static void OnSqlRowsCopied(
            object sender, SqlRowsCopiedEventArgs e)

        {
            Console.WriteLine(" {0} thousand rows copied, so far...", e.RowsCopied/1000);
        }

        public static bool SplitBulkToDb(DataTable elTable, BulkCopyOptions options, string tableName)
        {
            if (elTable.Rows.Count > 0)
            {
                var topDt = new DataTable();
                var bottomDt = new DataTable();

                var top = Convert.ToInt32(elTable.Rows.Count * .1);

                var bottom = elTable.Rows.Count - top;


                var dtTop = new DataTable();
                try
                {
                    dtTop = elTable.Rows.Cast<DataRow>().Take(elTable.Rows.Count / 2).CopyToDataTable();
                }
                catch (Exception ex)
                {
                    return false;
                }

                if (dtTop.Rows.Count > 0)
                {
                    topDt.Merge(dtTop);
                    if (dtTop.Rows.Count > 0)
                        if (BulktoDb(topDt, options, tableName))
                        {
                        }
                        else
                        {
                            var elTable2 = new DataTable();
                            elTable2.Merge(topDt.Copy());
                            LineByLineWriteToDb(elTable2);
                        }
                    topDt.Clear();
                    topDt.Dispose();
                    dtTop.Clear();
                    dtTop.Dispose();
                }


                var dtBottom = new DataTable();
                try
                {
                    dtBottom = elTable.Rows.Cast<DataRow>().Take(elTable.Rows.Count / 2).CopyToDataTable();
                }
                catch (Exception ex)
                {
                    return false;
                }
                if (dtBottom.Rows.Count > 0)
                {
                    bottomDt.Merge(dtBottom);

                    if (bottomDt.Rows.Count > 0)
                        if (BulktoDb(bottomDt, options, tableName))
                        {
                        }
                        else
                        {
                            var elTable2 = new DataTable();
                            elTable2.Merge(bottomDt.Copy());
                            LineByLineWriteToDb(elTable2);
                        }
                    bottomDt.Clear();
                    bottomDt.Dispose();
                    dtBottom.Clear();
                    dtBottom.Dispose();
                }

                return true;
            }
            return false;
        }

        public static bool LineByLineWriteToDb(DataTable elTable)
        {
            //MOE.Common.Data.MOETableAdapters.QueriesTableAdapter moeTA = new MOE.Common.Data.MOETableAdapters.QueriesTableAdapter();
            using (var db = new SPM())
            {
                foreach (DataRow row in elTable.Rows)
                    //Parallel.ForEach(elTable.AsEnumerable(), row =>
                    try
                    {
                        var r = new Controller_Event_Log();
                        r.SignalID = row[0].ToString();
                        r.Timestamp = Convert.ToDateTime(row[1]);
                        r.EventCode = Convert.ToInt32(row[2]);
                        r.EventParam = Convert.ToInt32(row[3]);


                        if (Settings.Default.WriteToConsole)
                            Console.WriteLine("---Inserting line for ControllerType {0} at {1}---", row[0], row[1]);

                        db.Controller_Event_Log.Add(r);
                        db.SaveChangesAsync();
                    }
                    catch (SqlException sqlex)
                    {
                        if (sqlex.Number == 2627)
                        {
                            if (Settings.Default.WriteToConsole)
                                Console.WriteLine("Duplicate line for signal {0} at {1}", row[0], row[1]);
                            //duplicateLineCount++;
                        }

                        else
                        {
                            //insertErrorCount++;
                            if (Settings.Default.WriteToConsole)
                                Console.WriteLine(
                                    "Exeption {0} \n While Inserting a line for controller {1} on timestamp {2}", sqlex,
                                    row[0], row[1]);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Settings.Default.WriteToConsole)
                            Console.WriteLine(
                                "Exeption {0} \n While Inserting a line for controller {1} on timestamp {2}", ex,
                                row[0], row[1]);
                    }
                //);

                return true;
            }
        }


        private static int SnmpGet(string controllerAddress, string objectIdentifier, string value, string type)
        {
            var ipControllerAddress = IPAddress.Parse(controllerAddress);
            var community = "public";
            var timeout = 1000;
            var version = VersionCode.V1;
            var receiver = new IPEndPoint(ipControllerAddress, 161);
            var oid = new ObjectIdentifier(objectIdentifier);
            var vList = new List<Variable>();
            ISnmpData data = new Integer32(int.Parse(value));
            var oiddata = new Variable(oid, data);
            vList.Add(new Variable(oid));
            var retrievedValue = 0;
            try
            {
                var variable = Messenger.Get(version, receiver, new OctetString(community), vList, timeout)
                    .FirstOrDefault();

                Console.WriteLine(controllerAddress + " - Check state = {0}", variable.Data.ToString());
                retrievedValue = int.Parse(variable.Data.ToString());
            }
            catch (SnmpException snmPex)
            {
                Console.WriteLine(controllerAddress + " - " + snmPex.ToString());
            }
            catch (SocketException socketex)
            {
                Console.WriteLine(controllerAddress + " - " + socketex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(controllerAddress + " - " + ex.ToString());
            }
            return retrievedValue;
        }


        private static void SmnpSet(string controllerAddress, string objectIdentifier, string value, string type, int snmpPort)
        {
            var ipControllerAddress = IPAddress.Parse(controllerAddress);
            var community = "public";
            var timeout = 1000;
            var version = VersionCode.V1;
            var receiver = new IPEndPoint(ipControllerAddress, snmpPort);
            var oid = new ObjectIdentifier(objectIdentifier);
            var vList = new List<Variable>();
            ISnmpData data = new Integer32(int.Parse(value));
            var oiddata = new Variable(oid, data);
            vList.Add(oiddata);
            try
            {
                Messenger.Set(version, receiver, new OctetString(community), vList, timeout);
                Console.WriteLine(vList.FirstOrDefault());
            }
            catch (SnmpException snmPex)
            {
                Console.WriteLine(controllerAddress + " - " + snmPex.ToString());
            }
            catch (SocketException socketex)
            {
                Console.WriteLine(controllerAddress + " - " + socketex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(controllerAddress + " - " + ex.ToString());
            }
        }

        public void EnableLogging()
        {
            for (var counter = 0; counter < SignalFtpOptions.SnmpRetry; counter++)
            {
                try
                {
                    SmnpSet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "0", "i", SignalFtpOptions.SnmpPort);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Signal.IPAddress + " - " + ex);
                }
                var snmpState = 10;
                try
                {
                    snmpState = SnmpGet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "0", "i");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Signal.IPAddress + " - " + ex);
                }
                if (snmpState == 0)
                    break;
                Thread.Sleep(SignalFtpOptions.SnmpTimeout);
            }
            Thread.Sleep(SignalFtpOptions.SnmpTimeout);


            //turn logging on
            for (var counter = 0; counter < SignalFtpOptions.SnmpRetry; counter++)
            {
                try
                {
                    SmnpSet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "1", "i", SignalFtpOptions.SnmpPort);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Signal.IPAddress + " - " + ex);
                }

                var snmpState = 10;
                try
                {
                    snmpState = SnmpGet(Signal.IPAddress, "1.3.6.1.4.1.1206.3.5.2.9.17.1.0", "1", "i");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Signal.IPAddress + " - " + ex);
                }
                if (snmpState == 1)
                    break;
                Thread.Sleep(SignalFtpOptions.SnmpTimeout);
            }
        }
    }

    public class SignalFtpOptions
    {
        public SignalFtpOptions(
            int snmpTimeout, 
            int snmpRetry,
            int snmpPort, 
            bool deleteAfterFtp, 
            string localDirectory, 
            int ftpConectionTimeoutInSeconds, 
            int ftpReadTimeoutInSeconds, 
            bool skipCurrentLog, 
            bool renameDuplicateFiles, 
            int waitBetweenFileDownloadInMilliseconds, 
            int maximumNumberOfFilesTransferAtOneTime)
        {
            SnmpTimeout = snmpTimeout;
            SnmpRetry = snmpRetry;
            SnmpPort = snmpPort;
            DeleteAfterFtp = deleteAfterFtp;
            LocalDirectory = localDirectory;
            FtpConectionTimeoutInSeconds = ftpConectionTimeoutInSeconds;
            FtpReadTimeoutInSeconds = ftpReadTimeoutInSeconds;
            SkipCurrentLog = skipCurrentLog;
            RenameDuplicateFiles = renameDuplicateFiles;
            WaitBetweenFileDownloadInMilliseconds = waitBetweenFileDownloadInMilliseconds;
            MaximumNumberOfFilesTransferAtOneTime = maximumNumberOfFilesTransferAtOneTime;
        }

        public int SnmpTimeout { get; set; }
        public int SnmpRetry { get; set; }
        public int SnmpPort { get; set; }
        public bool DeleteAfterFtp { get; set; }
        public int FtpConectionTimeoutInSeconds { get; set; }
        public int FtpReadTimeoutInSeconds { get; set; }
        public bool SkipCurrentLog { get; set; }
        public bool RenameDuplicateFiles { get; set; }
        public int WaitBetweenFileDownloadInMilliseconds { get; set; }
        public string LocalDirectory { get; set; }
        public int MaximumNumberOfFilesTransferAtOneTime { get; set; }
        
    }
    
}