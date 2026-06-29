using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WorldFaith.Client.Managers;

namespace WorldFaith.Client.UI.Lobby
{
    /// <summary>
    /// UI đăng nhập / đăng ký.
    /// Gắn vào Canvas -> LoginPanel.
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button loginTabBtn;
        [SerializeField] private Button registerTabBtn;

        [Header("Login Panel")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginBtn;
        [SerializeField] private TextMeshProUGUI loginErrorText;

        [Header("Register Panel")]
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private TMP_InputField regUsernameInput;
        [SerializeField] private TMP_InputField regEmailInput;
        [SerializeField] private TMP_InputField regPasswordInput;
        [SerializeField] private TMP_InputField regDisplayNameInput;
        [SerializeField] private Button registerBtn;
        [SerializeField] private TextMeshProUGUI registerErrorText;

        [Header("Loading")]
        [SerializeField] private GameObject loadingOverlay;

        private void Start()
        {
            loginTabBtn?.onClick.AddListener(ShowLoginPanel);
            registerTabBtn?.onClick.AddListener(ShowRegisterPanel);
            loginBtn?.onClick.AddListener(OnLoginClick);
            registerBtn?.onClick.AddListener(OnRegisterClick);

            ShowLoginPanel();

            // Nếu đã login thì chuyển thẳng vào lobby
            if (AuthManager.Instance?.IsLoggedIn == true)
                LoadLobbyScene();
        }

        private void ShowLoginPanel()
        {
            loginPanel?.SetActive(true);
            registerPanel?.SetActive(false);
            loginErrorText.text = "";
        }

        private void ShowRegisterPanel()
        {
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(true);
            registerErrorText.text = "";
        }

        private async void OnLoginClick()
        {
            var email = loginEmailInput?.text?.Trim() ?? "";
            var password = loginPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetLoginError("Vui lòng điền đầy đủ thông tin");
                return;
            }

            SetLoading(true);
            var success = await AuthManager.Instance.LoginAsync(email, password);
            SetLoading(false);

            if (success)
                LoadLobbyScene();
            else
                SetLoginError(AuthManager.Instance == null ? "Lỗi kết nối" : "Email hoặc mật khẩu không đúng");
        }

        private async void OnRegisterClick()
        {
            var username = regUsernameInput?.text?.Trim() ?? "";
            var email = regEmailInput?.text?.Trim() ?? "";
            var password = regPasswordInput?.text ?? "";
            var displayName = regDisplayNameInput?.text?.Trim() ?? username;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetRegisterError("Vui lòng điền đầy đủ thông tin");
                return;
            }

            SetLoading(true);
            var success = await AuthManager.Instance.RegisterAsync(username, email, password, displayName);
            SetLoading(false);

            if (success)
                LoadLobbyScene();
            else
                SetRegisterError("Đăng ký thất bại, thử username/email khác");
        }

        private void LoadLobbyScene() => SceneManager.LoadScene("LobbyScene");
        private void SetLoginError(string msg) { if (loginErrorText) loginErrorText.text = msg; }
        private void SetRegisterError(string msg) { if (registerErrorText) registerErrorText.text = msg; }
        private void SetLoading(bool active) { loadingOverlay?.SetActive(active); }
    }
}
