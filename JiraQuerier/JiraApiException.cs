using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace JiraQuerier
{
    [Serializable]
    internal class JiraApiException : Exception
    {
        public JiraApiException()
        {
        }

        public JiraApiException(string message)
            : base(message)
        {
        }

        public JiraApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected JiraApiException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
