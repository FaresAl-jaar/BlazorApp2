using Microsoft.AspNetCore.SignalR;

namespace BlazorApp2.Hubs;

public class DocumentHub : Hub
{
    public async Task NotifyDocumentReceived(int documentId, string fileName)
    {
        await Clients.All.SendAsync("DocumentReceived", documentId, fileName);
    }

    public async Task NotifyDocumentStatusChanged(int documentId, string newStatus)
    {
        await Clients.All.SendAsync("DocumentStatusChanged", documentId, newStatus);
    }

    public async Task NotifyDocumentDeleted(int documentId)
    {
        await Clients.All.SendAsync("DocumentDeleted", documentId);
    }
}
