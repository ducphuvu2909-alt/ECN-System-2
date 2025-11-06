using WebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace WebApp.Services {
  public class AiAdvisorService {
    private readonly EcnDbContext _db;
    public AiAdvisorService(EcnDbContext db){ _db=db; }
    public async Task<string> AskAsync(string q){
      string Grab(string pat){
        var m = Regex.Match(q, pat, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.ToUpper() : "";
      }
      var ecn = Grab(@"(ECN[-\s]?\d{3,})");
      var model = Grab(@"(MDL[-\s]?[\dA-Za-z-]+)");
      var ba = Regex.Match(q, @"([A-Za-z0-9]{2,12})\s*(?:->|→|to|sang|thanh)\s*([A-Za-z0-9]{2,12})");
      var before = ba.Success ? ba.Groups[1].Value.ToUpper() : "";
      var after  = ba.Success ? ba.Groups[2].Value.ToUpper() : "";
      var query = _db.ECNs.AsNoTracking().AsQueryable();
      if(!string.IsNullOrEmpty(ecn)) query = query.Where(x=>x.EcnNo.Replace(" ","").ToUpper()==ecn.Replace(" ","").ToUpper());
      if(!string.IsNullOrEmpty(model)) query = query.Where(x=>x.Model.ToUpper()==model);
      if(!string.IsNullOrEmpty(before)) query = query.Where(x=>x.Before.ToUpper()==before);
      if(!string.IsNullOrEmpty(after)) query = query.Where(x=>x.After.ToUpper()==after);
      var list = await query.Take(20).ToListAsync();
      if(list.Count==0){
        var qn = q.ToLower();
        list = await _db.ECNs.AsNoTracking()
          .Where(x=>x.EcnNo.ToLower().Contains(qn) || x.Model.ToLower().Contains(qn) || x.Before.ToLower().Contains(qn) || x.After.ToLower().Contains(qn))
          .Take(20).ToListAsync();
      }
      if(list.Count==0) return "ECN AI Advisor: Không tìm thấy dữ liệu khớp.";
      return "ECN AI Advisor — kết quả khớp:\n  - " + string.Join("\n  - ", list.Select(x=>$"{x.EcnNo} • {x.Model} • {x.Before}->{x.After} • ValidBOM={(x.ValidBOM??"N/A")} • {x.Status} • {x.Dept}"));
    }
  }
}