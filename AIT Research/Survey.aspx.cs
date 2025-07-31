// Updated version of the Survey logic to support conditional branching based on user input
// No schema change required — logic uses existing DisplayOrder and Options.NextQID only

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AIT_Research
{
    public partial class Survey : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Session["Answers"] = new List<UserAnswer>();
                LoadQuestion(1001); // Start with the first question
            }
        }

        public class UserAnswer
        {
            public int QID { get; set; }
            public int OptionID { get; set; }
        }

        private void LoadQuestion(int questionId)
        {
            string connStr = GetConnectionString();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                SqlCommand cmdQ = new SqlCommand("SELECT QuestionText, MaxSelectableOption, DisplayOrder FROM Questions WHERE QID = @qid", conn);
                cmdQ.Parameters.AddWithValue("@qid", questionId);
                SqlDataReader readerQ = cmdQ.ExecuteReader();

                int maxSelectable = 1;
                int displayOrder = 0;

                if (readerQ.Read())
                {
                    queslbl.Text = readerQ["QuestionText"].ToString();
                    ViewState["CurrentQID"] = questionId;

                    if (readerQ["MaxSelectableOption"] != DBNull.Value)
                        maxSelectable = Convert.ToInt32(readerQ["MaxSelectableOption"]);

                    displayOrder = Convert.ToInt32(readerQ["DisplayOrder"]);
                    ViewState["CurrentDisplayOrder"] = displayOrder;
                }
                readerQ.Close();
                ViewState["MaxSelectableOption"] = maxSelectable;

                RadioButtonListOptions.Items.Clear();
                CheckBoxListOptions.Items.Clear();
                RadioButtonListOptions.Visible = false;
                CheckBoxListOptions.Visible = false;
                TextBoxInput.Visible = false;

                SqlCommand cmdO = new SqlCommand("SELECT OptionID, OptionText, OptionType FROM Options WHERE QID = @qid", conn);
                cmdO.Parameters.AddWithValue("@qid", questionId);
                SqlDataReader readerO = cmdO.ExecuteReader();

                while (readerO.Read())
                {
                    string type = readerO["OptionType"].ToString();
                    string optionText = readerO["OptionText"].ToString();
                    int optionId = Convert.ToInt32(readerO["OptionID"]);

                    if (type == "MCQ")
                    {
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
                    }
                    else if (type == "TEXT")
                    {
                        TextBoxInput.Visible = true;
                    }
                }

                readerO.Close();
            }
        }

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev")
                ? AppConstant.AppConnection.DevConnection
                : ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString;
        }

        private int GetNextQIDFromOption(int currentQID, List<int> selectedOptionIDs)
        {
            if (selectedOptionIDs == null || selectedOptionIDs.Count == 0)
                return -1;

            string connStr = GetConnectionString();
            int nextQid = -1;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT TOP 1 NextQID FROM Options WHERE QID = @qid AND OptionID IN (" + string.Join(",", selectedOptionIDs) + ") AND NextQID IS NOT NULL";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@qid", currentQID);

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    nextQid = Convert.ToInt32(result);
            }
            return nextQid;
        }

        private int GetNextQIDByDisplayOrder(int currentDisplayOrder)
        {
            int nextQid = -1;
            string connStr = GetConnectionString();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT TOP 1 QID FROM Questions WHERE DisplayOrder > @currentDisplayOrder ORDER BY DisplayOrder ASC", conn);
                cmd.Parameters.AddWithValue("@currentDisplayOrder", currentDisplayOrder);

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    nextQid = Convert.ToInt32(result);
            }
            return nextQid;
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            int currentQID = (int)ViewState["CurrentQID"];
            int currentDisplayOrder = (int)ViewState["CurrentDisplayOrder"];
            var answerList = Session["Answers"] as List<UserAnswer>;
            var selectedOptionIDs = new List<int>();
            int nextQid = -1;

            if (TextBoxInput.Visible)
            {
                string input = TextBoxInput.Text.Trim();
                // Optional: store input somewhere
            }
            else if (RadioButtonListOptions.Visible)
            {
                if (RadioButtonListOptions.SelectedIndex >= 0)
                {
                    int selectedOptionId = Convert.ToInt32(RadioButtonListOptions.SelectedValue);
                    answerList.Add(new UserAnswer { QID = currentQID, OptionID = selectedOptionId });
                    selectedOptionIDs.Add(selectedOptionId);
                }
            }
            else if (CheckBoxListOptions.Visible)
            {
                int maxAllowed = Convert.ToInt32(ViewState["MaxSelectableOption"]);
                foreach (ListItem item in CheckBoxListOptions.Items)
                {
                    if (item.Selected)
                    {
                        int optId = Convert.ToInt32(item.Value);
                        answerList.Add(new UserAnswer { QID = currentQID, OptionID = optId });
                        selectedOptionIDs.Add(optId);
                    }
                }

                if (maxAllowed == -2 && selectedOptionIDs.Count < 2)
                {
                    Warning.Text = "Please select at least 2 options.";
                    Warning.Visible = true;
                    return;
                }
            }

            bool hasAnswer = selectedOptionIDs.Count > 0 || !string.IsNullOrWhiteSpace(TextBoxInput.Text);

            if (hasAnswer)
            {
                nextQid = GetNextQIDFromOption(currentQID, selectedOptionIDs);
            }

            if (nextQid <= 0)
            {
                nextQid = GetNextQIDByDisplayOrder(currentDisplayOrder);
            }

            Session["Answers"] = answerList;

            if (nextQid <= 0)
            {
                SaveAnswersToDatabase();
                Response.Redirect("MemebrQuestion.aspx");
            }
            else
            {
                Warning.Visible = false;
                LoadQuestion(nextQid);
            }
        }

        private void SaveAnswersToDatabase()
        {
            List<UserAnswer> answers = Session["Answers"] as List<UserAnswer>;
            string respondentId = Session["RespondentID"]?.ToString();

            if (answers == null || answers.Count == 0 || string.IsNullOrEmpty(respondentId)) return;

            string connStr = GetConnectionString();

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

            Session.Remove("Answers");
            Session.Remove("RespondentID");
        }
    }
}
