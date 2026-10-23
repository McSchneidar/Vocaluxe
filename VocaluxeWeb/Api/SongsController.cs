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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [Route("api/v1/songs")]
    [RequireSession]
    public sealed class SongsController : ApiControllerBase
    {
        private readonly CSongLibrary _Songs;
        private readonly CImageService _Images;

        public SongsController(CSongLibrary songs, CImageService images)
        {
            _Songs = songs;
            _Images = images;
        }

        /// <summary>Searches the song library. All words of the query must appear in artist, title or edition.</summary>
        [HttpGet]
        public async Task<ActionResult<SongPageDto>> Search([FromQuery] string query = null, [FromQuery] string language = null, [FromQuery] string genre = null,
            [FromQuery] int? decade = null, [FromQuery] bool? duet = null, [FromQuery] ESongSort sort = ESongSort.Artist,
            [FromQuery] int page = 0, [FromQuery] int pageSize = 50)
        {
            return await _Songs.SearchAsync(new CSongQuery
            {
                Text = query,
                Language = language,
                Genre = genre,
                Decade = decade,
                Duet = duet,
                Sort = sort,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>Available languages, genres and decades with the number of songs, for filters.</summary>
        [HttpGet("facets")]
        public async Task<ActionResult<SongFacetsDto>> GetFacets()
        {
            return await _Songs.GetFacetsAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SongDto>> Get(int id)
        {
            var song = await _Songs.GetAsync(id);
            if (song == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Song not found.");
            }

            return CSongLibrary.ToDto(song);
        }

        /// <summary>The cover as JPEG, scaled to fit the given size (96, 200 or 400).</summary>
        [HttpGet("{id}/cover")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCover(int id, [FromQuery] int size = 200)
        {
            var song = await _Songs.GetAsync(id);
            var file = _Images.GetScaled(song?.CoverPath, size, out var etag);
            if (file == null)
            {
                return NotFound();
            }

            return CImageResult.Create(this, file, etag);
        }
    }
}
