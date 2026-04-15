namespace CallService.Api.Realtime;

public sealed class CallRuntimeOptions
{
    public int InviteTimeoutSeconds { get; set; } = 30;
    public int StateTtlHours { get; set; } = 24;
    public string DispatcherInboxPrefix { get; set; } = "dispatcher-inbox-";
}
