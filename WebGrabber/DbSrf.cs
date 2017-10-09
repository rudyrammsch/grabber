using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WebGrabber.Entities;

namespace WebGrabber
{
    public class DbSrf : Db<DbContext>, IDb
    {
        public readonly List<ChannelInfo> ChannelList = new List<ChannelInfo>
        {
            new ChannelInfo { ChannelId = Guid.Parse("69e8ac16-4327-4af4-b873-fd5cd6e895a7"), ChannelName = "Srf1" },
            new ChannelInfo { ChannelId = Guid.Parse("c8537421-c9c5-4461-9c9c-c15816458b46"), ChannelName = "Srf2" },
            new ChannelInfo { ChannelId = Guid.Parse("dd0fa1ba-4ff6-4e1a-ab74-d7e49057d96f"), ChannelName = "Srf3" }
        };

        public List<ImportInfo> ImportLog { get; }

        public DbSrf()
        {
            ImportLog = new List<ImportInfo>();
        }

        public override bool CreateDb()
        {
            bool dbMustBeSeeded;

            OptionsBuilder.EnableSensitiveDataLogging(false);

            using (var db = new SrfPlayListContext(OptionsBuilder.Options))
            {
                dbMustBeSeeded = db.Database.EnsureCreated();
            }

            return dbMustBeSeeded;
        }

        private DateTime? GetLastUpdateTime(Guid channelId)
        {
            DateTime? dt;

            using (var db = new SrfPlayListContext(OptionsBuilder.Options))
            {
                if (db.MetaInfo.Any(i => i.ChannelId.Equals(channelId)))
                {
                    dt = db.MetaInfo.Single(m => m.ChannelId.Equals(channelId)).LastUpdate;
                }
                else
                    dt = null;
            }

            return dt;
        }

        private void InsertOrUpdateMetaInfo(Guid channelId, string channelName)
        {
            using (var db = new SrfPlayListContext(OptionsBuilder.Options))
            {
                var metaInfo = db.MetaInfo.Local.FirstOrDefault(s => s.ChannelId.Equals(channelId));

                if (metaInfo == default(MetaInfo))
                    metaInfo = db.MetaInfo.FirstOrDefault(s => s.ChannelId.Equals(channelId));

                if (metaInfo == default(MetaInfo))
                {
                    metaInfo = new MetaInfo
                    {
                        ChannelId = channelId,
                        ChannelName = channelName
                    };

                    db.Add(metaInfo);
                }

                metaInfo.LastUpdate = DateTime.Now;

                db.SaveChanges();
            }
        }

        public override void Import(Guid channelId = default(Guid))
        {
            var makeInitialImport = CreateDb();

            var isChannelIdGiven = channelId != default(Guid);

            using (var wc = new WebClient())
            {
                foreach (var channelInfo in ChannelList)
                {
                    var dt = new DateTime(2000, 01, 01);
                    var dtDelta = dt.AddDays(7);

                    if (!makeInitialImport)
                    {
                        if (isChannelIdGiven && !channelInfo.ChannelId.Equals(channelId))
                            continue;

                        var lastUpdate = GetLastUpdateTime(channelInfo.ChannelId);

                        if (lastUpdate != null)
                        {
                            dt = new DateTime(lastUpdate.Value.Year, lastUpdate.Value.Month, lastUpdate.Value.Day).AddDays(-1);
                            dtDelta = dt.AddDays(7);
                        }
                    }

                    do
                    {
                        var fromDate = String.Format("{0}-{1}-{2}T00%3A00%3A00", dt.Year, dt.Month.ToString().PadLeft(2, '0'), dt.Day.ToString().PadLeft(2, '0'));
                        var toDate = String.Format("{0}-{1}-{2}T23%3A59%3A59", dtDelta.Year, dtDelta.Month.ToString().PadLeft(2, '0'), dtDelta.Day.ToString().PadLeft(2, '0'));
                        var url = String.Format(@"https://ws.srf.ch/songlog/log/channel/{0}.json?callback=songLogPollerCallback_musik&fromDate={1}&toDate={2}&page.size=10000&page.page=0&page.sort=playedDate&page.sort.dir=asc", channelInfo.ChannelId, fromDate, toDate);
                        var jsonText = wc.DownloadString(url);

                        jsonText = jsonText.Remove(jsonText.LastIndexOf(';'), 1).Remove(jsonText.LastIndexOf(')'), 1).Remove(0, jsonText.IndexOf('(') + 1);

                        var msg = $"Inserting stuff for timespan {dt.ToShortDateString()} - {dtDelta.ToShortDateString()}";
                        var log = new ImportInfo { ChannelName = channelInfo.ChannelName };
                        log.Messages.Add(msg);
                        ImportLog.Add(log);

                        if (!Insert(jsonText))
                        {
                            var logNothingInserted = new ImportInfo { ChannelName = channelInfo.ChannelName };

                            logNothingInserted.Messages.Add($"Nothing inserted for timespan {dt.ToShortDateString()} - {dtDelta.ToShortDateString()}");
                            ImportLog.Add(logNothingInserted);
                        }

                        dt = dt.AddDays(8);
                        dtDelta = dt.AddDays(7);
                    } while (DateTime.Now > dt.AddDays(-8));

                    InsertOrUpdateMetaInfo(channelInfo.ChannelId, channelInfo.ChannelName);
                }
            }
        }

        public override bool Insert(string json)
        {
            var data = JsonConvert.DeserializeObject<RootObject>(json);

            if (data == null || !data.Songlog.Any())
                return false;

            if (data.pageSize < data.Songlog.Count) {
                var log = new ImportInfo();
                log.Messages.Add("data.pageSize < data.Songlog.Count: " + data.Id + " => " + data.pageSize + " < " + data.Songlog.Count);
                ImportLog.Add(log);
            }

            using (var db = new SrfPlayListContext(OptionsBuilder.Options))
            {
                foreach (var songLogData in data.Songlog)
                {
                    // check for already imported songlog
                    var songlog = db.Songlogs.Local.FirstOrDefault(s => s.id.Equals(songLogData.id));
                    if (songlog == default(Songlog))
                        songlog = db.Songlogs.FirstOrDefault(s => s.id.Equals(songLogData.id));
                    if (songlog != default(Songlog))
                        continue;

                    var artist = Artist.Add(db, songLogData);
                    var song = Song.Add(db, songLogData, artist);

                    // checked for existing above
                    songlog = Songlog.Add(db, songLogData, song);

                    var log = new ImportInfo
                    {
                        Artist = artist.name,
                        Song = song.title,
                        PlayedDate = songlog.playedDate
                    };
                    ImportLog.Add(log);
                }

                db.SaveChanges();
            }

            return true;
        }
    }

    public class ChannelInfo
    {
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
    }

    public class ImportInfo
    {
        public ImportInfo()
        {
            Messages = new List<string>();
        }

        public string ChannelName { get; set; }
        public string Song { get; set; }
        public string Artist { get; set; }
        public DateTime? PlayedDate { get; set; }
        public List<string> Messages { get; set; }
    }
}
