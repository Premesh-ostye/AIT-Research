using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Diagnostics;

namespace AIT_Research
{
    public partial class Survey : System.Web.UI.Page
    {
        // Class to store user responses
        public class UserAnswer
        {
            public int QID { get; set; }
            public int OptionID { get; set; }
        }

        // Handles the page load event to initialize the survey
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                // Initialize session and load first question only on initial page load
                if (!IsPostBack)
                {
                    Session["Answers"] = new List<UserAnswer>();
                    LoadQuestion(1001); // Start with question ID 1001
                }
            }
            catch (Exception ex)
            {
                // Log error and display user-friendly message
                LogError("Page_Load", ex);
                DisplayError("An error occurred while loading the survey. Please try again.");
            }
        }

        // Loads a question and its options from the database
        private void LoadQuestion(int questionId)
        {
            try
            {
                // Validate question ID
                if (questionId <= 0)
                {
                    throw new ArgumentException("Invalid question ID");
                }

                // Retrieve database connection string
                string connStr = "";

                    if (ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev"))
                {
                    connStr = AppConstant.AppConnection.DevConnection;
                }

                
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new ConfigurationErrorsException("Database connection string not found");
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Fetch question details
                    int maxSelectable = 1;
                    using (SqlCommand cmdQ = new SqlCommand("SELECT QuestionText, MaxSelectableOption FROM Questions WHERE QID = @qid", conn))
                    {
                        cmdQ.Parameters.AddWithValue("@qid", questionId);
                        using (SqlDataReader readerQ = cmdQ.ExecuteReader())
                        {
                            if (readerQ.Read())
                            {
                                queslbl.Text = readerQ["QuestionText"].ToString();
                                ViewState["CurrentQID"] = questionId;
                                maxSelectable = readerQ["MaxSelectableOption"] != DBNull.Value
                                    ? Convert.ToInt32(readerQ["MaxSelectableOption"])
                                    : 1;
                            }
                            else
                            {
                                throw new Exception($"Question with ID {questionId} not found");
                            }
                        }
                    }
                    ViewState["MaxSelectableOption"] = maxSelectable;

                    // Reset UI controls
                    ResetUIControls();

                    // Fetch question options
                    using (SqlCommand cmdO = new SqlCommand("SELECT OptionID, OptionText, OptionType, NextQID FROM Options WHERE QID = @qid", conn))
                    {
                        cmdO.Parameters.AddWithValue("@qid", questionId);
                        using (SqlDataReader readerO = cmdO.ExecuteReader())
                        {
                            while (readerO.Read())
                            {
                                string optionType = readerO["OptionType"].ToString();
                                string optionText = readerO["OptionText"].ToString();
                                int optionId = Convert.ToInt32(readerO["OptionID"]);

                                // Handle different option types
                                switch (optionType)
                                {
                                    case "MCQ":
                                        if (maxSelectable == 1)
                                        {
                                            RadioButtonListOptions.Items.Add(new ListItem(optionText, optionId.ToString()));
                                            RadioButtonListOptions.Visible = true;
                                        }
                                        else
                                        {
                                            CheckBoxListOptions.Items.Add(new ListItem(optionText, optionId.ToString()));
                                            CheckBoxListOptions.Visible = true;
                                        }
                                        break;
                                    case "TEXT":
                                        TextBoxInput.Visible = true;
                                        if (readerO["NextQID"] != DBNull.Value)
                                            ViewState["NextQID"] = readerO["NextQID"].ToString();
                                        break;
                                    default:
                                        // Log invalid option type
                                        LogError("LoadQuestion", new Exception($"Invalid option type: {optionType}"));
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Log database-specific error
                LogError("LoadQuestion", sqlEx);
                DisplayError("Database error while loading question. Please try again.");
            }
            catch (Exception ex)
            {
                // Log general error
                LogError("LoadQuestion", ex);
                DisplayError("Error loading question. Please try again.");
            }
        }

        // Handles the Next button click to process user answers
        protected void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                // Retrieve current question ID and answers list
                int currentQID = ViewState["CurrentQID"] != null ? (int)ViewState["CurrentQID"] : -1;
                var answerList = Session["Answers"] as List<UserAnswer> ?? new List<UserAnswer>();
                int nextQid = -1;
                bool usedNextQID = false;

                // Process text input
                if (TextBoxInput.Visible)
                {
                    string input = TextBoxInput.Text.Trim();
                    if (string.IsNullOrEmpty(input))
                    {
                        DisplayError("Please enter a response.");
                        return;
                    }
                    nextQid = ViewState["NextQID"] != null ? Convert.ToInt32(ViewState["NextQID"]) : -1;
                    usedNextQID = true;
                }
                // Process radio button selection
                else if (RadioButtonListOptions.Visible)
                {
                    if (RadioButtonListOptions.SelectedIndex < 0)
                    {
                        DisplayError("Please select an option.");
                        return;
                    }
                    int selectedOptionId = Convert.ToInt32(RadioButtonListOptions.SelectedValue);
                    answerList.Add(new UserAnswer { QID = currentQID, OptionID = selectedOptionId });
                    nextQid = GetNextQID(selectedOptionId);
                    usedNextQID = true;
                }
                // Process checkbox selections
                else if (CheckBoxListOptions.Visible)
                {
                    int selectableSetting = ViewState["MaxSelectableOption"] != null ? Convert.ToInt32(ViewState["MaxSelectableOption"]) : 1;
                    int minRequired = selectableSetting < 0 ? Math.Abs(selectableSetting) : 0;
                    int maxAllowed = selectableSetting > 0 ? selectableSetting : int.MaxValue;

                    var selected = new List<int>();
                    foreach (ListItem item in CheckBoxListOptions.Items)
                    {
                        if (item.Selected)
                            selected.Add(Convert.ToInt32(item.Value));
                    }

                    int selectedCount = selected.Count;
                    if (selectedCount < minRequired)
                    {
                        DisplayError($"Please select at least {minRequired} option(s).");
                        return;
                    }
                    if (selectedCount > maxAllowed)
                    {
                        DisplayError($"You can select up to {maxAllowed} option(s).");
                        return;
                    }

                    if (selectedCount > 0)
                    {
                        foreach (int optId in selected)
                            answerList.Add(new UserAnswer { QID = currentQID, OptionID = optId });
                        nextQid = GetNextQID(selected[0]);
                        usedNextQID = true;
                    }
                }

                // Update session with answers
                Session["Answers"] = answerList;

                // Determine next question if not already set
                if (!usedNextQID)
                    nextQid = GetNextQIDFromQID(currentQID);

                // If no next question, save answers and redirect
                if (nextQid <= 0)
                {
                    SaveAnswersToDatabase();
                    Response.Redirect("MemebrQuestion.aspx");
                    return;
                }
                else
                {
                    // Clear error message and load next question
                    Warning.Visible = false;
                    LoadQuestion(nextQid);
                }
            }
            catch (Exception ex)
            {
                // Log error and display message
                LogError("btnNext_Click", ex);
                DisplayError("Error processing your response. Please try again.");
            }
        }

        // Saves user answers to the database
        private void SaveAnswersToDatabase()
        {
            try
            {
                // Retrieve answers and respondent ID
                List<UserAnswer> answers = Session["Answers"] as List<UserAnswer>;
                string respondentId = Session["RespondentID"]?.ToString();

                // Validate data before proceeding
                if (answers == null || answers.Count == 0 || string.IsNullOrEmpty(respondentId))
                {
                    LogError("SaveAnswersToDatabase", new Exception("Invalid or missing answers/respondent ID"));
                    return;
                }

                // Retrieve database connection string
                string connStr = "";
                if (ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev"))
                {
                    connStr = AppConstant.AppConnection.DevConnection;
                }
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new ConfigurationErrorsException("Database connection string not found");
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    // Use transaction to ensure data consistency
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert each answer into the database
                            foreach (var ans in answers)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO Response (OptionID, QID, RespondentID) VALUES (@opt, @qid, @rid)", conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@opt", ans.OptionID);
                                    cmd.Parameters.AddWithValue("@qid", ans.QID);
                                    cmd.Parameters.AddWithValue("@rid", respondentId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
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

                // Clear session data
                Session.Remove("Answers");
                Session.Remove("RespondentID");
            }
            catch (SqlException sqlEx)
            {
                // Log database-specific error
                LogError("SaveAnswersToDatabase", sqlEx);
                DisplayError("Database error while saving answers. Please try again.");
            }
            catch (Exception ex)
            {
                // Log general error
                LogError("SaveAnswersToDatabase", ex);
                DisplayError("Error saving answers. Please try again.");
            }
        }

        // Retrieves the next question ID based on the selected option
        private int GetNextQID(int optionId)
        {
            try
            {
                // Retrieve database connection string
                string connStr = "";
                if (ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev"))
                {
                    connStr = AppConstant.AppConnection.DevConnection;
                }
                if (string.IsNullOrEmpty(connStr))
                {
                    throw new ConfigurationErrorsException("Database connection string not found");
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT NextQID FROM Options WHERE OptionID = @oid", conn))
                    {
                        cmd.Parameters.AddWithValue("@oid", optionId);
                        object result = cmd.ExecuteScalar();
                        return result != DBNull.Value && result != null ? Convert.ToInt32(result) : -1;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Log database error
                LogError("GetNextQID", sqlEx);
                return -1;
            }
            catch (Exception ex)
            {
                // Log general error
                LogError("GetNextQID", ex);
                return -1;
            }
        }

        // Retrieves the next question ID based on the current question's display order
        private int GetNextQIDFromQID(int qid)
        {
            try
            {
                // Retrieve database connection string
                string connStr = "";
                if (ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev"))
                {
                    connStr = AppConstant.AppConnection.DevConnection;
                }

                if (string.IsNullOrEmpty(connStr))
                {
                    throw new ConfigurationErrorsException("Database connection string not found");
                }

                string currentOrder = GetDisplayOrderString(qid);
                string currentPrefix = currentOrder.Split('.')[0];

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        @"SELECT QID, DisplayOrder 
                          FROM Questions 
                          WHERE DisplayOrder > @currOrder 
                          ORDER BY DisplayOrder", conn))
                    {
                        cmd.Parameters.AddWithValue("@currOrder", currentOrder);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int nextQID = Convert.ToInt32(reader["QID"]);
                                string nextOrderStr = reader["DisplayOrder"].ToString();

                                // Skip direct sub-questions (e.g., 7.1)
                                if (nextOrderStr.StartsWith(currentPrefix + "."))
                                {
                                    int dotCount = nextOrderStr.Split('.').Length - 1;
                                    if (dotCount == 1)
                                        continue;
                                }

                                return nextQID;
                            }
                        }
                    }
                }
                return -1;
            }
            catch (SqlException sqlEx)
            {
                // Log database error
                LogError("GetNextQIDFromQID", sqlEx);
                return -1;
            }
            catch (Exception ex)
            {
                // Log general error
                LogError("GetNextQIDFromQID", ex);
                return -1;
            }
        }

        // Retrieves the display order string for a given question ID
        private string GetDisplayOrderString(int qid)
        {
            try
            {
                // Retrieve database connection string
                string connStr = "";

                if (ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev"))
                {
                    connStr = AppConstant.AppConnection.DevConnection;
                }

                if (string.IsNullOrEmpty(connStr))
                {
                    throw new ConfigurationErrorsException("Database connection string not found");
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DisplayOrder FROM Questions WHERE QID = @qid", conn))
                    {
                        cmd.Parameters.AddWithValue("@qid", qid);
                        object result = cmd.ExecuteScalar();
                        return result?.ToString() ?? "0";
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Log database error
                LogError("GetDisplayOrderString", sqlEx);
                return "0";
            }
            catch (Exception ex)
            {
                // Log general error
                LogError("GetDisplayOrderString", ex);
                return "0";
            }
        }

        // Resets UI controls to their initial state
        private void ResetUIControls()
        {
            RadioButtonListOptions.Items.Clear();
            CheckBoxListOptions.Items.Clear();
            RadioButtonListOptions.Visible = false;
            CheckBoxListOptions.Visible = false;
            TextBoxInput.Visible = false;
            Warning.Visible = false;
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
                EventLog.WriteEntry("AIT_Research_Survey",
                    $"Error in {methodName}: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    EventLogEntryType.Error);
            }
            catch
            {
                
            }
        }
    }
}