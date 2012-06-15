using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.ComponentModel;
using System.Threading;

using System.Diagnostics;
using System.Security.Permissions;


namespace smtp_faker
{
    class SmtpServer
    {
        private TcpListener _Listener;
        private delegate void SetStatusDelegate(string value);
        private String DumpPath { get; set; }
        private const string _LineTerminator = "\r\n";

        public SmtpServer(String _DumpPath)
        {
            DumpPath = _DumpPath;
        }

        public void Connect()
        {
            _Listener = new TcpListener(System.Net.IPAddress.Any, 25);
                        try
            {
                _Listener.Start();
                while (_Listener != null)
                {
                    // wait for incomming message
                    SmtpSession session = new SmtpSession(_Listener);

                    // catch events
                    session.Connected += new EventHandler(Session_Connected);
                    session.MessageReceived += new EventHandler<MessageEventArgs>(Session_MessageReceived);
                    session.Disconnected += new EventHandler(Session_Disconnected);

                    // connect
                    session.Connect();
                }
            }
            catch (Exception ex)
            {
                SetStatus(string.Format("Error: {0}", ex.Message));
            }
        }

        private void SetStatus(string status)
        {
            try
            {
                //TODO - we should simply dump something to the console here..?
                Console.Write(_LineTerminator + status + _LineTerminator);                
            }
            // For now ignore errors trying to set the status garrgh
            catch (Exception) { }
        }


        
        #region Session Events

        void Session_Connected(object sender, EventArgs e)
        {
            SetStatus("Waiting");
        }

        void Session_Disconnected(object sender, EventArgs e)
        {
            SetStatus("Disconnected");
        }

        void Session_MessageReceived(object sender, MessageEventArgs e)
        {
            // Save the message in the preset pickup directory
            // ensure we can write
            FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Write, DumpPath);
            permission.Assert();

            StreamWriter file
                = File.AppendText(DumpPath);
            try
            {
                file.Write(_LineTerminator + "---------------START OF MESSAGE-------------------" + _LineTerminator);
                file.Write("Message Id: " + e.MessageId + _LineTerminator);
                file.Write(_LineTerminator + "From: " + e.From + _LineTerminator);
                file.Write("To: " + e.ToAsList() + _LineTerminator);
                file.Write(_LineTerminator + "Subject: " + e.Subject + _LineTerminator);
                file.Write("Data: " + e.Data + _LineTerminator);
                file.Write(_LineTerminator + "---------------END OF MESSAGE---------------------" + _LineTerminator);
                file.Close();

                SetStatus(String.Format("Recieved: {0}", e.Subject));
            }
            finally { file.Close(); }
        }

        #endregion




    }
}


