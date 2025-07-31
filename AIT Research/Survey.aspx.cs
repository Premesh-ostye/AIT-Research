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
                LoadQuestion(1001); // Start question
            }
        }

        public class UserAnswer
        {
            public int QID { get; set; }
            public int OptionID { get; set; }
        }

        private void LoadQuestion(int questionId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                SqlCommand cmdQ = new SqlCommand("SELECT QuestionText, MaxSelectableOption FROM Questions WHERE QID = @qid", conn);
                cmdQ.Parameters.AddWithValue("@qid", questionId);
                SqlDataReader readerQ = cmdQ.ExecuteReader();

                int maxSelectable = 1;

                if (readerQ.Read())
                {
                    queslbl.Text = readerQ["QuestionText"].ToString();
                    ViewState["CurrentQID"] = questionId;

                    if (readerQ["MaxSelectableOption"] != DBNull.Value)
                        maxSelectable = Convert.ToInt32(readerQ["MaxSelectableOption"]);
                }
                readerQ.Close();
                ViewState["MaxSelectableOption"] = maxSelectable;

                RadioButtonListOptions.Items.Clear();
                CheckBoxListOptions.Items.Clear();
                RadioButtonListOptions.Visible = false;
                CheckBoxListOptions.Visible = false;
                TextBoxInput.Visible = false;

                SqlCommand cmdO = new SqlCommand("SELECT OptionID, OptionText, OptionType, NextQID FROM Options WHERE QID = @qid", conn);
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
                        if (readerO["NextQID"] != DBNull.Value)
                            ViewState["NextQID"] = readerO["NextQID"].ToString();
                    }
                }

                readerO.Close();
            }
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            int nextQid = -1;
            int currentQID = (int)ViewState["CurrentQID"];
            var answerList = Session["Answers"] as List<UserAnswer>;
            bool usedNextQID = false;

            if (TextBoxInput.Visible)
            {
                string input = TextBoxInput.Text.Trim();
                nextQid = Convert.ToInt32(ViewState["NextQID"]);
                usedNextQID = true;
            }
            else if (RadioButtonListOptions.Visible)
            {
                if (RadioButtonListOptions.SelectedIndex >= 0)
                {
                    int selectedOptionId = Convert.ToInt32(RadioButtonListOptions.SelectedValue);
                    answerList.Add(new UserAnswer { QID = currentQID, OptionID = selectedOptionId });

                    nextQid = GetNextQID(selectedOptionId);
                    usedNextQID = true;
                }
            }
            else if (CheckBoxListOptions.Visible)
            {
                int maxAllowed = Convert.ToInt32(ViewState["MaxSelectableOption"]);
                var selected = new List<int>();

                foreach (ListItem item in CheckBoxListOptions.Items)
                {
                    if (item.Selected)
                        selected.Add(Convert.ToInt32(item.Value));
                }

                if (selected.Count > 0)
                {
                    foreach (int optId in selected)
                        answerList.Add(new UserAnswer { QID = currentQID, OptionID = optId });

                    nextQid = GetNextQID(selected[0]);
                    usedNextQID = true;
                }
            }

            Session["Answers"] = answerList;

            if (!usedNextQID)
                nextQid = GetNextQIDFromQID(currentQID);

            if (nextQid == 0 || nextQid == -1)
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

            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;
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

        private int GetNextQIDFromQID(int qid)
        {
            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;
            string currentOrder = GetDisplayOrderString(qid);
            string currentPrefix = currentOrder.Split('.')[0]; // "7" from "7" or "7.0"

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT QID, DisplayOrder 
            FROM Questions 
            WHERE DisplayOrder > @currOrder 
            ORDER BY DisplayOrder", conn);

                cmd.Parameters.AddWithValue("@currOrder", currentOrder);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int nextQID = Convert.ToInt32(reader["QID"]);
                    string nextOrderStr = reader["DisplayOrder"].ToString();

                    // Only skip direct subquestions like 7.1
                    if (nextOrderStr.StartsWith(currentPrefix + "."))
                    {
                        int dotCount = nextOrderStr.Split('.').Length - 1;

                        // Skip only if it's a direct sub-question (e.g., 7.1) — not 7.1.1
                        if (dotCount == 1)
                            continue;
                    }

                    return nextQID;
                }
            }

            return -1;
        }

          

        private string GetDisplayOrderString(int qid)
        {
            string connStr = ConfigurationManager.ConnectionStrings["AITResearchDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT DisplayOrder FROM Questions WHERE QID = @qid", conn);
                cmd.Parameters.AddWithValue("@qid", qid);
                object result = cmd.ExecuteScalar();

                return result?.ToString() ?? "0";
            }
        }
    }
}