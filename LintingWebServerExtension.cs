using System.ComponentModel.Composition;
using System.Net;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.WebServer;

namespace com.cinaq.MendixCLI.MendixExtension;

[Export(typeof(WebServerExtension))]
public class LintingWebServerExtension : WebServerExtension
{
    private readonly IExtensionFileService _extensionFileService;
    private readonly ILogService _logService;

    [ImportingConstructor]
    public LintingWebServerExtension(IExtensionFileService extensionFileService, ILogService logService)
    {
        _extensionFileService = extensionFileService;
        _logService = logService;
    }

    public override void InitializeWebServer(IWebServer webServer)
    {
        var wwwrootPath = _extensionFileService.ResolvePath("wwwroot");
        var files = Directory.GetFiles(wwwrootPath);

        foreach (var file in files)
        {
            var route = Path.GetFileName(file);
            webServer.AddRoute(route, (request, response, ct) => ServeFile(file, response, ct));
        }

        webServer.AddRoute("api", ServeAPI);
    }

    private async Task ServeFile(string filePath, HttpListenerResponse response, CancellationToken ct)
    {
        var mimeType = GetMimeType(filePath);
        await response.SendFileAndClose(mimeType, filePath, ct);
    }

    private string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension switch
        {
            ".html" => "text/html",
            ".js" => "text/javascript",
            ".css" => "text/css",
            _ => "application/octet-stream"
        };
    }

    private async Task ServeAPI(HttpListenerRequest request, HttpListenerResponse response, CancellationToken ct)
    {
        if (CurrentApp == null)
        {
            response.SendNoBodyAndClose(404);
            return;
        }

        // var toDoList = new ToDoStorage(CurrentApp, _logService).LoadToDoList();
        var toDoList = new List<string>();
        var jsonStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(jsonStream, toDoList, cancellationToken: ct);

        response.SendJsonAndClose(jsonStream);
    }
}