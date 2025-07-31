// Survey.aspx.cs
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
                LoadQuestion(1001);
            }
        }

        public class UserAnswer
        {
            public int QID { get; set; }
            public int OptionID { get; set; }
            public string TextAnswer { get; set; }
        }

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString.Equals("Dev")
                ? AppConstant.AppConnection.DevConnection
                : ConfigurationManager.ConnectionStrings["DevelopmentConnectionString"].ConnectionString;
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

                int maxSelectable = 1, displayOrder = 0;

                if (readerQ.Read())
                {
                    queslbl.Text = readerQ["QuestionText"].ToString();
                    ViewState["CurrentQID"] = questionId;
                    maxSelectable = Convert.ToInt32(readerQ["MaxSelectableOption"]);
                    displayOrder = Convert.ToInt32(readerQ["DisplayOrder"]);
                    ViewState["CurrentDisplayOrder"] = displayOrder;
                    ViewState["MaxSelectableOption"] = maxSelectable;
                }
                readerQ.Close();

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
                    string text = readerO["OptionText"].ToString();
                    int optionId = Convert.ToInt32(readerO["OptionID"]);

                    if (type == "MCQ")
                    {
                        if (maxSelectable == 1)
                        {
                            RadioButtonListOptions.Items.Add(new ListItem(text, optionId.ToString()));
                            RadioButtonListOptions.Visible = true;
                        }
                        else
                        {
                            CheckBoxListOptions.Items.Add(new ListItem(text, optionId.ToString()));
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
            string connStr = GetConnectionString();
            int nextQid = -1;

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
            int maxSelectable = (int)ViewState["MaxSelectableOption"];
            var answerList = Session["Answers"] as List<UserAnswer>;
            var selectedOptionIDs = new List<int>();
            Warning.Visible = false;

            if (TextBoxInput.Visible)
            {
                string input = TextBoxInput.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    answerList.Add(new UserAnswer { QID = currentQID, OptionID = -1, TextAnswer = input });
                }
            }
            else if (RadioButtonListOptions.Visible)
            {
                if (RadioButtonListOptions.SelectedIndex >= 0)
                {
                    int selected = Convert.ToInt32(RadioButtonListOptions.SelectedValue);
                    answerList.Add(new UserAnswer { QID = currentQID, OptionID = selected });
                    selectedOptionIDs.Add(selected);
                }
            }
            else if (CheckBoxListOptions.Visible)
            {
                foreach (ListItem item in CheckBoxListOptions.Items)
                {
                    if (item.Selected)
                    {
                        int optId = Convert.ToInt32(item.Value);
                        selectedOptionIDs.Add(optId);
                        answerList.Add(new UserAnswer { QID = currentQID, OptionID = optId });
                    }
                }
                if (maxSelectable < 0 && selectedOptionIDs.Count < Math.Abs(maxSelectable))
                {
                    Warning.Text = $"Please select at least {Math.Abs(maxSelectable)} options.";
                    Warning.Visible = true;
                    return;
                }
                if (maxSelectable > 0 && selectedOptionIDs.Count > maxSelectable)
                {
                    Warning.Text = $"Please select no more than {maxSelectable} options.";
                    Warning.Visible = true;
                    return;
                }
            }

            int nextQid = selectedOptionIDs.Count > 0 ? GetNextQIDFromOption(currentQID, selectedOptionIDs) : -1;
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
                LoadQuestion(nextQid);
            }
        }

        private void SaveAnswersToDatabase()
        {
            var answers = Session["Answers"] as List<UserAnswer>;
            var respondentId = Session["RespondentID"]?.ToString();

            if (answers == null || answers.Count == 0 || string.IsNullOrEmpty(respondentId)) return;

            string connStr = GetConnectionString();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                foreach (var ans in answers)
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO Response (OptionID, QID, RespondentID, TextAnswer) VALUES (@opt, @qid, @rid, @txt)", conn);
                    cmd.Parameters.AddWithValue("@opt", ans.OptionID);
                    cmd.Parameters.AddWithValue("@qid", ans.QID);
                    cmd.Parameters.AddWithValue("@rid", respondentId);
                    cmd.Parameters.AddWithValue("@txt", (object)ans.TextAnswer ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            Session.Remove("Answers");
            Session.Remove("RespondentID");
        }
    }
}
