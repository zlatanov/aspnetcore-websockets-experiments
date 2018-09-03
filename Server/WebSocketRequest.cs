using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Server
{
    internal static class WebSocketHeaders
    {
        public const String Connection = "Connection";
        public const String ConnectionUpgrade = "Upgrade";
        public const String Host = "Host";
        public const String SecWebSocketAccept = "Sec-WebSocket-Accept";
        public const String SecWebSocketExtensions = "Sec-WebSocket-Extensions";
        public const String SecWebSocketKey = "Sec-WebSocket-Key";
        public const String SecWebSocketProtocol = "Sec-WebSocket-Protocol";
        public const String SecWebSocketVersion = "Sec-WebSocket-Version";
        public const String Upgrade = "Upgrade";
        public const String UpgradeWebSocket = "websocket";

        public const String SupportedVersion = "13";
    }


    internal sealed class WebSocketRequest
    {
        private readonly HttpContext _context;
        private readonly IHttpUpgradeFeature _upgradeFeature;


        public WebSocketRequest( HttpContext context, IHttpUpgradeFeature upgradeFeature )
        {
            _context = context;
            _upgradeFeature = upgradeFeature;

            if ( upgradeFeature.IsUpgradableRequest )
            {
                IsWebSocketRequest = CheckSupportedWebSocketRequest();
            }
        }


        public Boolean IsWebSocketRequest { get; }


        public async Task<Stream> UpgradeAsync()
        {
            var response = _context.Response;

            response.Headers[ WebSocketHeaders.Connection ] = "Upgrade";
            response.Headers[ WebSocketHeaders.ConnectionUpgrade ] = "websocket";
            response.Headers[ WebSocketHeaders.SecWebSocketAccept ] = CreateResponseKey();

            return await _upgradeFeature.UpgradeAsync();
        }


        private Boolean CheckSupportedWebSocketRequest()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            var request = _context.Request;
            var version = request.Headers[ WebSocketHeaders.SecWebSocketVersion ];
            var key = request.Headers[ WebSocketHeaders.SecWebSocketKey ];

            if ( !comparer.Equals( request.Method, "GET" ) )
            {
                return false;
            }
            else if ( !comparer.Equals( request.Headers[ WebSocketHeaders.ConnectionUpgrade ], "websocket" ) )
            {
                return false;
            }
            else if ( !WebSocketHeaders.SupportedVersion.Equals( version ) || !IsRequestKeyValid( key ) )
            {
                return false;
            }

            return true;
        }


        private static Boolean IsRequestKeyValid( String value )
        {
            if ( String.IsNullOrWhiteSpace( value ) )
            {
                return false;
            }

            try
            {
                return Convert.FromBase64String( value ).Length == 16;
            }
            catch
            {
                return false;
            }
        }


        private String CreateResponseKey()
        {
            String key = _context.Request.Headers[ WebSocketHeaders.SecWebSocketKey ];

            // "The value of this header field is constructed by concatenating /key/, defined above in step 4
            // in Section 4.2.2, with the String "258EAFA5-E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
            // this concatenated value to obtain a 20-Byte value and base64-encoding"
            // https://tools.ietf.org/html/rfc6455#section-4.2.2
            using ( var sha1 = SHA1.Create() )
            {
                var mergedBytes = Encoding.UTF8.GetBytes( key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" );
                var hashedBytes = sha1.ComputeHash( mergedBytes );

                return Convert.ToBase64String( hashedBytes );
            }
        }
    }
}
