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

namespace VocaluxeWeb.Hosting
{
    public sealed class CWebServerOptions
    {
        public int Port = 3000;

        /// <summary>Serve HTTPS with a self-signed certificate instead of plain HTTP.</summary>
        public bool UseHttps;

        /// <summary>Folder with the built WebApp (index.html, assets).</summary>
        public string WebRoot;

        /// <summary>Where the self-signed certificate is stored (only used with HTTPS).</summary>
        public string CertificatePath;

        /// <summary>JSON file with the webserver settings (permissions, join token, ...).</summary>
        public string SettingsPath;

        /// <summary>Folder for scaled covers and avatars.</summary>
        public string CachePath;

        /// <summary>How long a request waits for the game's main thread.</summary>
        public TimeSpan MainThreadTimeout = TimeSpan.FromSeconds(5);

        /// <summary>Called for unhandled exceptions while processing a request.</summary>
        public Action<Exception> OnError;
    }
}
