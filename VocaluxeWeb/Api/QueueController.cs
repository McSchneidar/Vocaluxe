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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    /// <summary>Song wishes. Everybody gets one song per round; entries can be removed by their requester or by people allowed to manage the queue.</summary>
    [Route("api/v1/queue")]
    [RequireSession]
    public sealed class QueueController : ApiControllerBase
    {
        private readonly CQueueService _Queue;
        private readonly CSongLibrary _Songs;

        public QueueController(CQueueService queue, CSongLibrary songs)
        {
            _Queue = queue;
            _Songs = songs;
        }

        [HttpGet]
        public async Task<ActionResult<IList<QueueEntryDto>>> Get()
        {
            return Ok(await _Queue.GetAsync());
        }

        [HttpPost]
        [RequireSession(EPermission.AddToQueue)]
        public async Task<ActionResult<IList<QueueEntryDto>>> Add([FromBody] SongRefDto request)
        {
            if (await _Songs.GetAsync(request.SongId.Value) == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Song not found.");
            }

            return Ok(await _Queue.AddAsync(request.SongId.Value, CurrentSession));
        }

        [HttpDelete("{position}")]
        public async Task<ActionResult<IList<QueueEntryDto>>> Remove(int position)
        {
            return Ok(await _Queue.RemoveAsync(position, CurrentSession, HasPermission(EPermission.ManageQueue)));
        }

        [HttpPut("{position}/position")]
        [RequireSession(EPermission.ManageQueue)]
        public async Task<ActionResult<IList<QueueEntryDto>>> Move(int position, [FromBody] PositionDto request)
        {
            return Ok(await _Queue.MoveAsync(position, request.Position.Value));
        }
    }
}
