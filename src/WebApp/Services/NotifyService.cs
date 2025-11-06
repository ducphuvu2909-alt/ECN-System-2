namespace WebApp.Services {
  public class NotifyService {
    public Task SendEmailAsync(string to, string subject, string body){
      // TODO: hook SMTP here
      Console.WriteLine($"[SMTP] to={to} subject={subject} body={body}");
      return Task.CompletedTask;
    }
  }
}