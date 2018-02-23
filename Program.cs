using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

class Program
{

    static void Main(string[] args)
    {
        string strTab = "\t";
        string AppPath = AppDomain.CurrentDomain.BaseDirectory;
        string cfgServers = AppPath + "servers.txt";
        List<RDServer> myServers = new List<RDServer>();
        int clientCounter = 0;

        if(File.Exists(cfgServers))
        {
            try
            {
                var ServerList = System.IO.File.ReadAllLines(cfgServers);
                foreach (var Server in ServerList)
                {
                    if (Server.Length > 0)
                    {
                        Console.WriteLine("Collecting Sessions from " + Server);

                        List<TerminalSessionData> mySessions = new List<TerminalSessionData>();
                        mySessions = TermServicesManager.ListSessions(Server);

                        RDServer myRDServer = new RDServer();
                        List<RDSession> RDSessions = new List<RDSession>();

                        myRDServer.Servername = Server;

                        foreach (var mySession in mySessions)
                        {
                            if (mySession.ConnectionState == TermServicesManager.WTS_CONNECTSTATE_CLASS.Active)
                            {

                                RDSession myRDSession = new RDSession();
                                
                                myRDSession.Clientname = TermServicesManager.GetSessionInfo(Server, mySession.SessionId).ClientName;
                                myRDSession.Username = TermServicesManager.GetSessionInfo(Server, mySession.SessionId).UserName;
                                myRDSession.SessionID = TermServicesManager.GetSessionInfo(Server, mySession.SessionId).SessionId;
                                clientCounter = clientCounter + 1;
                                myRDSession.overallSessionID = clientCounter;
                                RDSessions.Add(myRDSession);
                            }
                        }

                        myRDServer.Sessions = RDSessions;
                        myServers.Add(myRDServer);
                    }
                        
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                Environment.Exit(1);
            }
            // Error: Use of unassigned local variable 'n'.  
        }

        Console.WriteLine(".. finished collecting.");

        foreach(var Server in myServers)
        {
            foreach(var Session in Server.Sessions)
            {
                Console.WriteLine("ID: " + Session.overallSessionID + strTab + Server.Servername + "-Session#" + Session.SessionID + strTab + Session.Username + strTab + Session.Clientname);
            }
        }

        string cInput = "";
        Console.WriteLine("ID zur fernsteuerung eingeben/mit Befehl exit beenden: (funktioniert noch nicht)");

        do
        {
            cInput = Console.ReadLine();

            if (IsNumeric(cInput))
            {

                Console.WriteLine("Versuche Session " + cInput + " zu starten...");

                int InputSessionID;

                Int32.TryParse(cInput, out InputSessionID);

                foreach (var Server in myServers)
                {
                    foreach (var Session in Server.Sessions)
                    {
                        if (Session.overallSessionID == InputSessionID)
                        {
                            System.Diagnostics.Process process = new System.Diagnostics.Process();
                            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            startInfo.FileName = "mstsc.exe";
                            startInfo.Arguments = "/v: " + Server.Servername + " /shadow:" + Session.SessionID;
                            process.StartInfo = startInfo;
                            process.Start();
                        }
                    }
                }
               
                


            }
            else
            {
                Console.WriteLine("Nummer.. sonst nix.. bitte: ");
            }

        } while (cInput != "exit");



    }

    public class RDServer
    {
        public RDServer() { Sessions = new List<RDSession>(); }

        public string Servername { get; set; }
        public List<RDSession> Sessions { get; set; }
    }

    public class RDSession
    {
        public string Username { get; set; }
        public string Clientname { get; set; }
        public int SessionID { get; set; }
        public int overallSessionID { get; set; }
    }

    public static System.Boolean IsNumeric(System.Object Expression)
    {
        if (Expression == null || Expression is DateTime)
            return false;

        if (Expression is Int16 || Expression is Int32 || Expression is Int64 || Expression is Decimal || Expression is Single || Expression is Double || Expression is Boolean)
            return true;

        try
        {
            if (Expression is string)
                Double.Parse(Expression as string);
            else
                Double.Parse(Expression.ToString());
            return true;
        }
        catch { } // just dismiss errors but return false
        return false;
    }


public class TermServicesManager
    {

        [DllImport("wtsapi32.dll")]
        static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass,
            out System.IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("wtsapi32.dll")]
        static extern Int32 WTSEnumerateSessions(IntPtr hServer, [MarshalAs(UnmanagedType.U4)] Int32 Reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 Version, ref IntPtr ppSessionInfo, [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionID;
            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_ADDRESS
        {
            public uint AddressFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_DISPLAY
        {
            public uint HorizontalResolution;
            public uint VerticalResolution;
            public uint ColorDepth;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        public enum WTS_INFO_CLASS
        {
            InitialProgram = 0,
            ApplicationName = 1,
            WorkingDirectory = 2,
            OEMId = 3,
            SessionId = 4,
            UserName = 5,
            WinStationName = 6,
            DomainName = 7,
            ConnectState = 8,
            ClientBuildNumber = 9,
            ClientName = 10,
            ClientDirectory = 11,
            ClientProductId = 12,
            ClientHardwareId = 13,
            ClientAddress = 14,
            ClientDisplay = 15,
            ClientProtocolType = 16
        }

        private static IntPtr OpenServer(string Name)
        {
            IntPtr server = WTSOpenServer(Name);
            return server;
        }

        private static void CloseServer(IntPtr ServerHandle)
        {
            WTSCloseServer(ServerHandle);
        }

        public static List<TerminalSessionData> ListSessions(string ServerName)
        {
            IntPtr server = IntPtr.Zero;
            List<TerminalSessionData> ret = new List<TerminalSessionData>();
            server = OpenServer(ServerName);

            try
            {
                IntPtr ppSessionInfo = IntPtr.Zero;

                Int32 count = 0;
                Int32 retval = WTSEnumerateSessions(server, 0, 1, ref ppSessionInfo, ref count);
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

                Int64 current = (int)ppSessionInfo;

                if (retval != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
                        current += dataSize;

                        ret.Add(new TerminalSessionData(si.SessionID, si.State, si.pWinStationName));
                    }

                    WTSFreeMemory(ppSessionInfo);
                }
            }
            finally
            {
                CloseServer(server);
            }

            return ret;
        }

        public static TerminalSessionInfo GetSessionInfo(string ServerName, int SessionId)
        {
            IntPtr server = IntPtr.Zero;
            server = OpenServer(ServerName);
            System.IntPtr buffer = IntPtr.Zero;
            uint bytesReturned;
            TerminalSessionInfo data = new TerminalSessionInfo();

            try
            {
                bool worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ApplicationName, out buffer, out bytesReturned);

                if (!worked)
                    return data;

                string strData = Marshal.PtrToStringAnsi(buffer);
                data.ApplicationName = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientAddress, out buffer, out bytesReturned);

                if (!worked)
                    return data;

                WTS_CLIENT_ADDRESS si = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure((System.IntPtr)buffer, typeof(WTS_CLIENT_ADDRESS));
                data.ClientAddress = si;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientBuildNumber, out buffer, out bytesReturned);

                if (!worked)
                    return data;

                int lData = Marshal.ReadInt32(buffer);
                data.ClientBuildNumber = lData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientDirectory, out buffer, out bytesReturned);

                if (!worked)
                    return data;

                strData = Marshal.PtrToStringAnsi(buffer);
                data.ClientDirectory = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientDisplay, out buffer, out bytesReturned);

                if (!worked)
                    return data;

                WTS_CLIENT_DISPLAY cd = (WTS_CLIENT_DISPLAY)Marshal.PtrToStructure((System.IntPtr)buffer, typeof(WTS_CLIENT_DISPLAY));
                data.ClientDisplay = cd;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientHardwareId, out buffer, out bytesReturned);

