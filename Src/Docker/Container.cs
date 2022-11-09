using System.Text;

namespace Medoz.Pmet.Docker;

public class Container
{
    public Container()
    {

    }

    public string Id { set; get; } = string.Empty;
    public string ImageId { set; get; } = string.Empty;
    public string Command { set; get; } = string.Empty;
    public string CreatedAt { set; get; } = string.Empty;
    public string RunningFor { set; get; } = string.Empty;
    public string Ports { set; get; } = string.Empty;
    public string Status { set; get; } = string.Empty;
    public string Size { set; get; } = string.Empty;
    public string Names { set; get; } = string.Empty;
    public string Label { set; get; } = string.Empty;
    public string Labels { set; get; } = string.Empty;
    public string Mounts { set; get; } = string.Empty;
    public string Networks { set; get; } = string.Empty;

    public static async Task<IEnumerable<Container>> ProcessList(bool isAll = false)
    {
        List<Container> containers = new();

        StringBuilder commandBuilder = new();
        commandBuilder.Append("docker ps ");
        if (isAll)
        {
            commandBuilder.Append("-a ");
        }
        commandBuilder.Append("--format ");
        commandBuilder.Append("\"");
        commandBuilder.Append("{{.ID}}\\t");
        commandBuilder.Append("{{.Image}}\\t");
        commandBuilder.Append("{{.Command}}\\t");
        commandBuilder.Append("{{.CreatedAt}}\\t");
        commandBuilder.Append("{{.RunningFor}}\\t");
        commandBuilder.Append("{{.Ports}}\\t");
        commandBuilder.Append("{{.Status}}\\t");
        commandBuilder.Append("{{.Size}}\\t");
        commandBuilder.Append("{{.Names}}\\t");
        commandBuilder.Append("{{.Label}}\\t");
        commandBuilder.Append("{{.Labels}}\\t");
        commandBuilder.Append("{{.Mounts}}\\t");
        commandBuilder.Append("{{.Networks}}");
        commandBuilder.Append("\"");

        await foreach(string item in ProcessExecutor.ExecuteAsync(commandBuilder.ToString()))
        {
        }
        return containers;
    }

    public static void Start()
    {

    }
}
