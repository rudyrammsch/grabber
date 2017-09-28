using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WebGrabber
{
    public class DbContextBase : DbContext
    {
        public DbContextOptions Options { get; }

        public static IConfigurationRoot Configuration { get; set; }

        public DbContextBase() { }
        public DbContextBase(DbContextOptions options)
        {
            Options = options;
        }

        public DbSet<MetaInfo> MetaInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // keys/indices for 'metainfo'
            builder.Entity<MetaInfo>()
                .HasKey(s => s.ChannelId)
                .ForSqlServerIsClustered(false);
        }
    }

    public class MetaInfo
    {
        public Guid ChannelId { get; set; }
        public string ChannelName { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
