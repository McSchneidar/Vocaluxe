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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [Route("api/v1")]
    public sealed class ServerController : ApiControllerBase
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CWebSettingsStore _Settings;

        public ServerController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CWebSettingsStore settings)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Settings = settings;
        }

        /// <summary>Name and version of the server and how logging in works.</summary>
        [HttpGet("server")]
        public ActionResult<ServerInfoDto> GetServerInfo()
        {
            var settings = _Settings.Current;
            return new ServerInfoDto
            {
                Name = "Vocaluxe",
                Version = _Remote.Version,
                RequireJoinToken = settings.RequireJoinToken,
                AllowProfileCreation = settings.AllowProfileCreation
            };
        }

        /// <summary>Current state of the game.</summary>
        [HttpGet("state")]
        [RequireSession]
        public async Task<ActionResult<StateDto>> GetState()
        {
            return await _Dispatcher.InvokeAsync(() => _Remote.GetState());
        }
    }
}
