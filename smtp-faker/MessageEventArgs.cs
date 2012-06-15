using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;

namespace smtp_faker
{
    public class MessageEventArgs : EventArgs
    {
        #region Constructor

        public MessageEventArgs(){
            _From = null;
            _To = new StringCollection();
            _Data = null;
        }

        #endregion

        #region Properties

        private string _MessageId;

        public string MessageId
        {
            get { return _MessageId; }
        }

        private string _Subject;

        public string Subject
        {
            get { return _Subject; }
        }

        private string _From;

        public string From
        {
            get { return _From; }
            set{ _From = value; }
        }

        private StringCollection _To;

        public StringCollection To{
            get{ return _To; }
        }

        public String ToAsList() 
        {
            String ToListed = String.Empty;
            for (int i = 0; i < To.Count; i++) 
            {
                ToListed += To[i] + " - ";
            }
            return ToListed;
        }

        private string _Data;

        public string Data
        {
            get { return _Data; }
            set
            {
                _Data = value;

                // Get selected header values
                _MessageId = TextFunctions.Between(_Data, "message-id:", "\r\n", StringComparison.CurrentCultureIgnoreCase)
                    .Trim().TrimStart('<').TrimEnd('>');
                if (_MessageId == null || _MessageId.Length == 0)
                {
                    _MessageId = Guid.NewGuid().ToString();
                }
                _Subject = TextFunctions.Between(_Data, "subject:", "\r\n", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        #endregion
    }
}

