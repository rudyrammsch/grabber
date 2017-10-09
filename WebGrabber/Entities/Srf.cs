using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
// ReSharper disable InconsistentNaming

namespace WebGrabber.Entities
{
    //class Srf
    //{
    //    public ICollection<RootObject> Data { get; set; }
    //}

    public class SrfPlayListContext : DbContextBase
    {
        public DbSet<Songlog> Songlogs { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Artist> Artists { get; set; }

        public SrfPlayListContext()
        {

        }

        public SrfPlayListContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            var connStr = Configuration["ConnectionStrings:GrabberDatabase"];

            options.UseSqlServer(connStr);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ignore columns
            //builder.Entity<Songlog>().Ignore(s => s.Song);
            builder.Entity<Artist>().Ignore(a => a.Songs);
            builder.Entity<Song>().Ignore(s => s.Artist);

            // keys/indices for 'songlog'
            builder.Entity<Songlog>()
                .HasKey(s => s.id)
                .ForSqlServerIsClustered(false);
            builder.Entity<Songlog>()
                .HasOne(sl => sl.Song)
                /*.WithOne(s => s.Songlog)*/;
            builder.Entity<Songlog>()
                .HasIndex(sl => sl.channelId);
            builder.Entity<Songlog>()
                .HasIndex(sl => sl.playedDate);

            // keys/indices for 'song'
            builder.Entity<Song>()
                .HasKey(s => s.id)
                .ForSqlServerIsClustered(false);
            builder.Entity<Song>()
                .HasIndex(s => s.id)
                .IsUnique();
            builder.Entity<Song>()
                .HasOne(s => s.Artist)
                .WithMany(a => a.Songs)
                .HasForeignKey(s => s.ArtistId)
                .HasConstraintName("FK_Song_Artist")
                .IsRequired();
            //builder.Entity<Song>()
            //    .HasOne(s => s.Songlog)
            //    .WithOne(l => l.Song);

            // keys/indices for 'artist'
            builder.Entity<Artist>()
                .HasKey(a => a.id).
                ForSqlServerIsClustered(false);
            builder.Entity<Artist>()
                .HasIndex(a => a.id)
                .IsUnique();
            builder.Entity<Artist>()
                .HasMany(a => a.Songs)
                .WithOne(s => s.Artist)
                .HasForeignKey(s => s.ArtistId)
                .IsRequired();
                //.WithMany(a => a.Songs)
                //.HasForeignKey(a => a.ArtistId)
                //.HasConstraintName("FK_Song_Artist");
        }
    }

    public class Artist
    {
        [Column("Created", Order = 2)]
        public DateTime createdDate { get; set; }
        [Column("Modified", Order = 3)]
        public DateTime modifiedDate { get; set; }

        [Column("ArtistId", Order = 4)]
        public Guid id { get; set; }

        [Required]
        [Column("Name", Order = 5)]
        public string name { get; set; }

        [Required]
        public ICollection<Song> Songs { get; set; }

        public static Artist Add(SrfPlayListContext context, Songlog songlog)
        {
            var artist = context.Artists.Local.FirstOrDefault(a => a.id.Equals(songlog.Song.Artist.id));

            if (artist == default(Artist))
                artist = context.Artists.FirstOrDefault(a => a.id.Equals(songlog.Song.Artist.id));

            if (artist != default(Artist))
            {
                artist.modifiedDate = songlog.Song.Artist.modifiedDate;
                artist.name = songlog.Song.Artist.name;
                context.Artists.Update(artist);
                return artist;
            }

            artist = new Artist
            {
                id = songlog.Song.Artist.id,
                createdDate = songlog.Song.Artist.createdDate,
                modifiedDate = songlog.Song.Artist.modifiedDate,
                name = songlog.Song.Artist.name,
            };

            context.Artists.Add(artist);

            return artist;
        }
    }

    public class Song
    {
        [Column("Created", Order = 2)]
        public DateTime createdDate { get; set; }

        [Column("Modified", Order = 3)]
        public DateTime modifiedDate { get; set; }

        [Column("SongId", Order = 4)]
        public Guid id { get; set; }

        [Column(Order = 5)]
        public Guid ArtistId { get; set; }

        [Required]
        [Column("Title", Order = 7)]
        public string title { get; set; }

        public Artist Artist { get; set; }

        public static Song Add(SrfPlayListContext context, Songlog songlog, Artist artist)
        {
            var song = context.Songs.Local.FirstOrDefault(s => s.id.Equals(songlog.Song.id));

            if (song == default(Song))
                song = context.Songs.FirstOrDefault(s => s.id.Equals(songlog.Song.id));

            if (song != default(Song))
            {
                song.modifiedDate = songlog.Song.modifiedDate;
                song.title = songlog.Song.title;
                context.Songs.Update(song);
                return song;
            }

            song = new Song
            {
                createdDate = songlog.Song.createdDate,
                modifiedDate = songlog.Song.modifiedDate,
                id = songlog.Song.id,
                title = songlog.Song.title,
                ArtistId = artist.id,
                Artist = artist
            };

            context.Songs.Add(song);

            return song;
        }
    }

    public class Songlog
    {
        public Guid id { get; set; }
        public Guid channelId { get; set; }
        public DateTime playedDate { get; set; }
        public bool isPlaying { get; set; }
        public Song Song { get; set; }

        public static Songlog Add(SrfPlayListContext context, Songlog songlog, Song song)
        {
            var newSonglog = new Songlog
            {
                id = songlog.id,
                channelId = songlog.channelId,
                isPlaying = songlog.isPlaying,
                playedDate = songlog.playedDate,
                Song = song
            };

            context.Songlogs.Add(songlog);

            return newSonglog;
        }
    }

    public class RootObject
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int totalPages { get; set; }
        public int totalElements { get; set; }
        public ICollection<Songlog> Songlog { get; set; }
    }
}
