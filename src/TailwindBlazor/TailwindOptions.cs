namespace TailwindBlazor;

public class TailwindOptions
{
    public string InputFile { get; set; } = "Styles/app.css";
    public string OutputFile { get; set; } = "wwwroot/css/tailwind.css";
    public string? CliPath { get; set; }
    public string TailwindVersion { get; set; } = "4.1.18";
}
