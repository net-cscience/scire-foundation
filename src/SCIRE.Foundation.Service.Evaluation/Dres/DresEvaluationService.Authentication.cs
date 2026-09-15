using System.Net;
using System.Text.Json;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.User;
using SCIRE.Foundation.Service.Evaluation.User;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

public sealed partial class DresEvaluationService
{
    private sealed record DresSession(EvaluationUser User, long Revision);

    /// <inheritdoc />
    public async Task<EvaluationUser> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ObjectDisposedException.ThrowIf(this._disposed != 0, this);
        await this._authenticationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            long revision;
            lock (this._statusLock)
            {
                revision = ++this._sessionRevision;
                this._session = null;
                this._status = this._status with { Authentication = EvaluationAuthenticationState.LoggedOut };
            }
            this.PublishStatus();
            var user = await this.SendAsync(HttpMethod.Post, "api/v2/login", new DresLoginRequest(username, password), null, async (response, token) =>
            {
                var dto = await ReadJsonAsync<DresApiUser>(response, token).ConfigureAwait(false);
                ValidateIdentity(dto);
                return dto with { SessionId = string.IsNullOrWhiteSpace(dto.SessionId) ? this.ReadSessionCookie(response) : dto.SessionId };
            }, cancellationToken, password).ConfigureAwait(false);

            var sessionId = user.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = await this.SendAsync(HttpMethod.Get, "api/v2/user/session", null, null, async (response, token) =>
                {
                    var text = (await response.Content.ReadAsStringAsync(token).ConfigureAwait(false)).Trim();
                    var resolved = text.StartsWith('"') ? JsonSerializer.Deserialize<string>(text) : text;
                    if (string.IsNullOrWhiteSpace(resolved) || resolved == "n/a")
                        throw new JsonException("DRES returned no usable session ID.");
                    return resolved;
                }, cancellationToken).ConfigureAwait(false);
            }
            if (string.IsNullOrWhiteSpace(sessionId) || sessionId == "n/a")
                throw new DresApiException(HttpStatusCode.OK, "DRES login returned no usable session ID.", "api/v2/login");
            cancellationToken.ThrowIfCancellationRequested();
            var authenticated = new EvaluationUser(user.Id, user.Username, user.Role, sessionId);
            lock (this._statusLock)
            {
                if (revision != this._sessionRevision || this._disposed != 0)
                    throw new InvalidOperationException("This login was superseded by a local session change.");
                this._session = new DresSession(authenticated, revision);
                this._status = this._status with { Authentication = EvaluationAuthenticationState.Authenticated };
            }
            this.PublishStatus();
            return authenticated;
        }
        finally
        {
            this._authenticationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed != 0, this);
        await this._authenticationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var session = this.GetSession();
            if (session is null)
                return;
            try
            {
                await this.SendJsonAsync<DresSuccessStatus>(HttpMethod.Get, "api/v2/logout", null, session, cancellationToken).ConfigureAwait(false);
            }
            catch (DresApiException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                // An already-rejected session is also logged out from the caller's perspective.
            }
            this.ClearSession();
        }
        finally
        {
            this._authenticationGate.Release();
        }
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        lock (this._statusLock)
        {
            this._sessionRevision++;
            this._session = null;
            this._status = this._status with { Authentication = EvaluationAuthenticationState.LoggedOut };
        }
        this.PublishStatus();
    }

    /// <inheritdoc />
    public Task<EvaluationUser> GetUserAsync(CancellationToken cancellationToken = default) => this.RefreshUserAsync(this.RequireUser(), cancellationToken);

    private async Task<EvaluationUser> RefreshUserAsync(DresSession session, CancellationToken cancellationToken)
    {
        var dto = await this.SendAsync(HttpMethod.Get, "api/v2/user", null, session, async (response, token) =>
        {
            var user = await ReadJsonAsync<DresApiUser>(response, token).ConfigureAwait(false);
            ValidateIdentity(user);
            return user;
        }, cancellationToken).ConfigureAwait(false);
        var updated = new EvaluationUser(dto.Id, dto.Username, dto.Role, session.User.SessionId);
        lock (this._statusLock)
        {
            if (this._sessionRevision == session.Revision && this._session is not null)
            {
                this._session = session with { User = updated };
            }
        }
        return updated;
    }

    private DresSession? GetSession()
    {
        lock (this._statusLock) { return this._session; }
    }

    private static void ValidateIdentity(DresApiUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Role) || user.Role.Equals("ANYONE", StringComparison.OrdinalIgnoreCase))
            throw new JsonException("DRES returned no authenticated user identity.");
    }

    private DresSession RequireUser()
    {
        ObjectDisposedException.ThrowIf(this._disposed != 0, this);
        return this.GetSession() ?? throw new InvalidOperationException("The evaluation service is not authenticated. Call LoginAsync first.");
    }

    private void RejectSession(DresSession session)
    {
        lock (this._statusLock)
        {
            if (this._sessionRevision != session.Revision)
                return;
            this._sessionRevision++;
            this._session = null;
            this._status = this._status with { Authentication = EvaluationAuthenticationState.Rejected };
        }
    }

    private string? ReadSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var headers))
            return null;
        var cookies = new CookieContainer();
        foreach (var header in headers)
        {
            try
            {
                cookies.SetCookies(this._baseUri, header);
            }
            catch (CookieException)
            {
                // Ignore unrelated malformed cookies; the explicit response token remains preferred.
            }
        }
        return cookies.GetAllCookies().Cast<Cookie>().FirstOrDefault(cookie => cookie.Name == "SESSIONID")?.Value;
    }
}
