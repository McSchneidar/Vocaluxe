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
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb
{
    /// <summary>
    ///     The only view the webserver has on the game. Implemented by Vocaluxe.
    ///     Methods marked as main-thread-only must be called through <see cref="Dispatching.CMainThreadDispatcher" />.
    /// </summary>
    public interface IVocaluxeRemote
    {
        /// <summary>Full version text of the running Vocaluxe. Thread-safe.</summary>
        string Version { get; }

        /// <summary>Current state of the game. Main thread only.</summary>
        StateDto GetState();

        #region profiles
        /// <summary>All profiles, sorted by name. Main thread only.</summary>
        IList<RemoteProfile> GetProfiles();

        /// <summary>Creates a profile and returns its id. Main thread only.</summary>
        Guid CreateProfile(ProfileChange change);

        /// <summary>Changes the given fields of a profile. Main thread only.</summary>
        void UpdateProfile(Guid profileId, ProfileChange change);
        #endregion

        #region songs
        /// <summary>Changes whenever songs are added or (re)loaded. Main thread only.</summary>
        int SongLibraryVersion { get; }

        /// <summary>All songs. Main thread only.</summary>
        IList<RemoteSong> GetSongs();
        #endregion

        #region playlists
        /// <summary>All playlists with their songs. Main thread only.</summary>
        IList<RemotePlaylist> GetPlaylists();

        /// <summary>Creates an empty playlist and returns its id. Main thread only.</summary>
        int CreatePlaylist(string name);

        /// <summary>Main thread only.</summary>
        void RenamePlaylist(int playlistId, string name);

        /// <summary>Main thread only.</summary>
        void DeletePlaylist(int playlistId);

        /// <summary>Replaces all songs of a playlist and saves it. Main thread only.</summary>
        void SetPlaylistEntries(int playlistId, IList<RemotePlaylistEntry> entries);
        #endregion

        #region input
        /// <summary>Sends a key press to the game as if it came from a keyboard. Thread-safe.</summary>
        void SendKey(ERemoteKey key);

        /// <summary>Types text into the game. Thread-safe.</summary>
        void SendText(string text);
        #endregion
    }
}
