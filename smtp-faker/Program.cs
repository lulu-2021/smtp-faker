using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;

namespace smtp_faker
{
    class SmtpFaker
    {

        static void Main(string[] args)
        {
            String DumpFile = "d:\\temp\\mail.log";
            SmtpServer Server = new SmtpServer(DumpFile);
            Server.Connect();
        }
    }
}
