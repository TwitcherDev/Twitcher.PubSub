using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Twitcher.PubSub.Models
{
    public class ResponceModel
    {
        [JsonPropertyName("nonce")]
        public string? Nonce { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
