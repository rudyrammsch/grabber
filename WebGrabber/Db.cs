using Microsoft.EntityFrameworkCore;
using System;

namespace WebGrabber
{
    public abstract class Db<T> where T : DbContext
    {
        protected Db()
        {
            OptionsBuilder = new DbContextOptionsBuilder<T>();
        }

        protected DbContextOptionsBuilder OptionsBuilder;

        public abstract bool CreateDb();

        public abstract void Import(Guid channelId = default(Guid));

        public abstract bool Insert(string json);
    }
}
