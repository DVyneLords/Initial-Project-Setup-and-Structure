using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Newtonsoft.Json;

namespace Plan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml - Login and Registration System with JSON Storage
    /// </summary>
    public partial class MainWindow : Window
    {
        // JSON file path for storing user data
        private readonly string userDataFilePath = "users.json";

        // In-memory user storage loaded from JSON
        private Dictionary<string, UserInfo> users;

        public MainWindow()
        {
            InitializeComponent();
            LoadUsersFromJson();

            // Set default focus to email field
            txtLoginEmail.Focus();
        }

        #region JSON Data Management

        /// <summary>
        /// Load users from JSON file or create default admin user
        /// </summary>
        private void LoadUsersFromJson()
        {
            try
            {
                if (File.Exists(userDataFilePath))
                {
                    string jsonContent = File.ReadAllText(userDataFilePath);
                    var usersList = JsonConvert.DeserializeObject<List<UserInfo>>(jsonContent);

                    users = usersList?.ToDictionary(u => u.Email.ToLower(), u => u)
                           ?? new Dictionary<string, UserInfo>();
                }
                else
                {
                    // Create initial admin user if no file exists
                    users = new Dictionary<string, UserInfo>();
                    CreateDefaultAdmin();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error loading user data: {ex.Message}", MessageType.Warning);
                users = new Dictionary<string, UserInfo>();
                CreateDefaultAdmin();
            }
        }

        /// <summary>
        /// Save users to JSON file
        /// </summary>
        private void SaveUsersToJson()
        {
            try
            {
                var usersList = users.Values.ToList();
                string jsonContent = JsonConvert.SerializeObject(usersList, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(userDataFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                ShowMessage($"Error saving user data: {ex.Message}", MessageType.Error);
            }
        }

        /// <summary>
        /// Create default admin user for first-time setup
        /// </summary>
        /// <summary>
        /// Create default admin user and test users for first-time setup
        /// </summary>
        /// <summary>
        /// Create default test users for first-time setup
        /// </summary>
        private void CreateDefaultAdmin()
        {
            // Test manager user
            var managerUser = new UserInfo
            {
                Email = "manager@cmcs.com",
                Password = "manager123",
                FullName = "Academic Manager",
                UserType = "Academic Manager",
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Test lecturer user
            var lecturerUser = new UserInfo
            {
                Email = "lecturer@cmcs.com",
                Password = "lecturer123",
                FullName = "Dr. John Lecturer",
                UserType = "Lecturer",
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Add test users
            users["manager@cmcs.com"] = managerUser;
            users["lecturer@cmcs.com"] = lecturerUser;

            SaveUsersToJson();
        }

        #endregion

        #region Navigation Between Panels

        /// <summary>
        /// Switch to Login Panel
        /// </summary>
        private void BtnSwitchToLogin_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginPanel();
        }

        /// <summary>
        /// Switch to Register Panel
        /// </summary>
        private void BtnSwitchToRegister_Click(object sender, RoutedEventArgs e)
        {
            ShowRegisterPanel();
        }

        /// <summary>
        /// Switch to Reset Password Panel
        /// </summary>
        private void BtnSwitchToReset_Click(object sender, RoutedEventArgs e)
        {
            ShowResetPanel();
        }

        /// <summary>
        /// Hyperlink to switch to Register from Login
        /// </summary>
        private void LinkToRegister_Click(object sender, RoutedEventArgs e)
        {
            ShowRegisterPanel();
        }

        /// <summary>
        /// Hyperlink to switch to Login from Register
        /// </summary>
        private void LinkToLogin_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginPanel();
        }

        /// <summary>
        /// Hyperlink to switch to Reset from Login
        /// </summary>
        private void LinkToReset_Click(object sender, RoutedEventArgs e)
        {
            ShowResetPanel();
        }

        /// <summary>
        /// Hyperlink to switch back to Login from Reset
        /// </summary>
        private void LinkBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginPanel();
        }

        /// <summary>
        /// Show Login Panel and hide other panels
        /// </summary>
        private void ShowLoginPanel()
        {
            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;
            ResetPanel.Visibility = Visibility.Collapsed;

            // Update header button states
            btnSwitchToLogin.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            btnSwitchToRegister.Background = Brushes.Transparent;
            btnSwitchToReset.Background = Brushes.Transparent;

            // Clear any previous messages
            HideMessage();

            // Focus on email field
            txtLoginEmail.Focus();
        }

        /// <summary>
        /// Show Register Panel and hide other panels
        /// </summary>
        private void ShowRegisterPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
            ResetPanel.Visibility = Visibility.Collapsed;

            // Update header button states
            btnSwitchToRegister.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            btnSwitchToLogin.Background = Brushes.Transparent;
            btnSwitchToReset.Background = Brushes.Transparent;

            // Clear any previous messages
            HideMessage();

            // Focus on name field
            txtRegisterName.Focus();
        }

        /// <summary>
        /// Show Reset Password Panel and hide other panels
        /// </summary>
        private void ShowResetPanel()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Collapsed;
            ResetPanel.Visibility = Visibility.Visible;

            // Update header button states
            btnSwitchToReset.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            btnSwitchToLogin.Background = Brushes.Transparent;
            btnSwitchToRegister.Background = Brushes.Transparent;

            // Clear any previous messages
            HideMessage();

            // Focus on email field
            txtResetEmail.Focus();
        }

        #endregion

        #region Login Functionality

        /// <summary>
        /// Handle Login Button Click
        /// </summary>
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input fields
                if (string.IsNullOrWhiteSpace(txtLoginEmail.Text))
                {
                    ShowMessage("Please enter your email address.", MessageType.Error);
                    txtLoginEmail.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLoginPassword.Password))
                {
                    ShowMessage("Please enter your password.", MessageType.Error);
                    txtLoginPassword.Focus();
                    return;
                }

                // Authenticate user with JSON data
                if (AuthenticateUser(txtLoginEmail.Text, txtLoginPassword.Password))
                {
                    ShowMessage("Login successful! Redirecting to dashboard...", MessageType.Success);

                    // Small delay to show success message before navigation
                    Task.Delay(1500).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() => {
                            NavigateToDashboard(users[txtLoginEmail.Text.ToLower()]);
                        });
                    });
                }
                else
                {
                    ShowMessage("Invalid email or password. Please try again.", MessageType.Error);
                    txtLoginPassword.Clear();
                    txtLoginEmail.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Login error: {ex.Message}", MessageType.Error);
            }
        }

        /// <summary>
        /// Authenticate user credentials from JSON data
        /// </summary>
        private bool AuthenticateUser(string email, string password)
        {
            string emailLower = email.ToLower();

            if (users.ContainsKey(emailLower))
            {
                var user = users[emailLower];
                return user.Password == password && user.IsActive;
            }

            return false;
        }

        #endregion

        #region Registration Functionality

        /// <summary>
        /// Handle Register Button Click
        /// </summary>
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate all required fields
                if (string.IsNullOrWhiteSpace(txtRegisterName.Text))
                {
                    ShowMessage("Please enter your full name.", MessageType.Error);
                    txtRegisterName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtRegisterEmail.Text))
                {
                    ShowMessage("Please enter your email address.", MessageType.Error);
                    txtRegisterEmail.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtRegisterPassword.Password))
                {
                    ShowMessage("Please enter a password.", MessageType.Error);
                    txtRegisterPassword.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtConfirmPassword.Password))
                {
                    ShowMessage("Please confirm your password.", MessageType.Error);
                    txtConfirmPassword.Focus();
                    return;
                }

                if (txtRegisterPassword.Password != txtConfirmPassword.Password)
                {
                    ShowMessage("Passwords do not match. Please try again.", MessageType.Error);
                    txtConfirmPassword.Clear();
                    txtConfirmPassword.Focus();
                    return;
                }

                if (cmbUserType.SelectedItem == null)
                {
                    ShowMessage("Please select a user type.", MessageType.Error);
                    cmbUserType.Focus();
                    return;
                }

                // Validate email format
                if (!IsValidEmail(txtRegisterEmail.Text))
                {
                    ShowMessage("Please enter a valid email address.", MessageType.Error);
                    txtRegisterEmail.Focus();
                    return;
                }

                // Validate password strength
                if (!IsValidPassword(txtRegisterPassword.Password))
                {
                    ShowMessage("Password must be at least 6 characters long.", MessageType.Error);
                    txtRegisterPassword.Focus();
                    return;
                }

                // Register user and save to JSON
                if (RegisterUser())
                {
                    ShowMessage("Registration successful! You can now login.", MessageType.Success);

                    // Clear form and switch to login after delay
                    Task.Delay(2000).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() => {
                            ClearRegistrationForm();
                            ShowLoginPanel();
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Registration error: {ex.Message}", MessageType.Error);
            }
        }

        /// <summary>
        /// Register new user and save to JSON
        /// </summary>
        private bool RegisterUser()
        {
            string email = txtRegisterEmail.Text.ToLower();

            // Check if user already exists
            if (users.ContainsKey(email))
            {
                ShowMessage("An account with this email already exists.", MessageType.Error);
                return false;
            }

            // Create new user
            ComboBoxItem selectedUserType = (ComboBoxItem)cmbUserType.SelectedItem;

            var newUser = new UserInfo
            {
                Email = email,
                Password = txtRegisterPassword.Password,
                FullName = txtRegisterName.Text,
                UserType = selectedUserType.Content.ToString(),
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Add to memory and save to JSON
            users[email] = newUser;
            SaveUsersToJson();

            return true;
        }

        /// <summary>
        /// Clear registration form
        /// </summary>
        private void ClearRegistrationForm()
        {
            txtRegisterName.Clear();
            txtRegisterEmail.Clear();
            txtRegisterPassword.Clear();
            txtConfirmPassword.Clear();
            cmbUserType.SelectedIndex = 0;
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
        }

        /// <summary>
        /// Basic email validation
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Password Reset Functionality

        /// <summary>
        /// Handle Reset Password Button Click
        /// </summary>
        private void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input fields
                if (string.IsNullOrWhiteSpace(txtResetEmail.Text))
                {
                    ShowMessage("Please enter your email address.", MessageType.Error);
                    txtResetEmail.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtNewPassword.Password))
                {
                    ShowMessage("Please enter a new password.", MessageType.Error);
                    txtNewPassword.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtConfirmNewPassword.Password))
                {
                    ShowMessage("Please confirm your new password.", MessageType.Error);
                    txtConfirmNewPassword.Focus();
                    return;
                }

                if (txtNewPassword.Password != txtConfirmNewPassword.Password)
                {
                    ShowMessage("Passwords do not match. Please try again.", MessageType.Error);
                    txtConfirmNewPassword.Clear();
                    txtConfirmNewPassword.Focus();
                    return;
                }

                // Validate email format
                if (!IsValidEmail(txtResetEmail.Text))
                {
                    ShowMessage("Please enter a valid email address.", MessageType.Error);
                    txtResetEmail.Focus();
                    return;
                }

                // Validate password strength
                if (!IsValidPassword(txtNewPassword.Password))
                {
                    ShowMessage("Password must be at least 6 characters long.", MessageType.Error);
                    txtNewPassword.Focus();
                    return;
                }

                // Reset password
                if (ResetUserPassword())
                {
                    ShowMessage("Password reset successful! You can now login with your new password.", MessageType.Success);

                    // Clear form and switch to login after delay
                    Task.Delay(2000).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() => {
                            ClearResetForm();
                            ShowLoginPanel();
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Password reset error: {ex.Message}", MessageType.Error);
            }
        }

        /// <summary>
        /// Reset user password and save to JSON
        /// </summary>
        private bool ResetUserPassword()
        {
            string email = txtResetEmail.Text.ToLower();

            // Check if user exists
            if (!users.ContainsKey(email))
            {
                ShowMessage("No account found with this email address.", MessageType.Error);
                return false;
            }

            // Update password
            users[email].Password = txtNewPassword.Password;
            users[email].LastModifiedDate = DateTime.Now;

            // Save to JSON
            SaveUsersToJson();

            return true;
        }

        /// <summary>
        /// Clear reset password form
        /// </summary>
        private void ClearResetForm()
        {
            txtResetEmail.Clear();
            txtNewPassword.Clear();
            txtConfirmNewPassword.Clear();
        }

        #endregion

        #region Dashboard Navigation (Placeholder for Part 2)

        /// <summary>
        /// Navigate to appropriate dashboard based on user type
        /// TODO: Create actual dashboard windows in Part 2
        /// </summary>
        /// <summary>
        /// Navigate to appropriate dashboard based on user type
        /// </summary>
        /// <summary>
        /// Navigate to appropriate dashboard based on user type
        /// </summary>
        private void NavigateToDashboard(UserInfo user)
        {
            try
            {
                switch (user.UserType)
                {
                    case "Lecturer":
                        LecturerDashboard lecturerDashboard = new LecturerDashboard(user);
                        lecturerDashboard.Show();
                        this.Close(); // Close login window
                        break;

                    case "Academic Manager":
                        ManagerDashboard managerDashboard = new ManagerDashboard(user);
                        managerDashboard.Show();
                        this.Close(); // Close login window
                        break;

                    default:
                        ShowMessage("Invalid user type. Please contact administrator.", MessageType.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error opening dashboard: {ex.Message}", MessageType.Error);
            }
        }

        #endregion

        #region Message Display System

        /// <summary>
        /// Show message to user
        /// </summary>
        private void ShowMessage(string message, MessageType type)
        {
            txtMessage.Text = message;

            switch (type)
            {
                case MessageType.Success:
                    MessagePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    break;
                case MessageType.Error:
                    MessagePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    break;
                case MessageType.Warning:
                    MessagePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                    break;
                case MessageType.Info:
                    MessagePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    break;
            }

            MessagePanel.Visibility = Visibility.Visible;

            // Auto-hide message after 5 seconds for success messages
            if (type == MessageType.Success)
            {
                Task.Delay(5000).ContinueWith(t =>
                {
                    Dispatcher.Invoke(() => HideMessage());
                });
            }
        }

        /// <summary>
        /// Hide message panel
        /// </summary>
        private void HideMessage()
        {
            MessagePanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Window Event Handlers

        /// <summary>
        /// Handle Enter key press for all panels
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (LoginPanel.Visibility == Visibility.Visible)
                {
                    BtnLogin_Click(this, new RoutedEventArgs());
                }
                else if (RegisterPanel.Visibility == Visibility.Visible)
                {
                    BtnRegister_Click(this, new RoutedEventArgs());
                }
                else if (ResetPanel.Visibility == Visibility.Visible)
                {
                    BtnResetPassword_Click(this, new RoutedEventArgs());
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle window closing to ensure data is saved
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveUsersToJson();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving user data: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            base.OnClosing(e);
        }

        #endregion
    }

    #region Helper Classes and Enums

    /// <summary>
    /// User information model for JSON serialization
    /// </summary>
    public class UserInfo
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string UserType { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Additional properties for future use
        public string PhoneNumber { get; set; }
        public string Department { get; set; }
        public DateTime LastLoginDate { get; set; }
        public int LoginAttempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
    }

    /// <summary>
    /// Message types for user feedback
    /// </summary>
    public enum MessageType
    {
        Success,
        Error,
        Warning,
        Info
    }

    #endregion
}