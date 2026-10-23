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

namespace VocaluxeWeb.Api
{
    [ApiController]
    [Route("api/v1")]
    [Produces("application/json")]
    public sealed class ServerController : ControllerBase
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;

        public ServerController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
        }

        /// <summary>Name and version of the server.</summary>
        [HttpGet("server")]
        public ActionResult<ServerInfoDto> GetServerInfo()
        {
            return new ServerInfoDto { Name = "Vocaluxe", Version = _Remote.Version };
        }

        /// <summary>Current state of the game.</summary>
        [HttpGet("state")]
        public async Task<ActionResult<StateDto>> GetState()
        {
            return await _Dispatcher.InvokeAsync(() => _Remote.GetState());
        }
    }
}
