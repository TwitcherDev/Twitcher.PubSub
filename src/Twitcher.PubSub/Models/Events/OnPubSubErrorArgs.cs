using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twitcher.PubSub.Enums;

namespace Twitcher.PubSub.Models.Events
{
    public class OnPubSubErrorArgs : EventArgs
    {
        public PubSubErrorType Type { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }
    }
}
