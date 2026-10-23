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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    /// <summary>Logging in by picking a profile ("Who are you?"), with PIN if the profile has one.</summary>
    [Route("api/v1/session")]
    public sealed class SessionController : ApiControllerBase
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CSessionStore _Sessions;
        private readonly CWebSettingsStore _Settings;

        public SessionController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CSessionStore sessions, CWebSettingsStore settings)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Sessions = sessions;
            _Settings = settings;
        }

        /// <summary>The current session.</summary>
        [HttpGet]
        [RequireSession]
        public async Task<ActionResult<SessionDto>> Get()
        {
            var session = CurrentSession;
            var profile = (await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles())).FirstOrDefault(p => p.Id == session.ProfileId);
            if (profile == null)
            {
                _Sessions.Remove(HttpContext);
                return Problem(StatusCodes.Status401Unauthorized, "Please log in.");
            }

            session.Role = profile.Role;
            session.ProfileName = profile.Name;
            return CreateDto(profile, _Settings.Current);
        }

        /// <summary>Logs in as the given profile.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<SessionDto>> Create([FromBody] SessionCreateDto request)
        {
            var settings = _Settings.Current;
            if (settings.RequireJoinToken && request.JoinToken != settings.JoinToken)
            {
                return Problem(StatusCodes.Status403Forbidden, "Please scan the QR code shown by Vocaluxe.");
            }

            if (_Sessions.IsLocked(request.ProfileId.Value))
            {
                return Problem(StatusCodes.Status429TooManyRequests, "Too many wrong PINs, please wait a moment.");
            }

            var profile = (await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles())).FirstOrDefault(p => p.Id == request.ProfileId.Value);
            if (profile == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Profile not found.");
            }

            if (profile.PinHash != null)
            {
                if (!CPinHasher.Verify(request.Pin ?? "", profile.PinHash, profile.PinSalt))
                {
                    _Sessions.RegisterFailedLogin(profile.Id);
                    return Problem(StatusCodes.Status403Forbidden, "Wrong PIN.");
                }

                if (CPinHasher.NeedsUpgrade(profile.PinSalt))
                {
                    CPinHasher.Hash(request.Pin, out var hash, out var salt);
                    await _Dispatcher.InvokeAsync(() => _Remote.UpdateProfile(profile.Id, new ProfileChange { PinHash = hash, PinSalt = salt }));
                }
            }

            _Sessions.Create(HttpContext, profile.Id, profile.Name, profile.Role);
            return CreateDto(profile, settings);
        }

        /// <summary>Logs out this device.</summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Delete()
        {
            _Sessions.Remove(HttpContext);
            return NoContent();
        }

        internal static SessionDto CreateDto(RemoteProfile profile, CWebSettings settings)
        {
            return new SessionDto { Profile = ToDto(profile), Permissions = settings.GetPermissions(profile.Role).ToList() };
        }
    }
}
