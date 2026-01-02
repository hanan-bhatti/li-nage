using System;
using System.Data.Entity;
using Linage.Core;
using Linage.Core.Authentication;

namespace Linage.Infrastructure
{
    public class LiNageDbContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }
        public DbSet<Commit> Commits { get; set; }
        public DbSet<Snapshot> Snapshots { get; set; }
        public DbSet<FileMetadata> Files { get; set; }
        public DbSet<LineChange> LineChanges { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Remote> Remotes { get; set; }
        public DbSet<AIActivity> AIActivities { get; set; }
        public DbSet<Conflict> Conflicts { get; set; }
        // Credential support via TPH
        public DbSet<Credential> Credentials { get; set; }

        public LiNageDbContext() : base("name=LinageDbContext")
        {
            // Use MigrateDatabaseToLatestVersion for automatic schema updates in development
            // For production, consider using explicit migrations
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<LiNageDbContext, Migrations.Configuration>());
            
            // Ensure database is created on first use
            Database.Initialize(force: false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Project Configuration
            modelBuilder.Entity<Project>()
                .HasKey(p => p.ProjectId);
            
            modelBuilder.Entity<Project>()
                .Property(p => p.ProjectName)
                .IsRequired()
                .HasMaxLength(255);
            
            modelBuilder.Entity<Project>()
                .Property(p => p.RepositoryPath)
                .HasMaxLength(500);

            // Commit Configuration
            modelBuilder.Entity<Commit>()
                .HasKey(c => c.CommitId);
            
            modelBuilder.Entity<Commit>()
                .Property(c => c.CommitHash)
                .IsRequired()
                .HasMaxLength(64); // SHA-256 hex string
            
            modelBuilder.Entity<Commit>()
                .Property(c => c.Message)
                .IsRequired()
                .HasMaxLength(4000);
            
            modelBuilder.Entity<Commit>()
                .Property(c => c.AuthorName)
                .HasMaxLength(255);
            
            modelBuilder.Entity<Commit>()
                .Property(c => c.AuthorEmail)
                .HasMaxLength(255);
            
            // Commit Self-Referencing Many-to-Many for Parents
            modelBuilder.Entity<Commit>()
                .HasMany(c => c.Parents)
                .WithMany()
                .Map(m =>
                {
                    m.ToTable("CommitParents");
                    m.MapLeftKey("CommitId");
                    m.MapRightKey("ParentCommitId");
                });
            
            // Commit -> Snapshot (1:1)
            modelBuilder.Entity<Commit>()
                .HasOptional(c => c.Snapshot)
                .WithMany()
                .Map(m => m.MapKey("SnapshotId"));

            // Snapshot Configuration
            modelBuilder.Entity<Snapshot>()
                .HasKey(s => s.SnapshotId);
            
            modelBuilder.Entity<Snapshot>()
                .Property(s => s.Hash)
                .HasMaxLength(64);

            // Snapshot -> Files (1:N)
            modelBuilder.Entity<Snapshot>()
                .HasMany(s => s.Files)
                .WithMany()
                .Map(m =>
                {
                    m.ToTable("SnapshotFiles");
                    m.MapLeftKey("SnapshotId");
                    m.MapRightKey("FileId");
                });

            // FileMetadata Configuration
            modelBuilder.Entity<FileMetadata>()
                .HasKey(f => f.FileId);
            
            modelBuilder.Entity<FileMetadata>()
                .Property(f => f.FilePath)
                .IsRequired()
                .HasMaxLength(1000);
            
            modelBuilder.Entity<FileMetadata>()
                .Property(f => f.FileHash)
                .HasMaxLength(64);

            // LineChange Configuration
            modelBuilder.Entity<LineChange>()
                .HasKey(l => l.ChangeId);
            
            modelBuilder.Entity<LineChange>()
                .Property(l => l.OldHash)
                .HasMaxLength(64);
            
            modelBuilder.Entity<LineChange>()
                .Property(l => l.NewHash)
                .HasMaxLength(64);
            
            modelBuilder.Entity<LineChange>()
                .Property(l => l.CommitId)
                .IsOptional();

            // Branch Configuration
            modelBuilder.Entity<Branch>()
                .HasKey(b => b.BranchId);
            
            modelBuilder.Entity<Branch>()
                .Property(b => b.BranchName)
                .IsRequired()
                .HasMaxLength(255);
            
            modelBuilder.Entity<Branch>()
                .HasOptional(b => b.HeadCommit)
                .WithMany()
                .Map(m => m.MapKey("HeadCommitId"));

            // Remote Configuration
            modelBuilder.Entity<Remote>()
                .HasKey(r => r.RemoteId);
            
            modelBuilder.Entity<Remote>()
                .Property(r => r.RemoteName)
                .IsRequired()
                .HasMaxLength(255);
            
            modelBuilder.Entity<Remote>()
                .Property(r => r.RemoteUrl)
                .IsRequired()
                .HasMaxLength(1000);

            // AIActivity Configuration
            modelBuilder.Entity<AIActivity>()
                .HasKey(a => a.ActivityId);
            
            modelBuilder.Entity<AIActivity>()
                .Property(a => a.AITool)
                .HasMaxLength(255);
            
            modelBuilder.Entity<AIActivity>()
                .Property(a => a.Description)
                .HasMaxLength(2000);

            // Conflict Configuration
            modelBuilder.Entity<Conflict>()
                .HasKey(c => c.ConflictId);
            
            modelBuilder.Entity<Conflict>()
                .Property(c => c.FilePath)
                .IsRequired()
                .HasMaxLength(1000);

            // Credential (TPH - Table Per Hierarchy)
            modelBuilder.Entity<Credential>()
                .Map<HttpCredential>(m => m.Requires("CredentialType").HasValue("HTTP"))
                .Map<SshCredential>(m => m.Requires("CredentialType").HasValue("SSH"))
                .Map<OAuthCredential>(m => m.Requires("CredentialType").HasValue("OAUTH"));
                
            modelBuilder.Entity<Credential>()
                .HasKey(c => c.CredentialId);
            
            modelBuilder.Entity<Credential>()
                .Property(c => c.RemoteUrl)
                .IsRequired()
                .HasMaxLength(1000);

            // HttpCredential
            modelBuilder.Entity<HttpCredential>()
                .Property(h => h.Username)
                .HasMaxLength(255);
            
            modelBuilder.Entity<HttpCredential>()
                .Property(h => h.Token)
                .HasMaxLength(500);

            // SshCredential
            modelBuilder.Entity<SshCredential>()
                .Property(s => s.Username)
                .HasMaxLength(255);
            
            modelBuilder.Entity<SshCredential>()
                .Property(s => s.PrivateKeyPath)
                .HasMaxLength(500);

            // OAuthCredential
            modelBuilder.Entity<OAuthCredential>()
                .Property(o => o.AccessToken)
                .HasMaxLength(1000);
            
            modelBuilder.Entity<OAuthCredential>()
                .Property(o => o.RefreshToken)
                .HasMaxLength(1000);

            // --- INDEXES FOR PERFORMANCE (Spec 10.1) ---

            // Index on Commit Hash (Non-unique for now to handle potential duplicates in existing DB)
            modelBuilder.Entity<Commit>()
                .HasIndex(c => c.CommitHash)
                .IsUnique(false);

            // Index on File Hash for faster lookups
            modelBuilder.Entity<FileMetadata>()
                .HasIndex(f => f.FileHash)
                .IsUnique(false);

            // Index on Branch Name (Non-unique to handle existing duplicates)
            modelBuilder.Entity<Branch>()
                .HasIndex(b => b.BranchName)
                .IsUnique(false);

            // Index on Remote Name (Non-unique to handle existing duplicates)
            modelBuilder.Entity<Remote>()
                .HasIndex(r => r.RemoteName)
                .IsUnique(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
