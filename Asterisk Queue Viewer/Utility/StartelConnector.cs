using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace Asterisk_Queue_Viewer.Utility
{
    internal delegate void StartelMessageRecievedHandler(string message);

    internal class StartelConnector
    {
        internal event StartelMessageRecievedHandler StartelMessageRecieved;

        internal bool IsRunning { get; private set; }
        internal bool IsDisposing { get; private set; }
        internal bool IsTPConnected { get; private set; }
        internal bool IsAPIConnected { get; private set; }
        private string APIServer { get; set; }
        private string TPServer { get; set; }
        private int APIPortNumber { get; set; }
        private int TPPortNumber { get; set; }
        private Socket TPSocket { get; set; }
        private Socket APISocket { get; set; }
        private Mutex SendMutex { get; set; }
        private Mutex ReceiveMutex { get; set; }
        private Task TPReceiveThread { get; set; }
        private Task APIReceiveThread { get; set; }
        private Logger Logger { get; set; }
        

        internal StartelConnector(string apiServer, int apiPort, string tpServer, int tpPort)
        {
            Logger = LogManager.GetCurrentClassLogger();
            TPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            APISocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SendMutex = new Mutex();
            IsDisposing = true;
            IsRunning = false;
            APIServer = apiServer;
            APIPortNumber = apiPort;
            TPServer = tpServer;
            TPPortNumber = tpPort;
        }

        public bool Start()
        {
            Logger.Debug("StartelConnector -> Starting Startel Connector");
            ReceiveMutex = new Mutex();
            try
            {
                TPSocket.Connect(TPServer, TPPortNumber);
                IsTPConnected = true;
                APISocket.Connect(APIServer, APIPortNumber);
                IsAPIConnected = true;
            }
            catch { return false; }
            IsDisposing = false;
            APIReceiveThread = Task.Run(() => StartAPIReceiveThread());
            TPReceiveThread = Task.Run(() => StartTPReceiveThread());
            IsRunning = true;
            return true;
        }

        public void Stop()
        {
            if (!IsDisposing)
            {
                Logger.Debug("StartelConnector -> Stopping Startel Connector");
                IsDisposing = true;
                APIReceiveThread.Dispose();
                TPReceiveThread.Dispose();
                IsRunning = false;
                Logger.Debug("StartelConnector -> Stopping TP and API Socket Threads");
                Task.WaitAll(new Task[] { TPReceiveThread, APIReceiveThread });
                Logger.Debug("StartelConnector -> TP and API Socket Threads Closed");
            }
        }

        private void StartAPIReceiveThread()
        {
            int maxMessageSize = 4096;
            byte[] buffer = new byte[maxMessageSize];
            LoginToAPI();
            while (!IsDisposing)
            {
                string messages = RecieveFromAPI(buffer, maxMessageSize);
                if (!string.IsNullOrEmpty(messages))
                {
                    foreach(string message in Regex.Split(messages, "\x0003"))
                    {
                        if (!string.IsNullOrWhiteSpace(messages))
                        {
                            Logger.Debug("StartelConnector -> Recieved From stlapid: {0}", message);
                            StartelMessageRecieved(message);
                        }
                    }
                }
            }
            Logger.Debug("StartelConnector -> Shutting Down APIReceiveThread");
            APISocket.Shutdown(SocketShutdown.Both);
            APISocket.Close();
        }
        private void StartTPReceiveThread()
        {
            int maxMessageSize = 128;
            byte[] buffer = new byte[maxMessageSize];
            LoginToTP();
            while (!IsDisposing)
            {
                string messages = RecieveFromTP(buffer, maxMessageSize);
                if (!string.IsNullOrEmpty(messages))
                {
                    foreach (string message in Regex.Split(messages, "\x0003"))
                    {
                        if (!string.IsNullOrWhiteSpace(messages))
                        {
                            Logger.Debug("StartelConnector -> Recieved From StlTp: {0}", message);
                            StartelMessageRecieved(message);
                        }
                    }
                }
            }
            TPSocket.Shutdown(SocketShutdown.Both);
            TPSocket.Close();
        }
        private void LoginToAPI()
        {
            SendAPICommand("\x00050:1536\x0003");
        }
        private void LoginToTP()
        {
            SendTPCommand("\x0005IDENTITY=SAMSVR\x0003");
        }
        private string RecieveFromAPI(byte[] buffer, int maxMessageSize)
        {
            int recieveSize = 0;
            int messageSize = 0;
            string message = string.Empty;
            while (!IsDisposing)
            {
                try
                {
                    recieveSize = APISocket.Receive(buffer, maxMessageSize, SocketFlags.None);
                }
                catch(SocketException sex)
                {
                    Logger.Error("StartelConnector -> API Socket Error: {0}", sex.ToString());
                    IsAPIConnected = false;
                    Thread.Sleep(5000);
                    ReconnectAPISocket();
                }
                catch(Exception ex)
                {
                    Logger.Error("StartelConnector -> API Socket Error: {0}", ex.ToString());
                }

                messageSize += recieveSize;
                if(recieveSize == 0)
                {
                    IsAPIConnected = false;
                    Thread.Sleep(5000);
                    ReconnectAPISocket();
                }
                else
                {
                    message += Encoding.ASCII.GetString(buffer, 0, recieveSize);
                    if(messageSize >= 4 && message.Substring(messageSize -1, 1) == "\x0003")
                    {
                        break;
                    }
                }
            }
            return message;
        }
        private string RecieveFromTP(byte[] buffer, int maxMessageSize)
        {
            int recieveSize = 0;
            int messageSize = 0;
            var staleConnectionCounter = 0;
            string message = string.Empty;
            while (!IsDisposing)
            {
                Logger.Debug("StartelConnector -> ReceiveFromTP -> Waking up from sleep");
                try
                {
                    TPSocket.ReceiveTimeout = 60000;
                    recieveSize = TPSocket.Receive(buffer, maxMessageSize, SocketFlags.None);
                }
                catch (SocketException sex)
                {
                    Logger.Error("StartelConnector -> TP Socket Error: {0}", sex.ToString());
                    IsTPConnected = false;
                    Thread.Sleep(5000);
                    ReconnectTPSocket();
                }
                catch(Exception ex)
                {
                    Logger.Error("StartelConnector -> TP Socket Error: {0}", ex.ToString());
                }

                messageSize += recieveSize;
                if (recieveSize == 0)
                {
                    if (staleConnectionCounter >= 60)
                    {
                        Logger.Error("StartelConnector -> Reconnecting TP Socket");
                        IsTPConnected = false;
                        ReconnectTPSocket();
                        staleConnectionCounter = 0;
                    }
                    else
                    {
                        staleConnectionCounter++;
                        Logger.Debug("StartelConnector -> ReceiveFromTP -> Stale Connection Counter is {0}", staleConnectionCounter);
                    }
                    Logger.Debug("StartelConnector -> ReceiveFromTP -> Sleeping for 1000ms");
                    Thread.Sleep(1000);
                }
                else
                {
                    message += Encoding.ASCII.GetString(buffer, 0, recieveSize);
                    if (messageSize >= 2 && message.Substring(messageSize - 1, 1) == "\x0003")
                    {
                        break;
                    }
                }
            }
            return message;
        }
        private void SendAPICommand(string command)
        {
            if (SendMutex.WaitOne(2000))
            {
                try
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(command);
                    APISocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
                }
                catch (SocketException sex)
                {
                    Logger.Error("StartelConnector -> API Socket Error: {0}", sex.ToString());
                    IsAPIConnected = false;
                    Thread.Sleep(5000);
                    ReconnectAPISocket();
                }
                catch(Exception ex)
                {
                    Logger.Error("StartelConnector -> API Socket Error: {0}", ex.ToString());
                }
                finally
                {
                    SendMutex.ReleaseMutex();
                }
            }
        }
        private void SendTPCommand(string command)
        {
            if (SendMutex.WaitOne(2000))
            {
                try
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(command);
                    TPSocket.Send(bytes, 0, bytes.Length, SocketFlags.None);
                }
                catch (SocketException sex)
                {
                    Logger.Error("StartelConnector -> TP Socket Error: {0}", sex.ToString());
                    IsTPConnected = false;
                    Thread.Sleep(5000);
                    ReconnectTPSocket();
                }
                catch(Exception ex)
                {
                    Logger.Error("StartelConnector -> TP Socket Error: {0}", ex.ToString());
                }
                finally
                {
                    SendMutex.ReleaseMutex();
                }
            }
        }
        private void ReconnectTPSocket()
        {
            var retryCount = 0;
            while (!IsTPConnected)
            {
                try
                {
                    Logger.Debug("ReconnectTPSocket -> Reconnecting to TP. Retry Count {0}", retryCount.ToString());
                    retryCount += 1;
                    TPSocket.Close();
                    TPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    TPSocket.Connect(TPServer, TPPortNumber);
                    IsTPConnected = true;
                    LoginToTP();
                    Logger.Debug("ReconnectTPSocket -> Connected to TP. Retry Count {0}", retryCount.ToString());
                }
                catch(Exception ex)
                {
                    Logger.Error("StartelConnector -> Error Reconnect TP Socket {0}", ex.ToString());

                    IsTPConnected = false;
                    if(retryCount >= 10)
                    {
                        Stop();
                    }
                    Thread.Sleep(10000);
                }
            }
        }
        private void ReconnectAPISocket()
        {
            var retryCount = 0;
            while (!IsAPIConnected)
            {
                try
                {
                    retryCount += 1;
                    APISocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    APISocket.Connect(APIServer, APIPortNumber);
                    IsAPIConnected = true;
                    LoginToAPI();
                }
                catch(Exception ex)
                {
                    Logger.Error("StartelConnector -> Error Reconnect API Socket {0}", ex.ToString());

                    IsAPIConnected = false;
                    if (retryCount >= 10)
                    {
                        Stop();
                    }
                    Thread.Sleep(10000);
                }
            }
        }
    }
}