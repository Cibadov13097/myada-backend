using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace MyAda.Testing.JwksStub;

/// <summary>Minimal loopback HTTP server that serves <c>/.well-known/jwks.json</c> from the embedded Auth public PEM (no AuthService assembly reference).</summary>
public sealed class LoopbackJwksServer : IAsyncDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public string JwksUrl { get; private set; } = "";

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        JwksUrl = $"http://127.0.0.1:{port}/.well-known/jwks.json";

        var jwksJson = await BuildJwksJsonAsync(cancellationToken).ConfigureAwait(false);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _acceptLoop = Task.Run(() => AcceptLoopAsync(jwksJson, _cts.Token), CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            if (_acceptLoop is not null)
                await _acceptLoop.ConfigureAwait(false);
        }
        catch
        {
            // best-effort shutdown
        }

        _cts?.Dispose();
        _listener = null;
    }

    private async Task AcceptLoopAsync(string jwksJson, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client, jwksJson), CancellationToken.None);
        }
    }

    private static async Task HandleClientAsync(TcpClient client, string jwksJson)
    {
        try
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.Latin1, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(requestLine))
                return;

            var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return;

            var path = parts[1];
            if (!path.StartsWith("/.well-known/jwks.json", StringComparison.Ordinal))
            {
                await WriteAsync(stream, "HTTP/1.1 404 Not Found\r\nConnection: close\r\nContent-Length: 0\r\n\r\n").ConfigureAwait(false);
                return;
            }

            var bodyBytes = Encoding.UTF8.GetBytes(jwksJson);
            var header =
                "HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\nConnection: close\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n\r\n";
            await WriteAsync(stream, header).ConfigureAwait(false);
            await stream.WriteAsync(bodyBytes).ConfigureAwait(false);
        }
        catch
        {
            // ignore per-connection errors
        }
        finally
        {
            client.Close();
        }
    }

    private static async Task WriteAsync(NetworkStream stream, string text)
    {
        var bytes = Encoding.Latin1.GetBytes(text);
        await stream.WriteAsync(bytes).ConfigureAwait(false);
    }

    private static async Task<string> BuildJwksJsonAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var asm = typeof(LoopbackJwksServer).Assembly;
        await using var pemStream = asm.GetManifestResourceStream("MyAda.Testing.JwksStub.AuthKeys.public.pem")
            ?? throw new InvalidOperationException("Missing embedded public.pem for JWKS stub.");

        using var reader = new StreamReader(pemStream, Encoding.UTF8);
        var pem = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(pem);
        var parameters = rsa.ExportParameters(false);

        var jwk = new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid = "my-key-id-1",
            n = Base64UrlEncoder.Encode(parameters.Modulus!),
            e = Base64UrlEncoder.Encode(parameters.Exponent!)
        };

        var doc = new { keys = new[] { jwk } };
        return JsonSerializer.Serialize(doc);
    }
}
