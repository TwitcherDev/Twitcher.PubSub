using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitcher.PubSub.Enums
{
    public enum PubSubErrorType
    {
        None,
        /// <summary>Responce return ERR_BADMESSAGE error</summary>
        BadMessage,
        /// <summary>Responce return ERR_SERVER error</summary>
        Server,
        /// <summary>Responce return ERR_BADTOPIC error</summary>
        BadTopic,
        /// <summary>Responce return ERR_AUTH error</summary>
        BadAuth,
        /// <summary>Responce return unknown error</summary>
        UnknownErrorType,
        /// <summary>Server send unknown message</summary>
        UnknownType,
        /// <summary>Server send message have unknown data type</summary>
        UnknownMessageType,
        /// <summary>Exception in json serialize / deserialize</summary>
        JsonException,
        /// <summary>Exception in TokenRefreshHandler</summary>
        RefreshException,
    }
}
