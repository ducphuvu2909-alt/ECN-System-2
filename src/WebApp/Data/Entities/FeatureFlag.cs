namespace WebApp.Data.Entities {
  public class FeatureFlag {
    public int Id {get;set;}
    public bool EmailEnabled {get;set;}
    public bool DesktopToastEnabled {get;set;}
    public bool UseChatGPT {get;set;}
  }
}