namespace WebApp.Data.Entities {
  public class Ecn {
    public int Id {get;set;}
    public string EcnNo {get;set;} = "";
    public string? SubEcn {get;set;}
    public string Model {get;set;} = "";
    public string Title {get;set;} = "";
    public string Before {get;set;} = "";
    public string After {get;set;} = "";
    public string? Effective {get;set;}
    public string? ValidBOM {get;set;}
    public string Status {get;set;} = "Pending";
    public string Dept {get;set;} = "";
  }
}