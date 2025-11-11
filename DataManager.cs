using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Plan
{
    /// <summary>
    /// Centralized data management for JSON persistence
    /// </summary>
    public static class DataManager
    {
        private static readonly string usersFilePath = "users.json";
        private static readonly string claimsFilePath = "claims.json";
        private static int nextClaimId = 1;

        #region User Management

        /// <summary>
        /// Load all users from JSON file
        /// </summary>
        public static List<UserInfo> LoadUsers()
        {
            try
            {
                if (File.Exists(usersFilePath))
                {
                    string jsonContent = File.ReadAllText(usersFilePath);
                    return JsonConvert.DeserializeObject<List<UserInfo>>(jsonContent) ?? new List<UserInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading users: {ex.Message}");
            }
            return new List<UserInfo>();
        }

        /// <summary>
        /// Save all users to JSON file
        /// </summary>
        public static void SaveUsers(List<UserInfo> users)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(users, Formatting.Indented);
                File.WriteAllText(usersFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving users: {ex.Message}");
            }
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        public static UserInfo GetUser(string email)
        {
            var users = LoadUsers();
            return users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
        }

        /// <summary>
        /// Get all academic managers
        /// </summary>
        public static List<UserInfo> GetAcademicManagers()
        {
            var users = LoadUsers();
            return users.Where(u => u.UserType == "Academic Manager" && u.IsActive).ToList();
        }

        #endregion

        #region Claim Management
        // Add this RIGHT AFTER the line "#region Claim Management Actions" in your ManagerDashboard.cs file
        // (around line 341)

       
        /// <summary>
        /// Load all claims from JSON file
        /// </summary>
        public static List<ClaimData> LoadClaims()
        {
            try
            {
                if (File.Exists(claimsFilePath))
                {
                    string jsonContent = File.ReadAllText(claimsFilePath);
                    var claims = JsonConvert.DeserializeObject<List<ClaimData>>(jsonContent) ?? new List<ClaimData>();

                    // Update next claim ID based on existing claims
                    if (claims.Any())
                    {
                        var maxId = claims.Max(c => c.ClaimNumber);
                        nextClaimId = maxId + 1;
                    }

                    return claims;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading claims: {ex.Message}");
            }
            return new List<ClaimData>();
        }

        /// <summary>
        /// Save all claims to JSON file
        /// </summary>
        public static void SaveClaims(List<ClaimData> claims)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(claims, Formatting.Indented);
                File.WriteAllText(claimsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving claims: {ex.Message}");
            }
        }

        /// <summary>
        /// Add new claim
        /// </summary>
        public static string AddClaim(ClaimData claim)
        {
            var claims = LoadClaims();
            claim.ClaimNumber = nextClaimId++;
            claim.ClaimId = $"C{DateTime.Now.Year}-{claim.ClaimNumber:D3}";
            claim.SubmitDate = DateTime.Now;
            claim.LastUpdated = DateTime.Now;

            claims.Add(claim);
            SaveClaims(claims);
            return claim.ClaimId;
        }

        /// <summary>
        /// Update existing claim
        /// </summary>
        public static void UpdateClaim(ClaimData updatedClaim)
        {
            var claims = LoadClaims();
            var existingClaim = claims.FirstOrDefault(c => c.ClaimId == updatedClaim.ClaimId);

            if (existingClaim != null)
            {
                var index = claims.IndexOf(existingClaim);
                updatedClaim.LastUpdated = DateTime.Now;
                claims[index] = updatedClaim;
                SaveClaims(claims);
            }
        }

        /// <summary>
        /// Get claims by lecturer email
        /// </summary>
        public static List<ClaimData> GetClaimsByLecturer(string lecturerEmail)
        {
            var claims = LoadClaims();
            return claims.Where(c => c.LecturerEmail.ToLower() == lecturerEmail.ToLower()).ToList();
        }

        /// <summary>
        /// Get claims by manager email
        /// </summary>
        public static List<ClaimData> GetClaimsByManager(string managerEmail)
        {
            var claims = LoadClaims();
            return claims.Where(c => c.AssignedManagerEmail != null &&
                               c.AssignedManagerEmail.ToLower() == managerEmail.ToLower()).ToList();
        }
        /// <summary>
        /// Bulk update multiple claims
        /// </summary>
        public static void BulkUpdateClaims(List<ClaimData> claimsToUpdate)
        {
            var allClaims = LoadClaims();

            foreach (var updatedClaim in claimsToUpdate)
            {
                var existingClaim = allClaims.FirstOrDefault(c => c.ClaimId == updatedClaim.ClaimId);
                if (existingClaim != null)
                {
                    var index = allClaims.IndexOf(existingClaim);
                    updatedClaim.LastUpdated = DateTime.Now;
                    allClaims[index] = updatedClaim;
                }
            }

            SaveClaims(allClaims);
        }
        /// <summary>
        /// Notification data model for JSON storage
        /// </summary>
        public class NotificationData
        {
            public string NotificationId { get; set; }
            public string RecipientEmail { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsRead { get; set; } = false;
            public string Type { get; set; } // "Rejection", "Approval", "General"
            public string RelatedClaimId { get; set; }
        }

        // Add these methods to DataManager class:

        private static readonly string notificationsFilePath = "notifications.json";

        /// <summary>
        /// Load all notifications from JSON file
        /// </summary>
        public static List<NotificationData> LoadNotifications()
        {
            try
            {
                if (File.Exists(notificationsFilePath))
                {
                    string jsonContent = File.ReadAllText(notificationsFilePath);
                    return JsonConvert.DeserializeObject<List<NotificationData>>(jsonContent) ?? new List<NotificationData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading notifications: {ex.Message}");
            }
            return new List<NotificationData>();
        }

        /// <summary>
        /// Save all notifications to JSON file
        /// </summary>
        public static void SaveNotifications(List<NotificationData> notifications)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(notifications, Formatting.Indented);
                File.WriteAllText(notificationsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Add new notification
        /// </summary>
        public static void AddNotification(NotificationData notification)
        {
            var notifications = LoadNotifications();
            notification.NotificationId = $"N{DateTime.Now:yyyyMMdd}-{(notifications.Count + 1):D3}";
            notification.CreatedDate = DateTime.Now;

            notifications.Add(notification);
            SaveNotifications(notifications);
        }

        /// <summary>
        /// Get notifications for specific user
        /// </summary>
        public static List<NotificationData> GetNotificationsByUser(string userEmail)
        {
            var notifications = LoadNotifications();
            return notifications.Where(n => n.RecipientEmail.ToLower() == userEmail.ToLower())
                               .OrderByDescending(n => n.CreatedDate)
                               .ToList();
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public static void MarkNotificationAsRead(string notificationId)
        {
            var notifications = LoadNotifications();
            var notification = notifications.FirstOrDefault(n => n.NotificationId == notificationId);

            if (notification != null)
            {
                notification.IsRead = true;
                SaveNotifications(notifications);
            }
        }

        /// <summary>
        /// Get unread notification count for user
        /// </summary>
        public static int GetUnreadNotificationCount(string userEmail)
        {
            var notifications = LoadNotifications();
            return notifications.Count(n => n.RecipientEmail.ToLower() == userEmail.ToLower() && !n.IsRead);
        }

        /// <summary>
        /// Create rejection notification
        /// </summary>
        public static void CreateRejectionNotification(string lecturerEmail, string claimId, string rejectionReason, string managerName)
        {
            var notification = new NotificationData
            {
                RecipientEmail = lecturerEmail,
                Title = "Claim Rejected",
                Message = $"Your claim {claimId} has been rejected by {managerName}.\n\nReason: {rejectionReason}\n\nIf you have questions about this decision, please contact the Academic Manager.",
                Type = "Rejection",
                RelatedClaimId = claimId
            };

            AddNotification(notification);
        }
        public static bool DeleteNotification(string notificationId)
        {
            try
            {
                var notifications = LoadNotifications();

                // Find and remove the notification with the specified ID
                var notificationToDelete = notifications.FirstOrDefault(n => n.NotificationId == notificationId);

                if (notificationToDelete != null)
                {
                    notifications.Remove(notificationToDelete);

                    // Save the updated notifications list back to the JSON file
                    SaveNotifications(notifications);

                    return true;
                }

                return false; // Notification not found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting notification: {ex.Message}");
                throw new Exception($"Failed to delete notification: {ex.Message}");
            }
        }

        // You might also want to add a method to delete multiple notifications at once for efficiency
        public static bool DeleteNotifications(List<string> notificationIds)
        {
            try
            {
                var notifications = LoadNotifications();

                // Remove all notifications with IDs in the provided list
                var notificationsToDelete = notifications.Where(n => notificationIds.Contains(n.NotificationId)).ToList();

                foreach (var notification in notificationsToDelete)
                {
                    notifications.Remove(notification);
                }

                // Save the updated notifications list back to the JSON file
                SaveNotifications(notifications);

                return notificationsToDelete.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting notifications: {ex.Message}");
                throw new Exception($"Failed to delete notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Bulk delete multiple claims
        /// </summary>
        public static void BulkDeleteClaims(List<string> claimIds)
        {
            var claims = LoadClaims();
            claims.RemoveAll(c => claimIds.Contains(c.ClaimId));
            SaveClaims(claims);
        }

        /// <summary>
        /// Delete claim (for rejected claims)
        /// </summary>
        public static void DeleteClaim(string claimId)
        {
            var claims = LoadClaims();
            claims.RemoveAll(c => c.ClaimId == claimId);
            SaveClaims(claims);
        }

        /// <summary>
        /// Get claims statistics for manager
        /// </summary>
        public static ManagerStats GetManagerStats(string managerEmail)
        {
            var claims = LoadClaims();
            var managerClaims = claims.Where(c => c.AssignedManagerEmail != null &&
                                            c.AssignedManagerEmail.ToLower() == managerEmail.ToLower()).ToList();

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            return new ManagerStats
            {
                PendingClaims = managerClaims.Count(c => c.Status == "Pending Review"),
                ApprovedThisMonth = managerClaims.Count(c => c.Status == "Approved" &&
                                                       c.LastUpdated.Month == currentMonth &&
                                                       c.LastUpdated.Year == currentYear),
                RejectedThisMonth = managerClaims.Count(c => c.Status == "Rejected" &&
                                                       c.LastUpdated.Month == currentMonth &&
                                                       c.LastUpdated.Year == currentYear),
                TotalPendingAmount = managerClaims.Where(c => c.Status == "Pending Review")
                                                 .Sum(c => c.TotalAmount)
            };
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// Claim data model for JSON storage
    /// </summary>
    public class ClaimData
    {
        public string ClaimId { get; set; }
        public int ClaimNumber { get; set; }
        public string LecturerEmail { get; set; }
        public string LecturerName { get; set; }
        public string AssignedManagerEmail { get; set; }
        public string AssignedManagerName { get; set; }
        public DateTime SubmitDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Hours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending Review";
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> AttachedDocuments { get; set; } = new List<string>();
        public string ManagerComments { get; set; }
        public DateTime? ManagerActionDate { get; set; }
    }

    /// <summary>
    /// Manager statistics model
    /// </summary>
    public class ManagerStats
    {
        public int PendingClaims { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public decimal TotalPendingAmount { get; set; }
    }

    #endregion
}