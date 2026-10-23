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
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [Route("api/v1/admin")]
    [RequireAdmin]
    public sealed class AdminController : ApiControllerBase
    {
        private readonly CWebSettingsStore _Settings;

        public AdminController(CWebSettingsStore settings)
        {
            _Settings = settings;
        }

        [HttpGet("settings")]
        public ActionResult<AdminSettingsDto> GetSettings()
        {
            return _ToDto(_Settings.Current);
        }

        [HttpPut("settings")]
        public ActionResult<AdminSettingsDto> UpdateSettings([FromBody] AdminSettingsDto request)
        {
            _Settings.Update(s =>
            {
                s.RequireJoinToken = request.RequireJoinToken;
                s.AllowProfileCreation = request.AllowProfileCreation;
                s.QueuePlaylistName = request.QueuePlaylistName.Trim();
                s.GuestPermissions = request.GuestPermissions.Distinct().ToList();
                s.PlayerPermissions = request.PlayerPermissions.Distinct().ToList();
            });
            return _ToDto(_Settings.Current);
        }

        private static AdminSettingsDto _ToDto(CWebSettings settings)
        {
            return new AdminSettingsDto
            {
                RequireJoinToken = settings.RequireJoinToken,
                AllowProfileCreation = settings.AllowProfileCreation,
                QueuePlaylistName = settings.QueuePlaylistName,
                GuestPermissions = settings.GuestPermissions,
                PlayerPermissions = settings.PlayerPermissions
            };
        }
    }
}
