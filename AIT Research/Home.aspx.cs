using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;

namespace AIT_Research
{
    public partial class Home : System.Web.UI.Page
    {
        // Handles the page load event
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                // No specific initialization required on page load
                // Placeholder for future functionality if needed
            }
            catch (Exception ex)
            {
                // Log any unexpected errors during page load
                LogError("Page_Load", ex);
                DisplayError("An error occurred while loading the page. Please try again.");
            }
        }

        // Handles the Start Survey button click to initialize a new session and redirect to the survey
        protected void btnStartSurvey_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the client's IP address
                string ipAddress = Request.UserHostAddress;
                if (string.IsNullOrEmpty(ipAddress))
                {
                    throw new Exception("Unable to retrieve client IP address");
                }

                // Convert IPv6 to IPv4 if necessary
                if (IPAddress.TryParse(ipAddress, out IPAddress parsedIP))
                {
                    if (parsedIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        ipAddress = parsedIP.MapToIPv4().ToString();
                    }
                }
                else
                {
                    throw new Exception("Invalid IP address format");
                }

                // Record the current timestamp
                DateTime startTime = DateTime.Now;

                // Retrieve database connection string
                string connStr = "";
                if (ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"]?.ConnectionString.Equals("Dev") == true)
                {
                    connStr = AppConstant.AppConnection.DevConnection;
                }
                else
                {
                    // Fallback to default connection string if needed
                    connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"]?.ConnectionString;
                }

                // Validate connection string
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new ConfigurationErrorsException("Database connection string not found");
                }

                int sessionId;
                int respondentId;

                // Insert session and respondent data into the database
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert new session record and get the session ID
                            using (SqlCommand cmdSession = new SqlCommand(
                                "INSERT INTO Session (Date, IPAddress) VALUES (@date, @ip); SELECT SCOPE_IDENTITY();", conn, transaction))
                            {
                                cmdSession.Parameters.AddWithValue("@date", startTime);
                                cmdSession.Parameters.AddWithValue("@ip", ipAddress);
                                sessionId = Convert.ToInt32(cmdSession.ExecuteScalar());
                            }

                            // Insert new respondent record and get the respondent ID
                            using (SqlCommand cmdRespondent = new SqlCommand(
                                "INSERT INTO Respondent (SessionID, IsAnonymous) VALUES (@sid, @anon); SELECT SCOPE_IDENTITY();", conn, transaction))
                            {
                                cmdRespondent.Parameters.AddWithValue("@sid", sessionId);
                                cmdRespondent.Parameters.AddWithValue("@anon", true);
                                respondentId = Convert.ToInt32(cmdRespondent.ExecuteScalar());
                            }

                            // Commit transaction
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            // Rollback transaction on error
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                // Store respondent ID in session
                Session["RespondentID"] = respondentId.ToString();

                // Redirect to the survey page
                Response.Redirect("Survey.aspx");
            }
            catch (SqlException sqlEx)
            {
                // Log database-specific error
                LogError("btnStartSurvey_Click", sqlEx);
                DisplayError("Database error while starting the survey. Please try again.");
            }
            catch (Exception ex)
            {
                // Log general error
                LogError("btnStartSurvey_Click", ex);
                DisplayError("Error starting the survey. Please try again.");
            }
        }

        // Handles the Staff Login button click to redirect to the login page
        protected void btnStaffLogin_Click(object sender, EventArgs e)
        {
            try
            {
                // Redirect to the staff login page
                Response.Redirect("StaffLogin.aspx");
            }
            catch (Exception ex)
            {
                // Log any errors during redirection
                LogError("btnStaffLogin_Click", ex);
                DisplayError("Error redirecting to staff login. Please try again.");
            }
        }

        // Displays an error message to the user
        private void DisplayError(string message)
        {
            Warning.Text = message;
            Warning.Visible = true;
        }

        // Logs errors to the system event log for debugging
        private void LogError(string methodName, Exception ex)
        {
            try
            {
                EventLog.WriteEntry("AIT_Research_Home",
                    $"Error in {methodName}: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    EventLogEntryType.Error);
            }
            catch
            {
                // Suppress logging errors to prevent infinite loops
            }
        }
    }
}