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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Contracts
{
    public enum EPermission
    {
        AddToQueue,
        ManageQueue,
        UseKeyboard,
        EditPlaylists,
        DeletePlaylists,
        EditOwnProfile,
        EditAllProfiles,
        CreateProfiles
    }

    #region profiles & session
    public sealed class ProfileDto
    {
        [Required] public Guid Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public ERemoteRole Role { get; set; }
        [Required] public bool HasPin { get; set; }
        [Required] public bool HasAvatar { get; set; }

        /// <summary>0 = easy, 1 = normal, 2 = hard</summary>
        [Required] public int Difficulty { get; set; }
    }

    public sealed class ProfileCreateDto
    {
        [Required, StringLength(32, MinimumLength = 1)]
        public string Name { get; set; }

        [StringLength(32, MinimumLength = 4)]
        public string Pin { get; set; }

        [Range(0, 2)]
        public int? Difficulty { get; set; }

        /// <summary>Token from the QR code, needed when joining is restricted.</summary>
        public string JoinToken { get; set; }
    }

    public sealed class ProfileUpdateDto
    {
        [StringLength(32, MinimumLength = 1)]
        public string Name { get; set; }

        [Range(0, 2)]
        public int? Difficulty { get; set; }

        /// <summary>New PIN. Use an empty string to remove the PIN.</summary>
        [StringLength(32)]
        public string Pin { get; set; }
    }

    public sealed class RoleUpdateDto
    {
        [Required] public ERemoteRole? Role { get; set; }
    }

    public sealed class SessionCreateDto
    {
        [Required] public Guid? ProfileId { get; set; }
        public string Pin { get; set; }

        /// <summary>Token from the QR code, needed when joining is restricted.</summary>
        public string JoinToken { get; set; }
    }

    public sealed class SessionDto
    {
        [Required] public ProfileDto Profile { get; set; }
        [Required] public IList<EPermission> Permissions { get; set; }
    }
    #endregion

    #region songs
    public sealed class SongDto
    {
        [Required] public int Id { get; set; }
        [Required] public string Title { get; set; }
        [Required] public string Artist { get; set; }
        public string Year { get; set; }
        public string Language { get; set; }
        public string Genre { get; set; }
        [Required] public bool IsDuet { get; set; }
        [Required] public bool HasCover { get; set; }
    }

    public sealed class SongPageDto
    {
        [Required] public IList<SongDto> Items { get; set; }
        [Required] public int Total { get; set; }
        [Required] public int Page { get; set; }
        [Required] public int PageSize { get; set; }
    }

    public sealed class FacetValueDto
    {
        [Required] public string Value { get; set; }
        [Required] public int Count { get; set; }
    }

    public sealed class SongFacetsDto
    {
        [Required] public IList<FacetValueDto> Languages { get; set; }
        [Required] public IList<FacetValueDto> Genres { get; set; }
        [Required] public IList<FacetValueDto> Decades { get; set; }
    }

    public enum ESongSort
    {
        Artist,
        Title,
        Year,
        Newest,
        Popular
    }
    #endregion

    #region playlists & queue
    public sealed class PlaylistSummaryDto
    {
        [Required] public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public int SongCount { get; set; }
    }

    public sealed class PlaylistEntryDto
    {
        [Required] public int Position { get; set; }
        [Required] public SongDto Song { get; set; }
    }

    public sealed class PlaylistDto
    {
        [Required] public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public IList<PlaylistEntryDto> Songs { get; set; }
    }

    public sealed class PlaylistNameDto
    {
        [Required, StringLength(64, MinimumLength = 1)]
        public string Name { get; set; }
    }

    public sealed class SongRefDto
    {
        [Required] public int? SongId { get; set; }

        /// <summary>Insert position; appended when missing.</summary>
        public int? Position { get; set; }
    }

    public sealed class PositionDto
    {
        [Required, Range(0, int.MaxValue)]
        public int? Position { get; set; }
    }

    public sealed class ProfileRefDto
    {
        [Required] public Guid Id { get; set; }
        [Required] public string Name { get; set; }
    }

    public sealed class QueueEntryDto
    {
        [Required] public int Position { get; set; }
        [Required] public SongDto Song { get; set; }

        /// <summary>Who wished the song; null if unknown (e.g. added in the game).</summary>
        public ProfileRefDto RequestedBy { get; set; }
    }
    #endregion

    #region input & admin
    public sealed class KeyDto
    {
        [Required] public ERemoteKey? Key { get; set; }
    }

    public sealed class TextDto
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Text { get; set; }
    }

    public sealed class AdminSettingsDto
    {
        /// <summary>Only devices that scanned the QR code can log in.</summary>
        [Required] public bool RequireJoinToken { get; set; }

        /// <summary>New profiles can be created on the login page.</summary>
        [Required] public bool AllowProfileCreation { get; set; }

        /// <summary>Name of the playlist used as queue.</summary>
        [Required, StringLength(64, MinimumLength = 1)]
        public string QueuePlaylistName { get; set; }

        [Required] public IList<EPermission> GuestPermissions { get; set; }
        [Required] public IList<EPermission> PlayerPermissions { get; set; }
    }
    #endregion
}
