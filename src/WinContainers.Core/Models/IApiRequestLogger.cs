namespace WinContainers.Core.Models;

public interface IApiRequestLogger
{
    void LogRequest(string method, string path, string remoteIp, bool isRemote);
}
