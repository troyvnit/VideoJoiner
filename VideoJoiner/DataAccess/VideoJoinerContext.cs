namespace VideoJoiner.DataAccess
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class VideoJoinerContext : DbContext
    {
        public VideoJoinerContext()
            : base("name=VideoJoinerContext")
        {
        }

        public virtual DbSet<Video> Videos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}
