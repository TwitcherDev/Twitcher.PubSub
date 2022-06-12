using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Twitcher.PubSub.Exceptions
{
    public class ChannelNotFountException : Exception
    {
        public string ChannelId { get; set; }

        public ChannelNotFountException(string channelId)
        {
            ChannelId = channelId;
        }
    }
}
