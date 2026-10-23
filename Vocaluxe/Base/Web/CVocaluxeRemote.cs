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

using VocaluxeWeb;
using VocaluxeWeb.Contracts;

namespace Vocaluxe.Base.Web
{
    /// <summary>Gives the webserver access to the game. Everything except <see cref="Version" /> runs on the main thread.</summary>
    class CVocaluxeRemote : IVocaluxeRemote
    {
        public string Version { get; } = CSettings.FullVersion;

        public StateDto GetState()
        {
            var song = CGame.GetSong();
            return new StateDto
            {
                Screen = CGraphics.CurrentScreenType.ToString(),
                CurrentSong = song == null ? null : new SongSummaryDto { Id = song.Id, Title = song.Title, Artist = song.Artist }
            };
        }
    }
}
