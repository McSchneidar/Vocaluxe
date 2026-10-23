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
using System.Linq;
using System.Threading.Tasks;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Services
{
    /// <summary>
    ///     The queue is a normal playlist of the game (name from the settings), so it can also be used in the song screen.
    ///     Who wished which song is only known to the webserver; entries added in the game have no requester.
    ///     New wishes are inserted fairly: everybody gets one song per round.
    /// </summary>
    public sealed class CQueueService
    {
        private sealed class COwner
        {
            public int SongId;
            public Guid ProfileId;
            public string Name;
        }

        public sealed class CForbiddenException : Exception { }

        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CWebSettingsStore _Settings;
        private readonly CSongLibrary _Songs;
        private readonly object _Lock = new object();
        private List<COwner> _Owners = new List<COwner>();

        public CQueueService(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CWebSettingsStore settings, CSongLibrary songs)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Settings = settings;
            _Songs = songs;
        }

        public async Task<IList<QueueEntryDto>> GetAsync()
        {
            var snapshot = await _ChangeAsync((entries, owners) => false);
            return await _ToDtosAsync(snapshot);
        }

        public async Task<IList<QueueEntryDto>> AddAsync(int songId, CSession session)
        {
            var snapshot = await _ChangeAsync((entries, owners) =>
            {
                var position = _FairPosition(owners, session.ProfileId);
                entries.Insert(position, new RemotePlaylistEntry { SongId = songId });
                owners.Insert(position, new COwner { SongId = songId, ProfileId = session.ProfileId, Name = session.ProfileName });
                return true;
            });
            return await _ToDtosAsync(snapshot);
        }

        public async Task<IList<QueueEntryDto>> RemoveAsync(int position, CSession session, bool canManage)
        {
            var snapshot = await _ChangeAsync((entries, owners) =>
            {
                if (position >= entries.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                if (!canManage && owners[position]?.ProfileId != session.ProfileId)
                {
                    throw new CForbiddenException();
                }

                entries.RemoveAt(position);
                owners.RemoveAt(position);
                return true;
            });
            return await _ToDtosAsync(snapshot);
        }

        public async Task<IList<QueueEntryDto>> MoveAsync(int position, int newPosition)
        {
            var snapshot = await _ChangeAsync((entries, owners) =>
            {
                if (position >= entries.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                newPosition = Math.Min(newPosition, entries.Count - 1);
                var entry = entries[position];
                var owner = owners[position];
                entries.RemoveAt(position);
                owners.RemoveAt(position);
                entries.Insert(newPosition, entry);
                owners.Insert(newPosition, owner);
                return true;
            });
            return await _ToDtosAsync(snapshot);
        }

        /// <summary>Reads the queue playlist and applies a change in one go on the main thread, so the game cannot interfere.</summary>
        private Task<List<(int SongId, COwner Owner)>> _ChangeAsync(Func<List<RemotePlaylistEntry>, List<COwner>, bool> change)
        {
            var name = _Settings.Current.QueuePlaylistName;
            return _Dispatcher.InvokeAsync(() =>
            {
                var playlist = _Remote.GetPlaylists().FirstOrDefault(p => p.Name == name);
                var playlistId = playlist?.Id ?? _Remote.CreatePlaylist(name);
                var entries = playlist?.Entries.ToList() ?? new List<RemotePlaylistEntry>();

                lock (_Lock)
                {
                    var owners = _Match(entries);
                    if (change(entries, owners))
                    {
                        _Remote.SetPlaylistEntries(playlistId, entries);
                    }

                    _Owners = owners;
                    return entries.Select((e, i) => (e.SongId, owners[i])).ToList();
                }
            });
        }

        /// <summary>Assigns the known requesters to the current playlist entries, which may have been changed in the game.</summary>
        private List<COwner> _Match(IList<RemotePlaylistEntry> entries)
        {
            var result = new List<COwner>(entries.Count);
            var next = 0;
            foreach (var entry in entries)
            {
                var found = _Owners.FindIndex(next, o => o != null && o.SongId == entry.SongId);
                if (found >= 0)
                {
                    result.Add(_Owners[found]);
                    next = found + 1;
                }
                else
                {
                    result.Add(null);
                }
            }

            return result;
        }

        /// <summary>
        ///     The n-th wish of a person goes behind everybody's n-th wish.
        ///     Entries without requester count as a wish of their own.
        /// </summary>
        private static int _FairPosition(IList<COwner> owners, Guid profileId)
        {
            var round = owners.Count(o => o?.ProfileId == profileId);
            var seen = new Dictionary<Guid, int>();
            var position = 0;
            for (var i = 0; i < owners.Count; i++)
            {
                var entryRound = 0;
                if (owners[i] != null)
                {
                    seen.TryGetValue(owners[i].ProfileId, out entryRound);
                    seen[owners[i].ProfileId] = entryRound + 1;
                }

                if (entryRound <= round)
                {
                    position = i + 1;
                }
            }

            return position;
        }

        private async Task<IList<QueueEntryDto>> _ToDtosAsync(List<(int SongId, COwner Owner)> snapshot)
        {
            var result = new List<QueueEntryDto>(snapshot.Count);
            for (var i = 0; i < snapshot.Count; i++)
            {
                var owner = snapshot[i].Owner;
                result.Add(new QueueEntryDto
                {
                    Position = i,
                    Song = await _Songs.GetDtoAsync(snapshot[i].SongId),
                    RequestedBy = owner == null ? null : new ProfileRefDto { Id = owner.ProfileId, Name = owner.Name }
                });
            }

            return result;
        }
    }
}
