using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace Plan
{
    public partial class ManagerDashboard : Window
    {
        private UserInfo currentUser;
        private ObservableCollection<ClaimInfo> claims;
        private ObservableCollection<ClaimInfo> filteredClaims;
        private ManagerStats currentStats;

        public ManagerDashboard()
        {
            InitializeComponent();
            InitializeDashboard();
        }

        public ManagerDashboard(UserInfo user)
        {
            InitializeComponent();
            currentUser = user;
            lblWelcomeMessage.Text = $"Welcome, {user.FullName}";
            InitializeDashboard();
        }

        private void InitializeDashboard()
        {
            LoadManagerClaims();
            RefreshDashboardStats();
            UpdateStatusBar();
            LoadRecentActivity();

            dgClaims.ItemsSource = filteredClaims;
        }

        #region Data Loading and Management

        private void LoadManagerClaims()
        {
            try
            {
                if (currentUser == null) return;

                var managerClaims = DataManager.GetClaimsByManager(currentUser.Email);
                claims = new ObservableCollection<ClaimInfo>();

                foreach (var claim in managerClaims)
                {
                    claims.Add(new ClaimInfo
                    {
                        ClaimId = claim.ClaimId,
                        LecturerName = claim.LecturerName,
                        SubmitDate = claim.SubmitDate,
                        Hours = claim.Hours,
                        HourlyRate = claim.HourlyRate,
                        TotalAmount = claim.TotalAmount,
                        Status = claim.Status,
                        CoordinatorComments = claim.Description,
                        CoordinatorApprovalDate = claim.SubmitDate,
                        ManagerComments = claim.ManagerComments,
                        ManagerApprovalDate = claim.ManagerActionDate
                    });
                }

                // Initialize filtered claims collection
                filteredClaims = new ObservableCollection<ClaimInfo>();

                // Load all claims initially, then apply filters
                foreach (var claim in claims)
                {
                    filteredClaims.Add(claim);
                }

                // Set the ComboBox to default selection if not already set
                if (cmbFilterStatus != null && cmbFilterStatus.SelectedIndex < 0)
                {
                    cmbFilterStatus.SelectedIndex = 0; // "All Claims"
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {claims.Count} claims for manager");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading claims: {ex.Message}",
                              "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                claims = new ObservableCollection<ClaimInfo>();
                filteredClaims = new ObservableCollection<ClaimInfo>();
            }
        }

        private void RefreshDashboardStats()
        {
            try
            {
                if (currentUser == null) return;

                currentStats = DataManager.GetManagerStats(currentUser.Email);

                lblPendingClaims.Text = currentStats.PendingClaims.ToString();
                lblApprovedClaims.Text = currentStats.ApprovedThisMonth.ToString();
                lblTotalAmount.Text = $"R {currentStats.TotalPendingAmount:N0}";
                lblClaimsCount.Text = $"({currentStats.PendingClaims} pending)";

                // Calculate average processing time (mock calculation)
                lblAvgProcessingTime.Text = currentStats.PendingClaims > 0 ? "2.3 days" : "N/A";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing stats: {ex.Message}",
                              "Stats Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecentActivity()
        {
            try
            {
                spRecentActivity.Children.Clear();

                if (currentStats != null)
                {
                    AddRecentActivityItem($"Approved {currentStats.ApprovedThisMonth} claims this month");
                    AddRecentActivityItem($"Rejected {currentStats.RejectedThisMonth} claims this month");
                    AddRecentActivityItem($"{currentStats.PendingClaims} claims awaiting review");
                    AddRecentActivityItem("Dashboard refreshed");
                }
            }
            catch (Exception ex)
            {
                AddRecentActivityItem($"Error loading activity: {ex.Message}");
            }
        }

        private void AddRecentActivityItem(string activity)
        {
            var timestamp = DateTime.Now.ToString("HH:mm");
            var activityText = $"• [{timestamp}] {activity}";

            var newActivity = new TextBlock
            {
                Text = activityText,
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };

            spRecentActivity.Children.Insert(0, newActivity);

            while (spRecentActivity.Children.Count > 10)
            {
                spRecentActivity.Children.RemoveAt(spRecentActivity.Children.Count - 1);
            }
        }

        private void UpdateStatusBar()
        {
            lblStatusMessage.Text = "Dashboard loaded successfully";
            lblLastRefresh.Text = $"Last refreshed: {DateTime.Now:HH:mm}";
        }

        #endregion

        #region Header Action Handlers

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            lblStatusMessage.Text = "Refreshing data...";

            try
            {
                LoadManagerClaims();
                RefreshDashboardStats();
                LoadRecentActivity();

                dgClaims.ItemsSource = null;
                dgClaims.ItemsSource = filteredClaims;

                UpdateStatusBar();
                ShowTemporaryMessage("Data refreshed successfully", MessageType.Success);
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Error refreshing: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            string reportData = $"Monthly Report for {currentUser?.FullName}\n\n" +
                              $"Pending Claims: {currentStats?.PendingClaims ?? 0}\n" +
                              $"Approved This Month: {currentStats?.ApprovedThisMonth ?? 0}\n" +
                              $"Rejected This Month: {currentStats?.RejectedThisMonth ?? 0}\n" +
                              $"Total Pending Amount: R {currentStats?.TotalPendingAmount ?? 0:N2}\n\n" +
                              "Detailed reporting functionality will be enhanced in future updates.";

            MessageBox.Show(reportData, "Quick Report", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #region Search and Filter

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Apply filters when dropdown selection changes
            if (IsLoaded) // Only apply if the window is fully loaded
            {
                ApplyFilters();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Apply filters as user types (with a small delay to avoid too frequent updates)
            if (IsLoaded && txtSearch.Text.Length > 2 || string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (claims == null || filteredClaims == null) return;

                var searchTerm = txtSearch?.Text?.ToLower() ?? "";
                var selectedFilter = ((ComboBoxItem)cmbFilterStatus?.SelectedItem)?.Content?.ToString() ?? "All Claims";

                var filtered = claims.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(c =>
                        (c.ClaimId?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.LecturerName?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.Status?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.ManagerComments?.ToLower().Contains(searchTerm) ?? false) ||
                        (c.CoordinatorComments?.ToLower().Contains(searchTerm) ?? false));
                }

                // Apply status filter
                switch (selectedFilter)
                {
                    case "All Claims":
                        // No additional filtering needed
                        break;
                    case "Pending Final Approval":
                        filtered = filtered.Where(c => c.Status == "Pending Review" || c.Status == "Pending Manager Review");
                        break;
                    case "High Amount (>R5000)":
                        filtered = filtered.Where(c => c.TotalAmount > 5000);
                        break;
                    case "This Week":
                        var weekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
                        filtered = filtered.Where(c => c.SubmitDate >= weekStart);
                        break;
                    case "Approved Claims":
                        filtered = filtered.Where(c => c.Status == "Approved");
                        break;
                    case "Recent Submissions":
                        var recentDate = DateTime.Now.AddDays(-7);
                        filtered = filtered.Where(c => c.SubmitDate >= recentDate);
                        break;
                }

                filteredClaims.Clear();
                foreach (var claim in filtered.OrderByDescending(c => c.SubmitDate))
                {
                    filteredClaims.Add(claim);
                }

                lblStatusMessage.Text = $"Found {filteredClaims.Count} claims matching criteria";
                AddRecentActivityItem($"Filtered claims: {filteredClaims.Count} results");
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Filter error: {ex.Message}", MessageType.Error);
            }
        }

        #endregion

        #region Claim Management Actions

        #region Claim Management Actions

        private void BtnViewClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim != null)
                {
                    string claimDetails = $"Claim Details:\n\n" +
                                        $"Claim ID: {claim.ClaimId}\n" +
                                        $"Lecturer: {claim.LecturerName}\n" +
                                        $"Submit Date: {claim.SubmitDate:yyyy-MM-dd}\n" +
                                        $"Hours Worked: {claim.Hours}\n" +
                                        $"Hourly Rate: R{claim.HourlyRate:N2}\n" +
                                        $"Total Amount: R{claim.TotalAmount:N2}\n" +
                                        $"Status: {claim.Status}\n" +
                                        $"Coordinator Comments: {claim.CoordinatorComments}\n" +
                                        $"Manager Comments: {claim.ManagerComments}";

                    var result = MessageBox.Show(claimDetails + "\n\nWould you like to view attached documents?",
                                                "Claim Details",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        ViewClaimDocuments(claim.ClaimId);
                    }
                }
            }
        }

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

        private void BtnApproveClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim == null) return;

                var result = MessageBox.Show($"Are you sure you want to approve claim {claimId}?\n\n" +
                                           $"Lecturer: {claim.LecturerName}\n" +
                                           $"Amount: R{claim.TotalAmount:N2}\n\n" +
                                           "This action will authorize payment to the lecturer.",
                                           "Confirm Approval",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Load the original claim data to update
                        var allClaims = DataManager.LoadClaims();
                        var originalClaim = allClaims.FirstOrDefault(c => c.ClaimId == claimId);

                        if (originalClaim != null)
                        {
                            originalClaim.Status = "Approved";
                            originalClaim.ManagerComments = "Approved for payment";
                            originalClaim.ManagerActionDate = DateTime.Now;

                            DataManager.UpdateClaim(originalClaim);

                            // Update local data
                            claim.Status = "Approved";
                            claim.ManagerComments = "Approved for payment";
                            claim.ManagerApprovalDate = DateTime.Now;

                            RefreshDashboardStats();
                            ApplyFilters(); // Refresh the filtered view

                            AddRecentActivityItem($"Approved claim {claimId} for {claim.LecturerName}");
                            ShowTemporaryMessage($"Claim {claimId} approved successfully", MessageType.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error approving claim: {ex.Message}",
                                      "Approval Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion


        private void BtnRejectClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim == null) return;

                string reason = PromptForInput("Please provide a reason for rejection:", "Reject Claim");

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    try
                    {
                        var allClaims = DataManager.LoadClaims();
                        var originalClaim = allClaims.FirstOrDefault(c => c.ClaimId == claimId);

                        if (originalClaim != null)
                        {
                            originalClaim.Status = "Rejected";
                            originalClaim.ManagerComments = reason;
                            originalClaim.ManagerActionDate = DateTime.Now;

                            DataManager.UpdateClaim(originalClaim);

                            // Create notification for the lecturer
                            DataManager.CreateRejectionNotification(
                                originalClaim.LecturerEmail,
                                claimId,
                                reason,
                                currentUser?.FullName ?? "Academic Manager"
                            );

                            // Update local data
                            claim.Status = "Rejected";
                            claim.ManagerComments = reason;
                            claim.ManagerApprovalDate = DateTime.Now;

                            RefreshDashboardStats();
                            ApplyFilters();

                            AddRecentActivityItem($"Rejected claim {claimId} - notification sent to lecturer");
                            ShowTemporaryMessage($"Claim {claimId} rejected and notification sent", MessageType.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error rejecting claim: {ex.Message}",
                                      "Rejection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnDeleteClaim_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string claimId)
            {
                var claim = claims.FirstOrDefault(c => c.ClaimId == claimId);
                if (claim == null) return;

                var result = MessageBox.Show($"Are you sure you want to permanently delete claim {claimId}?\n\n" +
                                           $"Lecturer: {claim.LecturerName}\n" +
                                           $"Amount: R{claim.TotalAmount:N2}\n\n" +
                                           "This action cannot be undone and will remove the claim from the database.",
                                           "Confirm Delete",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Delete from database/JSON file
                        DataManager.DeleteClaim(claimId);

                        // Remove from local collections
                        claims.Remove(claim);
                        if (filteredClaims.Contains(claim))
                        {
                            filteredClaims.Remove(claim);
                        }

                        RefreshDashboardStats();
                        AddRecentActivityItem($"Permanently deleted claim {claimId} by {claim.LecturerName}");
                        ShowTemporaryMessage($"Claim {claimId} deleted successfully", MessageType.Success);

                        lblStatusMessage.Text = $"Claim {claimId} has been permanently deleted";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting claim: {ex.Message}",
                                      "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private string PromptForInput(string prompt, string title)
        {
            Window inputWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock promptText = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            TextBox inputBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button okButton = new Button
            {
                Content = "OK",
                Width = 70,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 70,
                IsCancel = true
            };

            string result = null;

            okButton.Click += (s, e) => { result = inputBox.Text; inputWindow.DialogResult = true; };
            cancelButton.Click += (s, e) => { inputWindow.DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(promptText);
            panel.Children.Add(inputBox);
            panel.Children.Add(buttonPanel);

            inputWindow.Content = panel;
            inputBox.Focus();

            return inputWindow.ShowDialog() == true ? result : string.Empty;
        }

        private void DgClaims_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClaims.SelectedItem is ClaimInfo selectedClaim)
            {
                lblStatusMessage.Text = $"Selected claim {selectedClaim.ClaimId} - R{selectedClaim.TotalAmount:N2}";
            }
        }

        #endregion

        #region Quick Actions

        private void BtnBulkApprove_Click(object sender, RoutedEventArgs e)
        {
            var pendingClaims = filteredClaims.Where(c => c.Status == "Pending Review" || c.Status == "Pending Manager Review").ToList();

            if (pendingClaims.Count == 0)
            {
                MessageBox.Show("No pending claims available for bulk approval.",
                              "Bulk Approval", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to approve all {pendingClaims.Count} pending claims?\n\n" +
                                       $"Total amount: R{pendingClaims.Sum(c => c.TotalAmount):N2}",
                                       "Confirm Bulk Approval",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int approvedCount = 0;
                    var allClaims = DataManager.LoadClaims();

                    foreach (var claim in pendingClaims)
                    {
                        var originalClaim = allClaims.FirstOrDefault(c => c.ClaimId == claim.ClaimId);
                        if (originalClaim != null)
                        {
                            originalClaim.Status = "Approved";
                            originalClaim.ManagerComments = "Bulk approved for payment";
                            originalClaim.ManagerActionDate = DateTime.Now;

                            DataManager.UpdateClaim(originalClaim);

                            // Update local data
                            claim.Status = "Approved";
                            claim.ManagerComments = "Bulk approved for payment";
                            claim.ManagerApprovalDate = DateTime.Now;

                            approvedCount++;
                        }
                    }

                    RefreshDashboardStats();
                    ApplyFilters(); // Refresh the view

                    AddRecentActivityItem($"Bulk approved {approvedCount} claims");
                    ShowTemporaryMessage($"Successfully approved {approvedCount} claims", MessageType.Success);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in bulk approval: {ex.Message}",
                                  "Bulk Approval Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnBulkReject_Click(object sender, RoutedEventArgs e)
        {
            var pendingClaims = filteredClaims.Where(c => c.Status == "Pending Review" || c.Status == "Pending Manager Review").ToList();

            if (pendingClaims.Count == 0)
            {
                MessageBox.Show("No pending claims available for bulk rejection.",
                              "Bulk Rejection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string reason = PromptForInput($"Please provide a reason for rejecting all {pendingClaims.Count} pending claims:", "Bulk Reject Claims");

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var result = MessageBox.Show($"Are you sure you want to reject all {pendingClaims.Count} pending claims?\n\n" +
                                           $"Reason: {reason}\n" +
                                           $"Total amount: R{pendingClaims.Sum(c => c.TotalAmount):N2}\n\n" +
                                           "Rejection notifications will be sent to all affected lecturers.",
                                           "Confirm Bulk Rejection",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        int rejectedCount = 0;
                        var allClaims = DataManager.LoadClaims();

                        foreach (var claim in pendingClaims)
                        {
                            var originalClaim = allClaims.FirstOrDefault(c => c.ClaimId == claim.ClaimId);
                            if (originalClaim != null)
                            {
                                originalClaim.Status = "Rejected";
                                originalClaim.ManagerComments = $"Bulk rejection: {reason}";
                                originalClaim.ManagerActionDate = DateTime.Now;

                                DataManager.UpdateClaim(originalClaim);

                                // Update local data
                                claim.Status = "Rejected";
                                claim.ManagerComments = $"Bulk rejection: {reason}";
                                claim.ManagerApprovalDate = DateTime.Now;

                                // Send rejection notification to lecturer
                                SendRejectionNotification(claim.LecturerName, claim.ClaimId, reason);

                                rejectedCount++;
                            }
                        }

                        RefreshDashboardStats();
                        ApplyFilters();

                        AddRecentActivityItem($"Bulk rejected {rejectedCount} claims");
                        ShowTemporaryMessage($"Successfully rejected {rejectedCount} claims", MessageType.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error in bulk rejection: {ex.Message}",
                                      "Bulk Rejection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnBulkDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedClaims = filteredClaims.ToList(); // Or you could filter for specific statuses

            if (selectedClaims.Count == 0)
            {
                MessageBox.Show("No claims available for bulk deletion.",
                              "Bulk Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to permanently delete all {selectedClaims.Count} filtered claims?\n\n" +
                                       $"Total amount: R{selectedClaims.Sum(c => c.TotalAmount):N2}\n\n" +
                                       "This action cannot be undone and will remove all claims from the database.",
                                       "Confirm Bulk Delete",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                // Double confirmation for such a destructive action
                var finalConfirm = MessageBox.Show("This will PERMANENTLY delete all selected claims.\n\nType 'DELETE' to confirm you understand this action cannot be undone.",
                                                 "Final Confirmation Required",
                                                 MessageBoxButton.YesNo,
                                                 MessageBoxImage.Stop);

                if (finalConfirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        int deletedCount = 0;

                        foreach (var claim in selectedClaims.ToList()) // ToList to avoid collection modification issues
                        {
                            DataManager.DeleteClaim(claim.ClaimId);

                            // Remove from local collections
                            claims.Remove(claim);
                            if (filteredClaims.Contains(claim))
                            {
                                filteredClaims.Remove(claim);
                            }

                            deletedCount++;
                        }

                        RefreshDashboardStats();
                        AddRecentActivityItem($"Bulk deleted {deletedCount} claims");
                        ShowTemporaryMessage($"Successfully deleted {deletedCount} claims", MessageType.Success);

                        lblStatusMessage.Text = $"{deletedCount} claims have been permanently deleted";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error in bulk deletion: {ex.Message}",
                                      "Bulk Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SendRejectionNotification(string lecturerName, string claimId, string reason)
        {
            // This simulates sending a notification to the lecturer
            // In a real application, this would send an email or system notification
            string notificationMessage = $"Claim Rejection Notice\n\n" +
                                       $"Dear {lecturerName},\n\n" +
                                       $"Your claim {claimId} has been rejected by the Academic Manager.\n\n" +
                                       $"Reason: {reason}\n\n" +
                                       $"If you have questions about this decision, please contact the Academic Manager.\n\n" +
                                       $"Regards,\nCMCS System";

            // For now, we'll just log this notification
            // You could extend this to actually send emails or save to a notifications file
            AddRecentActivityItem($"Rejection notice sent to {lecturerName} for claim {claimId}");

            // Optional: Show the notification message (for demonstration)
            // MessageBox.Show(notificationMessage, "Notification Sent", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExportClaims_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportData = new StringBuilder();
                exportData.AppendLine("Claim Export Report");
                exportData.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                exportData.AppendLine($"Manager: {currentUser?.FullName}");
                exportData.AppendLine();
                exportData.AppendLine("Claims Summary:");
                exportData.AppendLine($"Total Claims: {claims.Count}");
                exportData.AppendLine($"Pending: {claims.Count(c => c.Status == "Pending Review" || c.Status == "Pending Manager Review")}");
                exportData.AppendLine($"Approved: {claims.Count(c => c.Status == "Approved")}");
                exportData.AppendLine($"Rejected: {claims.Count(c => c.Status == "Rejected")}");
                exportData.AppendLine();
                exportData.AppendLine("Claim Details:");
                exportData.AppendLine("Claim ID, Lecturer Name, Amount, Status, Submit Date");

                foreach (var claim in filteredClaims)
                {
                    exportData.AppendLine($"{claim.ClaimId}, {claim.LecturerName}, R{claim.TotalAmount:N2}, {claim.Status}, {claim.SubmitDate:yyyy-MM-dd}");
                }

                MessageBox.Show(exportData.ToString(), "Claims Export", MessageBoxButton.OK, MessageBoxImage.Information);
                AddRecentActivityItem("Exported claims data");
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Export error: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnViewAllLecturers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var users = DataManager.LoadUsers();
                var lecturers = users.Where(u => u.UserType == "Lecturer").ToList();

                var lecturerInfo = new StringBuilder();
                lecturerInfo.AppendLine("Registered Lecturers:");
                lecturerInfo.AppendLine();

                foreach (var lecturer in lecturers)
                {
                    var claimCount = claims.Count(c => c.LecturerName == lecturer.FullName);
                    lecturerInfo.AppendLine($"• {lecturer.FullName} ({lecturer.Email}) - {claimCount} claims");
                }

                MessageBox.Show(lecturerInfo.ToString(), "All Lecturers", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowTemporaryMessage($"Error loading lecturers: {ex.Message}", MessageType.Error);
            }
        }

        private void BtnSystemSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"System Settings\n\n" +
                          $"Current User: {currentUser?.FullName}\n" +
                          $"User Type: {currentUser?.UserType}\n" +
                          $"Email: {currentUser?.Email}\n\n" +
                          "Advanced settings will be available in future updates.",
                          "System Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private void ShowTemporaryMessage(string message, MessageType type)
        {
            lblStatusMessage.Text = message;

            Task.Delay(3000).ContinueWith(t =>
            {
                Dispatcher.Invoke(() => {
                    lblStatusMessage.Text = "Ready";
                });
            });
        }

        #endregion

        #region Window Events

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                BtnRefresh_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                txtSearch.Focus();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        #endregion
    }

    #region Data Models

    public class ClaimInfo : INotifyPropertyChanged
    {
        private string _status;

        public string ClaimId { get; set; }
        public string LecturerName { get; set; }
        public DateTime SubmitDate { get; set; }
        public int Hours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public DateTime? CoordinatorApprovalDate { get; set; }
        public string CoordinatorComments { get; set; }
        public DateTime? ManagerApprovalDate { get; set; }
        public string ManagerComments { get; set; }
        public List<string> DocumentPaths { get; set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    #endregion
}