using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using static Plan.DataManager;
using IOPath = System.IO.Path;

namespace Plan
{
    public partial class LecturerDashboard : Window
    {
        private UserInfo currentUser;
        private ObservableCollection<LecturerClaimInfo> myClaims;
        private ObservableCollection<LecturerClaimInfo> filteredClaims;
        private List<string> attachedDocuments;
        private List<UserInfo> academicManagers;
        private List<NotificationData> userNotifications;
        private List<CheckBox> notificationCheckBoxes; // Track checkboxes for bulk operations

        public LecturerDashboard()
        {
            InitializeComponent();
            InitializeDashboard();
        }

        public LecturerDashboard(UserInfo user)
        {
            InitializeComponent();
            currentUser = user;
            lblWelcomeMessage.Text = $"Welcome, {user.FullName}";
            InitializeDashboard();
        }

        private void InitializeDashboard()
        {
            attachedDocuments = new List<string>();
            notificationCheckBoxes = new List<CheckBox>();
            LoadAcademicManagers();
            LoadUserClaims();
            LoadUserProfile();
            LoadUserNotifications();
            RefreshDashboardStats();
            UpdateStatus("Dashboard loaded successfully");

            dgMyClaims.ItemsSource = filteredClaims;
        }

        #region Notifications Management

        private void LoadUserNotifications()
        {
            try
            {
                if (currentUser == null) return;

                userNotifications = DataManager.GetNotificationsByUser(currentUser.Email);
                UpdateNotificationDisplay();
                UpdateNotificationCount();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading notifications: {ex.Message}");
            }
        }

        private void UpdateNotificationCount()
        {
            var unreadCount = DataManager.GetUnreadNotificationCount(currentUser?.Email ?? "");
            lblNotificationCount.Text = unreadCount > 0 ? unreadCount.ToString() : "0";

            // Change button appearance if there are unread notifications
            if (unreadCount > 0)
            {
                btnNotifications.Background = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                btnNotifications.ClearValue(Button.BackgroundProperty);
            }
        }

        private void UpdateNotificationDisplay()
        {
            spNotifications.Children.Clear();
            notificationCheckBoxes.Clear();

            if (userNotifications == null || !userNotifications.Any())
            {
                var noNotifications = new TextBlock
                {
                    Text = "No notifications",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                spNotifications.Children.Add(noNotifications);
                return;
            }

            foreach (var notification in userNotifications)
            {
                var notificationCard = CreateNotificationCard(notification);
                spNotifications.Children.Add(notificationCard);
            }
        }

        private Border CreateNotificationCard(NotificationData notification)
        {
            var card = new Border
            {
                Background = notification.IsRead ?
                    new SolidColorBrush(Color.FromRgb(30, 41, 59)) :
                    new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(16),
                BorderBrush = notification.Type == "Rejection" ?
                    new SolidColorBrush(Colors.Red) :
                    new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1, 1, 1, notification.Type == "Rejection" ? 3 : 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Create notification checkbox
            var notificationCheckBox = new CheckBox
            {
                Tag = notification.NotificationId,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            notificationCheckBox.Checked += NotificationCheckBox_Changed;
            notificationCheckBox.Unchecked += NotificationCheckBox_Changed;
            notificationCheckBoxes.Add(notificationCheckBox);

            var contentStack = new StackPanel();

            // Title
            var titleBlock = new TextBlock
            {
                Text = notification.Title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Message
            var messageBlock = new TextBlock
            {
                Text = notification.Message,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Date and Claim ID
            var infoStack = new StackPanel { Orientation = Orientation.Horizontal };

            var dateBlock = new TextBlock
            {
                Text = notification.CreatedDate.ToString("MMM dd, yyyy HH:mm"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 16, 0)
            };

            if (!string.IsNullOrEmpty(notification.RelatedClaimId))
            {
                var claimBlock = new TextBlock
                {
                    Text = $"Claim: {notification.RelatedClaimId}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                };
                infoStack.Children.Add(claimBlock);
            }

            infoStack.Children.Add(dateBlock);

            contentStack.Children.Add(titleBlock);
            contentStack.Children.Add(messageBlock);
            contentStack.Children.Add(infoStack);

            // Create main content area with checkbox
            var mainContentGrid = new Grid();
            mainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(notificationCheckBox, 0);
            Grid.SetColumn(contentStack, 1);

            mainContentGrid.Children.Add(notificationCheckBox);
            mainContentGrid.Children.Add(contentStack);

            // Mark as read button and status
            var actionStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };

            if (!notification.IsRead)
            {
                var markReadButton = new Button
                {
                    Content = "Mark Read",
                    Tag = notification.NotificationId,
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 0, 8),
                    Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    FontSize = 11
                };
                markReadButton.Click += BtnMarkRead_Click;
                actionStack.Children.Add(markReadButton);

                // Unread indicator
                var unreadDot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Colors.Orange),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                actionStack.Children.Add(unreadDot);
            }

            Grid.SetColumn(mainContentGrid, 0);
            Grid.SetColumn(actionStack, 1);

            grid.Children.Add(mainContentGrid);
            grid.Children.Add(actionStack);

            card.Child = grid;
            return card;
        }

        // Missing event handlers that were causing the errors:

        private void ChkSelectAllNotifications_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var checkBox in notificationCheckBoxes)
                {
                    checkBox.IsChecked = true;
                }
                UpdateDeleteButtonState();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error selecting all notifications: {ex.Message}");
            }
        }

