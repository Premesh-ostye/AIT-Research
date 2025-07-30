using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AIT_Research
{
    public partial class Survey : System.Web.UI.Page
    {
        // Runs once when the page loads
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Store user's selected answers in session
                Session["Answers"] = new List<UserAnswer>();

                // Load the first question
                LoadQuestion(1001);
            }
        }

        // A simple class to hold user's selected answer
        public class UserAnswer
        {
            public int QID { get; set; }
            public int OptionID { get; set; }
        }

        // Loads a question from the database by its ID
        private void LoadQuestion(int questionId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;


            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Get the question text and max selectable options
                SqlCommand cmdQ = new SqlCommand("SELECT QuestionText, MaxSelectableOption FROM Questions WHERE QID = @qid", conn);
                cmdQ.Parameters.AddWithValue("@qid", questionId);
                SqlDataReader readerQ = cmdQ.ExecuteReader();

                int maxSelectable = 1; // default to single option

                if (readerQ.Read())
                {
                    queslbl.Text = readerQ["QuestionText"].ToString();
                    ViewState["CurrentQID"] = questionId;

                    if (readerQ["MaxSelectableOption"] != DBNull.Value)
                        maxSelectable = Convert.ToInt32(readerQ["MaxSelectableOption"]);
                }
                readerQ.Close();
                ViewState["MaxSelectableOption"] = maxSelectable;

                // Clear previous controls
                RadioButtonListOptions.Items.Clear();
                CheckBoxListOptions.Items.Clear();
                RadioButtonListOptions.Visible = false;
                CheckBoxListOptions.Visible = false;
                TextBoxInput.Visible = false;

                // Load the options for the question
                SqlCommand cmdO = new SqlCommand("SELECT OptionID, OptionText, OptionType, NextQID FROM Options WHERE QID = @qid", conn);
                cmdO.Parameters.AddWithValue("@qid", questionId);
                SqlDataReader readerO = cmdO.ExecuteReader();

                while (readerO.Read())
                {
                    string type = readerO["OptionType"].ToString();
                    string optionText = readerO["OptionText"].ToString();
                    int optionId = Convert.ToInt32(readerO["OptionID"]);

                    if (type == "MCQ") // Multiple Choice Question
                    {
                        if (maxSelectable == 1)
                        {
                            // Use radio buttons for single selection
                            RadioButtonListOptions.Items.Add(new ListItem(optionText, optionId.ToString()));
                            RadioButtonListOptions.Visible = true;
                        }
                        else
                        {
                            // Use checkboxes for multiple selections
                            CheckBoxListOptions.Items.Add(new ListItem(optionText, optionId.ToString()));
                            CheckBoxListOptions.Visible = true;
                        }
                    }
                    else if (type == "TEXT") // Open-ended input
                    {
                        TextBoxInput.Visible = true;
                        if (readerO["NextQID"] != DBNull.Value)
                            ViewState["NextQID"] = readerO["NextQID"].ToString(); // store next question ID
                    }
                }

                readerO.Close();
            }
        }

        // Called when user clicks "Next"
        protected void btnNext_Click(object sender, EventArgs e)
        {
            int nextQid = -1;
            int currentQID = (int)ViewState["CurrentQID"];
            var answerList = Session["Answers"] as List<UserAnswer>;

            if (TextBoxInput.Visible)
            {
                // For text answers
                string input = TextBoxInput.Text.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Warning.Text = "Please enter a response.";
                    Warning.Visible = true;
                    return;
                }

                // Store next question ID (already set earlier)
                nextQid = Convert.ToInt32(ViewState["NextQID"]);
            }
            else if (RadioButtonListOptions.Visible)
            {
                // For single selection questions
                if (RadioButtonListOptions.SelectedIndex < 0)
                {
                    Warning.Text = "Please select an option.";
                    Warning.Visible = true;
                    return;
                }

                int selectedOptionId = Convert.ToInt32(RadioButtonListOptions.SelectedValue);
                answerList.Add(new UserAnswer { QID = currentQID, OptionID = selectedOptionId });

                nextQid = GetNextQID(selectedOptionId); // Get the follow-up question
            }
            else if (CheckBoxListOptions.Visible)
            {
                // For multi-select questions
                int maxAllowed = Convert.ToInt32(ViewState["MaxSelectableOption"]);
                var selected = new List<int>();

                foreach (ListItem item in CheckBoxListOptions.Items)
                {
                    if (item.Selected)
                        selected.Add(Convert.ToInt32(item.Value));
                }

                // Special case: if min 2 options are required
                if (maxAllowed == -2 && selected.Count < 2)
                {
                    Warning.Text = "Please select at least 2 options.";
                    Warning.Visible = true;
                    return;
                }

                // Save selected answers
                foreach (int optId in selected)
                {
                    answerList.Add(new UserAnswer { QID = currentQID, OptionID = optId });
                }

                nextQid = GetNextQID(selected[0]); // Move to the next question
            }

            // Save back the session
            Session["Answers"] = answerList;

            if (nextQid == 0 || nextQid == -1)
            {
                // End of survey
                SaveAnswersToDatabase();
                Response.Redirect("MemebrQuestion.aspx");
            }
            else
            {
                Warning.Visible = false;
                LoadQuestion(nextQid); // Load the next question
            }
        }

        // Saves all collected answers to the DB
        private void SaveAnswersToDatabase()
        {
            List<UserAnswer> answers = Session["Answers"] as List<UserAnswer>;
            string respondentId = Session["RespondentID"]?.ToString();

            if (answers == null || answers.Count == 0 || string.IsNullOrEmpty(respondentId)) return;

            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;
            // DB connection
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                foreach (var ans in answers)
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO Response (OptionID, QID, RespondentID) VALUES (@opt, @qid, @rid)", conn);
                    cmd.Parameters.AddWithValue("@opt", ans.OptionID);
                    cmd.Parameters.AddWithValue("@qid", ans.QID);
                    cmd.Parameters.AddWithValue("@rid", respondentId);
                    cmd.ExecuteNonQuery();
                }
            }

            // Clear session
            Session.Remove("Answers");
            Session.Remove("RespondentID");
        }

        // Gets the next question ID based on selected option
        private int GetNextQID(int optionId)
        {
            int nextQid = -1;
            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT NextQID FROM Options WHERE OptionID = @oid", conn);
                cmd.Parameters.AddWithValue("@oid", optionId);
                object result = cmd.ExecuteScalar();

                if (result != DBNull.Value && result != null)
                    nextQid = Convert.ToInt32(result);
            }
            return nextQid;
        }
    }
}
