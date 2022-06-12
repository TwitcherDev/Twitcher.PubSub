using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Twitcher.PubSub.Models
{
    public class MessageModel
    {
        [JsonPropertyName("data")]
        public MessageDataModel? Data { get; set; }
    }
}
