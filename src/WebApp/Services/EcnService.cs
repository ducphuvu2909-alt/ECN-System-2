using WebApp.Data;
using WebApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Services {
  public class EcnService {
    private readonly EcnDbContext _db;
    public EcnService(EcnDbContext db){ _db=db; }
    public IQueryable<Ecn> Query() => _db.ECNs.AsNoTracking();
  }
}