        private void ChkSelectAllNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var checkBox in notificationCheckBoxes)
                {
                    checkBox.IsChecked = false;
                }
                UpdateDeleteButtonState();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error unselecting notifications: {ex.Message}");
            }
        }

        private void BtnDeleteSelectedNotifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedNotificationIds = notificationCheckBoxes
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Tag.ToString())
                    .ToList();

                if (!selectedNotificationIds.Any())
                {
                    MessageBox.Show("Please select notifications to delete.",
                                  "No Selection",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete {selectedNotificationIds.Count} selected notification(s)?\n\nThis action cannot be undone.",
                                           "Confirm Delete",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Delete notifications using the DataManager method
                    DataManager.DeleteNotifications(selectedNotificationIds);

                    // Refresh the display
                    LoadUserNotifications();
                    UpdateNotificationCount();

                    UpdateStatus($"{selectedNotificationIds.Count} notification(s) deleted successfully");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting notifications: {ex.Message}",
                              "Delete Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void NotificationCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDeleteButtonState();
        }

        private void UpdateDeleteButtonState()
        {
            var hasSelection = notificationCheckBoxes.Any(cb => cb.IsChecked == true);
            btnDeleteSelectedNotifications.IsEnabled = hasSelection;
        }

        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            var tabControl = this.FindVisualChildren<TabControl>().FirstOrDefault();
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 2; // Notifications tab
            }
            LoadUserNotifications();
            UpdateStatus("Notifications loaded");
        }

        private void BtnMarkRead_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string notificationId)
            {
                DataManager.MarkNotificationAsRead(notificationId);
                LoadUserNotifications(); // Refresh display
                UpdateNotificationCount();
                UpdateStatus("Notification marked as read");
            }
        }

        private void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var notification in userNotifications.Where(n => !n.IsRead))
                {
                    DataManager.MarkNotificationAsRead(notification.NotificationId);
                }

                LoadUserNotifications();
                UpdateNotificationCount();
                UpdateStatus("All notifications marked as read");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error marking notifications as read: {ex.Message}");
            }
        }

        #endregion

        #region Data Loading and Management

        private void LoadAcademicManagers()
        {
            try
            {
                academicManagers = DataManager.GetAcademicManagers();
                cmbAssignedManager.ItemsSource = academicManagers;

                if (academicManagers.Count == 0)
                {
                    MessageBox.Show("No Academic Managers found in the system. Please contact administrator.",
                                  "System Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading academic managers: {ex.Message}",
                              "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserClaims()
        {
            // Switch back to the Claims tab to show the new claim
            var tabControl = this.FindVisualChildren<TabControl>().FirstOrDefault();
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 0; // Claims Overview tab
            }
            try
            {
                if (currentUser == null) return;

                var userClaims = DataManager.GetClaimsByLecturer(currentUser.Email);
                myClaims = new ObservableCollection<LecturerClaimInfo>();

                foreach (var claim in userClaims)
                {
                    myClaims.Add(new LecturerClaimInfo
                    {
                        ClaimId = claim.ClaimId,
                        SubmitDate = claim.SubmitDate,
                        StartDate = claim.StartDate,
                        EndDate = claim.EndDate,
                        Hours = claim.Hours,
                        HourlyRate = claim.HourlyRate,
                        TotalAmount = claim.TotalAmount,
                        Status = claim.Status,
                        LastUpdated = claim.LastUpdated,
                        Description = claim.Description,
                        AttachedDocuments = claim.AttachedDocuments,
                        CanEdit = claim.Status == "Draft",
                        AssignedManagerName = claim.AssignedManagerName
                    });
                }

                // Initialize filtered claims collection
                filteredClaims = new ObservableCollection<LecturerClaimInfo>();

                // Load all claims initially
                foreach (var claim in myClaims)
                {
                    filteredClaims.Add(claim);
                }

                // Set default filter if ComboBox exists
                if (cmbClaimFilter != null && cmbClaimFilter.SelectedIndex < 0)
                {
                    cmbClaimFilter.SelectedIndex = 0; // "All Claims"
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {myClaims.Count} claims for lecturer");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading claims: {ex.Message}",
                              "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                myClaims = new ObservableCollection<LecturerClaimInfo>();
                filteredClaims = new ObservableCollection<LecturerClaimInfo>();
            }
        }

        // REPLACE the existing BtnDeleteClaim_Click method in LecturerDashboard.cs with this:

        private void BtnDeleteClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = myClaims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim != null && claim.CanEdit)
                {
                    var result = MessageBox.Show($"Are you sure you want to delete claim {claimId}?\n\nThis action cannot be undone and will also delete all attached documents.",
                                               "Confirm Delete",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Delete associated files first
                            FileStorageManager.DeleteClaimFiles(claimId);

                            // Then delete the claim
                            DataManager.DeleteClaim(claimId);

                            // Remove from local collections
                            myClaims.Remove(claim);
                            if (filteredClaims.Contains(claim))
                            {
                                filteredClaims.Remove(claim);
                            }

                            // Refresh UI
                            RefreshDashboardStats();
                            UpdateStatus($"Claim {claimId} and all associated documents deleted successfully");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting claim: {ex.Message}",
                                          "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Only draft claims can be deleted.",
                                  "Cannot Delete Claim",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
        }

        private void LoadUserProfile()
        {
            if (currentUser == null) return;

            txtFullName.Text = currentUser.FullName;
            txtEmail.Text = currentUser.Email;
            txtPhone.Text = currentUser.PhoneNumber ?? "";
            txtEmployeeId.Text = "EMP" + currentUser.Email.GetHashCode().ToString("X").Substring(0, 6);
            txtDepartment.Text = currentUser.Department ?? "Computer Science";
            txtDefaultRate.Text = "450.00";
            txtBankName.Text = "First National Bank";
            txtAccountNumber.Text = "****7890";
            txtBranchCode.Text = "250655";
        }

        private void RefreshDashboardStats()
        {
            var totalClaims = myClaims.Count;
            var pendingClaims = myClaims.Count(c => c.Status == "Pending Review" || c.Status == "Draft");
            var approvedClaims = myClaims.Count(c => c.Status == "Approved");
            var totalEarnings = myClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount);

            lblTotalClaims.Text = totalClaims.ToString();
            lblPendingClaims.Text = pendingClaims.ToString();
            lblApprovedClaims.Text = approvedClaims.ToString();
            lblTotalEarnings.Text = $"R {totalEarnings:N0}";
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = message;
            lblLastUpdate.Text = $"Last updated: {DateTime.Now:HH:mm}";
        }

        #endregion

        #region Header Actions

        private void BtnNewClaim_Click(object sender, RoutedEventArgs e)
        {
            var tabControl = this.FindVisualChildren<TabControl>().FirstOrDefault();
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 1;
            }

            ClearClaimForm();
            UpdateStatus("Ready to submit new claim");
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            var tabControl = this.FindVisualChildren<TabControl>().FirstOrDefault();
            if (tabControl != null)
            {
                tabControl.SelectedIndex = 3; // Profile tab
            }
            UpdateStatus("Profile information loaded");
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?",
                                       "Confirm Logout",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        #endregion

        #region Claims Management

        // Event handlers for filter and search
        private void CmbClaimFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && myClaims != null && filteredClaims != null)
            {
                ApplyClaimFilters();
            }
        }

        private void TxtClaimSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && (txtClaimSearch.Text.Length > 2 || string.IsNullOrWhiteSpace(txtClaimSearch.Text)))
            {
                ApplyClaimFilters();
            }
        }

        private void BtnSearchClaims_Click(object sender, RoutedEventArgs e)
        {
            ApplyClaimFilters();
        }

        private void ApplyClaimFilters()
        {
            try
            {
                if (myClaims == null || filteredClaims == null) return;

                var searchTerm = txtClaimSearch?.Text?.ToLower() ?? "";
                var selectedFilter = ((ComboBoxItem)cmbClaimFilter?.SelectedItem)?.Content?.ToString() ?? "All Claims";

                var filtered = myClaims.AsEnumerable();

                // Apply status filter first
                switch (selectedFilter)
                {
                    case "All Claims":
                        // No filtering needed
                        break;
                    case "Pending Review":
                        filtered = filtered.Where(c =>
                            c.Status == "Draft" ||
                            c.Status == "Pending Review" ||
                            c.Status == "Pending Manager Review");
                        break;
                    case "Approved":
                        filtered = filtered.Where(c => c.Status == "Approved");
                        break;
                    case "Rejected":
                        filtered = filtered.Where(c => c.Status == "Rejected");
                        break;
                    case "Draft":
                        filtered = filtered.Where(c => c.Status == "Draft");
                        break;
                    case "Recent Submissions":
                        var recentDate = DateTime.Now.AddDays(-14);
                        filtered = filtered.Where(c => c.SubmitDate >= recentDate);
                        break;
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(c =>
                        (c.ClaimId?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.Description?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.Status?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.AssignedManagerName?.ToLower().Contains(searchTerm) ?? false));
                }

                // Clear and repopulate filtered claims
                filteredClaims.Clear();
                var sortedResults = filtered.OrderByDescending(c => c.SubmitDate).ToList();

                foreach (var claim in sortedResults)
                {
                    filteredClaims.Add(claim);
                }

                // Update status message
                if (lblStatus != null)
                {
                    UpdateStatus($"Found {filteredClaims.Count} claims matching criteria (Filter: {selectedFilter})");
                }

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Lecturer Filter Applied: {selectedFilter}, Results: {filteredClaims.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
                UpdateStatus($"Filter error: {ex.Message}");
            }
        }

        private void BtnViewMyClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = myClaims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim != null)
                {
                    ShowClaimDetails(claim);
                }
            }
        }

        private void BtnEditClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = myClaims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim != null && claim.CanEdit)
                {
                    LoadClaimForEditing(claim);
                    var tabControl = this.FindVisualChildren<TabControl>().FirstOrDefault();
                    if (tabControl != null)
                    {
                        tabControl.SelectedIndex = 1;
                    }
                }
                else
                {
                    MessageBox.Show("This claim cannot be edited. Only draft claims can be modified.",
                                  "Cannot Edit Claim",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
        }
        // ADD this new method to ManagerDashboard.cs (place it after BtnViewClaim_Click in the Claim Management Actions region):

        private void ViewClaimDocuments(string claimId)
        {
            try
            {
                var claimFiles = FileStorageManager.GetClaimFiles(claimId);

                if (claimFiles == null || !claimFiles.Any())
                {
                    MessageBox.Show("No documents attached to this claim.",
                                  "No Documents",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    return;
                }

                // Create a window to display documents
                Window docWindow = new Window
                {
                    Title = $"Documents for Claim {claimId}",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    Icon = new BitmapImage(new Uri("pack://application:,,,/pic.ico"))
                };

                var mainPanel = new StackPanel { Margin = new Thickness(20) };

                var titleBlock = new TextBlock
                {
                    Text = $"Attached Documents ({claimFiles.Count})",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                mainPanel.Children.Add(titleBlock);

                // Create a ListBox for documents
                var docListBox = new ListBox
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Height = 250
                };

                foreach (var file in claimFiles)
                {
                    var filePanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 5, 0, 5)
                    };

                    var fileInfo = new StackPanel { Width = 350 };

                    var nameBlock = new TextBlock
                    {
                        Text = file.OriginalFileName,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Colors.White)
                    };

                    var detailsBlock = new TextBlock
                    {
                        Text = $"{file.FileType} • {FileStorageManager.FormatFileSize(file.FileSize)} • {file.UploadDate:yyyy-MM-dd HH:mm}",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                        Margin = new Thickness(0, 3, 0, 0)
                    };

                    fileInfo.Children.Add(nameBlock);
                    fileInfo.Children.Add(detailsBlock);

                    var openButton = new Button
                    {
                        Content = "Open",
                        Width = 80,
                        Margin = new Thickness(10, 0, 0, 0),
                        Tag = file.FileId,
                        Background = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                        Foreground = new SolidColorBrush(Colors.White),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(10, 5, 10, 5),
                        Cursor = Cursors.Hand
                    };

                    openButton.Click += (s, e) =>
                    {
                        try
                        {
                            FileStorageManager.OpenFile(file.FileId);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening file: {ex.Message}",
                                          "Open Error",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                        }
                    };

                    filePanel.Children.Add(fileInfo);
                    filePanel.Children.Add(openButton);

                    docListBox.Items.Add(filePanel);
                }

                mainPanel.Children.Add(docListBox);

                var closeButton = new Button
                {
                    Content = "Close",
                    Width = 100,
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Background = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(15, 8, 15, 8)
                };

                closeButton.Click += (s, e) => docWindow.Close();
                mainPanel.Children.Add(closeButton);

                docWindow.Content = mainPanel;
                docWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing documents: {ex.Message}",
                              "View Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // ALSO REPLACE the existing ShowClaimDetails method with this updated version:
        private void ShowClaimDetails(LecturerClaimInfo claim)
        {
            string details = $"Claim Details:\n\n" +
                           $"Claim ID: {claim.ClaimId}\n" +
                           $"Submit Date: {claim.SubmitDate:yyyy-MM-dd}\n" +
                           $"Hours Worked: {claim.Hours}\n" +
                           $"Hourly Rate: R{claim.HourlyRate:N2}\n" +
                           $"Total Amount: R{claim.TotalAmount:N2}\n" +
                           $"Status: {claim.Status}\n" +
                           $"Assigned Manager: {claim.AssignedManagerName}\n" +
                           $"Last Updated: {claim.LastUpdated:yyyy-MM-dd HH:mm}\n" +
                           $"Description: {claim.Description}";

            var result = MessageBox.Show(details + "\n\nWould you like to view attached documents?",
                                        "Claim Details",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                ViewClaimDocuments(claim.ClaimId);
            }
        }
        #endregion

        #region Claim Submission

        private void TxtHours_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void TxtHourlyRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            if (decimal.TryParse(txtHours.Text, out decimal hours) &&
                decimal.TryParse(txtHourlyRate.Text, out decimal rate))
            {
                var total = hours * rate;
                txtTotalAmount.Text = $"R {total:N2}";
            }
            else
            {
                txtTotalAmount.Text = "R 0.00";
            }
        }
        // REPLACE the existing BtnAddDocument_Click method in LecturerDashboard.cs with this:

        private void BtnAddDocument_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Supporting Document",
                Filter = "Documents (*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx)|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx|Images (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    try
                    {
                        // Validate file before adding
                        var fileInfo = new FileInfo(fileName);
                        var extension = fileInfo.Extension.ToLower();

                        // Check file size
                        bool isImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(extension);
                        long maxSize = isImage ? 10485760 : 52428800; // 10MB for images, 50MB for documents

                        if (fileInfo.Length > maxSize)
                        {
                            MessageBox.Show($"File '{IOPath.GetFileName(fileName)}' exceeds maximum size of {(isImage ? "10" : "50")} MB.\nFile size: {FileStorageManager.FormatFileSize(fileInfo.Length)}",
                                          "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                            continue;
                        }

                        if (!attachedDocuments.Contains(fileName))
                        {
                            attachedDocuments.Add(fileName);

                            // Add to ListBox with file size
                            var displayName = $"{IOPath.GetFileName(fileName)} ({FileStorageManager.FormatFileSize(fileInfo.Length)})";
                            lbDocuments.Items.Add(displayName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding file: {ex.Message}",
                                      "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                UpdateDocumentCount();
            }
        }

        private void UpdateDocumentCount()
        {
            if (attachedDocuments.Count == 0)
            {
                lblDocumentCount.Text = "No documents attached";
            }
            else if (attachedDocuments.Count == 1)
            {
                lblDocumentCount.Text = "1 document attached";
            }
            else
            {
                lblDocumentCount.Text = $"{attachedDocuments.Count} documents attached";
            }
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateClaimForm())
            {
                SaveClaim(isDraft: true);
                MessageBox.Show("Claim saved as draft successfully!",
                              "Draft Saved",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private void BtnSubmitClaim_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateClaimForm())
            {
                var result = MessageBox.Show("Are you sure you want to submit this claim for review?\n\nOnce submitted, you will not be able to edit it.",
                                           "Confirm Submission",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveClaim(isDraft: false);
                    MessageBox.Show("Claim submitted successfully!\n\nThe assigned manager will receive notification for review.",
                                  "Claim Submitted",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    ClearClaimForm();
                }
            }
        }

        private bool ValidateClaimForm()
        {
            var errors = new List<string>();

            if (dpClaimStart.SelectedDate == null)
                errors.Add("Please select a claim start date.");

            if (dpClaimEnd.SelectedDate == null)
                errors.Add("Please select a claim end date.");

            if (dpClaimStart.SelectedDate > dpClaimEnd.SelectedDate)
                errors.Add("Claim end date must be after start date.");

            if (string.IsNullOrWhiteSpace(txtHours.Text) || !decimal.TryParse(txtHours.Text, out decimal hours) || hours <= 0)
                errors.Add("Please enter valid hours worked.");

            if (string.IsNullOrWhiteSpace(txtHourlyRate.Text) || !decimal.TryParse(txtHourlyRate.Text, out decimal rate) || rate <= 0)
                errors.Add("Please enter a valid hourly rate.");

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
                errors.Add("Please provide a work description.");

            if (cmbAssignedManager.SelectedItem == null)
                errors.Add("Please select an academic manager to review this claim.");

            if (errors.Any())
            {
                MessageBox.Show("Please correct the following errors:\n\n" + string.Join("\n", errors),
                              "Validation Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // REPLACE the existing SaveClaim method in LecturerDashboard.cs with this:

        private void SaveClaim(bool isDraft)
        {
            try
            {
                var selectedManager = (UserInfo)cmbAssignedManager.SelectedItem;

                var newClaimData = new ClaimData
                {
                    LecturerEmail = currentUser.Email,
                    LecturerName = currentUser.FullName,
                    AssignedManagerEmail = selectedManager?.Email,
                    AssignedManagerName = selectedManager?.FullName,
                    StartDate = dpClaimStart.SelectedDate.Value,
                    EndDate = dpClaimEnd.SelectedDate.Value,
                    Hours = int.Parse(txtHours.Text),
                    HourlyRate = decimal.Parse(txtHourlyRate.Text),
                    TotalAmount = decimal.Parse(txtHours.Text) * decimal.Parse(txtHourlyRate.Text),
                    Status = isDraft ? "Draft" : "Pending Review",
                    Description = txtDescription.Text,
                    AttachedDocuments = new List<string>()
                };

                string claimId = DataManager.AddClaim(newClaimData);

                // Save files to storage using FileStorageManager
                if (attachedDocuments.Any())
                {
                    try
                    {
                        var savedPaths = FileStorageManager.SaveMultipleFiles(attachedDocuments, claimId);

                        // Update claim with saved file paths
                        newClaimData.AttachedDocuments = savedPaths;
                        newClaimData.ClaimId = claimId; // Ensure ClaimId is set
                        DataManager.UpdateClaim(newClaimData);

                        UpdateStatus($"Claim saved with {savedPaths.Count} document(s)");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Claim saved but some files could not be stored: {ex.Message}\n\nClaim ID: {claimId}",
                                      "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Refresh the claims list
                LoadUserClaims();
                RefreshDashboardStats();

                // Refresh the UI immediately
                dgMyClaims.ItemsSource = null;
                dgMyClaims.ItemsSource = filteredClaims;

                UpdateStatus(isDraft ? "Draft saved successfully" : "Claim submitted successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving claim: {ex.Message}",
                              "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearClaimForm()
        {
            dpClaimStart.SelectedDate = null;
            dpClaimEnd.SelectedDate = null;
            txtHours.Text = "";
            txtHourlyRate.Text = txtDefaultRate.Text;
            txtTotalAmount.Text = "R 0.00";
            txtDescription.Text = "";
            cmbAssignedManager.SelectedItem = null;
            attachedDocuments.Clear();
            lbDocuments.Items.Clear();
            UpdateDocumentCount();
        }

        private void LoadClaimForEditing(LecturerClaimInfo claim)
        {
            dpClaimStart.SelectedDate = claim.StartDate;
            dpClaimEnd.SelectedDate = claim.EndDate;
            txtHours.Text = claim.Hours.ToString();
            txtHourlyRate.Text = claim.HourlyRate.ToString("N2");
            txtDescription.Text = claim.Description;

            // Set assigned manager
            if (!string.IsNullOrEmpty(claim.AssignedManagerName))
            {
                var manager = academicManagers.FirstOrDefault(m => m.FullName == claim.AssignedManagerName);
                cmbAssignedManager.SelectedItem = manager;
            }

            attachedDocuments.Clear();
            lbDocuments.Items.Clear();

            if (claim.AttachedDocuments != null)
            {
                foreach (var doc in claim.AttachedDocuments)
                {
                    attachedDocuments.Add(doc);
                    lbDocuments.Items.Add(System.IO.Path.GetFileName(doc));
                }
            }

            UpdateDocumentCount();
            CalculateTotal();
        }

        #endregion

        #region Profile Management

        private void BtnUpdateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateProfileForm())
            {
                MessageBox.Show("Profile updated successfully!",
                              "Profile Updated",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

                UpdateStatus("Profile updated successfully");
            }
        }

        private bool ValidateProfileForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
                errors.Add("Full name is required.");

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
                errors.Add("Email address is required.");

            if (!IsValidEmail(txtEmail.Text))
                errors.Add("Please enter a valid email address.");

            if (string.IsNullOrWhiteSpace(txtDefaultRate.Text) || !decimal.TryParse(txtDefaultRate.Text, out decimal rate) || rate <= 0)
                errors.Add("Please enter a valid default hourly rate.");

            if (errors.Any())
            {
                MessageBox.Show("Please correct the following errors:\n\n" + string.Join("\n", errors),
                              "Validation Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

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

        #region Helper Methods

        private void ShowTemporaryMessage(string message)
        {
            lblStatus.Text = message;
            Task.Delay(3000).ContinueWith(t =>
            {
                Dispatcher.Invoke(() => {
                    lblStatus.Text = "Ready";
                });
            });
        }

        #endregion

        #region Window Events

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        #endregion
    }

    #region Data Models

    public class LecturerClaimInfo : INotifyPropertyChanged
    {
        private string _status;

        public string ClaimId { get; set; }
        public DateTime SubmitDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Hours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Description { get; set; }
        public bool CanEdit { get; set; }
        public List<string> AttachedDocuments { get; set; } = new List<string>();
        public string AssignedManagerName { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}

public static class VisualTreeExtensions
{
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}