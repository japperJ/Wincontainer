namespace WinContainers.Core;

public static class WslcCommands
{
    public static string Version() => "--version";

    public static string ContainerPs() => "container ps --all --format json";

    public static string ContainerStart(string id) => $"container start {Quote(id)}";

    public static string ContainerStop(string id) => $"container stop {Quote(id)}";

    public static string ContainerRestart(string id) => $"container restart {Quote(id)}";

    public static string ContainerRemove(string id) => $"container rm {Quote(id)}";

    public static string ContainerKill(string id) => $"container kill {Quote(id)}";

    public static string ContainerInspect(string id) => $"container inspect {Quote(id)}";

    public static string ContainerLogs(string id, int tail = 500) => $"logs --tail {tail} {Quote(id)}";

    public static string ContainerLogsFollow(string id) => $"logs --follow {Quote(id)}";

    public static string ContainerExec(string id) => $"exec --interactive --tty {Quote(id)} sh";
    public static string ContainerExecCommand(string id, string command) => $"container exec {Quote(id)} {command}";
    public static string ContainerExecShell(string id, string shellCommand)
    {
        var escaped = shellCommand.Replace("\"", "\\\"");
        return $"container exec {Quote(id)} sh -c \"{escaped}\"";
    }

    public static string ImageLs() => "image ls --format json";

    public static string ImagePull(string image) => $"image pull {Quote(image)}";

    public static string ImageRemove(string id) => $"image rm --force {Quote(id)}";

    public static string ImageInspect(string id) => $"image inspect {Quote(id)}";

    public static string VolumeLs() => "volume ls --format json";

    public static string VolumeCreate(string name) => $"volume create {Quote(name)}";

    public static string VolumeRemove(string name) => $"volume rm {Quote(name)}";

    public static string VolumeInspect(string name) => $"volume inspect {Quote(name)}";

    public static string NetworkLs() => "network ls --format json";

    public static string NetworkCreate(string name) => $"network create {Quote(name)}";

    public static string NetworkRemove(string name) => $"network rm {Quote(name)}";

    public static string NetworkInspect(string name) => $"network inspect {Quote(name)}";

    public static string Login(string host, string username) => $"login {Quote(host)} --username {Quote(username)} --password-stdin";

    public static string Run(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, string? restart = null)
    {
        var sb = new System.Text.StringBuilder("run --detach");
        if (!string.IsNullOrWhiteSpace(name))
            sb.Append($" --name {Quote(name)}");
        if (!string.IsNullOrWhiteSpace(restart) && restart != "no")
            sb.Append($" --restart {Quote(restart)}");
        if (ports is not null)
            foreach (var p in ports.Where(p => !string.IsNullOrWhiteSpace(p)))
                sb.Append($" --publish {Quote(p)}");
        if (volumes is not null)
            foreach (var v in volumes.Where(v => !string.IsNullOrWhiteSpace(v)))
                sb.Append($" --volume {Quote(v)}");
        if (env is not null)
            foreach (var e in env.Where(e => !string.IsNullOrWhiteSpace(e)))
                sb.Append($" --env {Quote(e)}");
        sb.Append($" {Quote(image)}");
        return sb.ToString();
    }

    private static string Quote(string value) =>
        value.Contains(' ') ? $"\"{value.Replace("\"", "\\\"")}\"" : value;

    private static string Optional(string flag, string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : $" {flag} {Quote(value)}";
}
