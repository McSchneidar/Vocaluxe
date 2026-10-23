#region license
// This file is part of Vocaluxe.
//
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using VocaluxeWeb.Dispatching;

namespace VocaluxeWeb.Hosting
{
    /// <summary>The embedded webserver (Kestrel). Serves the WebApp and the REST API.</summary>
    public sealed class CWebHost : IDisposable
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CWebServerOptions _Options;
        private IWebHost _Host;

        public CMainThreadDispatcher Dispatcher { get; }

        /// <summary>Address other devices in the network use to reach the server, e.g. http://192.168.0.10:3000/</summary>
        public Uri Address { get; private set; }

        public bool IsRunning => _Host != null;

        public CWebHost(IVocaluxeRemote remote, CWebServerOptions options)
        {
            _Remote = remote;
            _Options = options;
            Dispatcher = new CMainThreadDispatcher(options.MainThreadTimeout);
        }

        public void Start()
        {
            if (_Host != null)
            {
                return;
            }

            var hostName = Dns.GetHostName();
            var lanAddresses = CNetworkAddress.GetLanAddresses();
            var certificate = _Options.UseHttps ? CCertificateProvider.GetOrCreate(_Options.CertificatePath, hostName, lanAddresses) : null;

            var host = new WebHostBuilder()
                .UseKestrel(kestrel => _ConfigureKestrel(kestrel, certificate))
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureServices(services => CStartup.ConfigureServices(services, _Remote, Dispatcher, _Options))
                .Configure(app => CStartup.Configure(app, _Options))
                .Build();

            host.StartAsync().GetAwaiter().GetResult();
            _Host = host;

            var shownHost = lanAddresses.Count > 0 ? lanAddresses.First().ToString() : hostName;
            Address = new UriBuilder(_Options.UseHttps ? "https" : "http", shownHost, _Options.Port).Uri;
        }

        public void Stop()
        {
            if (_Host == null)
            {
                return;
            }

            try
            {
                _Host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            finally
            {
                _Host.Dispose();
                _Host = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void _ConfigureKestrel(KestrelServerOptions kestrel, X509Certificate2 certificate)
        {
            kestrel.AddServerHeader = false;
            kestrel.Limits.MaxRequestBodySize = 20 * 1024 * 1024; //Photos
            kestrel.Listen(IPAddress.Any, _Options.Port, listen =>
            {
                if (certificate != null)
                {
                    listen.UseHttps(certificate);
                }
            });
        }
    }
}
