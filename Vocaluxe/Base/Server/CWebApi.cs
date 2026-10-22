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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace Vocaluxe.Base.Server
{
    static class CWebApi
    {
        private static readonly JsonSerializerSettings _JsonSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            NullValueHandling = NullValueHandling.Include
        };

        public static void Map(IEndpointRouteBuilder app)
        {
            var service = new CWebservice();

            _Get(app, "/sendKeyEvent", () => service.SendKeyEvent(_Query("key")));
            _Get(app, "/sendKeyStringEvent", () => service.SendKeyStringEvent(_Query("keyString"), _QueryBool("shift"), _QueryBool("alt"), _QueryBool("ctrl")));

            #region profile
            _Get(app, "/getOwnProfileId", service.GetOwnProfileId);
            _Post<SProfileData>(app, "/sendProfile", service.SendProfile);
            _Get(app, "/getProfile", () => service.GetProfile(_QueryGuid("profileId")));
            _Get(app, "/getProfileList", service.GetProfileList);
            #endregion

            #region photo
            _Post<SPhotoData>(app, "/sendPhoto", service.SendPhoto);
            #endregion

            #region website
            _Get(app, "/login", () => service.Login(_Query("username"), _Query("password")));
            _Get(app, "/logout", service.Logout);
            _GetStream(app, "/", service.Index);
            _GetStream(app, "/js/{filename}", () => service.GetJsFile(_Route("filename")));
            _GetStream(app, "/css/{filename}", () => service.GetCssFile(_Route("filename")));
            _GetStream(app, "/css/images/{filename}", () => service.GetCssImageFile(_Route("filename")));
            _GetStream(app, "/img/{filename}", () => service.GetImgFile(_Route("filename")));
            _GetStream(app, "/locales/{filename}", () => service.GetLocaleFile(_Route("filename")));
            _Get(app, "/delayedImage", () => service.GetDelayedImage(_Query("id")));
            _Get(app, "/isServerOnline", service.IsServerOnline);
            _Get(app, "/getServerVersion", service.GetServerVersion);
            #endregion

            #region songs
            _Get(app, "/getSong", () => service.GetSong(_QueryInt("songId")));
            _Get(app, "/getCurrentSongId", service.GetCurrentSongId);
            _Get(app, "/getAllSongs", service.GetAllSongs);
            _GetStream(app, "/getMp3", () => service.GetMp3File(_QueryInt("songId")));
            #endregion

            #region playlist
            _Get(app, "/getPlaylists", service.GetPlaylists);
            _Get(app, "/getPlaylist", () => service.GetPlaylist(_QueryInt("id")));
            _Get(app, "/addSongToPlaylist", () => service.AddSongToPlaylist(_QueryInt("songId"), _QueryInt("playlistId"), _QueryBool("duplicates")));
            _Get(app, "/removeSongFromPlaylist", () => service.RemoveSongFromPlaylist(_QueryInt("position"), _QueryInt("playlistId"), _QueryInt("songId")));
            _Get(app, "/moveSongInPlaylist", () => service.MoveSongInPlaylist(_QueryInt("newPosition"), _QueryInt("playlistId"), _QueryInt("songId")));
            _Get(app, "/playlistContainsSong", () => service.PlaylistContainsSong(_QueryInt("songId"), _QueryInt("playlistId")));
            _Get(app, "/getPlaylistSongs", () => service.GetPlaylistSongs(_QueryInt("playlistId")));
            _Get(app, "/removePlaylist", () => service.RemovePlaylist(_QueryInt("playlistId")));
            _Get(app, "/addPlaylist", () => service.AddPlaylist(_Query("playlistName")));
            #endregion

            #region user management
            _Get(app, "/getUserRole", () => service.GetUserRole(_QueryGuid("profileId")));
            _Get(app, "/setUserRole", () => service.SetUserRole(_QueryGuid("profileId"), _QueryInt("userRole")));
            _Get(app, "/hasUserRight", () => service.HasUserRight(_QueryInt("right")));
            #endregion
        }

        #region endpoint helpers
        private static void _Get<T>(IEndpointRouteBuilder app, string pattern, Func<T> handler)
        {
            app.MapGet(pattern, context => _Handle(context, async () => await _WriteJson(context, handler())));
        }

        private static void _Get(IEndpointRouteBuilder app, string pattern, Action handler)
        {
            app.MapGet(pattern, context => _Handle(context, () =>
            {
                handler();
                return Task.CompletedTask;
            }));
        }

        private static void _Post<T>(IEndpointRouteBuilder app, string pattern, Action<T> handler)
        {
            app.MapPost(pattern, context => _Handle(context, async () =>
            {
                string body;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }

                handler(JsonConvert.DeserializeObject<T>(body, _JsonSettings));
            }));
        }

        private static void _GetStream(IEndpointRouteBuilder app, string pattern, Func<Stream> handler)
        {
            app.MapGet(pattern, context => _Handle(context, async () =>
            {
                using (var stream = handler())
                {
                    if (stream != null)
                    {
                        await stream.CopyToAsync(context.Response.Body);
                    }
                }
            }));
        }

        private static async Task _Handle(HttpContext context, Func<Task> handler)
        {
            CWebContext.Current = context;
            try
            {
                await handler();
            }
            catch (CBadRequestException e)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    CWebContext.SetStatusDescription(e.Message);
                }
            }
            catch (JsonException e)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    CWebContext.SetStatusDescription("Invalid JSON: " + e.Message);
                }
            }
            finally
            {
                CWebContext.Current = null;
            }
        }

        private static async Task _WriteJson<T>(HttpContext context, T result)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(result, _JsonSettings), Encoding.UTF8);
        }
        #endregion

        #region parameter helpers
        private class CBadRequestException : Exception
        {
            public CBadRequestException(string message) : base(message) { }
        }

        private static string _Query(string name)
        {
            var values = CWebContext.Current.Request.Query[name];
            return values.Count == 0 ? null : values.ToString();
        }

        private static string _Route(string name)
        {
            return CWebContext.Current.Request.RouteValues[name] as string;
        }

        private static int _QueryInt(string name)
        {
            var value = _Query(name);
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            int result;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                throw new CBadRequestException("Invalid value for " + name);
            }

            return result;
        }

        private static bool _QueryBool(string name)
        {
            var value = _Query(name);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            bool result;
            if (!bool.TryParse(value, out result))
            {
                throw new CBadRequestException("Invalid value for " + name);
            }

            return result;
        }

        private static Guid _QueryGuid(string name)
        {
            var value = _Query(name);
            if (string.IsNullOrEmpty(value))
            {
                return Guid.Empty;
            }

            Guid result;
            if (!Guid.TryParse(value, out result))
            {
                throw new CBadRequestException("Invalid value for " + name);
            }

            return result;
        }
        #endregion
    }
}
