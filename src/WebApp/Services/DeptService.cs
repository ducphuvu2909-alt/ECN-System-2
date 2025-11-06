namespace WebApp.Services {
  public class DeptService {
    public bool CanAccess(string userDept, string targetDept, bool isAdmin) {
      if(isAdmin) return true;
      return string.Equals(userDept, targetDept, System.StringComparison.OrdinalIgnoreCase);
    }
  }
}