using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Server
{
    public class Program
    {
        public static void Main( string[] args )
        {
            WebHost.CreateDefaultBuilder( args )
                   .UseStartup<Program>()
                   .Build()
                   .Run();
        }


        public Program( IConfiguration configuration, ILoggerFactory logger )
        {
            Configuration = configuration;
            Logger = logger.CreateLogger( "WebSockets" );
        }

        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }


        public void ConfigureServices( IServiceCollection services )
        {
        }


        public void Configure( IApplicationBuilder app, IHostingEnvironment env )
        {
            app.Run( RunAsync );
        }


        private async Task RunAsync( HttpContext context )
        {
            // Detect if an opaque upgrade is available. If so, add a websocket upgrade.
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();

            if ( upgradeFeature != null )
            {
                var request = new WebSocketRequest( context, upgradeFeature );

                if ( request.IsWebSocketRequest )
                {
                    var stream = await request.UpgradeAsync();
                    var buffer = new Byte[ 1024 ];

                    while ( true )
                    {
                        var byteCount = await stream.ReadAsync( buffer );

                        if ( byteCount == 0 )
                        {
                            Logger.Log( LogLevel.Information, "Aborted" );
                            break;
                        }
                        else
                        {
                            Debug.Assert( byteCount == 2 );
                            Debug.Assert( ( buffer[ 0 ] & 0b0000_1111 ) == 0x9/*Ping*/ );

                            Logger.Log( LogLevel.Information, "Ping received" );

                            // Reply with pong
                            await stream.WriteAsync( new Byte[] { 0b1000_1010, 0 } );
                        }
                    }
                }
            }

        }
    }
}
