﻿using DataServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServers.SAEA
{
    public class SAEAServer:ISockteServer
    {
        #region 字段

        //socket参数
        private Socket listenSocket;
        private string ipString;
        private IPAddress ipAddress;
        private int ipPort;
        private IPEndPoint ipEndPoint;
        private int maxConnectNum;
        private ILog log;
        private TimeOut timeOut;
        private int readCacheSize;

        private SaeaConnectStatePool connectStatePool;
        #endregion
        #region 属性
        public string IPString
        {
            get { return ipString; }
            set { ipString = value; }
        }

        //public IPAddress IpAddress
        //{
        //    get { return ipAddress; }
        //    set { ipAddress = value; }
        //}
        public int IpPort
        {
            get { return ipPort; }
            set { ipPort = value; }
        }
        public IPEndPoint IpEndPoint
        {
            get { return ipEndPoint; }
            set { ipEndPoint = value; }
        }

        public ILog Log
        {
            get { return log; }
            set { log = value; }
        }

        public SaeaConnectStatePool ConnectStatePool
        {
            get { return connectStatePool; }
            private set { connectStatePool = value; }
        }
        #endregion
        #region 方法
        public event Action<IConnectState> AcceptComplete;
        public event Action<IConnectState> ReadComplete;
        public event Action<IConnectState> SendComplete;
        public SAEAServer(string ipstring, int ipport, ILog log, TimeOut timeOut, int maxConnect,int readsize)
        {
            this.ipString = ipstring;
            this.ipPort = ipport;
            this.log = log;
            this.timeOut = timeOut;
            this.maxConnectNum = maxConnect;
            this.readCacheSize = readsize;
        }
        public bool Init()
        {
            try
            {
                //初始化Sokect
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //初始化IP地址
                ipAddress = IPAddress.Parse(ipString);
                ipEndPoint = new IPEndPoint(ipAddress, ipPort);

                //初始化缓存池
                connectStatePool = new SaeaConnectStatePool(log, timeOut);
                connectStatePool.Init(maxConnectNum);
           
                return true;
            }
            catch (Exception e)
            {
                string errorInfor = string.Format("Server init error: {0}", e.Message);
                log.ErrorLog(errorInfor);
                return false;
            }

        }

        public bool Start()
        {
            try
            {
                listenSocket.Bind(ipEndPoint);
                listenSocket.Listen(maxConnectNum);
                startAccept(null);
                return true;
            }
            catch (Exception e)
            {
                string errorInfor = string.Format("Server start error: {0}", e.Message);
                log.ErrorLog(errorInfor);
                return false;
            }
        }

        private void startAccept(SocketAsyncEventArgs accpetEventArg)
        {
            if (accpetEventArg == null)
            {
                accpetEventArg = new SocketAsyncEventArgs();
                accpetEventArg.Completed += AccpetEventArg_Completed;
            }
            else
            {
                //Use for next.
                accpetEventArg.AcceptSocket = null;
            }
            var willRaiseEvent =  listenSocket.AcceptAsync(accpetEventArg);
            //
            //if (!willRaiseEvent)
            //{
                //ProcessAccept(accpetEventArg);
            //}
                
        }

        private void AccpetEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs accpetEventArg)
        {
            SaeaConnectState connectState = connectStatePool.Get();
            connectState.ReadComplete += ConnectState_ReadComplete;
            connectState.SendComplete += ConnectState_SendComplete;
            SocketAsyncEventArgs readWiteEventArg = connectState.ReadSocketArg;
            readWiteEventArg.SetBuffer(new byte[1024], 0, 1024);
            readWiteEventArg.UserToken = new AsyncUserToken { AcceptSocket = accpetEventArg.AcceptSocket };
            accpetEventArg.AcceptSocket.ReceiveAsync(readWiteEventArg);
            
            log.NormalLog(string.Format("connect information,ReceiveAsync Start,ID:{0} , IPAdderss:{1}", connectState.ID, accpetEventArg.AcceptSocket.RemoteEndPoint));

            //Accept Next;
            startAccept(accpetEventArg);

        }

        private void ConnectState_SendComplete(SaeaConnectState obj)
        {
            ReadComplete?.Invoke(obj);
        }

        private void ConnectState_ReadComplete(SaeaConnectState obj)
        {
            SendComplete?.Invoke(obj);
        }

        public bool Stop()
        {
            listenSocket.Shutdown(SocketShutdown.Both);
            listenSocket.Close();
            return true;
           
        }
   

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    listenSocket.Dispose();
                    connectStatePool.Dispose();
                }
                listenSocket = null;

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~SAEAServer()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion
    }

    internal class AsyncUserToken
    {
        public Socket AcceptSocket
        {
            get;
            set;
        }
        public SaeaConnectState ConnectState
        {
            get;
            set;
        }
    }
}
