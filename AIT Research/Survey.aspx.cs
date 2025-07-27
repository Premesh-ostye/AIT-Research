using System;
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
                LoadQuestion(1001);
            }
        }

        private void LoadQuestion(int questionId)
        {
            string connStr = "Data Source=SQL5110.site4now.net;Initial Catalog=db_9ab8b7_25dda13046;User Id=db_9ab8b7_25dda13046_admin;Password=rMwc7NAV;";

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
                    string optionId = readerO["OptionID"].ToString();

                    if (type == "MCQ")
                    {
                        if (maxSelectable == 1)
                        {
                            RadioButtonListOptions.Items.Add(new ListItem(optionText, optionId));
                            RadioButtonListOptions.Visible = true;
                        }
                        else
                        {
                            CheckBoxListOptions.Items.Add(new ListItem(optionText, optionId));
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

            if (TextBoxInput.Visible)
            {
                string input = TextBoxInput.Text.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Warning.Text = "Please enter a response.";
                    Warning.Visible = true;
                    return;
                }

                nextQid = Convert.ToInt32(ViewState["NextQID"]);
            }
            else if (RadioButtonListOptions.Visible)
            {
                if (RadioButtonListOptions.SelectedIndex < 0)
                {
                    Warning.Text = "Please select an option.";
                    Warning.Visible = true;
                    return;
                }

                string selectedOptionId = RadioButtonListOptions.SelectedValue;
                ViewState["LastAnswerText"] = RadioButtonListOptions.SelectedItem.Text.ToLower(); // Save last answer
                nextQid = GetNextQID(selectedOptionId);
            }
            else if (CheckBoxListOptions.Visible)
            {
                int maxAllowed = Convert.ToInt32(ViewState["MaxSelectableOption"]);
                var selected = new System.Collections.Generic.List<string>();

                foreach (ListItem item in CheckBoxListOptions.Items)
                {
                    if (item.Selected)
                        selected.Add(item.Value);
                }

                // Special rule: if MaxSelectableOption is -2, require at least 2 selections
                if (maxAllowed == -2)
                {
                    if (selected.Count < 2)
                    {
                        Warning.Text = "Please select at least 2 options.";
                        Warning.Visible = true;
                        return;
                    }
                }
                else
                {
                    if (selected.Count == 0 || selected.Count > maxAllowed)
                    {
                        Warning.Text = $"Please select up to {maxAllowed} option(s).";
                        Warning.Visible = true;
                        return;
                    }
                }

                nextQid = GetNextQID(selected[0]);
            }

            if (nextQid == 0 || nextQid == -1) // End of survey logic
            {
                Response.Redirect("MemebrQuestion.aspx");
            }
            else if (nextQid > 0)
            {
                Warning.Visible = false;
                LoadQuestion(nextQid);
            }
        }

       

        

       

        private int GetNextQID(string optionId)
        {
            int nextQid = -1;
            string connStr = "Data Source=SQL5110.site4now.net;Initial Catalog=db_9ab8b7_25dda13046;User Id=db_9ab8b7_25dda13046_admin;Password=rMwc7NAV;";
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
