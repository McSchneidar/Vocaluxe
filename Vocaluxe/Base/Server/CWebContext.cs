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
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Vocaluxe.Base.Server
{
    static class CWebContext
    {
        private static readonly AsyncLocal<HttpContext> _Current = new AsyncLocal<HttpContext>();

        public static HttpContext Current
        {
            get { return _Current.Value; }
            set { _Current.Value = value; }
        }

        public static string GetRequestHeader(string name)
        {
            return Current?.Request.Headers[name].ToString();
        }

        public static void SetStatusCode(HttpStatusCode statusCode)
        {
            if (Current != null)
            {
                Current.Response.StatusCode = (int)statusCode;
            }
        }

        public static void SetStatusDescription(string description)
        {
            var responseFeature = Current?.Features.Get<IHttpResponseFeature>();
            if (responseFeature != null)
            {
                responseFeature.ReasonPhrase = description;
            }
        }

        public static void SetContentType(string contentType)
        {
            if (Current != null)
            {
                Current.Response.ContentType = contentType;
            }
        }

        public static void SetLastModified(DateTime lastModified)
        {
            if (Current != null)
            {
                Current.Response.Headers.LastModified = lastModified.ToUniversalTime().ToString("r");
            }
        }

        public static void SetExpires(DateTime expires)
        {
            if (Current != null)
            {
                Current.Response.Headers.Expires = expires.ToUniversalTime().ToString("r");
            }
        }
    }
}
