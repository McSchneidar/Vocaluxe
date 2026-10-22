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
using System.IO;
using Microsoft.Data.Sqlite;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Database
{
    public struct SDBStatisticsScore
    {
        public string Artist;
        public string Title;
        public string PlayerName;
        public int Score;
        public DateTime Date;
        public EGameDifficulty Difficulty;
        public bool Duet;
    }

    public struct SDBPlayerStatistics
    {
        public string PlayerName;
        public int NumSongs;
        public int NumDuets;
        public double AverageScore;
        public int BestScore;
    }

    public class CHighscoreDB : CDatabaseBase
    {
        public CHighscoreDB(string filePath) : base(filePath) { }

        public override bool Init()
        {
            if (!_CreateOrConvert(_FilePath))
            {
                CLog.Fatal("Cannot init Highscore DB: Error opening database: {DBFile}", CLog.Params(_FilePath));
                return false;
            }

            return true;
        }

        public override void Close()
        {
            //Do nothing
        }

        public bool GetDataBaseSongInfos(string artist, string title, out int numPlayed, out DateTime dateAdded, out int highscoreId)
        {
            string sArtist;
            string sTitle;
            int songId;
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception) { }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    songId = _GetDataBaseSongId(artist, title, 0, command);
                    highscoreId = songId;
                }
            }

            return _GetDataBaseSongInfos(songId, out sArtist, out sTitle, out numPlayed, out dateAdded, _FilePath);
        }

        public void IncreaseSongCounter(int dataBaseSongId)
        {
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception) { }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    _IncreaseSongCounter(dataBaseSongId, command);
                }
            }
        }

        public int AddScore(string playerName, int score, int lineNr, long date, int medley, int duet, int shortSong, int difficulty,
            string artist, string title, int numPlayed, string filePath)
        {
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return -1;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    var dataBaseSongId = _GetDataBaseSongId(artist, title, numPlayed, command);
                    var result = _AddScore(playerName, score, lineNr, date, medley, duet, shortSong, difficulty, dataBaseSongId, command);
                    return result;
                }
            }
        }

        public int AddScore(SPlayer player)
        {
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return -1;
                }

                var medley = 0;
                var duet = 0;
                var shortSong = 0;
                switch (player.GameMode)
                {
                    case EGameMode.TR_GAMEMODE_MEDLEY:
                        medley = 1;
                        break;
                    case EGameMode.TR_GAMEMODE_DUET:
                        duet = 1;
                        break;
                    case EGameMode.TR_GAMEMODE_SHORTSONG:
                        shortSong = 1;
                        break;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    var dataBaseSongId = CSongs.GetSong(player.SongId).DataBaseSongId;
                    return _AddScore(CProfiles.GetPlayerName(player.ProfileId), (int)Math.Round(player.Points), player.VoiceNr, player.DateTicks, medley,
                        duet, shortSong, (int)CProfiles.GetDifficulty(player.ProfileId), dataBaseSongId, command);
                }
            }
        }

        private int _AddScore(string playerName, int score, int lineNr, long date, int medley, int duet, int shortSong, int difficulty,
            int dataBaseSongId, SqliteCommand command)
        {
            var lastInsertId = -1;

            if (dataBaseSongId >= 0)
            {
                command.CommandText = "SELECT id FROM Scores WHERE SongId = @SongId AND PlayerName = @PlayerName AND Score = @Score AND " +
                                      "LineNr = @LineNr AND Date = @Date AND Medley = @Medley AND Duet = @Duet AND ShortSong = @ShortSong AND Difficulty = @Difficulty";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@SongId", dataBaseSongId);
                command.Parameters.AddWithValue("@PlayerName", playerName ?? string.Empty);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@LineNr", lineNr);
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@Medley", medley);
                command.Parameters.AddWithValue("@Duet", duet);
                command.Parameters.AddWithValue("@ShortSong", shortSong);
                command.Parameters.AddWithValue("@Difficulty", difficulty);

                SqliteDataReader reader = null;
                try
                {
                    reader = command.ExecuteReader();
                }
                catch (Exception) { }

                if (reader != null && reader.HasRows)
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }
                }

                if (reader != null)
                {
                    reader.Dispose();
                }

                command.CommandText = "INSERT INTO Scores (SongId, PlayerName, Score, LineNr, Date, Medley, Duet, ShortSong, Difficulty) " +
                                      "VALUES (@SongId, @PlayerName, @Score, @LineNr, @Date, @Medley, @Duet, @ShortSong, @Difficulty)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@SongId", dataBaseSongId);
                command.Parameters.AddWithValue("@PlayerName", playerName ?? string.Empty);
                command.Parameters.AddWithValue("@Score", score);
                command.Parameters.AddWithValue("@LineNr", lineNr);
                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@Medley", medley);
                command.Parameters.AddWithValue("@Duet", duet);
                command.Parameters.AddWithValue("@ShortSong", shortSong);
                command.Parameters.AddWithValue("@Difficulty", difficulty);
                command.ExecuteNonQuery();

                //Read last insert line
                command.CommandText = "SELECT id FROM Scores ORDER BY id DESC LIMIT 0, 1";

                reader = command.ExecuteReader();

                if (reader != null && reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lastInsertId = reader.GetInt32(0);
                    }

                    reader.Dispose();
                }
            }

            return lastInsertId;
        }

        public List<SDBScoreEntry> LoadScore(int songId, EGameMode gameMode, EHighscoreStyle style)
        {
            var scores = new List<SDBScoreEntry>();
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return scores;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    var medley = 0;
                    var duet = 0;
                    var shortSong = 0;
                    switch (gameMode)
                    {
                        case EGameMode.TR_GAMEMODE_MEDLEY:
                            medley = 1;
                            break;
                        case EGameMode.TR_GAMEMODE_DUET:
                            duet = 1;
                            break;
                        case EGameMode.TR_GAMEMODE_SHORTSONG:
                            shortSong = 1;
                            break;
                    }

                    var dataBaseSongId = _GetDataBaseSongId(songId, command);
                    if (dataBaseSongId < 0)
                    {
                        return scores;
                    }

                    switch (style)
                    {
                        case EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_BEST:
                            command.CommandText = "SELECT os.PlayerName, os.Score, os.Date, os.Difficulty, os.LineNr, os.id " +
                                                  "FROM Scores os " +
                                                  "INNER JOIN ( " +
                                                  "SELECT sc.PlayerName, sc.Score, sc.Difficulty, sc.LineNr, MIN(sc.Date) AS Date " +
                                                  "FROM Scores sc " +
                                                  "INNER JOIN ( " +
                                                  "SELECT Playername, MAX(Score) AS Score, Difficulty, LineNr " +
                                                  "FROM Scores " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "GROUP BY PlayerName, Difficulty, LineNr " +
                                                  ") AS mc " +
                                                  "ON sc.PlayerName = mc.PlayerName AND sc.Difficulty = mc.Difficulty AND sc.LineNr = mc.LineNr AND sc.Score = mc.Score " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "GROUP BY sc.PlayerName, sc.Difficulty, sc.LineNr, sc.Score " +
                                                  ") AS iq " +
                                                  "ON os.PlayerName = iq.PlayerName AND os.Difficulty = iq.Difficulty AND os.LineNr = iq.LineNr AND os.Score = iq.Score AND os.Date = iq.Date " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "GROUP BY os.PlayerName, os.Difficulty, os.LineNr, os.Score " +
                                                  "ORDER BY os.Score DESC, os.Date ASC";
                            break;
                        case EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL:
                            command.CommandText = "SELECT PlayerName, Score, Date, Difficulty, LineNr, id " +
                                                  "FROM Scores " +
                                                  "WHERE [SongId] = @SongId AND [Medley] = @Medley AND [Duet] = @Duet AND [ShortSong] = @ShortSong " +
                                                  "ORDER BY [Score] DESC, [Date] ASC";
                            break;
                    }

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@SongId", dataBaseSongId);
                    command.Parameters.AddWithValue("@Medley", medley);
                    command.Parameters.AddWithValue("@Duet", duet);
                    command.Parameters.AddWithValue("@ShortSong", shortSong);

                    var reader = command.ExecuteReader();
                    if (reader != null && reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var score = new SDBScoreEntry
                            {
                                Name = reader.GetString(0),
                                Score = reader.GetInt32(1),
                                Date = new DateTime(reader.GetInt64(2)).ToString("dd/MM/yyyy"),
                                Difficulty = (EGameDifficulty)reader.GetInt32(3),
                                VoiceNr = reader.GetInt32(4),
                                Id = reader.GetInt32(5)
                            };

                            scores.Add(score);
                        }

                        reader.Dispose();
                    }
                }
            }

            return scores;
        }

        private const string _StatisticsScoresQuery = "SELECT so.Artist, so.Title, sc.PlayerName, sc.Score, sc.Date, sc.Difficulty, sc.Duet " +
                                                      "FROM Scores sc INNER JOIN Songs so ON sc.SongId = so.id ";
        private const string _RecentScoresQuery = _StatisticsScoresQuery + "ORDER BY sc.Date DESC, sc.id DESC LIMIT @Count";
        private const string _TopScoresQuery = _StatisticsScoresQuery + "WHERE sc.Difficulty = @Difficulty ORDER BY sc.Score DESC, sc.Date ASC LIMIT @Count";

        public List<SDBStatisticsScore> LoadRecentScores(int count)
        {
            return _LoadStatisticsScores(count, null);
        }

        public List<SDBStatisticsScore> LoadTopScores(EGameDifficulty difficulty, int count)
        {
            return _LoadStatisticsScores(count, difficulty);
        }

        private List<SDBStatisticsScore> _LoadStatisticsScores(int count, EGameDifficulty? difficulty)
        {
            var scores = new List<SDBStatisticsScore>();
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return scores;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = difficulty.HasValue ? _TopScoresQuery : _RecentScoresQuery;
                    command.Parameters.AddWithValue("@Count", count);
                    if (difficulty.HasValue)
                    {
                        command.Parameters.AddWithValue("@Difficulty", (int)difficulty.Value);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scores.Add(new SDBStatisticsScore
                            {
                                Artist = reader.GetString(0),
                                Title = reader.GetString(1),
                                PlayerName = reader.GetString(2),
                                Score = reader.GetInt32(3),
                                Date = new DateTime(reader.GetInt64(4), DateTimeKind.Local),
                                Difficulty = (EGameDifficulty)reader.GetInt32(5),
                                Duet = reader.GetInt32(6) != 0
                            });
                        }
                    }
                }
            }

            return scores;
        }

        public List<SDBPlayerStatistics> LoadPlayerStatistics()
        {
            var players = new List<SDBPlayerStatistics>();
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + _FilePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return players;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT PlayerName, COUNT(*), SUM(Duet), AVG(Score), MAX(Score) FROM Scores " +
                                          "GROUP BY PlayerName ORDER BY COUNT(*) DESC, AVG(Score) DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            players.Add(new SDBPlayerStatistics
                            {
                                PlayerName = reader.GetString(0),
                                NumSongs = reader.GetInt32(1),
                                NumDuets = reader.GetInt32(2),
                                AverageScore = reader.GetDouble(3),
                                BestScore = reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return players;
        }

        private void _IncreaseSongCounter(int dataBaseSongId, SqliteCommand command)
        {
            command.CommandText = "UPDATE Songs SET NumPlayed = NumPlayed + 1 WHERE [id] = @id";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@id", dataBaseSongId);
            command.ExecuteNonQuery();
        }

        private int _GetDataBaseSongId(int songId, SqliteCommand command)
        {
            var song = CSongs.GetSong(songId);

            if (song == null)
            {
                return -1;
            }

            return _GetDataBaseSongId(song.Artist, song.Title, 0, command);
        }

        private int _GetDataBaseSongId(string artist, string title, int defNumPlayed, SqliteCommand command)
        {
            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@title", title ?? string.Empty);
            command.Parameters.AddWithValue("@artist", artist ?? string.Empty);

            var reader = command.ExecuteReader();

            if (reader != null && reader.HasRows)
            {
                reader.Read();
                var id = reader.GetInt32(0);
                reader.Dispose();
                return id;
            }

            if (reader != null)
            {
                reader.Close();
            }

            command.CommandText = "INSERT INTO Songs (Title, Artist, NumPlayed, DateAdded) " +
                                  "VALUES (@title, @artist, @numplayed, @dateadded)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@title", title ?? string.Empty);
            command.Parameters.AddWithValue("@artist", artist ?? string.Empty);
            command.Parameters.AddWithValue("@numplayed", defNumPlayed);
            command.Parameters.AddWithValue("@dateadded", DateTime.Now.Ticks);
            command.ExecuteNonQuery();

            command.CommandText = "SELECT id FROM Songs WHERE [Title] = @title AND [Artist] = @artist";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@title", title ?? string.Empty);
            command.Parameters.AddWithValue("@artist", artist ?? string.Empty);

            reader = command.ExecuteReader();

            if (reader != null)
            {
                reader.Read();
                var id = reader.GetInt32(0);
                reader.Dispose();
                return id;
            }

            return -1;
        }

        private bool _GetDataBaseSongInfos(int songId, out string artist, out string title, out int numPlayed, out DateTime dateAdded, string filePath)
        {
            artist = string.Empty;
            title = string.Empty;
            numPlayed = 0;
            dateAdded = DateTime.Today;

            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT Artist, Title, NumPlayed, DateAdded FROM Songs WHERE [id] = @id";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@id", songId);

                    SqliteDataReader reader;
                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception)
                    {
                        return false;
                    }

                    if (reader != null && reader.HasRows)
                    {
                        reader.Read();

                        if (!reader.IsDBNull(0))
                        {
                            artist = reader.GetString(0);
                        }

                        if (!reader.IsDBNull(1))
                        {
                            title = reader.GetString(1);
                        }

                        if (!reader.IsDBNull(2))
                        {
                            numPlayed = reader.GetInt32(2);
                        }

                        if (!reader.IsDBNull(3))
                        {
                            dateAdded = new DateTime(reader.GetInt64(3));
                        }

                        reader.Dispose();
                        return true;
                    }

                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                }
            }

            return false;
        }

        private void _CreateHighscoreDB(string filePath)
        {
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Version ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, Value INTEGER NOT NULL);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Version (id, Value) VALUES(NULL, @Value)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Value", CSettings.DatabaseHighscoreVersion);
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Songs ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "Artist TEXT NOT NULL, Title TEXT NOT NULL, NumPlayed INTEGER, DateAdded BIGINT);";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE IF NOT EXISTS Scores ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                          "SongId INTEGER NOT NULL, PlayerName TEXT NOT NULL, Score INTEGER NOT NULL, LineNr INTEGER NOT NULL, Date BIGINT NOT NULL, " +
                                          "Medley INTEGER NOT NULL, Duet INTEGER NOT NULL, ShortSong INTEGER NOT NULL, Difficulty INTEGER NOT NULL);";
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        ///     Creates a new Vocaluxe Database if no file exists.
        /// </summary>
        /// <param name="filePath">Database file path</param>
        /// <returns></returns>
        private bool _CreateOrConvert(string filePath)
        {
            var result = true;
            using (var connection = new SqliteConnection())
            {
                connection.ConnectionString = "Data Source=" + filePath;

                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    return false;
                }

                using (var command = new SqliteCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT Value FROM Version";
                    SqliteDataReader reader = null;

                    try
                    {
                        reader = command.ExecuteReader();
                    }
                    catch (Exception) { }

                    if (reader == null || reader.FieldCount == 0)
                    {
                        // create new database/tables
                        _CreateHighscoreDB(filePath);
                    }
                    else
                    {
                        reader.Read();
                        var currentVersion = reader.GetInt32(0);
                        if (currentVersion < CSettings.DatabaseHighscoreVersion)
                        {
                            // update database
                            result &= _UpdateDatabase(currentVersion, connection);
                        }
                    }

                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                }
            }

            return result;
        }

        private bool _UpdateDatabase(int currentVersion, SqliteConnection connection)
        {
            var updated = true;

            if (currentVersion < 2)
            {
                updated &= _ConvertV1toV2(connection);
            }
            else if (currentVersion < 3)
            {
                updated &= _ConvertV2toV3(connection);
            }

            return updated;
        }

        private bool _ConvertV1toV2(SqliteConnection connection)
        {
            using (var command = new SqliteCommand())
            {
                command.Connection = connection;
                command.CommandText = "ALTER TABLE Scores ADD ShortSong INTEGER";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Scores SET [ShortSong] = @ShortSong";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@ShortSong", 0);
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE Version SET [Value] = @version";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@version", 2);
                command.ExecuteNonQuery();
            }

            return true;
        }

        private bool _ConvertV2toV3(SqliteConnection connection)
        {
            var command = new SqliteCommand("ALTER TABLE Songs ADD DateAdded BIGINT", connection);

            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Songs SET [DateAdded] = @DateAdded";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@DateAdded", DateTime.Now.Ticks);
            command.ExecuteNonQuery();
            command.CommandText = "UPDATE Version SET [Value] = @version";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@version", 3);
            command.ExecuteNonQuery();

            //Read NumPlayed from Scores and save to Songs
            command.CommandText = "SELECT SongId, Date FROM Scores ORDER BY Date ASC";

            SqliteDataReader reader;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception)
            {
                return false;
            }

            long lastDateAdded = -1;
            var lastId = -1;
            var dt = new DateTime(1, 1, 1, 0, 0, 5);
            var sec = dt.Ticks;
            var ids = new List<int>();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var dateAdded = reader.GetInt64(1);
                if (id != lastId || dateAdded > lastDateAdded + sec)
                {
                    ids.Add(id);
                    lastId = id;
                    lastDateAdded = dateAdded;
                }
            }

            reader.Dispose();

            foreach (var id in ids)
            {
                _IncreaseSongCounter(id, command);
            }

            command.Dispose();

            return true;
        }
    }
}