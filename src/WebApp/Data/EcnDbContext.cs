using Microsoft.EntityFrameworkCore;
using WebApp.Data.Entities;

namespace WebApp.Data {
  public class EcnDbContext : DbContext {
    public EcnDbContext(DbContextOptions<EcnDbContext> options): base(options) {}
    public DbSet<User> Users => Set<User>();
    public DbSet<Ecn> ECNs => Set<Ecn>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
  }

  public static class Seed {
    public static void Run(EcnDbContext db){
      if(!db.Users.Any()){
        db.Users.AddRange(
          new User{ Id="U004", Name="Bao Pham", Email="bao.pham@company.com", Dept="QC", Role="Admin", PasswordHash=BCrypt.Net.BCrypt.HashPassword("bao") },
          new User{ Id="U001", Name="Minh Nguyen", Email="minh.nguyen@company.com", Dept="SMT", Role="Editor", PasswordHash=BCrypt.Net.BCrypt.HashPassword("minh") },
          new User{ Id="U002", Name="Lan Tran", Email="lan.tran@company.com", Dept="PE", Role="Approver", PasswordHash=BCrypt.Net.BCrypt.HashPassword("lan") },
          new User{ Id="U003", Name="Quang Le", Email="quang.le@company.com", Dept="FE", Role="Viewer", PasswordHash=BCrypt.Net.BCrypt.HashPassword("quang") }
        );
        db.ECNs.AddRange(
          new Ecn{ EcnNo="ECN-101", SubEcn="A", Model="MDL-220", Title="Spec tighten", Before="ZA", After="ZB", Effective="2025-11-01", ValidBOM="BOM-OK", Status="InProgress", Dept="SMT" },
          new Ecn{ EcnNo="ECN-202", SubEcn="B", Model="MDL-500", Title="Connector swap", Before="XC", After="XD", Effective="2025-11-15", ValidBOM=null, Status="Pending", Dept="PE" }
        );
        db.SaveChanges();
      }
    }
  }
}