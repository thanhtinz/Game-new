using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using WorldFaith.Shared.Contracts.Auth;

namespace WorldFaith.Client.Managers
{
    /// <summary>
    /// Quản lý authentication: register, login, refresh token.
    /// Lưu tokens trong PlayerPrefs (mobile-safe).
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [Header("Server Config")]
        [SerializeField] private string serverUrl = "http://localhost:5000";

        private const string KeyAccessToken = "wf_access_token";
        private const string KeyRefreshToken = "wf_refresh_token";
        private const string KeyExpiresAt = "wf_expires_at";
        private const string KeyPlayerId = "wf_player_id";
        private const string KeyDisplayName = "wf_display_name";

        public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);
        public string AccessToken => PlayerPrefs.GetString(KeyAccessToken, "");
        public string RefreshToken => PlayerPrefs.GetString(KeyRefreshToken, "");
        public string PlayerId => PlayerPrefs.GetString(KeyPlayerId, "");
        public string DisplayName => PlayerPrefs.GetString(KeyDisplayName, "");

        public PlayerProfileDto CurrentPlayer { get; private set; }

        public event Action<PlayerProfileDto> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLoggedOut;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            // Auto-login nếu có refresh token hợp lệ
            if (!string.IsNullOrEmpty(RefreshToken))
                await TryAutoRefreshAsync();
        }

        // ─── Public Methods ──────────────────────────────────────

        public async Task<bool> RegisterAsync(string username, string email, string password, string displayName)
        {
            var request = new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = password,
                DisplayName = displayName
            };

            var response = await PostAsync<RegisterRequest, AuthResponse>("api/auth/register", request);
            if (response == null) { OnLoginFailed?.Invoke("Lỗi kết nối server"); return false; }

            if (!response.Success) { OnLoginFailed?.Invoke(response.Error ?? "Đăng ký thất bại"); return false; }

            SaveTokens(response);
            OnLoginSuccess?.Invoke(response.Player!);
            return true;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await PostAsync<LoginRequest, AuthResponse>("api/auth/login", request);

            if (response == null) { OnLoginFailed?.Invoke("Lỗi kết nối server"); return false; }
            if (!response.Success) { OnLoginFailed?.Invoke(response.Error ?? "Đăng nhập thất bại"); return false; }

            SaveTokens(response);
            OnLoginSuccess?.Invoke(response.Player!);
            return true;
        }

        public async Task<bool> TryAutoRefreshAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken)) return false;

            var request = new RefreshTokenRequest { RefreshToken = RefreshToken };
            var response = await PostAsync<RefreshTokenRequest, AuthResponse>("api/auth/refresh", request);

            if (response == null || !response.Success)
            {
                ClearTokens();
                return false;
            }

            SaveTokens(response);
            return true;
        }

        public async Task LogoutAsync()
        {
            if (!string.IsNullOrEmpty(RefreshToken))
            {
                var request = new RefreshTokenRequest { RefreshToken = RefreshToken };
                await PostAsync<RefreshTokenRequest, object>("api/auth/logout", request, authenticated: true);
            }

            ClearTokens();
            OnLoggedOut?.Invoke();
        }

        // ─── Token Management ────────────────────────────────────

        private void SaveTokens(AuthResponse response)
        {
            PlayerPrefs.SetString(KeyAccessToken, response.AccessToken ?? "");
            PlayerPrefs.SetString(KeyRefreshToken, response.RefreshToken ?? "");
            PlayerPrefs.SetString(KeyExpiresAt, response.ExpiresAt.ToString());
            PlayerPrefs.SetString(KeyPlayerId, response.Player?.Id ?? "");
            PlayerPrefs.SetString(KeyDisplayName, response.Player?.DisplayName ?? "");
            PlayerPrefs.Save();

            CurrentPlayer = response.Player!;
        }

        private void ClearTokens()
        {
            PlayerPrefs.DeleteKey(KeyAccessToken);
            PlayerPrefs.DeleteKey(KeyRefreshToken);
            PlayerPrefs.DeleteKey(KeyExpiresAt);
            PlayerPrefs.DeleteKey(KeyPlayerId);
            PlayerPrefs.DeleteKey(KeyDisplayName);
            PlayerPrefs.Save();
            CurrentPlayer = null;
        }

        // ─── HTTP Helpers ────────────────────────────────────────

        private async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint, TRequest body, bool authenticated = false)
            where TResponse : class
        {
            var url = $"{serverUrl}/{endpoint}";
            var json = JsonUtility.ToJson(body);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (authenticated && !string.IsNullOrEmpty(AccessToken))
                request.SetRequestHeader("Authorization", $"Bearer {AccessToken}");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AuthManager] HTTP {request.responseCode}: {request.error}");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Parse error: {ex.Message}");
                return null;
            }
        }
    }
}