                if (!worked)
                    return data;

                lData = Marshal.ReadInt32(buffer);
                data.ClientHardwareId = lData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientName, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.ClientName = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientProductId, out buffer, out bytesReturned);
                Int16 intData = Marshal.ReadInt16(buffer);
                data.ClientProductId = intData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ClientProtocolType, out buffer, out bytesReturned);
                intData = Marshal.ReadInt16(buffer);
                data.ClientProtocolType = intData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.ConnectState, out buffer, out bytesReturned);
                lData = Marshal.ReadInt32(buffer);
                data.ConnectState = (WTS_CONNECTSTATE_CLASS)Enum.ToObject(typeof(WTS_CONNECTSTATE_CLASS), lData);

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.DomainName, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.DomainName = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.InitialProgram, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.InitialProgram = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.OEMId, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.OEMId = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.SessionId, out buffer, out bytesReturned);
                lData = Marshal.ReadInt32(buffer);
                data.SessionId = lData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.UserName, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.UserName = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.WinStationName, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.WinStationName = strData;

                worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.WorkingDirectory, out buffer, out bytesReturned);
                strData = Marshal.PtrToStringAnsi(buffer);
                data.WorkingDirectory = strData;
            }
            finally
            {
                WTSFreeMemory(buffer);
                buffer = IntPtr.Zero;
                CloseServer(server);
            }

            return data;
        }

    }

    public class TerminalSessionData
    {
        public int SessionId;
        public TermServicesManager.WTS_CONNECTSTATE_CLASS ConnectionState;
        public string StationName;

        public TerminalSessionData(int sessionId, TermServicesManager.WTS_CONNECTSTATE_CLASS connState, string stationName)
        {
            SessionId = sessionId;
            ConnectionState = connState;
            StationName = stationName;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", SessionId, ConnectionState, StationName);
        }
    }

    public class TerminalSessionInfo
    {
        public string InitialProgram;
        public string ApplicationName;
        public string WorkingDirectory;
        public string OEMId;
        public int SessionId;
        public string UserName;
        public string WinStationName;
        public string DomainName;
        public TermServicesManager.WTS_CONNECTSTATE_CLASS ConnectState;
        public int ClientBuildNumber;
        public string ClientName;
        public string ClientDirectory;
        public int ClientProductId;
        public int ClientHardwareId;
        public TermServicesManager.WTS_CLIENT_ADDRESS ClientAddress;
        public TermServicesManager.WTS_CLIENT_DISPLAY ClientDisplay;
        public int ClientProtocolType;
    }

}
