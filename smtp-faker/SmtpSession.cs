using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Security.Permissions;


namespace smtp_faker
{


    public class SmtpSession
    {
        #region Properties

        private const int _ReadCount = 100;
        private const string _LineTerminator = "\r\n";
        private const string _DataTerminator = "\r\n.\r\n";
        private TcpListener _Listener;
        private Socket _Socket;
        private NetworkStream _NetworkStream;
        private string ConnectedId  { get; set; }

        private enum StatusEnum
        {
            Connected,
            Identified,
            Mail,
            Recipient,
            Data,
            Disconnected
        }

        private StatusEnum _Status;

        private enum ErrorEnum
        {
            SyntaxError = 500,
            ParameterSyntaxError = 501,
            CommandNotImplemented = 502,
            BadCommandSequence = 503,
            CommandParameterNotImplemented = 504,
        }

        #endregion

        // Constructor
        public SmtpSession(TcpListener listener)
        {
            _Listener = listener;
            ConnectedId = null;
        }


        #region Events

        public event EventHandler Connected;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler Disconnected;

        #endregion

        #region Reader methods

        private string Read(string terminator)
        {
            byte[] bytes = new byte[_ReadCount];
            string data = null;

            try
            {
                while (true)
                {
                    int count = _NetworkStream.Read(bytes, 0, _ReadCount);
                    if (count == 0) { break; }
                    {
                        data += Encoding.UTF8.GetString(bytes, 0, count);

                        if (data.EndsWith(terminator)) { break; }
                    }
                }
            }
            catch (Exception)
            {
                Disconnect();
            }

            System.Diagnostics.Debug.Write(data);
            return data;
        }

        #endregion

        #region Writer methods

        private void WriteLine(string data)
        {
            data += _LineTerminator;

            byte[] bytes = Encoding.UTF8.GetBytes(data);
            _NetworkStream.Write(bytes, 0, bytes.Length);

            System.Diagnostics.Debug.Write(data);
        }

        private void WriteOk()
        {
            this.WriteLine("250 OK");
        }

        private void WriteError(ErrorEnum error, string description)
        {
            this.WriteLine(String.Format("{0:D} {1}", error, description));
        }

        private void WriteGreeting(string id)
        {
            ConnectedId = id;
            this.WriteLine(String.Format("250 Hello {0}", ConnectedId));
        }

        private void WriteSendData()
        {
            this.WriteLine("354 Send Data, end with <CRLF>.<CRLF>");
        }

        #endregion

        #region Connect/Disconnect methods

        /// <summary>
        /// Connect and check incomming data
        /// </summary>
        public void Connect()
        {
            _Status = StatusEnum.Connected;
            if (this.Connected != null)
            {
                this.Connected(this, new EventArgs());
            }

            MessageEventArgs message = null;

            try
            {
                _Socket = _Listener.AcceptSocket();
                _NetworkStream = new NetworkStream(_Socket, FileAccess.ReadWrite, true);

                WriteLine(
                    string.Format(
                        "220 Welcome {0}, LOCALHOST-SMTP:",
                        ((IPEndPoint)_Socket.RemoteEndPoint).Address));

                while (_Socket.Connected)
                {
                    string data = this.Read(_LineTerminator);

                    if (data == null) { break; }
                    else
                    {
                        if (data.StartsWith("QUIT", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Disconnect();
                        }
                        else if (data.StartsWith("EHLO", StringComparison.InvariantCultureIgnoreCase)
                            || data.StartsWith("HELO", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.WriteGreeting(data.Substring(4).Trim());
                            _Status = StatusEnum.Identified;
                        }
                        else if (_Status < StatusEnum.Identified)
                        {
                            this.WriteError(ErrorEnum.BadCommandSequence, "Expected HELO <Your Name>");
                        }
                        else
                        {
                            if (data.StartsWith("MAIL", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (_Status != StatusEnum.Identified
                                    && _Status != StatusEnum.Data)
                                {
                                    this.WriteError(ErrorEnum.BadCommandSequence, "Command out of sequence");
                                }
                                else
                                {
                                    // create a new message
                                    message = new MessageEventArgs();
                                    message.From = TextFunctions.Tail(data, ":");

                                    _Status = StatusEnum.Mail;
                                    this.WriteOk();
                                }
                            }
                            else if (data.StartsWith("RCPT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (_Status != StatusEnum.Mail
                                    && _Status != StatusEnum.Recipient)
                                {
                                    this.WriteError(ErrorEnum.BadCommandSequence, "Command out of sequence");
                                }
                                else
                                {
                                    message.To.Add(TextFunctions.Tail(data, ":"));

                                    _Status = StatusEnum.Recipient;
                                    this.WriteOk();
                                }
                            }
                            else if (data.StartsWith("DATA", StringComparison.InvariantCultureIgnoreCase))
                            {
                                // request data
                                this.WriteSendData();
                                message.Data = this.Read(_DataTerminator);

                                // raise event
                                if (this.MessageReceived != null)
                                {
                                    this.MessageReceived(this, message);
                                    message = null;
                                }

                                _Status = StatusEnum.Data;
                                this.WriteOk();
                            }
                            else
                            {
                                this.WriteError(ErrorEnum.CommandNotImplemented, "Command not implemented");
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Trace.Write(ex);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_Socket != null
                && _Socket.Connected)
            {
                WriteLine("250 Good bye");
                _NetworkStream.Close();
            }

            ConnectedId = null;
            _Status = StatusEnum.Disconnected;
            if (this.Disconnected != null)
            {
                this.Disconnected(this, new EventArgs());
            }
        }

        #endregion
    }
}
