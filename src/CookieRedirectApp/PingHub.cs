using Microsoft.AspNetCore.SignalR;

namespace CookieRedirectApp;

/// <summary>
/// A hub with one method, present only so the SignalR endpoint shape can be
/// measured alongside the others.
/// </summary>
public sealed class PingHub : Hub
{
    public string Ping() => "pong";
}
